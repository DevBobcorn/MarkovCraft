#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using CraftSharp.Resource;

namespace MarkovCraft
{
    public static class BlockMeshGenerator
    {
        private static readonly Bounds cubeBounds = new Bounds(new(0.5F, 0.5F, 0.5F), new(1F, 1F, 1F));

        public static Mesh[] GenerateMeshes(VertexBuffer[] buffers)
        {
            var meshDataArr = Mesh.AllocateWritableMeshData(buffers.Length);

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            vertAttrs[3] = new(VertexAttribute.Color,     dimension: 3, stream: 3);

            var resultMeshes = new Mesh[buffers.Length];

            for (int mi = 0;mi < buffers.Length;mi++)
            {
                var meshData = meshDataArr[mi];
                var visualBuffer = buffers[mi];

                int vertexCount = visualBuffer.vert.Length;
                int triIdxCount = (vertexCount / 2) * 3;
                
                // Set mesh params
                meshData.SetVertexBufferParams(vertexCount, vertAttrs);
                meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

                // Set vertex data
                // Positions
                var positions = meshData.GetVertexData<float3>(0);
                positions.CopyFrom(visualBuffer.vert);
                // Tex Coordinates
                var texCoords = meshData.GetVertexData<float3>(1);
                texCoords.CopyFrom(visualBuffer.txuv);
                // Animation Info
                var animInfos = meshData.GetVertexData<float4>(2);
                animInfos.CopyFrom(visualBuffer.uvan);
                // Vertex colors
                var vertColors = meshData.GetVertexData<float3>(3);
                vertColors.CopyFrom(visualBuffer.tint);

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
