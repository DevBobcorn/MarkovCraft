#nullable enable
using System.IO;
using System.Text;
using UnityEngine;

namespace MarkovCraft
{
    public static class McFuncExporter
    {
        public static void Export(int SizeX, int SizeY, int SizeZ, CustomMappingItem[] resultPalette,
                int[] blockData, string filePath)
        {
            var funcText = new StringBuilder();
            int mcSizeX = SizeZ, mcSizeY = SizeY, mcSizeZ = SizeX;

            for (int mcy = 0; mcy < mcSizeY; mcy++) for (int mcx = 0; mcx < mcSizeX; mcx++) for (int mcz = 0; mcz < mcSizeZ; mcz++)
            {
                int resultIndex = blockData[mcz + mcx * mcSizeZ + mcy * mcSizeZ * mcSizeX];
                var blockState = resultPalette[resultIndex].BlockState;
                
                funcText.AppendLine($"setblock ~{(mcx == 0 ? null : mcx)} ~{(mcy == 0 ? null : mcy)} ~{(mcz == 0 ? null : mcz)} {blockState}");
            }
            
            File.WriteAllText(filePath, funcText.ToString());
            Debug.Log($"McFunction file exported to {filePath}");
        }
    }
}