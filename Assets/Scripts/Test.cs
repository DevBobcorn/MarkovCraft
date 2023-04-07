#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using MarkovJunior;
using MarkovBlocks.Mapping;

namespace MarkovBlocks
{
    public class Test : MonoBehaviour
    {
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;

        [SerializeField] public TMP_Text? playbackSpeedText, generationText;
        [SerializeField] public Slider? playbackSpeedSlider;
        [SerializeField] public RawImage? graphImage;

        [SerializeField] public string selectedModelPath = string.Empty;

        private MarkovJuniorModel? currentModel = null;
        private Interpreter? interpreter = null;
        private float playbackSpeed = 1F;
        private bool running = false;

        // Palettes and meshes
        private readonly ResourcePackManager packManager = new();
        private Mesh[] blockMeshes = { };
        private int blockMeshCount = 0;
        private readonly Dictionary<char, int2> fullPalette = new();
        private Material? blockMaterial;

        private readonly LoadStateInfo loadInfo = new();

        private void RedrawProcedureGraph(Dictionary<char, int2> palette)
        {
            if (currentModel != null && interpreter != null && graphImage != null)
            {
                int imageX = 200, imageY = 600;
                var image = new int[imageX * imageY];

                MarkovJunior.GUI.Draw(currentModel.Name, interpreter.root, null, image, imageX, imageY, palette);
                
                Texture2D texture = new(imageX, imageY);

                var color32s = new Color32[imageX * imageY];

                for (int y = 0; y < imageY; y++) for (int x = 0; x < imageX; x++)
                {
                    int rgb = image[x + (imageY - 1 - y) * imageX];
                    color32s[x + y * imageX] = ColorConvert.GetOpaqueColor32(rgb);
                }

                texture.SetPixels32(color32s);
                texture.Apply(false);
                
                graphImage.texture = texture;
                graphImage.SetNativeSize();

            }
            else
                Debug.LogWarning("Failed to update procedure graph due to stuffs missing!");

        }

        private void GenerateBlockMeshes(Dictionary<int, int> stateId2Mesh) // StateId => Mesh index
        {
            var statePalette = BlockStatePalette.INSTANCE;
            var buffers = new VertexBuffer[blockMeshCount];
            
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
                    modelTable[stateId].Geometries[0].Build(ref buffers[pair.Value], float3.zero, 0b111111,
                            statePalette.GetBlockColor(stateId, dummyWorld, Location.Zero, statePalette.FromId(stateId)));
                else
                {
                    Debug.LogWarning($"Model for block state #{stateId} ({statePalette.FromId(stateId)}) is not available. Using cube model instead.");
                    CubeGeometry.Build(ref buffers[pair.Value], AtlasManager.HAKU, 0, 0, 0, 0b111111, new float3(1F, 1F, 1F));
                }
            }

            // Set result to blockMeshes
            blockMeshes = BlockMeshGenerator.GenerateMeshes(buffers);
        }

        public void SetGenerationModel(MarkovJuniorModel model)
        {
            // Assign new generation model
            currentModel = model;

            string fileName = PathHelper.GetExtraDataFile($"models/{model.Name}.xml");
            Debug.Log($"{model.Name} > {fileName}");

            XDocument modeldoc;
            try { modeldoc = XDocument.Load(fileName, LoadOptions.SetLineInfo); }
            catch (Exception)
            {
                Debug.LogWarning($"ERROR: Couldn't open xml file at {fileName}");
                return;
            }

            interpreter = Interpreter.Load(modeldoc.Root, model.SizeX, model.SizeY, model.SizeZ);

            if (interpreter == null)
            {
                Debug.LogWarning("ERROR: Failed to creating model interpreter");
                return;
            }

            var statePalette = BlockStatePalette.INSTANCE;
            var stateId2Mesh = new Dictionary<int, int>(); // StateId => Mesh index

            fullPalette.Clear();

            Dictionary<char, int> basePalette = XDocument.Load(PathHelper.GetExtraDataFile("palette.xml")).Root.Elements("color")
                    .ToDictionary(x => x.Get<char>("symbol"), x => (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16));
            
            foreach (var item in basePalette) // Use mesh #0 by default (cube mesh)
                fullPalette.Add(item.Key, new(0, item.Value));

            blockMeshCount = 1; // #0 is preserved for default cube mesh

            foreach (var remap in model.CustomRemapping) // Read and assign custom remapping
            {
                int rgba = ColorConvert.GetRGBA(remap.RemapColor);

                if (!string.IsNullOrWhiteSpace(remap.RemapTarget))
                {
                    int remapStateId = BlockStateRemapper.GetStateIdFromString(remap.RemapTarget);
                    
                    if (remapStateId != BlockStateRemapper.INVALID_BLOCKSTATE)
                    {
                        var state = statePalette.StatesTable[remapStateId];

                        Debug.Log($"Remapped '{remap.Symbol}' to [{remapStateId}] {state}");

                        if (stateId2Mesh.TryAdd(remapStateId, blockMeshCount))
                            fullPalette[remap.Symbol] = new(blockMeshCount++, rgba);
                        else // The mesh of this block state is already regestered, just use it
                            fullPalette[remap.Symbol] = new(stateId2Mesh[remapStateId], rgba);
                    }
                    else // Default cube mesh with custom color
                        fullPalette[remap.Symbol] = new(0, rgba);
                }
                else // Default cube mesh with custom color
                    fullPalette[remap.Symbol] = new(0, rgba);
            }

            // Update procedure graph
            RedrawProcedureGraph(fullPalette);

            // Generate block meshes
            GenerateBlockMeshes(stateId2Mesh);
        }

