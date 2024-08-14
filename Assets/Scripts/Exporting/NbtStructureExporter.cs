#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

using CraftSharp;

namespace MarkovCraft
{
    public static class NbtStructureExporter
    {
        public static void Export(int SizeX, int SizeY, int SizeZ, CustomMappingItem[] resultPalette,
                int[] blockData, string filePath, int dataVersionInt)
        {
            var nbtObj = new Dictionary<string, object>();
            int mcSizeX = SizeY, mcSizeY = SizeZ, mcSizeZ = SizeX;

            // Append size info
            nbtObj.Add("size", new object[] { mcSizeX, mcSizeY, mcSizeZ });
            // Append empty entity list
            nbtObj.Add("entities", new object[]{ });

            var statePalette = BlockStatePalette.INSTANCE;
            var exportPalette = new List<BlockState>();
            // Character => index in structure palette
            var resultIndex2exportIndex = new Dictionary<int, int>();
            for (int resultIndex = 0;resultIndex < resultPalette.Length;resultIndex++)
            {
                var stateStr = resultPalette[resultIndex].BlockState;
                // Replace invalid blockstates with air
                int stateId = statePalette.GetStateIdFromString(stateStr, 0);
                var blockState = statePalette.GetByNumId(stateId);

                if (exportPalette.Contains(blockState))
                {
                    var exportIndex = exportPalette.FindIndex(x => x == blockState);
                    resultIndex2exportIndex.Add(resultIndex, exportIndex);
                }
                else // Blockstate not remapped yet, add it to the structure palette
                {
                    var exportIndex = exportPalette.Count;
                    resultIndex2exportIndex.Add(resultIndex, exportIndex);
                    //Debug.Log($"[{exportIndex}] {resultIndex} => [{stateId}] {blockState}");
                    exportPalette.Add(blockState);
                }
            }

            // Append structure data
            var blocksData = new List<(int, int, int, int)>();
            for (int mcy = 0;mcy < mcSizeY;mcy++) for (int mcx = 0;mcx < mcSizeX;mcx++) for (int mcz = 0;mcz < mcSizeZ;mcz++)
            {
                int resultIndex = blockData[mcz + mcx * mcSizeZ + mcy * mcSizeZ * mcSizeX];
                int exportIndex = resultIndex2exportIndex[resultIndex];

                blocksData.Add((mcx, mcy, mcz, exportIndex));
            }
            // - blocks field
            nbtObj.Add("blocks", blocksData.Select(x => new Dictionary<string, object>() {
                        ["pos"] = new object[] { x.Item1, x.Item2, x.Item3 },
                        ["state"] = (object) x.Item4
                    }).ToArray() );
            // - palette field
            nbtObj.Add("palette", exportPalette.Select(x => {
                        var stateAsDict = new Dictionary<string, object>();
                        if (x.Properties.Count > 0)
                            stateAsDict.Add("Properties", x.Properties.ToDictionary(y => y.Key, y => (object) y.Value));
                        stateAsDict.Add("Name", x.BlockId.ToString());
                        return stateAsDict;
                    }).ToArray());
            
            // Append data version
            nbtObj.Add("DataVersion", dataVersionInt);

            // Turn dictionary object into byte array
            var nbtBlob = NBTDataHelper.GetNbt(nbtObj);

            // Compress nbt blob and save it
            using (var compressedStream = File.Open(filePath, FileMode.Create))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    using (var blobStream = new MemoryStream(nbtBlob))
                    {
                        blobStream.CopyTo(zipStream);
                    }

            Debug.Log($"Nbt structure file exported to {filePath} (Data Version {dataVersionInt})");
        }
    }
}