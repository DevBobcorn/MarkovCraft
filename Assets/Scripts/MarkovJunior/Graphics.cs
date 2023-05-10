#nullable enable
using System;
using System.IO;
using UnityEngine;

using MarkovCraft;

namespace MarkovJunior
{
    static class Graphics
    {
        public static (int[]?, int, int, int) LoadBitmap(string filename)
        {
            try
            {
                int width  = 2;
                int height = 2;
                int[]? result = null;

                bool completed = false;

                Loom.QueueOnMainThread(() => {
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(File.ReadAllBytes(filename));

                    width = tex.width;
                    height = tex.height;

                    var pixels = tex.GetPixels32();
                    result = new int[width * height];

                    for (int y = 0;y < height;y++)
                        for (int x = 0;x < width;x++)
                            result[(height - 1 - y) * width + x] = ColorConvert.GetRGB(pixels[y * width + x]);
                    
                    completed = true;
                });

                while (!completed) { /* Wait */ }

                return (result, width, height, 1);
            }
            catch (Exception e) {
                Debug.LogWarning($"An exception occurred: {e}");
                return (null, -1, -1, -1);
            }
            
        }
    }

}