#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MarkovCraft
{
    public static class McFuncExporter
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        // Unity     X  Y  Z
        // Markov    X  Z  Y
        // Minecraft Z  Y  X

        private static bool checkAir3d(byte index) => index == 0;

        public static void Export(string[] info, byte[] state, char[] legend, int FX, int FY, int FZ,
                Dictionary<char, CustomMappingItem> exportPalette, DirectoryInfo dirInfo)
        {
            var funcText = new StringBuilder();

            for (int z = 0; z < FZ; z++) for (int y = 0; y < FY; y++) for (int x = 0; x < FX; x++)
            {
                byte v = state[x + y * FX + z * FX * FY];
                char ch = legend[v];
                
                if (FZ == 1) // 2d mode, byte 0 is not air
                {
                    //posData.Add(new int3(x, z, y) + pos);
                    //meshData.Add(palette[v]);
                    funcText.AppendLine($"setblock ~{(y == 0 ? null : y)} ~{(z == 0 ? null : z)} ~{(x == 0 ? null : x)} {exportPalette[ch].BlockState}");
                }
                else if (!checkAir3d(v)) // 3d mode, byte 0 is air
                {
                    //posData.Add(new int3(x, z, y) + pos);
                    //meshData.Add(palette[v]);
                    funcText.AppendLine($"setblock ~{(y == 0 ? null : y)} ~{(z == 0 ? null : z)} ~{(x == 0 ? null : x)} {exportPalette[ch].BlockState}");
                }
            }

            var fileName = $"{info[0][0..^4].ToLower()}_{info[1]}.mcfunction";
            var filePath = $"{dirInfo.FullName}{SP}{fileName}";
            
            File.WriteAllText(filePath, funcText.ToString());

            Debug.Log($"McFunction file exported to {filePath}");

        }
    }
}