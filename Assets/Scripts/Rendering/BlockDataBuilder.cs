#nullable enable
using System.Collections.Generic;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public static class BlockDataBuilder
    {
        private static bool checkNotOpaque3d(byte block) => block == 0;

        public static (int3[], int2[]) GetInstanceData(byte[] states, int FX, int FY, int FZ, int3 pos, bool is3d, int2[] palette)
        {
            List<int3> posData = new();
            List<int2> meshData = new();

            for (int z = 0; z < FZ; z++) for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
            {
                byte v = states[x + y * FX + z * FX * FY];
                
                if (is3d) // 3d structure
                {
                    if (!checkNotOpaque3d(v)) // Not air, do face culling
                    {
                        var cull = 0; // All sides are hidden at start

                        if (z == FZ - 1 || checkNotOpaque3d(states[x + y * FX + (z + 1) * FX * FY])) // Unity +Y (Up)    | Markov +Z
                            cull |= (1 << 0);
                        
                        if (z ==      0 || checkNotOpaque3d(states[x + y * FX + (z - 1) * FX * FY])) // Unity -Y (Down)  | Markov -Z
                            cull |= (1 << 1);

                        if (x == FX - 1 || checkNotOpaque3d(states[(x + 1) + y * FX + z * FX * FY])) // Unity +X (South) | Markov +X
                            cull |= (1 << 2);
                        
                        if (x ==      0 || checkNotOpaque3d(states[(x - 1) + y * FX + z * FX * FY])) // Unity -X (North) | Markov -X
                            cull |= (1 << 3);
                        
                        if (y == FY - 1 || checkNotOpaque3d(states[x + (y + 1) * FX + z * FX * FY])) // Unity +Z (East)  | Markov +Y
                            cull |= (1 << 4);
                        
                        if (y ==      0 || checkNotOpaque3d(states[x + (y - 1) * FX + z * FX * FY])) // Unity -Z (East)  | Markov +Y
                            cull |= (1 << 5);

                        if (cull != 0) // At least one side of this cube is visible
                        {
                            posData.Add(new int3(x, z, y) + pos);
                            meshData.Add(palette[v]);
                        }
                        
                    }
                }
                else // 2d structure, all blocks should be shown even those with value 0. In other words, there's no air block
                {
                    // No cube can be totally occluded in 2d mode
                    posData.Add(new int3(x, z, y) + pos);
                    meshData.Add(palette[v]);
                }
            }

            return (posData.ToArray(), meshData.ToArray());
        }

        public static int4[] GetInstanceData(int[] colors, int FX, int FY, int ox, int oy, int oz)
        {
            List<int4> instanceData = new();

            for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
            {
                int v = colors[(FX - 1 - x) + y * FX];
                
                instanceData.Add(new(x + ox, 0, y + oz, v));
            }

            return instanceData.ToArray();
        }
    
        public static VertexBuffer GetChunkMeshData(byte[] states, ref float proc, int FX, int FY, int FZ, int3 pos, bool is3d, int2[] palette, BlockGeometry?[] blockGeometries, float3[] blockTints)
        {
            VertexBuffer result = new();

            for (int z = 0; z < FZ; z++)
            {
                proc = z / (float)FZ;

                for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
                {
                    byte v = states[x + y * FX + z * FX * FY];
                    
                    if (is3d) // 3d structure
                    {
                        if (!checkNotOpaque3d(v)) // Not air, do face culling
                        {
                            var cull = 0; // All sides are hidden at start

                            if (z == FZ - 1 || checkNotOpaque3d(states[x + y * FX + (z + 1) * FX * FY])) // Unity +Y (Up)    | Markov +Z
                                cull |= (1 << 0);
                            
                            if (z ==      0 || checkNotOpaque3d(states[x + y * FX + (z - 1) * FX * FY])) // Unity -Y (Down)  | Markov -Z
                                cull |= (1 << 1);

                            if (x == FX - 1 || checkNotOpaque3d(states[(x + 1) + y * FX + z * FX * FY])) // Unity +X (South) | Markov +X
                                cull |= (1 << 2);
                            
                            if (x ==      0 || checkNotOpaque3d(states[(x - 1) + y * FX + z * FX * FY])) // Unity -X (North) | Markov -X
                                cull |= (1 << 3);
                            
                            if (y == FY - 1 || checkNotOpaque3d(states[x + (y + 1) * FX + z * FX * FY])) // Unity +Z (East)  | Markov +Y
                                cull |= (1 << 4);
                            
                            if (y ==      0 || checkNotOpaque3d(states[x + (y - 1) * FX + z * FX * FY])) // Unity -Z (East)  | Markov +Y
                                cull |= (1 << 5);

                            if (cull != 0) // At least one side of this cube is visible
                            {
                                var geometry = blockGeometries[palette[v].x];

                                if (geometry is not null)
                                    geometry.Build(ref result, pos + new float3(x, z, y), cull, blockTints[palette[v].x]);
                                else
                                {
                                    var color = palette[v].y;

                                    CubeGeometry.Build(ref result, AtlasManager.HAKU, pos.x + x, pos.y + z, pos.z + y, cull,
                                            new float3( ((color & 0xFF0000) >> 16) / 255F, ((color & 0xFF00) >> 8) / 255F, (color & 0xFF) / 255F ));
                                            //new float3( 1F, 0F, 0F ));
                                }
                            }
                            
                        }
                    }
                    else // 2d structure, all blocks should be shown even those with value 0. In other words, there's no air block
                    {
                        //

                    }
                }
            }

            proc = 1F;

            return result;
        }
    }
}
