#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MarkovCraft
{
    // See https://github.com/SpongePowered/Schematic-Specification
    // for specification of this structure format
    public static class SpongeSchemExporter
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        // Unity     X  Y  Z
        // Markov    X  Z  Y
        // Minecraft Z  Y  X

        private static bool checkAir3d(byte index) => index == 0;

        public static void Export(byte[] state, char[] legend, int FX, int FY, int FZ,
                Dictionary<char, CustomMappingItem> exportPalette, DirectoryInfo dirInfo, string fileName, HashSet<char> minimumCharSet, int dataVersionInt)
        {
            var schemObj = new Dictionary<string, object>(); // Root object

            // Sponge Schematic v2 (v3 is published already but is not yet supported by map editors like Amulet)
            schemObj.Add("Version", 2);
            // Minecraft data version
            schemObj.Add("DataVersion", dataVersionInt);

            short width = (short) FY, height = (short) FZ, length = (short) FX;
            // Append size info
            schemObj.Add("Width",  width );
            schemObj.Add("Height", height);
            schemObj.Add("Length", length);

            // Append empty entity list
            schemObj.Add("Entities", new object[]{ });

            var statePalette = BlockStatePalette.INSTANCE;
            var structurePalette = new List<BlockState>();
            // Character => index in structure palette
            var structureRemap = new Dictionary<char, int>();

            foreach (var item in exportPalette)
            {
                if (!minimumCharSet.Contains(item.Key))
                {
                    // Export palette may still contain a few unused entries, filter them out
                    continue;
                }

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
                    //Debug.Log($"[{idx}] {item.Key} => [{stateId}] {blockState}");
                    structurePalette.Add(blockState);
                }
            }

            // Append structure data
            var blocksData = new List<byte>();
            // Varint count of block data array
            blocksData.AddRange(DataHelper.GetInt(height * width * length));
            // Sponge schem uses Y-Z-X order
            for (int mcy = 0;mcy < height;mcy++) for (int mcz = 0;mcz < length;mcz++) for (int mcx = 0;mcx < width;mcx++)
            {
                byte v = state[mcz + mcx * length + mcy * length * width];
                int remappedId = structureRemap[legend[v]];
                // Store it as varint in byte array
                while ((remappedId & -128) != 0) {
                    blocksData.Add((byte) (remappedId & 127 | 128));
                    // Do an UNSIGNED right shift
                    remappedId = (int)((uint)remappedId >> 7);
                }
                // Should be less than 0b 1000 0000 now, safe to cast to byte
                blocksData.Add((byte) remappedId);
            }

            int paletteMax = structurePalette.Count;
            var schemPalette = new Dictionary<string, object>(paletteMax);

            for (int i = 0;i < paletteMax;i++)
            {
                schemPalette[structurePalette[i].ToString()] = i;
            }

            // - palette max field
            schemObj.Add("PaletteMax", paletteMax);
            // - palette field
            schemObj.Add("Palette", schemPalette);
            // - block data field
            schemObj.Add("BlockData", blocksData.ToArray());
            
            // Turn dictionary object into byte array
            var nbtBlob = DataHelper.GetNbt(schemObj);
            var filePath = $"{dirInfo.FullName}{SP}{fileName}";

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