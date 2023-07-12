#nullable enable
using System.Collections.Generic;

namespace MarkovCraft
{
    public record ColoredBlockStateInfo
    {
        public string Color; // Hex RGB color code
        public string BlockState;

        public ColoredBlockStateInfo(string color, string blockState)
        {
            Color = color;
            BlockState = blockState;
        }
    }

    public record RecordingData
    {
        public Dictionary<string, ColoredBlockStateInfo> Palette;
        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public List<string> FrameData;

        public RecordingData(Dictionary<string, ColoredBlockStateInfo> palette, int sizeX, int sizeY, int sizeZ, List<string> frameData)
        {
            Palette = palette;
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            FrameData = frameData;
        }
    }
}