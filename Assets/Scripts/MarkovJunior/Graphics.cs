#nullable enable
using System;
using System.IO;
using System.Threading;
using UnityEngine;

using MarkovCraft;

namespace MarkovJunior
{
    static class Graphics
    {
        public static (int[]?, int, int, int) LoadBitmap(string filename)
        {
            int width  = 2;
            int height = 2;
            int[]? result = null;

            bool completed = false;

            void loadPixels()
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(filename));

                width = tex.width;
                height = tex.height;

                var pixels = tex.GetPixels32();
                result = new int[width * height];

                for (int y = 0;y < height;y++)
                    for (int x = 0;x < width;x++)
                        result[(height - 1 - y) * width + x] = ColorConvert.GetRGB(pixels[y * width + x]);
            };

            if (Thread.CurrentThread == MarkovGlobal.UnityThread)
            {
                try {
                    loadPixels();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"An exception occurred: {e}");
                }
            }
            else
            {
                Loom.QueueOnMainThread(() => {
                    try {
                        loadPixels();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"An exception occurred: {e}");
                    }
                    finally { completed = true; }
                });

                while (!completed) { /* Wait */ }
            }

            return (result, width, height, 1);
        }
    }

}