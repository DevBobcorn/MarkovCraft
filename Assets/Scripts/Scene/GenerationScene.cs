#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using TMPro;

using MarkovJunior;
using CraftSharp;

namespace MarkovCraft
{
    public class GenerationScene : GameScene
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        private static readonly int HIDDEN = Animator.StringToHash("Hidden");
        private const string CONFIGURED_MODEL_FOLDER = "configured_models";
        private static readonly Vector3 BLOCK_SELECTION_HIDDEN_POS = new(0F, -100F, 0F);

        // Scene game objects
        [SerializeField] public CameraController? CamController;
        [SerializeField] public LayerMask VolumeLayerMask;
        [SerializeField] public LayerMask BlockMeshLayerMask;
        [SerializeField] public VolumeSelection? VolumeSelection;
        [SerializeField] public GameObject? BlockSelection;
        [SerializeField] public GameObject? GenerationResultPrefab;
        // HUD Controls
        [SerializeField] private ScreenManager? screenManager;
        [SerializeField] public CanvasGroup? BlockSelectionPanelGroup;
        [SerializeField] public TMP_Text? BlockSelectionText;
        [SerializeField] public ExportItem? BlockSelectionMappingItem;
        [SerializeField] public TMP_Text? VolumeText, GenerationText, FPSText;
        //[SerializeField] public ModelGraph? ModelGraphUI;
        [SerializeField] public ModelGraphV2? ModelGraphUIv2;
        [SerializeField] public Animator? ResultOperationPanelAnimator;
        [SerializeField] public Button? RemoveButton, ExportButton;
        [SerializeField] public Button? ResizeButton, ExtrudeButton, RCONButton, UnlockButton;
        [SerializeField] public TabPanel? ControlTabPanel;
        // HUD Controls - Generation Panel
        [SerializeField] public Toggle? RecordToggle;
        [SerializeField] public TMP_Text? PlaybackSpeedText;
        [SerializeField] public TMP_Dropdown? ConfiguredModelDropdown;
        [SerializeField] public Slider? PlaybackSpeedSlider;
        [SerializeField] public Button? CreateButton, ConfigButton, ExecuteButton;
        // HUD Controls - Import Vox Panel
        [SerializeField] public TMP_InputField? VoxPathInput;
        [SerializeField] public Button? VoxImportButton;

        // Character => RGB Color specified in base palette
        // This palette should be loaded for only once
        private readonly Dictionary<char, int> baseColorPalette = new();
        private bool baseColorPaletteLoaded = false;

        public string ConfiguredModelFile { get; private set; } = string.Empty;

        private readonly Dictionary<int, string> loadedConfModels = new();
        public ConfiguredModel? ConfiguredModel { get; private set; } = null;

        private GenerationResult? selectedResult = null;
        // Character => (meshIndex, matIndex, meshColor)
        private readonly Dictionary<char, int3> meshPalette = new();
        // Character => CustomMappingItem, its items SHOULD NOT be edited once loaded from file
        private readonly Dictionary<char, CustomMappingItem> fullPalette = new();
        private Interpreter? interpreter = null;
        private float playbackSpeed = 1F;
        private bool executing = false;

        private void GenerateProcedureGraph(string modelName)
        {
            if (ModelGraphUIv2)
            {
                if (interpreter != null)
                {
                    var graphPalette = meshPalette.ToDictionary(x => x.Key, x => ColorConvert.GetOpaqueColor32(x.Value.z));
                    ModelGraphGeneratorV2.GenerateGraph(ModelGraphUIv2, modelName, interpreter.root, graphPalette);
                }
                else
                {
                    ModelGraphUIv2.gameObject.SetActive(false);
                }
            }
            
            /* MODEL GRAPH V1
            if (ModelGraphUI)
            {
                if (interpreter != null)
                {
                    var graphPalette = meshPalette.ToDictionary(x => x.Key, x => ColorConvert.GetOpaqueColor32(x.Value.z));
                    ModelGraphGenerator.GenerateGraph(ModelGraphUI, modelName, interpreter.root, graphPalette);
                }
                else
                {
                    ModelGraphUI.gameObject.SetActive(false);
                }
            } */
        }

