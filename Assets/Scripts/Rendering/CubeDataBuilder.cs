#nullable enable
using System.Collections.Generic;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public static class CubeDataBuilder
    {
        private static bool checkNotOpaque3d(byte block) => block == 0;

        public static int4[] GetInstanceData(byte[] states, int FX, int FY, int FZ, int ox, int oy, int oz, bool is3d, int[] palette)
        {
            List<int4> instanceData = new();

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
    }
}
