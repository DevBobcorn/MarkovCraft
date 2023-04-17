#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Mathematics;
using TMPro;

using MarkovJunior;
using MarkovBlocks.Mapping;
using System.Threading.Tasks;

namespace MarkovBlocks
{
    [RequireComponent(typeof (ScreenManager))]
    public class Test : MonoBehaviour
    {
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;

        [SerializeField] public CameraController? CamController;
        [SerializeField] public LayerMask VolumeLayerMask;
        [SerializeField] public VolumeSelection? VolumeSelection;

        [SerializeField] public TMP_Text? VolumeText, PlaybackSpeedText, GenerationText, FPSText;
        [SerializeField] public TMP_Dropdown? ConfiguredModelDropdown;
        [SerializeField] public Slider? PlaybackSpeedSlider;
        [SerializeField] public Button? ConfigButton, ExecuteButton, ExportButton;
        [SerializeField] public RawImage? GraphImage;

        [SerializeField] public GameObject? GenerationResultPrefab;
        private readonly List<GenerationResult> generationResults = new();
        private GenerationResult? selectedResult = null;

        private string confModelFile = string.Empty;
        public string ConfiguredModelFile => confModelFile;
        private readonly Dictionary<int, string> loadedConfModels = new();
        private ConfiguredModel? currentConfModel = null;

        private Interpreter? interpreter = null;
        private float playbackSpeed = 1F;
        private bool executing = false;

        // Palettes and meshes
        private readonly ResourcePackManager packManager = new();
        private Mesh[] blockMeshes = { };
        private BlockGeometry?[] blockGeometries = { };
        private float3[] blockTints = { };
        private int blockMeshCount = 0;
        private readonly Dictionary<char, int2> palette = new();
        private Material? blockMaterial;

        private readonly LoadStateInfo loadInfo = new();

        private static Test? instance;
        public static Test Instance
        {
            get {
                if (instance == null)
                    instance = Component.FindObjectOfType<Test>();

                return instance!;
            }
        }

        private bool isPaused = true;
        public bool IsPaused
        {
            get => isPaused;

            set {
                isPaused = value;

                if (isPaused)
                    Time.timeScale = 0F;
                else
                    Time.timeScale = 1F;

            }
        }

        private void RedrawProcedureGraph(ConfiguredModel confModel)
        {
            if (interpreter != null && GraphImage != null)
            {
                int imageX = 200, imageY = 600;
                var image = new int[imageX * imageY];

                MarkovJunior.GUI.Draw(confModel.Model, interpreter.root, null, image, imageX, imageY, palette);
                
                Texture2D texture = new(imageX, imageY);
                texture.filterMode = FilterMode.Point;

                var color32s = new Color32[imageX * imageY];

                for (int y = 0; y < imageY; y++) for (int x = 0; x < imageX; x++)
                {
                    int rgb = image[x + (imageY - 1 - y) * imageX];
                    color32s[x + y * imageX] = ColorConvert.GetOpaqueColor32(rgb);
                }

                texture.SetPixels32(color32s);
                texture.Apply(true, false);
                
                GraphImage.texture = texture;
                GraphImage.SetNativeSize();

                GraphImage.gameObject.SetActive(true);
            }
            else
                GraphImage?.gameObject.SetActive(false);

        }

        private void GenerateBlockMeshes(Dictionary<int, int> stateId2Mesh) // StateId => Mesh index
        {
            var statePalette = BlockStatePalette.INSTANCE;
            var buffers = new VertexBuffer[blockMeshCount];

            blockGeometries = new BlockGeometry[blockMeshCount];
            blockTints = new float3[blockMeshCount];
            
            for (int i = 0;i < buffers.Length;i++)
                buffers[i] = new VertexBuffer();

            // #0 is default cube mesh
            CubeGeometry.Build(ref buffers[0], AtlasManager.HAKU, 0, 0, 0, 0b111111, new float3(1F));

            var dummyWorld = new MarkovBlocks.Mapping.World();
            var modelTable = packManager.StateModelTable;
            
            foreach (var pair in stateId2Mesh) // StateId => Mesh index
            {
                var stateId = pair.Key;

                if (modelTable.ContainsKey(stateId))
                {
                    var blockGeometry = modelTable[stateId].Geometries[0];
                    var blockTint = statePalette.GetBlockColor(stateId, dummyWorld, Location.Zero, statePalette.FromId(stateId));

                    blockGeometry.Build(ref buffers[pair.Value], float3.zero, 0b111111, blockTint);
                    
                    blockGeometries[pair.Value] = blockGeometry;
                    blockTints[pair.Value] = blockTint;
                }
                else
                {
                    Debug.LogWarning($"Model for block state #{stateId} ({statePalette.FromId(stateId)}) is not available. Using cube model instead.");
                    CubeGeometry.Build(ref buffers[pair.Value], AtlasManager.HAKU, 0, 0, 0, 0b111111, new float3(1F));
                }
            }

            // Set result to blockMeshes
            blockMeshes = BlockMeshGenerator.GenerateMeshes(buffers);
        }