        private void EnsureBasePaletteLoaded()
        {
            if (!baseColorPaletteLoaded)
            {
                XDocument.Load(MarkovGlobal.GetDataFile("palette.xml")).Root!.Elements("color").ToList().ForEach(x =>
                {
                    var character = x.Get<char>("symbol");
                    var rgb = ColorConvert.RGBFromHexString(x.Get<string>("value"));
                    baseColorPalette.Add(character, rgb);
                });

                baseColorPaletteLoaded = true;
            }
        }

        public override void HideSpecialGUI()
        {
            // Hide UI Toolkit Model Graph (V2) which doesn't use proper rendering
            if (ModelGraphUIv2)
            {
                ModelGraphUIv2.HidePanel();
            }
        }

        public override void ShowSpecialGUI()
        {
            UpdateBlockSelection(null);
            // Also show UI Toolkit Model Graph (V2) if it is ready
            if (ModelGraphUIv2)
            {
                ModelGraphUIv2.ShowPanelIfNotEmpty();
            }
        }

        public Dictionary<char, int> GetBaseColorPalette()
        {
            // Load base palette if not loaded yet
            EnsureBasePaletteLoaded();
            return baseColorPalette;
        }        

        public Dictionary<char, CustomMappingItem> GetFullPaletteAsLoaded()
        {
            return fullPalette;
        }

