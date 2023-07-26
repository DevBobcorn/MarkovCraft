#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

using MarkovJunior;

namespace MarkovCraft
{
    public static class VoxModelExporter
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
            var filePath = $"{dirInfo.FullName}{SP}{fileName}";

            var voxPalette = legend.Select(ch => ColorConvert.GetRGB(exportPalette[ch].Color)).ToArray();
            VoxHelper.SaveVox(state, (byte) FX, (byte) FY, (byte) FZ, FZ != 1, voxPalette, filePath);

            Debug.Log($"Vox model file exported to {filePath}");

        }
    }
}