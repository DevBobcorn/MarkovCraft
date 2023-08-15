#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

using CraftSharp;

namespace MarkovCraft
{
    // See https://github.com/SpongePowered/Schematic-Specification
    // for specification of this structure format
    public static class SpongeSchemExporter
    {
        public static void Export(int SizeX, int SizeY, int SizeZ, CustomMappingItem[] resultPalette,
                int[] blockData, string filePath, int dataVersionInt)
        {
            var schemObj = new Dictionary<string, object>
            {
                // Sponge Schematic v2 (v3 is published already but is not yet supported by map editors like Amulet)
                { "Version", 2 },
                // Minecraft data version
                { "DataVersion", dataVersionInt }
            }; // Root object

            short width = (short) SizeY, height = (short) SizeZ, length = (short) SizeX;
            // Append size info
            schemObj.Add("Width",  width );
            schemObj.Add("Height", height);
            schemObj.Add("Length", length);

            // Append empty entity list
            schemObj.Add("Entities", new object[]{ });

            var statePalette = BlockStatePalette.INSTANCE;
            var exportPalette = new List<BlockState>();
            // Character => index in structure palette
            var resultIndex2exportIndex = new Dictionary<int, int>();
            for (int resultIndex = 0;resultIndex < resultPalette.Length;resultIndex++)
            {
                var stateStr = resultPalette[resultIndex].BlockState;
                var stateId = BlockStateHelper.GetStateIdFromString(stateStr);
                if (stateId == BlockStateHelper.INVALID_BLOCKSTATE)
                    stateId = 0; // Replace invalid blockstates with air
                var blockState = statePalette.StatesTable[stateId];

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
            var blocksData = new List<byte>();
            // Varint count of block data array
            blocksData.AddRange(DataHelper.GetInt(height * width * length));
            // Sponge schem uses Y-Z-X order
            for (int mcy = 0;mcy < height;mcy++) for (int mcz = 0;mcz < length;mcz++) for (int mcx = 0;mcx < width;mcx++)
            {
                int resultIndex = blockData[mcz + mcx * length + mcy * length * width];
                int exportIndex = resultIndex2exportIndex[resultIndex];
                
                // Store it as varint in byte array
                while ((exportIndex & -128) != 0) {
                    blocksData.Add((byte) (exportIndex & 127 | 128));
                    // Do an UNSIGNED right shift
                    exportIndex = (int)((uint)exportIndex >> 7);
                }
                // Should be less than 0b 1000 0000 now, safe to cast to byte
                blocksData.Add((byte) exportIndex);
            }

            int paletteMax = exportPalette.Count;
            var schemPalette = new Dictionary<string, object>(paletteMax);

            for (int i = 0;i < paletteMax;i++)
            {
                schemPalette[exportPalette[i].ToString()] = i;
            }

            // DEBUG - RESULT PALETTE
            var rp = string.Join(", ", resultPalette.Select((x, idx) => $"{idx} {x.BlockState} // {x.Color}"));
            Debug.Log($"Result Palette: {rp}");
            // DEBUG - EXPORT PALETTE
            var ep = string.Join(", ", exportPalette.Select((x, idx) => $"{idx} {x}"));
            Debug.Log($"Export Palette: {ep}");

            // - palette max field
            schemObj.Add("PaletteMax", paletteMax);
            // - palette field
            schemObj.Add("Palette", schemPalette);
            // - block data field
            schemObj.Add("BlockData", blocksData.ToArray());
            
            // Turn dictionary object into byte array
            var nbtBlob = DataHelper.GetNbt(schemObj);

            // Compress nbt blob and save it
            using (var compressedStream = File.Open(filePath, FileMode.Create))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    using (var blobStream = new MemoryStream(nbtBlob))
                    {
                        blobStream.CopyTo(zipStream);
                    }

            Debug.Log($"Sponge Schem structure file exported to {filePath} (Data Version {dataVersionInt})");
        }
    }
}