        public IEnumerator UpdateConfiguredModel(string confModelFile, ConfiguredModel confModel)
        {
            Loading = true;
            GenerationText!.text = GetL10nString("status.info.load_conf_model", confModelFile);

            ExecuteButton!.interactable = false;
            var localizedLoadText = GetL10nString("control.text.load_conf_model");
            ExecuteButton.GetComponentInChildren<TMP_Text>().text = localizedLoadText;

            // Clear up scene
            ClearUpScene();

            // Clear up current model graph
            if (ModelGraphUIv2)
            {
                ModelGraphUIv2.ClearUp();
            }
            
            /* MODEL GRAPH V1
            if (ModelGraphUI)
            {
                ModelGraphUI.ClearUp();
            } */

            string fileName = MarkovGlobal.GetDataFile($"models{SP}{confModel.Model}.xml");
            Debug.Log($"{confModelFile} ({confModel.Model}) > {fileName}");

            XDocument? modelDoc = null;

            if (File.Exists(fileName))
            {
                FileStream fs = new(fileName, FileMode.Open);

                var task = XDocument.LoadAsync(fs, LoadOptions.SetLineInfo, CancellationToken.None);

                while (!task.IsCompleted)
                    yield return null;
                
                fs.Close();
                
                if (task.IsCompletedSuccessfully)
                    modelDoc = task.Result;
            }
            
            if (modelDoc == null)
            {
                Debug.LogWarning($"ERROR: Couldn't open xml file at {fileName}");
                Loading = false;
                GenerationText!.text = GetL10nString("status.error.open_xml_failure", fileName);
                yield break;
            }

            yield return null;

            var loadComplete = false;
            var tokenSource = new CancellationTokenSource();
            interpreter = null;

            Task.Run(() => {
                // Use a task to load this in so that the main thread doesn't get blocked
                interpreter = Interpreter.Load(modelDoc.Root, confModel.SizeX, confModel.SizeY, confModel.SizeZ);

                loadComplete = true;
            }, tokenSource.Token);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (!loadComplete)
            {
                var secs = sw.Elapsed.TotalSeconds;

                if (secs > 8) // Loading process taking too long
                {
                    tokenSource.Cancel();
                    Debug.Log("Loading process taking too long, cancelling...");
                    // break the loop
                    break;
                }

                GenerationText!.text = $"{localizedLoadText} {secs:0.00}s";
                yield return null;
            }

            sw.Stop();

            if (interpreter == null)
            {
                Debug.LogWarning("ERROR: Failed to create model interpreter");
                Loading = false;
                GenerationText!.text = GetL10nString("status.error.model_interpreter_failure");
                yield break;
            }

            yield return null;

            var statePalette = BlockStatePalette.INSTANCE;
            var stateId2Mesh = new Dictionary<int, int>(); // StateId => Mesh index

            // Update full palette and mesh palette
            meshPalette.Clear();
            fullPalette.Clear();

            // Load base palette if not loaded yet
            EnsureBasePaletteLoaded();

            // First apply all items in base palette
            foreach (var pair in baseColorPalette)
            {
                meshPalette.Add( pair.Key, new(0, DEFAULT_MATERIAL_INDEX, pair.Value) );
                fullPalette.Add( pair.Key, new CustomMappingItem {
                        Symbol = pair.Key, Color = ColorConvert.GetOpaqueColor32(pair.Value) } );
            }

            nextBlockMeshIndex = 1; // #0 is preserved for default cube mesh

            var renderTypeTable = BlockStatePalette.INSTANCE.RenderTypeTable;

            // Read and apply custom mappings
            foreach (var item in confModel.CustomMapping)
            {
                int rgb = ColorConvert.GetRGB(item.Color);

                if (!string.IsNullOrWhiteSpace(item.BlockState))
                {
                    if (statePalette.TryGetStateIdCandidatesFromString(item.BlockState, out int[] stateIds))
                    {
                        var stateId = stateIds.First();
                        var state = statePalette.GetByNumId(stateId);
                        //Debug.Log($"Mapped '{item.Character}' to [{stateId}] {state}");

                        if (stateId2Mesh.TryAdd(stateId, nextBlockMeshIndex))
                            meshPalette[item.Symbol] = new(nextBlockMeshIndex++, getStateMaterial(state), rgb);
                        else // The mesh of this block state is already regestered, just use it
                            meshPalette[item.Symbol] = new(stateId2Mesh[stateId], getStateMaterial(state), rgb);
                    }
                    else // Default cube mesh with custom color
                    {
                        meshPalette[item.Symbol] = new(0, DEFAULT_MATERIAL_INDEX, rgb);
                    }
                }
                else // Default cube mesh with custom color
                {
                    meshPalette[item.Symbol] = new(0, DEFAULT_MATERIAL_INDEX, rgb);
                }
                
                fullPalette[item.Symbol] = item;
                
                yield return null;
            }

            // Update model graph
            GenerateProcedureGraph(confModel.Model);
            yield return null;

            // Generate block meshes
            GenerateBlockMeshes(stateId2Mesh, appendEmptyMesh: true);
            yield return null;

            Loading = false;

            ExecuteButton!.interactable = true;
            ExecuteButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.start_execution");
            ExecuteButton.onClick.RemoveAllListeners();
            ExecuteButton.onClick.AddListener(StartExecution);

            GenerationText!.text = GetL10nString("status.info.loaded_conf_model", loadedDataVersionName, loadedDataVersionInt, confModelFile);
            yield break;

            int getStateMaterial(BlockState blockState)
            {
                return GetMaterialIndex(renderTypeTable.GetValueOrDefault(blockState.BlockId, RenderType.SOLID));
            }
        }

        private static int nextResultId = 10001;

