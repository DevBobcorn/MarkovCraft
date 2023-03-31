#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using TMPro;

using MarkovJunior;

namespace MarkovBlocks
{
    public class Test : MonoBehaviour
    {
        private static readonly Bounds cubeBounds = new Bounds(new(0.5F, 0.5F, 0.5F), new(1F, 1F, 1F));
        private static readonly RenderBounds renderBounds = new RenderBounds { Value = cubeBounds.ToAABB() };

        [SerializeField] public Material? cubeMaterial;
        private Mesh[] cubeMeshes = new Mesh[1];

        [SerializeField] public TMP_InputField? modelInput;

        private void GenerateCubeMeshes()
        {
            var meshDataArr = Mesh.AllocateWritableMeshData(cubeMeshes.Length);

            var buffer = new VertexBuffer();
            CubeGeometry.Build(ref buffer, 0, 0, 0, 0b111111, float3.zero);

            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);

            int vertexCount = buffer.vert.Length;
            int triIdxCount = (vertexCount / 2) * 3;
            
            // Set mesh params
            meshData.SetVertexBufferParams(vertexCount, vertAttrs);
            vertAttrs.Dispose();

            meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

            // Set vertex data
            // Positions
            var positions = meshData.GetVertexData<float3>(0);
            positions.CopyFrom(buffer.vert);

            // Set face data
            var triIndices = meshData.GetIndexData<uint>();
            uint vi = 0; int ti = 0;
            for (;vi < vertexCount;vi += 4U, ti += 6)
            {
                triIndices[ti]     = vi;
                triIndices[ti + 1] = vi + 3U;
                triIndices[ti + 2] = vi + 2U;
                triIndices[ti + 3] = vi;
                triIndices[ti + 4] = vi + 1U;
                triIndices[ti + 5] = vi + 3U;
            }

