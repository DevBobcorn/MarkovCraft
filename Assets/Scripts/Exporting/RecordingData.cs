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
        public List<ColoredBlockStateInfo> Palette;
        public int Dimension; // Could only be 2 for 2d or 3 for 3d
        public int SizeX;
        public int SizeY;
        public int SizeZ; // SizeZ should be 1 if it is 2d
        public List<string> FrameData;

        public RecordingData(List<ColoredBlockStateInfo> palette, int dimension, int sizeX, int sizeY, int sizeZ, List<string> frameData)
        {
            Palette = palette;
            Dimension = dimension;
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            FrameData = frameData;
        }
    }
}