        private IEnumerator RunGeneration()
        {
            if (executing || !ConfiguredModel || ConfiguredModel.Amount == 0 || interpreter == null)
            {
                Debug.LogWarning("Generation cannot be initiated");
                StopExecution();
                yield break;
            }

            ClearUpScene();
            // Delay a bit so that the first frame of generation doesn't get cleared
            yield return new WaitForSecondsRealtime(0.1F);

            var materials = MaterialManager!.GetMaterialArray(BLOCK_RENDER_TYPES);

            executing = true;
            var model = ConfiguredModel;

            var resultPerLine = Mathf.CeilToInt(Mathf.Sqrt(model.Amount));
            System.Random rand = new();
            var seeds = model.Seeds;

            int3 maxFrameSize = int3.zero;
            int stepsPerFrame = model.StepsPerRefresh;

            var record = RecordToggle!.isOn;

            for (int k = 1; k <= model.Amount; k++)
            {
                if (!executing) // Stop execution
                    break;
                
                int seed = seeds != null && k <= seeds.Length ? seeds[k - 1] : rand.Next();
                int xCount = (k - 1) % resultPerLine, yCount = (k - 1) / resultPerLine;
                
                var resultObj = Instantiate(GenerationResultPrefab);
                var resultId = nextResultId++;
                resultObj!.name = $"Result #{resultId} (Seed: {seed})";
                var result = resultObj.GetComponent<GenerationResult>();
                result.GenerationSeed = seed;
                result.ResultId = resultId;
                
                GenerationText!.text = GetL10nString("status.info.generation_start", k);

                (byte[] state, char[] legend, int FX, int FY, int FZ, int stepCount) dataFrame = new();

                var enumerator = interpreter.Run(seed, model.Steps, model.Animated).GetEnumerator();

                bool hasNext = true;
                var recordedFrames = new List<GenerationFrameRecord>();

                var pos = new int3(2 + xCount * (maxFrameSize.x + 2), 0, 2 + yCount * (maxFrameSize.y + 2));
                var lastFrameSize = int3.zero;

                while (hasNext)
                {
                    bool frameCompleted = false;

                    Task.Run(() => {
                        for (int s = 0;s < stepsPerFrame;s++)
                        {
                            if (hasNext)
                                hasNext = enumerator.MoveNext();
                        }

                        frameCompleted = true;
                    });

                    while (!frameCompleted)
                        yield return null;
                    
                    dataFrame = enumerator.Current;

                    if (!executing) // Stop execution
                    {
                        Destroy(resultObj);
                        break;
                    }

                    float tick = 1F / playbackSpeed;

                    var frameSize = new int3(dataFrame.FX, dataFrame.FY, dataFrame.FZ); // Markov coordinates
                    var frameSizeChanged = !lastFrameSize.Equals(frameSize);

                    if (frameSizeChanged) // Frame size changed, update related values
                    {
                        lastFrameSize = frameSize;
                        maxFrameSize = math.max(maxFrameSize, frameSize);

                        pos = new int3(2 + xCount * (maxFrameSize.x + 2), 0, 2 + yCount * (maxFrameSize.y + 2));
                        result.UpdateVolume(pos, dataFrame.FX, dataFrame.FY, dataFrame.FZ);
                    }

                    if (model.Animated) // Visualize this step
                    {
                        // Update generation text
                        GenerationText.text = GetL10nString("status.info.generation_step", k, dataFrame.stepCount, (int) (tick * 1000));

                        var stateChar = dataFrame.state.Select(v => dataFrame.legend[v]).ToArray();

                        int3[] gridData;
                        
                        if (frameSize.z == 1) // 2d mode, byte 0 is not air
                        {
                            gridData = stateChar.Select(x => meshPalette[x]).ToArray();
                        }
                        else // 3d mode, byte 0 is air
                        {
                            // The last element in mesh array is empty, used for air blocks
                            var airMeshData = new int3(blockMeshes.Length - 1, 0, 0); // mesh index, material index, color
                            gridData = dataFrame.state.Select(v => v == 0 ? airMeshData : meshPalette[dataFrame.legend[v]]).ToArray();
                        }

                        if (frameSizeChanged) // Size changed
                        {
                            // Regenerate block grid
                            BlockInstanceSpawner.ClearGrid();
                            yield return null;

                            // Initialize with new content
                            BlockInstanceSpawner.InitializeGrid(pos, frameSize, materials, blockMeshes, gridData);
                        }
                        else // Size remains the same, just update content
                        {
                            BlockInstanceSpawner.UpdateGrid(gridData);
                        }

                        yield return null;

                        if (record) // Record the data frame
                        {
                            recordedFrames.Add(new GenerationFrameRecord(frameSize, stateChar));
                        }
                        
                        // Update active node on graph
                        // MODEL GRAPH V1ModelGraphGenerator.UpdateGraph(ModelGraphUI!, interpreter.current);
                        ModelGraphGeneratorV2.UpdateGraph(ModelGraphUIv2!, interpreter.current);
                    }

                    yield return new WaitForSeconds(tick);
                }

                if (executing) // Visualize final state (last step)
                {
                    var stateClone = new byte[dataFrame.state!.Length];
                    Array.Copy(dataFrame.state!, stateClone, stateClone.Length);

                    var legendClone = new char[dataFrame.legend!.Length];
                    Array.Copy(dataFrame.legend!, legendClone, legendClone.Length);

                    // Set result data and generate final mesh
                    result.SetData(fullPalette, stateClone, legendClone, dataFrame.FX, dataFrame.FY, dataFrame.FZ, dataFrame.stepCount, ConfiguredModelFile, seed);

                    Debug.Log($"Iteration {k} complete. Steps: {dataFrame.stepCount} Frames: {recordedFrames.Count}");
                    // MODEL GRAPH V1 ModelGraphUI!.SetActiveNode(-1); // Deselect active node
                    ModelGraphUIv2!.SetActiveNode(-1); // Deselect active node
                    GenerationText.text = GetL10nString("status.info.generation_complete", k);

                    if (record) // Save the recording file
                    {
                        var fileName = (ConfiguredModel!.Model ?? "Untitled") + $"_#{k}";
                        StartCoroutine(RecordingExporter.SaveRecording(fullPalette, fileName,
                                maxFrameSize.x, maxFrameSize.y, maxFrameSize.z, recordedFrames.ToArray()));
                    }
                }

                // Clear block grid after an iteration is completed
                BlockInstanceSpawner.ClearGrid();
            }

            if (executing) // If the execution hasn't been forced stopped
                StopExecution();
        }

