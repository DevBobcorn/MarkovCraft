using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace MarkovCraft
{
    public static class RecordingExporter
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        // Unity     X  Y  Z
        // Markov    X  Z  Y
        // Minecraft Z  Y  X

        public static IEnumerator SaveRecording(Dictionary<char, CustomMappingItem> fullPalette, string recordingName,
                int sizeX, int sizeY, int sizeZ, GenerationFrameRecord[] recordedFrames)
        {
            Debug.Log($"Exporting generation process with {recordedFrames.Length} frames");
            yield return null;

            // Character => new palette index
            Dictionary<char, int> charMap = new();
            var stateMap = new CustomMappingItem[fullPalette.Count];
            var recPalette = new ColoredBlockStateInfo[fullPalette.Count];

            int index = 0;
            foreach (var item in fullPalette)
            {
                charMap.Add(item.Key, index);
                stateMap[index] = item.Value;
                recPalette[index] = new(ColorConvert.GetHexRGBString(item.Value.Color),
                        item.Value.BlockState);
                index++;
            }

            // Export frames
            var simulationBox = new int[sizeX * sizeY * sizeZ];

            // In 3d mode, 0 is the index of air block whereas 2d mode has no air block
            int initValue = (sizeZ == 1) ? -1 : 0;
            Array.Fill(simulationBox, initValue);

            List<string> frameData = new();
            // Simulate the whole thing frame by frame
            for (int frameIdx = 0;frameIdx < recordedFrames.Length;frameIdx++)
            {
                List<int> blockChanges = new();

                var frame = recordedFrames[frameIdx];
                int fX = frame.Size.x, fY = frame.Size.y, fZ = frame.Size.z;
                var frameStates = frame.States;

                for (byte z = 0; z < fZ; z++) for (byte y = 0; y < fY; y++) for (byte x = 0; x < fX; x++)
                {
                    int framePos = x + y * fX + z * fX * fY;
                    int boxPos = x + y * sizeX + z * sizeX * sizeY;
                    int paletteIndex = charMap[frameStates[framePos]];

                    if (simulationBox[boxPos] != paletteIndex) // The block is changed in this frame
                    {
                        simulationBox[boxPos] = paletteIndex;

                        // Register the block change
                        blockChanges.AddRange(new int[]{ x, y, z, paletteIndex });
                    }
                }

                frameData.Add(string.Join(" ", blockChanges));
                Debug.Log($"Exporting frame #{frameIdx} changes: [{blockChanges.Count * 4}]");
            }

            RecordingData recData = new(recPalette.ToList(), sizeX, sizeY, sizeZ, frameData);

            string jsonText = JsonConvert.SerializeObject(recData);
            var folderName = PathHelper.GetRecordingFile(string.Empty);

            if (!Directory.Exists(folderName)) // Create folder if not present
            {
                Directory.CreateDirectory(folderName);
            }

            File.WriteAllText(PathHelper.GetRecordingFile($"{recordingName}.json"), jsonText);
        }
    }
}