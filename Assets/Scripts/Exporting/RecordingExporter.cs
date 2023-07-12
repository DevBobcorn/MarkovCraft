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

        public static IEnumerator SaveRecording(Dictionary<char, CustomMappingItem> recPalette, string recordingName,
                int maxX, int maxY, int maxZ, GenerationFrameRecord[] recordedFrames)
        {
            Debug.Log($"Exporting generation process with {recordedFrames.Length} frames");
            yield return null;

            //Dictionary<string, object> exportData = new();
            // Character => new palette index
            Dictionary<char, int> charMap = new();
            var stateMap = new CustomMappingItem[recPalette.Count];

            int index = 0;
            foreach (var item in recPalette)
            {
                charMap.Add(item.Key, index);
                stateMap[index] = item.Value;
                index++;
            }

            // First export the current palette
            //exportData.Add("palette", recPalette.ToDictionary(x => charMap[x.Key].ToString(), x => (object) x.Value));
            //exportData.Add("size_x", maxX);
            //exportData.Add("size_y", maxY);
            //exportData.Add("size_z", maxZ);

            // Export frames
            var simulationBox = new int[maxX * maxY * maxZ];
            int frameLimit = 10;

            int mcSizeX = maxY, mcSizeY = maxZ, mcSizeZ = maxX;

            // Fill the array with -1 which indicates uninitialized
            Array.Fill(simulationBox, -1);
            //List<object> frameData = new();
            List<string> frameData = new();
            // Simulate the whole thing frame by frame
            for (int frameIdx = 0;frameIdx < frameLimit && frameIdx < recordedFrames.Length;frameIdx++)
            {
                Debug.Log($"Exporting frame #{frameIdx}");
                List<int> blockChanges = new();

                var frame = recordedFrames[frameIdx];
                int fX = frame.Size.x, fY = frame.Size.y, fZ = frame.Size.z;
                var frameStates = frame.States;

                for (byte z = 0; z < fZ; z++) for (byte y = 0; y < fY; y++) for (byte x = 0; x < fX; x++)
                {
                    int framePos = x + y * fX + z * fX * fY;
                    int boxPos = x + y * maxX + z * maxX * maxY;

                    if (simulationBox[boxPos] != frameStates[framePos]) // The block is changed in this frame
                    {
                        // Register the block change
                        blockChanges.AddRange(new int[]{ x, y, z, frameStates[framePos] });
                    }
                }

                frameData.Add(string.Join(" ", blockChanges));
            }

            // Export frame data
            //exportData.Add("frame_data", frameData);

            RecordingData recData = new(
                    recPalette.ToDictionary(x => charMap[x.Key].ToString(), x =>
                            new ColoredBlockStateInfo( ColorConvert.GetHexRGBString(x.Value.Color), x.Value.BlockState )),
                    maxX, maxY, maxZ, frameData
            );

            string jsonText = JsonConvert.SerializeObject(recData);
            File.WriteAllText(PathHelper.GetRecordingFile($"{recordingName}.json"), jsonText);
            //File.WriteAllText(PathHelper.GetRecordingFile($"{recordingName}.json"), Json.Object2Json(exportData));
        }
    }
}