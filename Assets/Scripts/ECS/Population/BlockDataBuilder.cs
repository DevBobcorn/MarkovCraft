#nullable enable
using System.Collections.Generic;
using Unity.Mathematics;

namespace MarkovCraft
{
    public static class BlockDataBuilder
    {
        private static bool CheckAir3d(byte index) => index == 0;

        public static (int3[], int3[]) GetInstanceData(byte[] state, int FX, int FY, int FZ, int3 pos, int3[] meshPalette)
        {
            List<int3> posData = new();
            List<int3> meshData = new();

            for (int z = 0; z < FZ; z++) for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
            {
                byte v = state[x + y * FX + z * FX * FY];
                
                if (FZ == 1) // 2d mode, byte 0 is not air
                {
                    posData.Add(new int3(x, z, y) + pos);
                    meshData.Add(meshPalette[v]);
                }
                else if (!CheckAir3d(v)) // 3d mode, byte 0 is air
                {
                    var notCulled = false;

                    if      (z == FZ - 1 || CheckAir3d(state[x + y * FX + (z + 1) * FX * FY])) // Unity +Y (Up)    | Markov +Z
                        notCulled = true;
                    else if (z ==      0 || CheckAir3d(state[x + y * FX + (z - 1) * FX * FY])) // Unity -Y (Down)  | Markov -Z
                        notCulled = true;
                    else if (x == FX - 1 || CheckAir3d(state[(x + 1) + y * FX + z * FX * FY])) // Unity +X (South) | Markov +X
                        notCulled = true;
                    else if (x ==      0 || CheckAir3d(state[(x - 1) + y * FX + z * FX * FY])) // Unity -X (North) | Markov -X
                        notCulled = true;
                    else if (y == FY - 1 || CheckAir3d(state[x + (y + 1) * FX + z * FX * FY])) // Unity +Z (East)  | Markov +Y
                        notCulled = true;
                    else if (y ==      0 || CheckAir3d(state[x + (y - 1) * FX + z * FX * FY])) // Unity -Z (East)  | Markov +Y
                        notCulled = true;

                    if (notCulled) // At least one side of this cube is visible
                    {
                        posData.Add(new int3(x, z, y) + pos);
                        meshData.Add(meshPalette[v]);
                    }
                }
            }

            return (posData.ToArray(), meshData.ToArray());
        }

        public static float3[] GetColliderData(int[] blockData, HashSet<int> airIndices, int FX, int FY, int FZ)
        {
            List<float3> posData = new();

            bool checkAirCustom(int blockIndex) => airIndices.Contains(blockIndex);

            for (int z = 0; z < FZ; z++) for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
            {
                int v = blockData[x + y * FX + z * FX * FY];
                
                if (FZ == 1) // 2d mode
                {
                    if (!checkAirCustom(v)) // Itself is not air
                    {
                        posData.Add(new float3(x, z, y));
                    }
                }
                else // 3d mode, do culling
                {
                    if (!checkAirCustom(v)) // Itself is not air
                    {
                        var notCulled = false;

                        if      (z == FZ - 1 || checkAirCustom(blockData[x + y * FX + (z + 1) * FX * FY])) // Unity +Y (Up)    | Markov +Z
                            notCulled = true;
                        else if (z ==      0 || checkAirCustom(blockData[x + y * FX + (z - 1) * FX * FY])) // Unity -Y (Down)  | Markov -Z
                            notCulled = true;
                        else if (x == FX - 1 || checkAirCustom(blockData[(x + 1) + y * FX + z * FX * FY])) // Unity +X (South) | Markov +X
                            notCulled = true;
                        else if (x ==      0 || checkAirCustom(blockData[(x - 1) + y * FX + z * FX * FY])) // Unity -X (North) | Markov -X
                            notCulled = true;
                        else if (y == FY - 1 || checkAirCustom(blockData[x + (y + 1) * FX + z * FX * FY])) // Unity +Z (East)  | Markov +Y
                            notCulled = true;
                        else if (y ==      0 || checkAirCustom(blockData[x + (y - 1) * FX + z * FX * FY])) // Unity -Z (East)  | Markov +Y
                            notCulled = true;

                        if (notCulled) // At least one side of this cube is visible
                        {
                            posData.Add(new float3(x, z, y));
                        }
                    }
                }
            }

            return posData.ToArray();
        }
    }
}