            // Set sub mesh and bounds
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                bounds = cubeBounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            // Create mesh and apply mesh data
            cubeMeshes[0] = new Mesh { bounds = cubeBounds };
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, cubeMeshes);
            cubeMeshes[0].RecalculateNormals();
        }

        private IEnumerator RunTest()
        {
            #region Init Start =====================================================================
            // Generate cube meshes
            GenerateCubeMeshes();
            var cubeCount = 0;
            #endregion Init End =======================================================================
        
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var folder = System.IO.Directory.CreateDirectory("output");
            foreach (var file in folder.GetFiles()) file.Delete();

            Dictionary<char, int> palette = XDocument.Load(PathHelper.GetExtraDataFile("palette.xml")).Root.Elements("color")
                    .ToDictionary(x => x.Get<char>("symbol"), x => (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16));

            if (modelInput == null)
            {
                Debug.LogError("Model input field is not assigned");
                yield break;
            }

            XElement? xmodel = null;

            try
            {
                xmodel = XElement.Load(new StringReader(modelInput.text), LoadOptions.None);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse model input: {e}");
                yield break;
            }
            
            string name = xmodel.Get<string>("name");
            int linearSize = xmodel.Get("size", -1);
            int dimension = xmodel.Get("d", 2);
            int MX = xmodel.Get("length", linearSize);
            int MY = xmodel.Get("width", linearSize);
            int MZ = xmodel.Get("height", dimension == 2 ? 1 : linearSize);

            System.Random rand = new();

            Debug.Log($"{name} > ");
            string filename = PathHelper.GetExtraDataFile($"models/{name}.xml");

            XDocument modeldoc;
            try { modeldoc = XDocument.Load(filename, LoadOptions.SetLineInfo); }
            catch (Exception)
            {
                Debug.Log($"ERROR: couldn't open xml file {filename}");
                yield break;
            }

            Interpreter interpreter = Interpreter.Load(modeldoc.Root, MX, MY, MZ);
            if (interpreter == null)
            {
                Debug.Log("ERROR");
                yield break;
            }

            int amount = xmodel.Get("amount", 2);
            int pixelsize = xmodel.Get("pixelsize", 4);
            string? seedString = xmodel.Get<string?>("seeds", null);
            int[]? seeds = seedString?.Split(' ').Select(s => int.Parse(s)).ToArray();
            bool gif = xmodel.Get("gif", false);
            bool iso = xmodel.Get("iso", false);
            int steps = xmodel.Get("steps", gif ? 1000 : 50000);
            int gui = xmodel.Get("gui", 0);
            if (gif) amount = 1;

            Dictionary<char, int> customPalette = new(palette);

            foreach (var x in xmodel.Elements("color"))
                customPalette[x.Get<char>("symbol")] = (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16);

            var tick = 0.1F;
            var wait = new WaitForSeconds(tick);

            var resultPerLine = Mathf.RoundToInt(Mathf.Sqrt(amount));
            if (resultPerLine <= 0)
                resultPerLine = 1;

            for (int k = 0; k < amount; k++)
            {
                int seed = seeds != null && k < seeds.Length ? seeds[k] : rand.Next();
                int frameCount = 0;

                foreach ((byte[] result, char[] legend, int FX, int FY, int FZ) in interpreter.Run(seed, steps, gif))
                {
                    int[] colors = legend.Select(ch => customPalette[ch]).ToArray();

                    frameCount++;

                    int xCount = k % resultPerLine, zCount = k / resultPerLine;
                    int ox = xCount * (FX + 2), oz = zCount * (FY + 2);

                    var instanceDataRaw = GetInstanceData(result, (byte)FX, (byte)FY, (byte)FZ, ox, 0, oz, FZ > 1, colors);

                    VisualizeState(instanceDataRaw, cubeMeshes, ref cubeCount, tick);

                    yield return wait;
                }

                Debug.Log($"DONE Frame Count: {frameCount}");

                yield return wait;
            }
            

            #region Finalize Start =================================================================
            //entityManager.DestroyEntity(prototype);

            #endregion Finalize End ===================================================================

            Debug.Log($"time = {sw.ElapsedMilliseconds}, totalCubeCount = {cubeCount}");
        }

        private void VisualizeState(int4[] instanceDataRaw, Mesh[] meshes, ref int cubeCount, float persistence)
        {
            var entityCount = instanceDataRaw.Length;
            cubeCount += entityCount;

            var instanceData = new NativeArray<int4>(entityCount, Allocator.TempJob);
            instanceData.CopyFrom(instanceDataRaw);

            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            EntityCommandBuffer ecbJob = new EntityCommandBuffer(Allocator.TempJob);
            
            #region Prepare entity prototype
            var filterSettings = RenderFilterSettings.Default;

            var renderMeshArray = new RenderMeshArray(new[] { cubeMaterial }, cubeMeshes);
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
            entityManager.AddComponentData(prototype, new URPMaterialPropertyBaseColor());
            entityManager.AddComponentData(prototype, new MagicComponent { TimeLeft = persistence + 0.01F });
            #endregion

            // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
            // which can be either a Prefab or an entity created at run time like in this sample.
            // This is the fastest and most efficient way to create entities at run time.
            var spawnJob = new SpawnJob
            {
                Ecb = ecbJob.AsParallelWriter(),
                Prototype = prototype,
                RenderBounds = renderBounds,
                InstanceData = instanceData,
                LifeTime = 1F
            };

            var spawnHandle = spawnJob.Schedule(entityCount, 128);

            spawnHandle.Complete();

            ecbJob.Playback(entityManager);
            ecbJob.Dispose();

            entityManager.DestroyEntity(prototype);
            
        }

        private static bool checkNotOpaque3d(byte block) => block == 0;

        private int4[] GetInstanceData(byte[] state, byte FX, byte FY, byte FZ, int ox, int oy, int oz, bool is3d, int[] palette)
        {
            if (cubeMaterial == null)
            {
                Debug.LogWarning("Cube material not assigned!");
                return new int4[] { };
            }

            List<int4> instanceData = new();

            for (byte z = 0; z < FZ; z++) for (byte y = 0; y < FY; y++) for (byte x = 0; x < FX; x++)
            {
                byte v = state[x + y * FX + z * FX * FY];
                
                if (is3d) // 3d structure
                {
                    if (!checkNotOpaque3d(v)) // Not air, do face culling
                    {
                        var cull = 0; // All sides are hidden at start

                        if (z == FZ - 1 || checkNotOpaque3d(state[x + y * FX + (z + 1) * FX * FY])) // Unity +Y (Up)    | Markov +Z
                            cull |= (1 << 0);
                        
                        if (z ==      0 || checkNotOpaque3d(state[x + y * FX + (z - 1) * FX * FY])) // Unity -Y (Down)  | Markov -Z
                            cull |= (1 << 1);

                        if (x == FX - 1 || checkNotOpaque3d(state[(x + 1) + y * FX + z * FX * FY])) // Unity +X (South) | Markov +X
                            cull |= (1 << 2);
                        
                        if (x ==      0 || checkNotOpaque3d(state[(x - 1) + y * FX + z * FX * FY])) // Unity -X (North) | Markov -X
                            cull |= (1 << 3);
                        
                        if (y == FY - 1 || checkNotOpaque3d(state[x + (y + 1) * FX + z * FX * FY])) // Unity +Z (East)  | Markov +Y
                            cull |= (1 << 4);
                        
                        if (y ==      0 || checkNotOpaque3d(state[x + (y - 1) * FX + z * FX * FY])) // Unity -Z (East)  | Markov +Y
                            cull |= (1 << 5);

                        if (cull != 0) // At least one side of this cube is visible
                            instanceData.Add(new(x + ox, z + oy, y + oz, palette[v]));
                        
                    }
                }
                else // 2d structure, all blocks should be shown even those with value 0. In other words, there's no air block
                {
                    // No cube can be totally occluded in 2d mode
                    instanceData.Add(new(x + ox, z + oy, y + oz, palette[v]));
                }
            }

            return instanceData.ToArray();
        }

        void Start()
        {
            // Start it up!
            StartCoroutine(RunTest());

        }

    }
}