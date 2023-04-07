#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    public static class ColorConvert
    {
        public static int GetRGBA(Color32 color)
        {
            return (color.a << 24) + (color.r << 16) + (color.g << 8) + color.b;
        }

        public static int GetRGB(Color32 color)
        {
            return (color.r << 16) + (color.g << 8) + color.b;
        }

        public static int GetOpqaueRGB(Color32 color)
        {
            return (255 << 24) + (color.r << 16) + (color.g << 8) + color.b;
        }

        public static Color32 GetColor32(int rgba)
        {
            return new((byte)((rgba & 0xFF0000) >> 16), (byte)((rgba & 0xFF00) >> 8), (byte)(rgba & 0xFF), (byte)(rgba >> 24));
        }

        public static Color32 GetOpaqueColor32(int rgb)
        {
            return new((byte)((rgb & 0xFF0000) >> 16), (byte)((rgb & 0xFF00) >> 8), (byte)(rgb & 0xFF), 255);
        }

    }
}