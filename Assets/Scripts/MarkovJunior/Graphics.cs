// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)

using System;
using System.IO;
using UnityEngine;

namespace MarkovJunior
{
    static class Graphics
    {
        private static int GetColorInt(Color32 color)
        {
            return (color.a << 24) + (color.r << 16) + (color.g << 8) + color.b;
        }

        public static (int[], int, int, int) LoadBitmap(string filename)
        {
            try
            {
                var tex = new Texture2D(2, 2);

                tex.LoadImage(File.ReadAllBytes(filename));
                int width = tex.width;
                int height = tex.height;
                var result = new int[width * height];
                var pixels = tex.GetPixels32();

                for (int y = 0;y < height;y++)
                    for (int x = 0;x < width;x++)
                        result[(height - 1 - y) * width + x] = GetColorInt(pixels[y * width + x]);

                return (result, width, height, 1);
            }
            catch (Exception) { return (null, -1, -1, -1); }
            
        }
    }

}