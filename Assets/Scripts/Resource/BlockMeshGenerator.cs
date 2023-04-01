#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public static class BlockMeshGenerator
    {
        private static readonly Bounds cubeBounds = new Bounds(new(0.5F, 0.5F, 0.5F), new(1F, 1F, 1F));

        public static Mesh[] GenerateMeshes(VertexBuffer[] buffers)
        {
            var meshDataArr = Mesh.AllocateWritableMeshData(buffers.Length);

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);

            var resultMeshes = new Mesh[buffers.Length];

            for (int mi = 0;mi < buffers.Length;mi++)
            {
                var meshData = meshDataArr[mi];
                var buffer = buffers[mi];

                int vertexCount = buffer.vert.Length;
                int triIdxCount = (vertexCount / 2) * 3;
                
                // Set mesh params
                meshData.SetVertexBufferParams(vertexCount, vertAttrs);
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

                // Create mesh
                resultMeshes[mi] = new Mesh { bounds = cubeBounds };
            }

            vertAttrs.Dispose();
            
            // Apply mesh data
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, resultMeshes);

            for (int mi = 0;mi < buffers.Length;mi++)
                resultMeshes[mi].RecalculateNormals();
            
            return resultMeshes;
        }
    }
}
