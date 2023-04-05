#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using TMPro;

using MarkovJunior;
using MarkovBlocks.Mapping;

namespace MarkovBlocks
{
    public class Test : MonoBehaviour
    {
        private static readonly Bounds cubeBounds = new Bounds(new(0.5F, 0.5F, 0.5F), new(1F, 1F, 1F));
        private static readonly RenderBounds renderBounds = new RenderBounds { Value = cubeBounds.ToAABB() };

        [SerializeField] MarkovJuniorModel? generationModel;

        [SerializeField] public TMP_Text? playbackSpeedText, generationText;
        [SerializeField] public Slider? playbackSpeedSlider;

        [SerializeField] public Material? blockMaterial;
        [SerializeField] public RawImage? graphImage;

        private float playbackSpeed = 1F;
        private readonly List<Mesh> blockMeshes = new();

        private readonly LoadStateInfo loadStateInfo = new();

        private IEnumerator RunTest(MarkovJuniorModel model, string dataVersion, string[] packs)
        {
            #region Load model data
            var wait = new WaitForSecondsRealtime(0.1F);

            loadStateInfo.loggingIn = true;

            // First load all possible Block States...
            var loadFlag = new DataLoadFlag();
            StartCoroutine(BlockStatePalette.INSTANCE.PrepareData(dataVersion, loadFlag, loadStateInfo));

            while (!loadFlag.Finished)
                yield return wait;
            
            // Then load all Items...
            // [Code removed]

            // Create a new resource pack manager...
            var packManager = new ResourcePackManager();

            // Load resource packs...
            packManager.ClearPacks();
            // Collect packs
            foreach (var packName in packs)
                packManager.AddPack(new(packName));
            // Load valid packs...
            loadFlag.Finished = false;
            StartCoroutine(packManager.LoadPacks(this, loadFlag, loadStateInfo));

            while (!loadFlag.Finished)
                yield return null;
            
            loadStateInfo.loggingIn = false;

            if (loadFlag.Failed)
            {
                Debug.LogWarning("Block data loading failed");
                yield break;
            }

            MaterialManager.EnsureInitialized();
            blockMaterial = MaterialManager.GetAtlasMaterial(RenderType.SOLID);

            #endregion
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var folder = System.IO.Directory.CreateDirectory("output");
            foreach (var file in folder.GetFiles()) file.Delete();

            // Mesh indices are set to #0 by default
            Dictionary<char, int2> colorPalette = XDocument.Load(PathHelper.GetExtraDataFile("palette.xml")).Root.Elements("color")
                    .ToDictionary(x => x.Get<char>("symbol"), x => new int2(0, (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16)));

            System.Random rand = new();

            Debug.Log($"{model.Name} > ");
            string filename = PathHelper.GetExtraDataFile($"models/{model.Name}.xml");

            XDocument modeldoc;
            try { modeldoc = XDocument.Load(filename, LoadOptions.SetLineInfo); }
            catch (Exception)
            {
                Debug.Log($"ERROR: Couldn't open xml file {filename}");
                yield break;
            }

            Interpreter interpreter = Interpreter.Load(modeldoc.Root, model.SizeX, model.SizeY, model.SizeZ);
            if (interpreter == null)
            {
                Debug.Log("ERROR: Failed to creating model interpreter");
                yield break;
            }

            var modelSeeds = model.Seeds;

            if (modelSeeds != null && modelSeeds.Trim().Equals(string.Empty)) // Seed not specified
                modelSeeds = null;
            
            int[]? seeds = modelSeeds?.Split(' ').Select(s => int.Parse(s)).ToArray();
            
            var fullPalette = new Dictionary<char, int2>(colorPalette);

            int meshCount = 1; // #0 is preserved for default cube mesh
            var meshTable = new Dictionary<int, int>(); // StateId => Mesh index

            var statePalette = BlockStatePalette.INSTANCE;

            foreach (var remap in model.CustomRemapping)
            {
                var color = remap.RemapColor;
                int rgba = (color.a << 24) + (color.r << 16) + (color.g << 8) + color.b;

                if (!string.IsNullOrEmpty(remap.RemapTarget))
                {
                    int remapStateId = BlockStateRemapper.GetStateIdFromString(remap.RemapTarget);
                    
                    if (remapStateId != BlockStateRemapper.INVALID_BLOCKSTATE)
                    {
                        var state = statePalette.StatesTable[remapStateId];

                        Debug.Log($"Remapped '{remap.Symbol}' to [{remapStateId}] {state}");

                        if (meshTable.TryAdd(remapStateId, meshCount))
                        {
                            Debug.Log($"Assigned mesh for [{remapStateId}] {state} to #{meshCount}");

                            fullPalette[remap.Symbol] = new(meshCount++, rgba);
                        }
                        else // The mesh of this block state is already regestered, just use it
                            fullPalette[remap.Symbol] = new(meshTable[remapStateId], rgba);
                    }
                    else // Default cube mesh with custom color
                        fullPalette[remap.Symbol] = new(0, rgba);
                }
                else // Default cube mesh with custom color
                    fullPalette[remap.Symbol] = new(0, rgba);
            }

            var resultPerLine = Mathf.RoundToInt(Mathf.Sqrt(model.Amount));

            if (resultPerLine <= 0)
                resultPerLine = 1;
            
            #region Generate meshes
            // Generate block meshes
            var buffers = new VertexBuffer[meshCount];
            
            for (int i = 0;i < buffers.Length;i++)
                buffers[i] = new VertexBuffer();

            // #0 is default cube mesh
            CubeGeometry.Build(ref buffers[0], AtlasManager.HAKU, 0, 0, 0, 0b111111, new float3(1F));

            var dummyWorld = new MarkovBlocks.Mapping.World();
            
            foreach (var pair in meshTable) // StateId => Mesh index
            {
                var stateId = pair.Key;

                packManager.StateModelTable[stateId].Geometries[0].Build(ref buffers[pair.Value], float3.zero, 0b111111,
                        statePalette.GetBlockColor(stateId, dummyWorld, Location.Zero, statePalette.FromId(stateId)));

            }

            blockMeshes.AddRange(BlockMeshGenerator.GenerateMeshes(buffers));
            #endregion
            
            var meshesArr = blockMeshes.ToArray();

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
                        VisualizeState(instanceDataRaw.Value, meshesArr, tick);

                    // Update generation text
                    if (generationText != null)
                        generationText.text = $"Iteration: {k + 1}\nFrame: {frameCount}\nTick: {(int)(tick * 1000)}ms";

                    frameCount++;

                    int xCount = k / resultPerLine, zCount = k % resultPerLine;
                    int ox = xCount * (FX + 2), oz = zCount * (FY + 2);

                    instanceDataRaw = CubeDataBuilder.GetInstanceData(result,
                            FX, FY, FZ, ox, 0, oz, FZ > 1, stepPalette);

                    yield return new WaitForSeconds(tick);
                }

