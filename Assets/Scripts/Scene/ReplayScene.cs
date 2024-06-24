#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using TMPro;
using Newtonsoft.Json;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    public class ReplayScene : GameScene
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        // HUD Controls
        [SerializeField] private ScreenManager? screenManager;
        [SerializeField] public TMP_Text? ReplayText, FPSText;
        // HUD Controls - Replay Panel
        [SerializeField] public TMP_Dropdown? RecordingDropdown;
        [SerializeField] public Slider? PlaybackSpeedSlider;
        [SerializeField] public TMP_Text? PlaybackSpeedText;
        [SerializeField] public Toggle? OptimizedPlaybackToggle;
        [SerializeField] public Button? ReplayButton; // , ExportButton;

        private string recordingFile = string.Empty;
        public string RecordingFile => recordingFile;
        private readonly Dictionary<int, string> loadedRecordings = new();
        private ColoredBlockStateInfo[] recordingPalette = { }; // Palette of active recording
        private int sizeX, sizeY, sizeZ; // Size of active recording
        private bool is2d; // Whether or not the active recording is 2d
        private readonly List<BlockChangeInfo[]> frameData = new(); // Frame data of active recording
        // Recording palette index => (meshIndex, meshColor)
        private int3[] meshPalette = { };
        private float playbackSpeed = 1F;
        private bool replaying = false;

        public class BlockChangeInfo
        {
            public readonly int startFrame;
            public readonly int x;
            public readonly int y;
            public readonly int z;
            public readonly int newBlock;
            // Persistent frame count
            public int persistence;

            public BlockChangeInfo(int f, int x, int y, int z, int b)
            {
                startFrame = f;
                this.x = x;
                this.y = y;
                this.z = z;
                newBlock = b;
                // 0 or negative values means persistent, positive values indicate a
                // certain amount of frames for the new block to stay in the world
                persistence = -1;
            }
        }

        void Start()
        {
            // First load Minecraft data & resources
            StartCoroutine(LoadMCBlockData(
                () => {
                    ReplayButton!.interactable = false;
                    ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.load_resource");
                },
                (status) => ReplayText!.text = GetL10nString(status),
                () => {
                    PlaybackSpeedSlider!.onValueChanged.AddListener(UpdatePlaybackSpeed);
                    UpdatePlaybackSpeed(PlaybackSpeedSlider.value);

                    var dir = MarkovGlobal.GetRecordingFile(string.Empty);
                    if (Directory.Exists(dir) && RecordingDropdown != null)
                    {
                        var options = new List<TMP_Dropdown.OptionData>();
                        loadedRecordings.Clear();
                        int index = 0;
                        foreach (var m in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                        {
                            var recordingPath = m[(m.LastIndexOf(SP) + 1)..];
                            options.Add(new(recordingPath));
                            loadedRecordings.Add(index++, recordingPath);
                        }

                        RecordingDropdown.AddOptions(options);
                        RecordingDropdown.onValueChanged.AddListener(UpdateDropdownOption);

                        Debug.Log($"Loaded recordings: {string.Join(", ", loadedRecordings)}");

                        if (options.Count > 0) // Use first recording by default
                            UpdateDropdownOption(0);
                    }
                })
            );
        }

        private void VisualizePalette()
        {
            var materials = MaterialManager!.GetMaterialArray(BLOCK_RENDER_TYPES);
            int side = Mathf.FloorToInt(Mathf.Sqrt(meshPalette.Length));

            for (int index = 0;index < meshPalette.Length;index++)
            {
                var obj = new GameObject($"[{index}] [{recordingPalette[index].BlockState}]");
                var item = meshPalette[index];
                int x = index % side, z = index / side;
                obj.transform.position = new Vector3(x, 0F, z);

                var meshFilter = obj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = blockMeshes[item.x];

                var meshRenderer = obj.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = materials[DEFAULT_MATERIAL_INDEX];
                if (item.x == 0) // Using plain cube mesh, color it
                {
                    // Set color for its own material, not shared material
                    meshRenderer.material.color = ColorConvert.GetOpaqueColor32(item.y);
                }
            }
        }

        private const int FRAME_LIMIT = int.MaxValue; // 5;

        public IEnumerator UpdateRecording(string recordingFile)
        {
            Loading = true;
            ReplayText!.text = GetL10nString("status.info.load_recording", recordingFile);

            ReplayButton!.interactable = false;
            ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.load_recording");

            // Clear up scene
            ClearUpScene();

            string fileName = MarkovGlobal.GetRecordingFile(recordingFile);
            var succeeded = false;

            RecordingData? recData = null;

            if (File.Exists(fileName))
            {
                var task = File.ReadAllTextAsync(fileName);

                while (!task.IsCompleted)
                    yield return null;
                
                if (task.IsCompletedSuccessfully)
                {
                    recData = JsonConvert.DeserializeObject<RecordingData>(task.Result);

                    if (recData is not null)
                    {
                        Debug.Log($"Recording [{fileName}] loaded: Dimension: {recData.Dimension}d Size: {recData.SizeX}x{recData.SizeY}x{recData.SizeZ} Frames: {recData.FrameData.Count}");

                        succeeded = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse recording [{fileName}]");
                    }
                }
            }

            if (!succeeded || recData == null)
            {
                Debug.LogWarning($"ERROR: Couldn't open json file at {fileName}");
                Loading = false;
                ReplayText!.text = GetL10nString("status.error.open_json_failure", fileName);
                yield break;
            }

            // Assign recording palette
            recordingPalette = recData.Palette.ToArray();
            meshPalette = new int3[recData.Palette.Count];

            var statePalette = BlockStatePalette.INSTANCE;
            var stateId2Mesh = new Dictionary<int, int>(); // StateId => Mesh index
            nextBlockMeshIndex = 1; // #0 is preserved for default cube mesh

            var renderTypeTable = BlockStatePalette.INSTANCE.RenderTypeTable;
            int getStateMaterial(BlockState blockState)
            {
                return GetMaterialIndex(renderTypeTable.GetValueOrDefault(blockState.BlockId, RenderType.SOLID));
            };

            // Fill mesh palette
            for (int index = 0;index < recData.Palette.Count;index++) // Read and assign palette
            {
                var item = recData.Palette[index];
                int rgb = ColorConvert.RGBFromHexString(item.Color);
                // Assign in mesh palette
                if (!string.IsNullOrWhiteSpace(item.BlockState))
                {
                    if (BlockStatePalette.TryGetStateIdFromString(item.BlockState, out int stateId))
                    {
                        var state = statePalette.StatesTable[stateId];
                        //Debug.Log($"Mapped '{index}' to [{stateId}] {state}");

                        if (stateId2Mesh.TryAdd(stateId, nextBlockMeshIndex))
                            meshPalette[index] = new(nextBlockMeshIndex++, getStateMaterial(state), rgb);
                        else // The mesh of this block state is already regestered, just use it
                            meshPalette[index] = new(stateId2Mesh[stateId], getStateMaterial(state), rgb);
                    }
                    else // Default cube mesh with custom color
                    {
                        meshPalette[index] = new(0, DEFAULT_MATERIAL_INDEX, rgb);
                    }
                }
                else // Default cube mesh with custom color
                {
                    meshPalette[index] = new(0, DEFAULT_MATERIAL_INDEX, rgb);
                }
                
                yield return null;
            }

            // Generate block meshes
            GenerateBlockMeshes(stateId2Mesh, appendEmptyMesh: true);
            yield return null;

            sizeX = recData.SizeX;
            sizeY = recData.SizeY;
            sizeZ = recData.SizeZ;

            frameData.Clear();

            // An array to track the last block change done to a cell
            var changeTrackingBox = new int[sizeX * sizeY * sizeZ];
            // Initialize the array with -1 which indicates not changed yet
            Array.Fill(changeTrackingBox, -1);

            // Pre-process these frames
            List<BlockChangeInfo> allBlockChanges = new();

            switch (recData.Dimension)
            {
                case 2:
                    is2d = true;
                    break;
                case 3:
                    is2d = false;
                    break;
                default:
                    Debug.LogWarning($"Invalid dimension: {recData.Dimension}");
                    Loading = false;
                    ReplayText!.text = GetL10nString("status.error.open_json_failure", fileName);
                    yield break;
            }

            // For 3d, every item contains 4 numbers: x, y, z and palette index,
            // and as for 2d, it's 3 numbers with z values omitted
            int itemLength = is2d ? 3 : 4;
            
            for (int f = 0;f < recData.FrameData.Count && f < FRAME_LIMIT;f++) // For each frame i the recording
            {
                var nums = recData.FrameData[f].Split(' ');

                if (nums.Length % itemLength != 0)
                {
                    if (nums.Length > 1) // Not empty string, which return an array with a length of 1
                        Debug.Log($"Malformed frame data with a length of {nums.Length}");
                    continue;
                }

                List<BlockChangeInfo> blockChangesCurrentFrame = new();

                for (int i = 0;i < nums.Length;i += itemLength)
                {
                    int x = int.Parse(nums[i    ]);
                    int y = int.Parse(nums[i + 1]);
                    int z = is2d ? 0                      : int.Parse(nums[i + 2]);
                    int b = is2d ? int.Parse(nums[i + 2]) : int.Parse(nums[i + 3]);

                    int posInBox = x + y * sizeX + z * sizeX * sizeY;

                    if (changeTrackingBox[posInBox] >= 0)
                    {
                        // The block in this cell have been changed in previous 
                        // frames, track back to the last change of this cell,
                        // and assign persistent frames of that change
                        // Persistent frame count = End frame - Start frame
                        var lastChange = allBlockChanges[changeTrackingBox[posInBox]];
                        lastChange.persistence = f - lastChange.startFrame;

                        if (lastChange.persistence <= 0)
                        {
                            Debug.LogWarning($"Faulty persistence: {lastChange.persistence}");
                            lastChange.persistence = 1;
                        }
                    }

                    // The current allBlockChanges.Count is the list index
                    // which will be taken by this block change. Put it
                    // into the tracking array
                    changeTrackingBox[posInBox] = allBlockChanges.Count;
                    var blockChange = new BlockChangeInfo(f, x, y, z, b);

                    blockChangesCurrentFrame.Add(blockChange);
                    allBlockChanges.Add(blockChange);
                }
                frameData.Add(blockChangesCurrentFrame.ToArray());
            }

            //VisualizePalette();

            Loading = false;

            ReplayButton!.interactable = true;
            ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.start_replay");
            ReplayButton.onClick.RemoveAllListeners();
            ReplayButton.onClick.AddListener(StartReplay);

            ReplayText!.text = GetL10nString("status.info.loaded_recording", loadedDataVersionName, loadedDataVersionInt, recordingFile);
        }

        void Update()
        {
            if (FPSText != null)
                FPSText.text = $"FPS:{(int)(1 / Time.unscaledDeltaTime), 4}";
            
        }

        public void UpdatePlaybackSpeed(float newValue)
        {
            playbackSpeed = newValue;

            if (PlaybackSpeedText != null)
                PlaybackSpeedText.text = $"{newValue:0.0}";
            
        }
        public void UpdateDropdownOption(int newValue)
        {
            if (loadedRecordings.ContainsKey(newValue))
                SetRecording(loadedRecordings[newValue]);
        }

        public void SetRecording(string newRecordingFile)
        {
            if (replaying)
                StopReplay();
            
            recordingFile = newRecordingFile;

            if (!Loading)
                StartCoroutine(UpdateRecording(newRecordingFile));
        }

        public IEnumerator ReplayRecording()
        {
            if (replaying || recordingPalette.Length == 0 || frameData.Count == 0 || ReplayText == null)
            {
                Debug.LogWarning("Replay cannot be initiated");
                StopReplay();
                yield break;
            }

            ClearUpScene();
            // Delay a bit so that the first frame of replay doesn't get cleared
            // This can cause missing blocks if using optimized block population
            // (calling ReplayOptimized() coroutine) 
            yield return new WaitForSecondsRealtime(0.1F);

            replaying = true;

            if (OptimizedPlaybackToggle?.isOn ?? true)
                yield return StartCoroutine(ReplayOptimized());
            else
                yield return StartCoroutine(ReplayRegular());

            if (replaying) // If the replay hasn't been forced stopped
                StopReplay();
        }

        private IEnumerator ReplayRegular()
        {
            var materials = MaterialManager!.GetMaterialArray(BLOCK_RENDER_TYPES);
            var simulationBox = new byte[sizeX * sizeY * sizeZ];

            for (int f = 0;f < frameData.Count;f++)
            {
                if (!replaying) // Replaying terminated
                {
                    break;
                }

                float tick = 1F / playbackSpeed;

                foreach (var change in frameData[f])
                {
                    simulationBox[change.x + change.y * sizeX + change.z * sizeX * sizeY] = (byte) change.newBlock;
                }

                var instanceData = BlockDataBuilder.GetInstanceData(simulationBox, sizeX, sizeY, sizeZ, int3.zero, meshPalette);
                BlockInstanceSpawner.VisualizeFrameState(instanceData, materials, blockMeshes, tick);

                ReplayText!.text = GetL10nString("replay.text.current_frame", f, frameData.Count);
                yield return new WaitForSeconds(tick);
            }

            if (replaying)
            {
                var instanceData = BlockDataBuilder.GetInstanceData(simulationBox, sizeX, sizeY, sizeZ, int3.zero, meshPalette);
                BlockInstanceSpawner.VisualizePersistentState(instanceData, materials, blockMeshes);
            }
        }

        private IEnumerator ReplayOptimized()
        {
            // For optimized playback, playback speed cannot be hot-updated when playing
            float constTick = 1F / playbackSpeed;
            // Lock playback speed slider
            if (PlaybackSpeedSlider != null)
                PlaybackSpeedSlider.interactable = false;

            var materials = MaterialManager!.GetMaterialArray(BLOCK_RENDER_TYPES);

            for (int f = 0;f < frameData.Count;f++)
            {
                if (!replaying) // Replaying terminated
                {
                    break;
                }
                
                BlockChangeInfo[] changes;

                if (is2d) // 2d mode, nothing special needed
                {
                    changes = frameData[f];
                }
                else // 3d mode, filter out air block placement
                {
                    changes = frameData[f].Where(x => x.newBlock != 0).ToArray();
                }

                var instanceData = (
                        changes.Select(x => new int3(x.x, x.z, x.y)).ToArray(),
                        changes.Select(x => meshPalette[x.newBlock]).ToArray(),
                        changes.Select(x => x.persistence * constTick).ToArray()
                );
                BlockInstanceSpawner.VisualizeState(instanceData, materials, blockMeshes);

                ReplayText!.text = GetL10nString("replay.text.current_frame", f, frameData.Count);
                yield return new WaitForSeconds(constTick);
            }

            // Unlock playback speed slider
            if (PlaybackSpeedSlider != null)
                PlaybackSpeedSlider.interactable = true;
        }

        public void StartReplay()
        {
            if (Loading || replaying)
            {
                Debug.LogWarning("Replay cannot be started.");
                return;
            }

            StartCoroutine(ReplayRecording());

            if (ReplayButton != null)
            {
                ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.stop_replay");
                ReplayButton.onClick.RemoveAllListeners();
                ReplayButton.onClick.AddListener(StopReplay);
            }
        }

        public void StopReplay()
        {
            if (Loading)
            {
                Debug.LogWarning("Replay cannot be stopped.");
                return;
            }

            replaying = false;

            if (ReplayButton != null)
            {
                ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.start_replay");
                ReplayButton.onClick.RemoveAllListeners();
                ReplayButton.onClick.AddListener(StartReplay);
            }
        }

        private void ClearUpScene()
        {
            // Clear up persistent entities
            BlockInstanceSpawner.ClearUpPersistentState();
        }

        public override void ReturnToMenu()
        {
            if (replaying)
                StopReplay();
            
            ClearUpScene();

            // Unpause game to restore time scale
            screenManager!.IsPaused = false;

            SceneManager.LoadScene("Scenes/Welcome", LoadSceneMode.Single);
        }
    }
}