        public Dictionary<char, CustomMappingItem>? GetExportPalette(HashSet<char> charSet)
        {
            if (currentConfModel is null || loadInfo.Loading)
                return null;
            
            var mapAsDict = currentConfModel.CustomMapping.ToDictionary(x => x.Character, x => x);
            
            return palette.Where(x => charSet.Contains(x.Key)).ToDictionary(x => x.Key, x => mapAsDict.ContainsKey(x.Key) ? mapAsDict[x.Key] :
                    new CustomMappingItem() { Character = x.Key, BlockState = string.Empty, Color = ColorConvert.GetOpaqueColor32(x.Value.y) });
        }

        public IEnumerator UpdateConfiguredModel(string confModelFile, ConfiguredModel confModel)
        {
            loadInfo.Loading = true;
            loadInfo.InfoText = $"Loading configured model [{confModelFile}]...";

            ClearUpScene();

            string fileName = PathHelper.GetExtraDataFile($"models/{confModel.Model}.xml");
            Debug.Log($"{confModel.Model} > {fileName}");

            XDocument? modelDoc = null;

            if (File.Exists(fileName))
            {
                FileStream fs = new(fileName, FileMode.Open);

                var task = XDocument.LoadAsync(fs, LoadOptions.SetLineInfo, new());

                while (!task.IsCompleted)
                    yield return null;
                
                fs.Close();
                
                if (task.IsCompletedSuccessfully)
                    modelDoc = task.Result;
            }
            
            if (modelDoc is null)
            {
                Debug.LogWarning($"ERROR: Couldn't open xml file at {fileName}");
                loadInfo.Loading = false;
                GenerationText!.text = $"Couldn't open xml file at {fileName}";
                yield break;
            }

            yield return null;

            var loadComplete = false;

            Task.Run(() => {
                // Use a task to load this in so that the main thread doesn't get blocked
                interpreter = Interpreter.Load(modelDoc.Root, confModel.SizeX, confModel.SizeY, confModel.SizeZ);

                loadComplete = true;
            });

            while (!loadComplete)
                yield return null;

            if (interpreter == null)
            {
                Debug.LogWarning("ERROR: Failed to create model interpreter");
                loadInfo.Loading = false;
                GenerationText!.text = "Failed to create model interpreter";
                yield break;
            }

            yield return null;

            var statePalette = BlockStatePalette.INSTANCE;
            var stateId2Mesh = new Dictionary<int, int>(); // StateId => Mesh index

            palette.Clear();

            XDocument.Load(PathHelper.GetExtraDataFile("palette.xml")).Root.Elements("color").ToList().ForEach(x =>
                    palette.Add(x.Get<char>("symbol"), new(0, ColorConvert.RGBFromHexString(x.Get<string>("value")))));

            blockMeshCount = 1; // #0 is preserved for default cube mesh

            foreach (var item in confModel.CustomMapping) // Read and assign custom mapping
            {
                int rgb = ColorConvert.GetRGB(item.Color);

                if (!string.IsNullOrWhiteSpace(item.BlockState))
                {
                    int stateId = BlockStateHelper.GetStateIdFromString(item.BlockState);
                    
                    if (stateId != BlockStateHelper.INVALID_BLOCKSTATE)
                    {
                        var state = statePalette.StatesTable[stateId];
                        //Debug.Log($"Mapped '{item.Character}' to [{stateId}] {state}");

                        if (stateId2Mesh.TryAdd(stateId, blockMeshCount))
                            palette[item.Character] = new(blockMeshCount++, rgb);
                        else // The mesh of this block state is already regestered, just use it
                            palette[item.Character] = new(stateId2Mesh[stateId], rgb);
                    }
                    else // Default cube mesh with custom color
                        palette[item.Character] = new(0, rgb);
                }
                else // Default cube mesh with custom color
                    palette[item.Character] = new(0, rgb);
                
                yield return null;
            }

            // Update procedure graph
            RedrawProcedureGraph(confModel);

            yield return null;

            // Generate block meshes
            GenerateBlockMeshes(stateId2Mesh);

            yield return null;

            loadInfo.Loading = false;

            GenerationText!.text = $"[{confModelFile}] loaded";
        }