        private IEnumerator ImportVoxResult()
        {
            if (executing)
            {
                Debug.LogWarning("Import cannot be initiated");
                yield break;
            }

            yield return null;

            ClearUpScene();
            // Delay a bit so that the imported stuffs doesn't get cleared
            yield return new WaitForSecondsRealtime(0.1F);

            // Get and sanitize file name
            string fileName = VoxPathInput!.text.Trim().Trim('"');

            var (pieces, rgbPalette) = VoxHelper.LoadFullVox(fileName);
            if (pieces == null || pieces.Length == 0)
            {
                Debug.LogWarning("Import failed");
                yield break;
            }

            var resultPerLine = Mathf.CeilToInt(Mathf.Sqrt(pieces.Length));

            int maxX = 0;
            int maxY = 0;

            foreach (var piece in pieces)
            {
                if (piece.SizeX > maxX) maxX = piece.SizeX;
                if (piece.SizeY > maxY) maxY = piece.SizeY;
            }

            for (int i = 0;i < pieces.Length;i++)
            {
                int xCount = i % resultPerLine, yCount = i / resultPerLine;
                var piece = pieces[i];

                var resultObj = Instantiate(GenerationResultPrefab);
                var resultId = nextResultId++;
                resultObj!.name = $"Result #{resultId} (Imported from vox)";
                var result = resultObj.GetComponent<GenerationResult>();
                result.GenerationSeed = 0;
                result.ResultId = resultId;

                var pos = new int3(2 + xCount * (maxX + 2), 0, 2 + yCount * (maxY + 2));
                result.UpdateVolume(pos, piece.SizeX, piece.SizeY, piece.SizeZ);

                result.SetData(piece.BlockData, rgbPalette, piece.SizeX, piece.SizeY, piece.SizeZ);
            }
        }