                if (instanceDataRaw != null)
                    VisualizeState(instanceDataRaw.Value, meshesArr, 0F); // The final visualization is persistent

                Debug.Log($"Generation complete. Frame Count: {frameCount}");
            }

            Debug.Log($"Time elapsed: {sw.ElapsedMilliseconds}ms");
        }

        private void VisualizeState((int3[], int2[]) instanceDataRaw, Mesh[] meshes, float persistence)
        {
            bool persistent = persistence <= 0F;
            var entityCount = instanceDataRaw.Item1.Length;

            var posData = new NativeArray<int3>(entityCount, Allocator.TempJob);
            posData.CopyFrom(instanceDataRaw.Item1);
            var meshData = new NativeArray<int2>(entityCount, Allocator.TempJob);
            meshData.CopyFrom(instanceDataRaw.Item2);

            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            EntityCommandBuffer ecbJob = new EntityCommandBuffer(Allocator.TempJob);
            
            #region Prepare entity prototype
            var filterSettings = RenderFilterSettings.Default;

            var renderMeshArray = new RenderMeshArray(new[] { blockMaterial }, meshes);
            var renderMeshDescription = new RenderMeshDescription
            {
                FilterSettings = filterSettings,
                LightProbeUsage = LightProbeUsage.Off,
            };

            var prototype = entityManager.CreateEntity();

            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                renderMeshDescription,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            
            entityManager.AddComponentData(prototype, new InstanceBlockColor());

            if (!persistent) // Add magic component to make it expire after some time
                entityManager.AddComponentData(prototype, new MagicComponent());
            
            #endregion

            // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
            // which can be either a Prefab or an entity created at run time like in this sample.
            // This is the fastest and most efficient way to create entities at run time.
            var spawnJob = new SpawnJob
            {
                Ecb = ecbJob.AsParallelWriter(),
                Prototype = prototype,
                RenderBounds = renderBounds,
                PositionData = posData,
                MeshData = meshData,
                LifeTime = persistence
            };

            var spawnHandle = spawnJob.Schedule(entityCount, 128);

            spawnHandle.Complete();

            ecbJob.Playback(entityManager);
            ecbJob.Dispose();

            entityManager.DestroyEntity(prototype);
        }

        void Start()
        {
            if (playbackSpeedSlider != null) // Initialize playback speed
                UpdatePlaybackSpeed(playbackSpeedSlider.value);
            else
                Debug.LogWarning("Playback speed slider is not assigned!");
            
            if (generationModel == null)
            {
                Debug.LogWarning("Markov Junior model not assigned!");
                return;
            }

            // Start it up!
            StartCoroutine(RunTest(generationModel, "markov", new string[] { "vanilla-1.16.5", "vanilla_fix", "default" }));

        }

        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;

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

            if (loadStateInfo.loggingIn && generationText != null)
            {
                generationText.text = loadStateInfo.infoText;
            }

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