#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MarkovCraft
{
    public static class NbtStructureExporter
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        // Unity     X  Y  Z
        // Markov    X  Z  Y
        // Minecraft Z  Y  X

        private static bool checkAir3d(byte index) => index == 0;

        public static void Export(string[] info, byte[] state, char[] legend, int FX, int FY, int FZ,
                Dictionary<char, CustomMappingItem> exportPalette, DirectoryInfo dirInfo, int dataVersionInt)
        {
            var nbtObj = new Dictionary<string, object>();
            int mcSizeX = FY, mcSizeY = FZ, mcSizeZ = FX;

            // Append size info
            nbtObj.Add("size", new object[] { mcSizeX, mcSizeY, mcSizeZ });
            // Append empty entity list
            nbtObj.Add("entities", new object[]{ });

            var statePalette = BlockStatePalette.INSTANCE;
            var structurePalette = new List<BlockState>();
            // Character => index in structure palette
            var structureRemap = new Dictionary<char, int>();

            foreach (var item in exportPalette)
            {
                var stateStr = item.Value.BlockState;
                var stateId = BlockStateHelper.GetStateIdFromString(stateStr);
                if (stateId == BlockStateHelper.INVALID_BLOCKSTATE)
                    stateId = 0; // Replace invalid blockstates with air
                var blockState = statePalette.StatesTable[stateId];

                if (structurePalette.Contains(blockState))
                    structureRemap.Add(item.Key, structurePalette.FindIndex(x => x == blockState));
                else // Blockstate not remapped yet, add it to the structure palette
                {
                    var idx = structurePalette.Count;
                    structureRemap.Add(item.Key, idx);
                    //Debug.Log($"[{idx}] => [{stateId}] {blockState}");
                    structurePalette.Add(blockState);
                }
            }

            // Append structure data
            var blocksData = new List<(int, int, int, int)>();
            for (int mcy = 0;mcy < mcSizeY;mcy++) for (int mcx = 0;mcx < mcSizeX;mcx++) for (int mcz = 0;mcz < mcSizeZ;mcz++)
            {
                byte v = state[mcz + mcx * mcSizeZ + mcy * mcSizeZ * mcSizeX];
                char ch = legend[v];

                blocksData.Add((mcx, mcy, mcz, structureRemap[ch]));
            }
            // - blocks field
            nbtObj.Add("blocks", blocksData.Select(x => new Dictionary<string, object>() {
                        ["pos"] = new object[] { x.Item1, x.Item2, x.Item3 },
                        ["state"] = (object) x.Item4
                    }).ToArray() );
            // - palette field
            nbtObj.Add("palette", structurePalette.Select(x => {
                        var stateAsDict = new Dictionary<string, object>();
                        if (x.Properties.Count > 0)
                            stateAsDict.Add("Properties", x.Properties.ToDictionary(y => y.Key, y => (object) y.Value));
                        stateAsDict.Add("Name", x.BlockId.ToString());
                        return stateAsDict;
                    }).ToArray());
            
            // Append data version
            nbtObj.Add("DataVersion", dataVersionInt);

            // Turn dictionary object into byte array
            var nbtBlob = DataHelper.GetNbt(nbtObj);

            var fileName = $"{info[0][0..^4].ToLower()}_{info[1]}.nbt";
            var filePath = $"{dirInfo.FullName}{SP}{fileName}";

            // Compress nbt blob and save it
            using (var compressedStream = File.Open(filePath, FileMode.Create))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    using (var blobStream = new MemoryStream(nbtBlob))
                    {
                        blobStream.CopyTo(zipStream);
                    }

            Debug.Log($"Nbt structure file exported to {filePath}");

        }
    }
}