#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using TMPro;
using Newtonsoft.Json;

using MinecraftClient;
using MinecraftClient.Resource;
using MinecraftClient.Mapping;

namespace MarkovCraft
{
    public class ReplayScene : GameScene
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        [SerializeField] private ScreenManager? screenManager;
        [SerializeField] public TMP_Text? PlaybackSpeedText, ReplayText, FPSText;
        [SerializeField] public TMP_Dropdown? RecordingDropdown;
        [SerializeField] public Slider? PlaybackSpeedSlider;
        [SerializeField] public Button? ReplayButton; // , ExportButton;

        private string recordingFile = string.Empty;
        public string RecordingFile => recordingFile;
        private readonly Dictionary<int, string> loadedRecordings = new();
        private readonly Dictionary<int, ColoredBlockStateInfo> recordingPalette = new(); 
        // Recording palette index => (meshIndex, meshColor)
        private readonly Dictionary<int, int2> meshPalette = new();
        private float playbackSpeed = 1F;
        private bool replaying = false;

        void Start()
        {
            // First load Minecraft data & resources
            var ver = VersionHolder!.Versions[VersionHolder.SelectedVersion];

            StartCoroutine(LoadMCBlockData(ver.DataVersion, ver.ResourceVersion,
                () => {
                    ReplayButton!.interactable = false;
                    ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("hud.text.load_resource");
                },
                (status) => ReplayText!.text = GetL10nString(status),
                () => {
                    if (PlaybackSpeedSlider != null)
                    {
                        PlaybackSpeedSlider.onValueChanged.AddListener(UpdatePlaybackSpeed);
                        UpdatePlaybackSpeed(PlaybackSpeedSlider.value);
                    }

                    var dir = PathHelper.GetRecordingFile(string.Empty);
                    if (Directory.Exists(dir) && RecordingDropdown != null)
                    {
                        var options = new List<TMP_Dropdown.OptionData>();
                        loadedRecordings.Clear();
                        int index = 0;
                        foreach (var m in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                        {
                            var recordingPath = m.Substring(m.LastIndexOf(SP) + 1);
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

        public IEnumerator UpdateRecording(string recordingFile)
        {
            Loading = true;
            ReplayText!.text = GetL10nString("status.info.load_recording", recordingFile);

            ReplayButton!.interactable = false;
            ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("hud.text.load_recording");

            // Clear up scene
            ClearUpScene();

            string fileName = PathHelper.GetRecordingFile(recordingFile);
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
                        Debug.Log($"Recording [{fileName}] loaded: Size: {recData.SizeX}x{recData.SizeY}x{recData.SizeZ} Frames: {recData.FrameData.Count}");

                        succeeded = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse recording [{fileName}]");
                    }
                    
                }
            }

            if (!succeeded || recData is null)
            {
                Debug.LogWarning($"ERROR: Couldn't open json file at {fileName}");
                Loading = false;
                ReplayText!.text = GetL10nString("status.error.open_json_failure", fileName);
                yield break;
            }

            var statePalette = BlockStatePalette.INSTANCE;
            var stateId2Mesh = new Dictionary<int, int>(); // StateId => Mesh index

            recordingPalette.Clear();
            meshPalette.Clear();

            blockMeshCount = 1; // #0 is preserved for default cube mesh

            foreach (var pair in recData.Palette) // Read and assign palette
            {
                int index = int.Parse(pair.Key);
                var item = pair.Value;
                int rgb = ColorConvert.RGBFromHexString(item.Color);

                // Assign in recording palette
                recordingPalette.Add(index, item);
                // Assign in mesh palette
                if (!string.IsNullOrWhiteSpace(item.BlockState))
                {
                    int stateId = BlockStateHelper.GetStateIdFromString(item.BlockState);
                    
                    if (stateId != BlockStateHelper.INVALID_BLOCKSTATE)
                    {
                        var state = statePalette.StatesTable[stateId];
                        //Debug.Log($"Mapped '{index}' to [{stateId}] {state}");

                        if (stateId2Mesh.TryAdd(stateId, blockMeshCount))
                            meshPalette[index] = new(blockMeshCount++, rgb);
                        else // The mesh of this block state is already regestered, just use it
                            meshPalette[index] = new(stateId2Mesh[stateId], rgb);
                    }
                    else // Default cube mesh with custom color
                        meshPalette[index] = new(0, rgb);
                }
                else // Default cube mesh with custom color
                    meshPalette[index] = new(0, rgb);
                
                yield return null;
            }

            // Generate block meshes
            GenerateBlockMeshes(stateId2Mesh);
            yield return null;

            int side = Mathf.FloorToInt(Mathf.Sqrt(meshPalette.Count));

            foreach (var pair in meshPalette)
            {
                var obj = new GameObject($"#{pair.Key} [{recordingPalette[pair.Key].BlockState}]");
                int x = pair.Key % side, z = pair.Key / side;
                obj.transform.position = new Vector3(x, 0F, z);

                var meshFilter = obj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = blockMeshes[pair.Value.x];

                var meshRenderer = obj.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = BlockMaterial;
                if (pair.Value.x == 0) // Using plain cube mesh, color it
                {
                    // Set color for its own material, not shared material
                    meshRenderer.material.color = ColorConvert.GetOpaqueColor32(pair.Value.y);
                }
            }

            Loading = false;

            ReplayButton!.interactable = true;
            ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("hud.text.start_replay");
            ReplayButton.onClick.RemoveAllListeners();
            ReplayButton.onClick.AddListener(StartReplay);

            ReplayText!.text = GetL10nString("status.info.loaded_recording", recordingFile);
        }

        void Update()
        {
            if (FPSText != null)
                FPSText.text = $"FPS:{((int)(1 / Time.unscaledDeltaTime)).ToString().PadLeft(4, ' ')}";
            
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

        public void StartReplay()
        {
            if (Loading || replaying)
            {
                Debug.LogWarning("Replay cannot be started.");
                return;
            }

            //StartCoroutine(RunGeneration());

            if (ReplayButton != null)
            {
                ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("hud.text.stop_replay");
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
                ReplayButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("hud.text.start_replay");
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