        private IEnumerator LoadMCData(string dataVersion, string[] packs, Action? callback = null)
        {
            loadInfo.Loading = true;

            // First load all possible Block States...
            var loadFlag = new DataLoadFlag();
            StartCoroutine(BlockStatePalette.INSTANCE.PrepareData(dataVersion, loadFlag, loadInfo));

            while (!loadFlag.Finished)
                yield return null;
            
            // Then load all Items...
            // [Code removed]

            // Load resource packs...
            packManager.ClearPacks();
            // Collect packs
            foreach (var packName in packs)
                packManager.AddPack(new(packName));
            // Load valid packs...
            loadFlag.Finished = false;
            StartCoroutine(packManager.LoadPacks(this, loadFlag, loadInfo));

            while (!loadFlag.Finished)
                yield return null;
            
            loadInfo.Loading = false;

            if (loadFlag.Failed)
            {
                Debug.LogWarning("Block data loading failed");
                yield break;
            }

            MaterialManager.EnsureInitialized();
            blockMaterial = MaterialManager.GetAtlasMaterial(RenderType.SOLID);

            if (callback is not null)
                callback.Invoke();
        }

        private IEnumerator RunGeneration()
        {
            if (executing || currentConfModel is null || interpreter is null || blockMaterial is null || GenerationText == null || GenerationResultPrefab is null)
            {
                Debug.LogWarning("Generation cannot be initiated");
                StopExecution();
                yield break;
            }

            ClearUpScene();

            executing = true;
            var model = currentConfModel;

            var resultPerLine = Mathf.CeilToInt(Mathf.Sqrt(model.Amount));
            resultPerLine = Mathf.Max(resultPerLine, 1);
            
            Material[] materials = { blockMaterial };

            System.Random rand = new();
            var seeds = model.Seeds;

            List<GenerationResult> results = new();

            for (int k = 1; k <= model.Amount; k++)
            {
                if (!executing) // Stop execution
                    break;
                
                int seed = seeds != null && k <= seeds.Length ? seeds[k - 1] : rand.Next();
                int xCount = (k - 1) % resultPerLine,  zCount = (k - 1) / resultPerLine;
                
                var resultObj = GameObject.Instantiate(GenerationResultPrefab);
                resultObj.name = $"Result #{k} (Seed: {seed})";
                var result = resultObj!.GetComponent<GenerationResult>();
                result.GenerationSeed = seed;
                result.Iteration = k;

                results.Add(result);
                
                GenerationText.text = $"Iteration: #{k}\nWorking...";
                int frameCount = 1;

                (byte[] state, char[] legend, int FX, int FY, int FZ) data = new();

                int stepsPerFrame = model.Animated ? model.Steps : 50000;

                var enumerator = interpreter.Run(seed, stepsPerFrame, model.Animated).GetEnumerator();

                bool hasNext = true;

                while (hasNext)
                {
                    bool frameCompleted = false;

                    Task.Run(() => {
                        hasNext = enumerator.MoveNext();
                        frameCompleted = true;
                    });

                    while (!frameCompleted)
                        yield return null;
                    
                    data = enumerator.Current;

                    if (!executing) // Stop execution
                    {
                        Destroy(resultObj);
                        break;
                    }

                    
                    float tick = 1F / playbackSpeed;

                    if (model.Animated) // Visualize this frame
                    {
                        // Update generation text
                        GenerationText.text = $"Iteration: #{k}\nFrame: {frameCount++} ({(int)(tick * 1000)}ms/frame)";

                        var pos = new int3(xCount * (data.FX + 2), 0, zCount * (data.FY + 2));
                        result.UpdateVolume(pos, new(data.FX, data.FZ, data.FY));

                        var instanceData = BlockDataBuilder.GetInstanceData(data.state!, data.FX, data.FY, data.FZ, pos, data.legend.Select(ch => palette[ch]).ToArray());
                        BlockInstanceSpawner.VisualizeState(instanceData, materials, blockMeshes, tick, 0.5F);
                    }

                    yield return new WaitForSeconds(tick);
                }

                if (executing) // Visualize final state (last frame)
                {
                    var pos = new int3(xCount * (data.FX + 2), 0, zCount * (data.FY + 2));
                    result.UpdateVolume(pos, new(data.FX, data.FZ, data.FY));

                    var instanceData = BlockDataBuilder.GetInstanceData(data.state!, data.FX, data.FY, data.FZ, pos,
                            data.legend.Select(ch => palette[ch]).ToArray());

                    // The final visualization is persistent
                    BlockInstanceSpawner.VisualizePersistentState(instanceData, materials, blockMeshes);

                    var stateClone = new byte[data.state!.Length];
                    Array.Copy(data.state!, stateClone, stateClone.Length);

                    var legendClone = new char[data.legend!.Length];
                    Array.Copy(data.legend!, legendClone, legendClone.Length);

                    result.SetData((new[] { confModelFile, $"{seed}" }, stateClone, legendClone, data.FX, data.FY, data.FZ));

                    Debug.Log($"Iteration #{k} complete. Frame Count: {frameCount}");
                    GenerationText.text = $"Iteration: #{k}\nGeneration Complete";
                }
            }

            if (executing) // If the execution wasn't forced stopped
                StopExecution();
        }