        private IEnumerator LoadMCData(string dataVersion, string[] packs, Action? callback = null)
        {
            var wait = new WaitForSecondsRealtime(0.1F);

            loadInfo.Loading = true;

            // First load all possible Block States...
            var loadFlag = new DataLoadFlag();
            StartCoroutine(BlockStatePalette.INSTANCE.PrepareData(dataVersion, loadFlag, loadInfo));

            while (!loadFlag.Finished)
                yield return wait;
            
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
            if (currentModel is null || interpreter is null || blockMaterial is null || loadInfo.Loading || running)
            {
                Debug.LogWarning("Generation cannot be initiated");
                yield break;
            }

            running = true;

            var model = currentModel;

            var resultPerLine = Mathf.RoundToInt(Mathf.Sqrt(model.Amount));
            resultPerLine = Mathf.Max(resultPerLine, 1);
            
            Material[] materials = { blockMaterial };

            System.Random rand = new();
            var seeds = model.Seeds;

            // Set up stopwatch
            var sw = System.Diagnostics.Stopwatch.StartNew();

            for (int k = 0; k < model.Amount; k++)
            {
                int seed = seeds != null && k < seeds.Length ? seeds[k] : rand.Next();
                int frameCount = 0;

                (int3[], int2[])? instanceDataRaw = null;

                foreach ((byte[] result, char[] legend, int FX, int FY, int FZ) in interpreter.Run(seed, model.Steps, model.Animated))
                {
                    int2[] stepPalette = legend.Select(ch => fullPalette[ch]).ToArray();
                    float tick = 1F / playbackSpeed;

                    if (instanceDataRaw != null)
                        BlockInstanceSpawner.VisualizeState(instanceDataRaw.Value, materials, blockMeshes, tick);

                    // Update generation text
                    if (generationText != null)
                        generationText.text = $"Iteration: {k + 1}\nFrame: {frameCount}\nTick: {(int)(tick * 1000)}ms";

                    frameCount++;

                    int xCount = k / resultPerLine, zCount = k % resultPerLine;
                    int ox = xCount * (FX + 2), oz = zCount * (FY + 2);

                    instanceDataRaw = BlockDataBuilder.GetInstanceData(result,
                            FX, FY, FZ, ox, 0, oz, FZ > 1, stepPalette);

                    yield return new WaitForSeconds(tick);
                }

                if (instanceDataRaw != null)
                    BlockInstanceSpawner.VisualizeState(instanceDataRaw.Value, materials, blockMeshes, 0F); // The final visualization is persistent

                Debug.Log($"Generation complete. Frame Count: {frameCount}");
            }

            Debug.Log($"Time elapsed: {sw.ElapsedMilliseconds}ms");

            sw.Stop(); // Stop stopwatch
            running = false;
        }

        void Start()
        {
            if (playbackSpeedSlider != null) // Initialize playback speed
                UpdatePlaybackSpeed(playbackSpeedSlider.value);
            else
                Debug.LogWarning("Playback speed slider is not assigned!");

            // First load Minecraft data & resources
            StartCoroutine(LoadMCData("markov", new string[] {
                    "vanilla-1.16.5", "vanilla_fix", "default"
                }, () => {
                    //AssetBundle.LoadFromFileAsync()
                    var dir = PathHelper.GetExtraDataFile("configured_models");
                    if (Directory.Exists(dir))
                    {
                        var models = Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories);

                        foreach (var m in models)
                        {
                            var modelPath = m.Substring(dir.Length + 1);
                            Debug.Log($"Configured model: {modelPath}");
                            
                            if (string.IsNullOrWhiteSpace(selectedModelPath))
                                selectedModelPath = modelPath;
                            
                        }

                        if (!string.IsNullOrWhiteSpace(selectedModelPath))
                        {
                            // Set default generation model
                            var xdoc = XDocument.Load($"{dir}/{selectedModelPath}");
                            SetGenerationModel(MarkovJuniorModel.CreateFromXMLDoc(xdoc));

                            // Start it up!
                            StartCoroutine(RunGeneration());
                        }
                        
                    }
                    else
                        Debug.LogWarning("Configured model folder not found!");
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

            if (loadInfo.Loading && generationText != null)
                generationText.text = loadInfo.InfoText;

        }

        public void UpdatePlaybackSpeed(float newValue)
        {
            if (playbackSpeedText != null) // Update playback speed
            {
                playbackSpeedText.text = $"{newValue:0.0}";
                playbackSpeed = newValue;
            }
            else
                Debug.LogWarning("Playback speed text is not assigned!");
            
        }

    }
}