        void Start()
        {
            // First load Minecraft data & resources
            StartCoroutine(LoadMCBlockData(
                () => {
                    ExecuteButton!.interactable = false;
                    ExecuteButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.load_resource");
                    VoxImportButton!.interactable = false;
                    VoxImportButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.load_resource");
                },
                (status, progress) => GenerationText!.text = GetL10nString(status) + progress,
                () => {
                    PlaybackSpeedSlider!.onValueChanged.AddListener(UpdatePlaybackSpeed);
                    UpdatePlaybackSpeed(PlaybackSpeedSlider.value);

                    CreateButton!.onClick.RemoveAllListeners();
                    CreateButton.onClick.AddListener(() => {
                        HideSpecialGUI();
                        screenManager!.SetActiveScreenByType<ConfiguredModelCreatorScreen>();
                    });

                    ConfigButton!.onClick.RemoveAllListeners();
                    ConfigButton.onClick.AddListener(() => {
                        HideSpecialGUI();
                        screenManager!.SetActiveScreenByType<ConfiguredModelEditorScreen>();
                    });

                    RemoveButton!.onClick.RemoveAllListeners();
                    RemoveButton.onClick.AddListener(RemoveSelectedResult);

                    ExportButton!.onClick.RemoveAllListeners();
                    ExportButton.onClick.AddListener(() => {
                        HideSpecialGUI();
                        screenManager!.SetActiveScreenByType<ResultExporterScreen>();
                    });

                    ResizeButton!.onClick.RemoveAllListeners();
                    ResizeButton.onClick.AddListener(() => {
                        HideSpecialGUI();
                        screenManager!.SetActiveScreenByType<ResultSizeUpperScreen>();
                    });

                    ExtrudeButton!.onClick.RemoveAllListeners();
                    ExtrudeButton.onClick.AddListener(HideSpecialGUI);

                    RCONButton!.onClick.RemoveAllListeners();
                    RCONButton.onClick.AddListener(() => {
                        HideSpecialGUI();
                        screenManager!.SetActiveScreenByType<ResultRCONScreen>();
                    });

                    UnlockButton!.onClick.RemoveAllListeners();
                    UnlockButton.onClick.AddListener(UnlockSelectedResult);

                    VoxImportButton!.interactable = true;
                    VoxImportButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.import_vox");
                    VoxImportButton.onClick.RemoveAllListeners();
                    VoxImportButton.onClick.AddListener(() => StartCoroutine(ImportVoxResult()));

                    ControlTabPanel!.OnSelectionChange.RemoveAllListeners();
                    ControlTabPanel.OnSelectionChange.AddListener(ClearUpScene);

                    UpdateConfModelList();

                    if (loadedConfModels.Count > 0) // Use first model by default
                        UpdateDropdownOption(0);
                })
            );
        }

        private void UpdateBlockSelection(CustomMappingItem? selectedItem, string text = "")
        {
            if (selectedItem == null)
            {
                BlockSelectionMappingItem!.SetBlockState(string.Empty);
                BlockSelectionPanelGroup!.alpha = 0F;
            }
            else
            {
                BlockSelectionPanelGroup!.alpha = 1F;
                BlockSelectionText!.text = text;
                // Update mapping item information
                BlockSelectionMappingItem!.SetBlockState(selectedItem.BlockState);
                //BlockSelectionMappingItem!.SetCharacter(selectedItem.Symbol);
                var hexString = ColorConvert.GetHexRGBString(selectedItem.Color);
                BlockSelectionMappingItem!.OnColorCodeInputValidate(hexString);
                BlockSelectionMappingItem!.OnColorCodeInputValueChange(hexString);
            }
        }

