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

        public static void Export(byte[] state, char[] legend, int FX, int FY, int FZ,
                Dictionary<char, CustomMappingItem> exportPalette, DirectoryInfo dirInfo, string fileName)
        {
            var funcText = new StringBuilder();
            int mcSizeX = FY, mcSizeY = FZ, mcSizeZ = FX;

            for (int mcy = 0; mcy < mcSizeY; mcy++) for (int mcx = 0; mcx < mcSizeX; mcx++) for (int mcz = 0; mcz < mcSizeZ; mcz++)
            {
                byte v = state[mcz + mcx * mcSizeZ + mcy * mcSizeZ * mcSizeX];
                char ch = legend[v];
                
                if (FZ == 1) // 2d mode, byte 0 is not air
                {
                    //posData.Add(new int3(x, z, y) + pos);
                    //meshData.Add(palette[v]);
                    funcText.AppendLine($"setblock ~{(mcx == 0 ? null : mcx)} ~{(mcy == 0 ? null : mcy)} ~{(mcz == 0 ? null : mcz)} {exportPalette[ch].BlockState}");
                }
                else if (!checkAir3d(v)) // 3d mode, byte 0 is air
                {
                    //posData.Add(new int3(x, z, y) + pos);
                    //meshData.Add(palette[v]);
                    funcText.AppendLine($"setblock ~{(mcx == 0 ? null : mcx)} ~{(mcy == 0 ? null : mcy)} ~{(mcz == 0 ? null : mcz)} {exportPalette[ch].BlockState}");
                }
            }

            var filePath = $"{dirInfo.FullName}{SP}{fileName}";
            
            File.WriteAllText(filePath, funcText.ToString());

            Debug.Log($"McFunction file exported to {filePath}");

        }
    }
}