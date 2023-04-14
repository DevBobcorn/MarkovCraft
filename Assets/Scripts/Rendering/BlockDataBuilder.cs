#nullable enable
using System.Collections.Generic;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public static class BlockDataBuilder
    {
        private static bool checkAir(byte block) => block == 0;

        public static (int3[], int2[]) GetInstanceData(byte[] states, int FX, int FY, int FZ, int3 pos, int2[] palette)
        {
            List<int3> posData = new();
            List<int2> meshData = new();

            for (int z = 0; z < FZ; z++) for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
            {
                byte v = states[x + y * FX + z * FX * FY];
                
                if (!checkAir(v)) // Not air, do face culling
                {
                    var notCulled = false;

                    if      (z == FZ - 1 || checkAir(states[x + y * FX + (z + 1) * FX * FY])) // Unity +Y (Up)    | Markov +Z
                        notCulled = true;
                    else if (z ==      0 || checkAir(states[x + y * FX + (z - 1) * FX * FY])) // Unity -Y (Down)  | Markov -Z
                        notCulled = true;
                    else if (x == FX - 1 || checkAir(states[(x + 1) + y * FX + z * FX * FY])) // Unity +X (South) | Markov +X
                        notCulled = true;
                    else if (x ==      0 || checkAir(states[(x - 1) + y * FX + z * FX * FY])) // Unity -X (North) | Markov -X
                        notCulled = true;
                    else if (y == FY - 1 || checkAir(states[x + (y + 1) * FX + z * FX * FY])) // Unity +Z (East)  | Markov +Y
                        notCulled = true;
                    else if (y ==      0 || checkAir(states[x + (y - 1) * FX + z * FX * FY])) // Unity -Z (East)  | Markov +Y
                        notCulled = true;

                    if (notCulled) // At least one side of this cube is visible
                    {
                        posData.Add(new int3(x, z, y) + pos);
                        meshData.Add(palette[v]);
                    }
                    
                }
            }

            return (posData.ToArray(), meshData.ToArray());
        }

    }
}