        private void Update()
        {
            if (FPSText)
                FPSText.text = $"FPS:{(int)(1 / Time.unscaledDeltaTime), 4}";
            
            if (screenManager && !screenManager.AllowsMovementInput) return;
            
            var cam = CamController!.ViewCamera;
            
            if (cam && VolumeSelection)
            {
                if (!VolumeSelection.Locked) // Update selected volume
                {
                    var ray = cam.ScreenPointToRay(Input.mousePosition);

                    if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000F, VolumeLayerMask))
                    {
                        UpdateSelectedResult(hit.collider.gameObject.GetComponentInParent<GenerationResult>());

                        if (Input.GetKeyDown(KeyCode.Mouse0) && selectedResult!.Completed) // Lock can only be applied to completed results
                        {
                            // Lock volume selection
                            VolumeSelection!.Lock();
                            // Set camera center
                            CamController!.SetCenterPosition(selectedResult.GetVolumePosition());
                            // Enable block colliders
                            selectedResult.EnableBlockColliders();
                            // Show export button
                            ResultOperationPanelAnimator!.SetBool(HIDDEN, false);
                        }
                    }
                    else
                    {
                        UpdateSelectedResult(null);
                    }
                }
                else // Selection is locked
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        var ray = cam.ScreenPointToRay(Input.mousePosition);

