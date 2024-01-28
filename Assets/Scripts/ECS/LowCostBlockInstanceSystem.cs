using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;

namespace MarkovCraft
{
    public partial struct LowCostBlockInstanceSystem : ISystem
    {
        private static readonly float4 WHITE = new(1F);

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LowCostBlockInstanceComponent>();
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // An EntityCommandBuffer created from an EntityCommandBufferSystem singleton will be
            // played back and disposed by the EntityCommandBufferSystem the next time it updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp, mmm, ccc, entity) in
                    SystemAPI.Query<
                        RefRW<LowCostBlockInstanceComponent>,
                        RefRW<MaterialMeshInfo>,
                        RefRW<InstanceBlockColorComponent>
                    >().WithEntityAccess())
            {
                comp.ValueRW.Timer += SystemAPI.Time.DeltaTime; // Time left increases

                // mesh index, material index, color
                int3 mesh = new( ((int) SystemAPI.Time.ElapsedTime) % 20, 0, 0xFFFFFF);

                ecb.SetComponent(entity, MaterialMeshInfo.FromRenderMeshArrayIndices(mesh.y, mesh.x));

                // Update mesh
                if (mmm.ValueRO.Mesh != -mesh.x - 1)
                {
                    mmm.ValueRW.Mesh = -mesh.x - 1;
                    mmm.ValueRW.Material = -mesh.y - 1;

                    //ecb.SetComponent(entity, MaterialMeshInfo.FromRenderMeshArrayIndices(mesh.y, mesh.x));

                    ccc.ValueRW.Value = mesh.x == 0 ? ComputeColor(mesh.z) : WHITE;
                }

                float lt = comp.ValueRO.LifeTime;

                if (lt <= 0F) // Persistent entities
                    continue;

                if (comp.ValueRO.Timer > lt + 0.1F) // Entity expired, destroy them a bit later here to prevent glitches
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
            }
        }

        public static float4 ComputeColor(int rgb)
        {
            return new(((rgb & 0xFF0000) >> 16) / 255F, ((rgb & 0xFF00) >> 8) / 255F, (rgb & 0xFF) / 255F, 1F);
        }
    }
}
