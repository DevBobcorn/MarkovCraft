#nullable enable
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarkovCraft
{
    public static class BlockInstanceSpawner
    {
        // Regular block population - persistent
        public static void VisualizePersistentState((int3[], int3[]) instanceDataRaw, Material[] materials, Mesh[] meshes)
        {
            VisualizeFrameState(instanceDataRaw, materials, meshes, 0F);
        }

        // Regular block population - one frame
        public static void VisualizeFrameState((int3[], int3[]) instanceDataRaw, Material[] materials, Mesh[] meshes, float lifeTime)
        {
            var entityCount = instanceDataRaw.Item1.Length;

            var posData = new NativeArray<int3>(entityCount, Allocator.TempJob);
            posData.CopyFrom(instanceDataRaw.Item1);
            var meshData = new NativeArray<int3>(entityCount, Allocator.TempJob);
            meshData.CopyFrom(instanceDataRaw.Item2);

            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var ecbJob = new EntityCommandBuffer(Allocator.TempJob);
            
            #region Prepare entity prototype
            var filterSettings = RenderFilterSettings.Default;

            var renderMeshArray = new RenderMeshArray(materials, meshes);
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
            
            entityManager.AddComponentData(prototype, new InstanceBlockColorComponent());
            entityManager.AddComponentData(prototype, new RegularBlockInstanceComponent());
            
            #endregion

            // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
            // which can be either a Prefab or an entity created at run time like in this sample.
            // This is the fastest and most efficient way to create entities at run time.
            var spawnJob = new RegularSpawnJob
            {
                Ecb = ecbJob.AsParallelWriter(),
                Prototype = prototype,
                PositionData = posData,
                MeshData = meshData,
                LifeTime = lifeTime
            };

            var spawnHandle = spawnJob.Schedule(entityCount, 128);
            spawnHandle.Complete();

            ecbJob.Playback(entityManager);
            ecbJob.Dispose();

            posData.Dispose(spawnHandle);
            meshData.Dispose(spawnHandle);

            entityManager.DestroyEntity(prototype);
        }

        // Optimized block population - instanced life time
        public static void VisualizeState((int3[], int3[], float[]) instanceDataRaw, Material[] materials, Mesh[] meshes)
        {
            var entityCount = instanceDataRaw.Item1.Length;

            var posData = new NativeArray<int3>(entityCount, Allocator.TempJob);
            posData.CopyFrom(instanceDataRaw.Item1);
            var meshData = new NativeArray<int3>(entityCount, Allocator.TempJob);
            meshData.CopyFrom(instanceDataRaw.Item2);
            var lifeTime = new NativeArray<float>(entityCount, Allocator.TempJob);
            lifeTime.CopyFrom(instanceDataRaw.Item3);

            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var ecbJob = new EntityCommandBuffer(Allocator.TempJob);
            
            #region Prepare entity prototype
            var filterSettings = RenderFilterSettings.Default;

            var renderMeshArray = new RenderMeshArray(materials, meshes);
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
            
            entityManager.AddComponentData(prototype, new InstanceBlockColorComponent());
            entityManager.AddComponentData(prototype, new OptimizedBlockInstanceComponent());
            
            #endregion

            // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
            // which can be either a Prefab or an entity created at run time like in this sample.
            // This is the fastest and most efficient way to create entities at run time.
            var spawnJob = new OptimizedSpawnJob
            {
                Ecb = ecbJob.AsParallelWriter(),
                Prototype = prototype,
                PositionData = posData,
                MeshData = meshData,
                LifeTime = lifeTime
            };

            var spawnHandle = spawnJob.Schedule(entityCount, 128);
            spawnHandle.Complete();

            ecbJob.Playback(entityManager);
            ecbJob.Dispose();

            posData.Dispose(spawnHandle);
            meshData.Dispose(spawnHandle);
            lifeTime.Dispose(spawnHandle);

            entityManager.DestroyEntity(prototype);
        }

        private static Entity gridStorageEntity = Entity.Null;

        // LowCost block population - initialize grid
        public static void InitializeGrid(int3 pos, int3 size, Material[] materials, Mesh[] meshes, int3[] initData)
        {
            var entityCount = size.x * size.y * size.z; // Unity coordinates
            var posArray = new int3[entityCount];

            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    for (int z = 0; z < size.z; z++)
                    {
                        // In the index we use Markov coordinates, so that we can pass arrays
                        // directly into the buffer without having to convert them.
                        // While on the right-hand side we use Unity coordinates for rendering.
                        posArray[x + y * size.x + z * size.x * size.y] = new int3(x, z, y) + pos;
                    }

            var posData = new NativeArray<int3>(entityCount, Allocator.TempJob);
            posData.CopyFrom(posArray);

            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var ecbJob = new EntityCommandBuffer(Allocator.TempJob);
            
            #region Prepare entity prototype
            var filterSettings = RenderFilterSettings.Default;

            var renderMeshArray = new RenderMeshArray(materials, meshes);
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
            
            entityManager.AddComponentData(prototype, new InstanceBlockColorComponent());
            entityManager.AddComponentData(prototype, new LowCostBlockInstanceComponent());

            // Create data storage
            gridStorageEntity = entityManager.CreateEntity();

            var gridDataBuffer = entityManager.AddBuffer<BlockMeshBufferElement>(gridStorageEntity);
            gridDataBuffer.Length = entityCount;

            // Reinterpret the buffer for later convenience
            DynamicBuffer<int3> bufferAsInt3s = gridDataBuffer.Reinterpret<int3>();
            bufferAsInt3s.CopyFrom(initData);

            Debug.Log($"Dynamic buffer created for grid. Size: {size.x}x{size.y}x{size.z} ({entityCount})");

            #endregion

            // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
            // which can be either a Prefab or an entity created at run time like in this sample.
            // This is the fastest and most efficient way to create entities at run time.
            var spawnJob = new LowCostSpawnJob
            {
                Ecb = ecbJob.AsParallelWriter(),
                Prototype = prototype,
                PositionData = posData,
                // The last mesh in array is an empty mesh
                EmptyMeshIndex = meshes.Length - 1
            };

            var spawnHandle = spawnJob.Schedule(entityCount, 128);
            spawnHandle.Complete();

            ecbJob.Playback(entityManager);
            ecbJob.Dispose();

            posData.Dispose(spawnHandle);

            entityManager.DestroyEntity(prototype);
        }

        // LowCost block population - update grid
        public static void UpdateGrid(int3[] gridMeshData)
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var gridDataBuffer = entityManager.GetBuffer<BlockMeshBufferElement>(gridStorageEntity);

            DynamicBuffer<int3> bufferAsInt3s = gridDataBuffer.Reinterpret<int3>();
            bufferAsInt3s.CopyFrom(gridMeshData);
        }

        // LowCost block population - clear grid
        public static void ClearGrid()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var clearTagEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(clearTagEntity, new LowCostClearTagComponent());

            entityManager.DestroyEntity(gridStorageEntity);
            gridStorageEntity = Entity.Null;

            Debug.Log($"Dynamic buffer destroyed for grid.");
        }

        public static void ClearUpPersistentState()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            var clearTagEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(clearTagEntity, new ClearTagComponent());
        }
    }
}