        void Start()
        {
            // First load Minecraft data & resources
            StartCoroutine(LoadMCData("1.16", new string[] {
                    "vanilla-1.16.5", "vanilla_fix", "default"
                }, () => {
                    if (PlaybackSpeedSlider != null)
                    {
                        PlaybackSpeedSlider.onValueChanged.AddListener(UpdatePlaybackSpeed);
                        UpdatePlaybackSpeed(PlaybackSpeedSlider.value);
                    }

                    if (ConfigButton != null)
                    {
                        ConfigButton.onClick.RemoveAllListeners();
                        ConfigButton.onClick.AddListener(() => GetComponent<ScreenManager>().SetActiveScreenByType<ModelEditorScreen>() );
                    }

                    if (ExportButton != null)
                    {
                        ExportButton.onClick.RemoveAllListeners();
                        ExportButton.onClick.AddListener(() => GetComponent<ScreenManager>().SetActiveScreenByType<ExporterScreen>() );
                    }

                    if (ExecuteButton != null)
                    {
                        ExecuteButton.GetComponentInChildren<TMP_Text>().text = "Start Execution";
                        ExecuteButton.onClick.RemoveAllListeners();
                        ExecuteButton.onClick.AddListener(StartExecution);
                    }

                    var dir = PathHelper.GetExtraDataFile("configured_models");
                    if (Directory.Exists(dir) && ConfiguredModelDropdown != null)
                    {
                        var options = new List<TMP_Dropdown.OptionData>();
                        loadedConfModels.Clear();
                        int index = 0;
                        foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories))
                        {
                            var modelPath = m.Substring(dir.Length + 1);
                            options.Add(new(modelPath));
                            loadedConfModels.Add(index++, modelPath);
                        }

                        ConfiguredModelDropdown.AddOptions(options);
                        ConfiguredModelDropdown.onValueChanged.AddListener(UpdateDropdownOption);

                        if (options.Count > 0) // Use first model by default
                            UpdateDropdownOption(0);
                    }
                }));
            
            
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) // Toggle full screen
            {
                if (Screen.fullScreen)
                {
                    Screen.SetResolution(WINDOWED_APP_WIDTH, WINDOWED_APP_HEIGHT, false);
                    Screen.fullScreen = false;
                }
                else
                {
                    var maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
                    Screen.SetResolution(maxRes.width, maxRes.height, true);
                    Screen.fullScreen = true;
                }
                
            }

            if (loadInfo.Loading && GenerationText != null)
                GenerationText.text = loadInfo.InfoText;
            
            if (FPSText != null)
                FPSText.text = $"FPS:{((int)(1 / Time.unscaledDeltaTime)).ToString().PadLeft(4, ' ')}";
            
            if (isPaused) return;
            
            var cam = CamController?.ViewCamera;
            
            if (cam != null && VolumeSelection != null)
            {
                if (!VolumeSelection.Locked) // Update selected volume
                {
                    var ray = cam.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray.origin, ray.direction, out hit, 1000F, VolumeLayerMask))
                    {
                        UpdateSelectedResult(hit.collider.gameObject.GetComponent<GenerationResult>());

                        if (Input.GetKeyDown(KeyCode.Mouse0) && selectedResult!.Completed) // Lock can only be applied to completed results
                        {
                            VolumeSelection!.Lock();

                            // Show export button
                            ExportButton?.GetComponent<Animator>()?.SetBool("Hidden", false);
                        }
                    }
                    else
                        UpdateSelectedResult(null);
                }
                else // Selection is locked
                {
                    if (!EventSystem.current.IsPointerOverGameObject() && Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        VolumeSelection!.Unlock();

                        // Hide export button
                        ExportButton?.GetComponent<Animator>()?.SetBool("Hidden", true);
                    }
                }

            }

        }

        private void UpdateSelectedResult(GenerationResult? newResult)
        {
            if (selectedResult == newResult) return;

            if (newResult != null && newResult.Valid) // Valid
            {
                var size = newResult.GenerationSize;

                VolumeText!.text = $"Result #{newResult.Iteration} Seed: {newResult.GenerationSeed}\nSize: {size.x}x{size.y}x{size.z}";
                VolumeSelection!.UpdateVolume(newResult.GetVolumePosition(), newResult.GetVolumeSize());
            
                selectedResult = newResult;
            }
            else // Null or going to be destroyed
            {
                VolumeText!.text = string.Empty;
                VolumeSelection!.HideVolume();

                // Hide export button
                ExportButton?.GetComponent<Animator>()?.SetBool("Hidden", true);

                selectedResult = null;
            }
        }

        public (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ)? GetSelectedResultData()
        {
            if (selectedResult == null || !selectedResult.Valid || !selectedResult.Completed)
                return null;

            // Result data should be present if the generation is completed
            return selectedResult.Data;
        }

        private void ClearUpScene()
        {
            // Clear up persistent entities
            BlockInstanceSpawner.ClearUpPersistentState();

            // Clear up generation results
            UpdateSelectedResult(null);

            var results = Component.FindObjectsOfType<GenerationResult>().ToArray();

            for (int i = 0;i < results.Length;i++)
            {
                results[i].Valid = false;
                Destroy(results[i].gameObject);
            }
        }

        public void UpdatePlaybackSpeed(float newValue)
        {
            playbackSpeed = newValue;

            if (PlaybackSpeedText != null)
                PlaybackSpeedText.text = $"{newValue:0.0}";
            
        }

        public void UpdateDropdownOption(int newValue)
        {
            if (loadedConfModels.ContainsKey(newValue))
                SetConfiguredModel(loadedConfModels[newValue]);
        }

        public void SetConfiguredModel(string newConfModelFile)
        {
            if (executing)
                StopExecution();
            
            confModelFile = newConfModelFile;

            var xdoc = XDocument.Load($"{PathHelper.GetExtraDataFile("configured_models")}/{confModelFile}");
            var newConfModel = ConfiguredModel.CreateFromXMLDoc(xdoc);

            // Assign new configured model
            currentConfModel = newConfModel;

            if (!loadInfo.Loading)
                StartCoroutine(UpdateConfiguredModel(newConfModelFile, newConfModel));
        }

        public void StartExecution()
        {
            if (loadInfo.Loading || executing)
            {
                Debug.LogWarning("Execution cannot be started.");
                return;
            }

            StartCoroutine(RunGeneration());

            if (ExecuteButton != null)
            {
                ExecuteButton.GetComponentInChildren<TMP_Text>().text = "Stop Execution";
                ExecuteButton.onClick.RemoveAllListeners();
                ExecuteButton.onClick.AddListener(StopExecution);
            }
        }

        public void StopExecution()
        {
            if (loadInfo.Loading)
            {
                Debug.LogWarning("Execution cannot be stopped.");
                return;
            }

            executing = false;

            if (ExecuteButton != null)
            {
                ExecuteButton.GetComponentInChildren<TMP_Text>().text = "Start Execution";
                ExecuteButton.onClick.RemoveAllListeners();
                ExecuteButton.onClick.AddListener(StartExecution);
            }
        }

    }
}