                        // Volume is still locked, update block selection
                        if (selectedResult && Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000F, BlockMeshLayerMask)) // Mouse pointer is over a block
                        {
                            // Get block position in the volume (local space in volume)
                            var blockPos = hit.point - hit.normal * 0.01F;

                            var (x, y, z, unityPos, item) = selectedResult.GetBlockPosInVolume(blockPos);
                            UpdateBlockSelection(item, $"({x}, {y}, {z})");

                            /* if (item == null && Input.GetKeyDown(KeyCode.Mouse0)) // Unlock volume
                            {
                                UnlockSelectedResult();
                            } */

                            // Update cursor position (world space)
                            BlockSelection!.transform.position = unityPos;
                        }
                        else // Mouse pointer is over no block
                        {
                            /* if (Input.GetKeyDown(KeyCode.Mouse0)) // Unlock volume
                            {
                                UnlockSelectedResult();
                            } */

                            // Reset block selection
                            BlockSelection!.transform.position = BLOCK_SELECTION_HIDDEN_POS;
                            UpdateBlockSelection(null);
                        }
                    }
                }
            }
        }

        public void UpdateSelectedResult(GenerationResult? newResult)
        {
            if (selectedResult == newResult) return;

            if (selectedResult)
            {
                // Disable block colliders
                selectedResult.DisableBlockColliders();
            }

            if (newResult && newResult.Valid) // Valid
            {
                var fsc = newResult.FinalStepCount;

                VolumeText!.text = GetL10nString("hud.text.result_info", newResult.ResultId, newResult.GenerationSeed,
                        newResult.SizeX, newResult.SizeY, newResult.SizeZ, fsc == 0 ? "-" : fsc.ToString());
                VolumeSelection!.UpdateVolume(newResult.GetVolumePosition(), newResult.GetVolumeSize());
            
                selectedResult = newResult;
            }
            else // Null or going to be destroyed
            {
                VolumeText!.text = string.Empty;
                VolumeSelection!.HideVolume();
                // Reset block selection
                BlockSelection!.transform.position = BLOCK_SELECTION_HIDDEN_POS;
                UpdateBlockSelection(null);

                // Hide export button
                ResultOperationPanelAnimator!.SetBool(HIDDEN, true);

                selectedResult = null;
            }
        }

        private void UnlockSelectedResult()
        {
            // Unlock volume selection
            VolumeSelection!.Unlock();
            // Disable block colliders
            selectedResult!.DisableBlockColliders();
            // Hide export button
            ResultOperationPanelAnimator!.SetBool(HIDDEN, true);
        }

        public GenerationResult? GetSelectedResult()
        {
            if (!selectedResult || !selectedResult.Valid || !selectedResult.Completed)
                return null;

            // Result data should be present if the generation is completed
            return selectedResult;
        }

        private void RemoveSelectedResult()
        {
            var selected = GetSelectedResult();

            if (selected)
            {
                // Update result selection
                UpdateSelectedResult(null);
                // Destroy gameobject of the selected result
                Destroy(selected.gameObject);
            }
        }

        private void ClearUpScene()
        {
            // Clear up persistent entities
            BlockInstanceSpawner.ClearUpPersistentState();
            // Update result selection
            UpdateSelectedResult(null);
            // Clear up generation results
            var results = Component.FindObjectsByType<GenerationResult>(FindObjectsSortMode.None).ToArray();
            for (int i = 0;i < results.Length;i++)
            {
                results[i].Valid = false;
                Destroy(results[i].gameObject);
            }
        }

        private void UpdatePlaybackSpeed(float newValue)
        {
            playbackSpeed = newValue;

            if (PlaybackSpeedText)
                PlaybackSpeedText.text = $"{newValue:0.0}";
            
        }

        private void UpdateDropdownOption(int newValue)
        {
            if (loadedConfModels.TryGetValue(newValue, out var model))
                SetConfiguredModel(model);
        }

        private void UpdateConfModelList()
        {
            var dir = MarkovGlobal.GetDataFile(CONFIGURED_MODEL_FOLDER);

            if (Directory.Exists(dir))
            {
                var options = new List<TMP_Dropdown.OptionData>();
                loadedConfModels.Clear();
                int index = 0;
                foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories))
                {
                    var modelPath = m[(m.LastIndexOf(SP) + 1)..];
                    options.Add(new(modelPath));
                    loadedConfModels.Add(index++, modelPath);
                }

                ConfiguredModelDropdown!.onValueChanged.RemoveAllListeners();
                ConfiguredModelDropdown!.ClearOptions();
                ConfiguredModelDropdown.AddOptions(options);
                ConfiguredModelDropdown.onValueChanged.AddListener(UpdateDropdownOption);
            }
        }

        public void UpdateConfiguredModel(string confModelFile)
        {
            // First update configure model list
            UpdateConfModelList();
            // Find index of this item in our list
            int selectedIndex = -1;
            foreach (var pair in loadedConfModels.Where(pair => pair.Value == confModelFile))
            {
                selectedIndex = pair.Key;
            }

            if (selectedIndex != -1)
            {
                ConfiguredModelDropdown!.value = selectedIndex;
                UpdateDropdownOption(selectedIndex);
            }
        }

        private void SetConfiguredModel(string newConfModelFile)
        {
            if (executing)
                StopExecution();
            
            ConfiguredModelFile = newConfModelFile;

            var xdoc = XDocument.Load($"{MarkovGlobal.GetDataFile(CONFIGURED_MODEL_FOLDER)}/{ConfiguredModelFile}");
            var newConfModel = ConfiguredModel.CreateFromXMLDoc(xdoc);

            // Assign new configured model
            ConfiguredModel = newConfModel;

            if (!Loading)
                StartCoroutine(UpdateConfiguredModel(newConfModelFile, newConfModel));
        }

        private void StartExecution()
        {
            if (Loading || executing)
            {
                Debug.LogWarning("Execution cannot be started.");
                return;
            }

            StartCoroutine(RunGeneration());

            if (ExecuteButton)
            {
                ExecuteButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.stop_execution");
                ExecuteButton.onClick.RemoveAllListeners();
                ExecuteButton.onClick.AddListener(StopExecution);
            }

            if (RecordToggle)
            {
                RecordToggle.enabled = false;
            }
        }

        private void StopExecution()
        {
            if (Loading)
            {
                Debug.LogWarning("Execution cannot be stopped.");
                return;
            }

            executing = false;
            //ModelGraphUI!.SetActiveNode(-1);
            ModelGraphUIv2!.SetActiveNode(-1);

            if (ExecuteButton)
            {
                ExecuteButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("control.text.start_execution");
                ExecuteButton.onClick.RemoveAllListeners();
                ExecuteButton.onClick.AddListener(StartExecution);
            }

            if (RecordToggle)
            {
                RecordToggle.enabled = true;
            }
        }

        public override void ReturnToMenu()
        {
            if (executing)
                StopExecution();
            
            ClearUpScene();

            // Unpause game to restore timescale
            screenManager!.IsPaused = false;

            SceneManager.LoadScene("Scenes/Welcome", LoadSceneMode.Single);
        }
    }
}