// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MarkovJunior
{
    static class VoxHelper
    {
        public static (int[], int, int, int) LoadVox(string filename)
        {
            try
            {
                using FileStream file = File.Open(filename, FileMode.Open);
                var stream = new BinaryReader(file);

                int[] result = null;
                int MX = -1, MY = -1, MZ = -1;

                string magic = new(stream.ReadChars(4));
                int version = stream.ReadInt32();

                while (stream.BaseStream.Position < stream.BaseStream.Length)
                {
                    byte[] bt = stream.ReadBytes(1);
                    char head = Encoding.ASCII.GetChars(bt)[0];

                    if (head == 'S')
                    {
                        string tail = Encoding.ASCII.GetString(stream.ReadBytes(3));
                        if (tail != "IZE") continue;

                        int chunkSize = stream.ReadInt32();
                        stream.ReadBytes(4);
                        //Debug.Log("found SIZE chunk");
                        MX = stream.ReadInt32();
                        MY = stream.ReadInt32();
                        MZ = stream.ReadInt32();
                        stream.ReadBytes(chunkSize - 4 * 3);
                        //Debug.Log($"size = ({MX}, {MY}, {MZ})");
                    }
                    else if (head == 'X')
                    {
                        string tail = Encoding.ASCII.GetString(stream.ReadBytes(3));
                        if (tail != "YZI") continue;

                        if (MX <= 0 || MY <= 0 || MZ <= 0) return (null, MX, MY, MZ);
                        result = new int[MX * MY * MZ];
                        for (int i = 0; i < result.Length; i++) result[i] = -1;

                        //Debug.Log("found XYZI chunk");
                        stream.ReadBytes(8);
                        int numVoxels = stream.ReadInt32();
                        //Debug.Log($"number of voxels = {numVoxels}");
                        for (int i = 0; i < numVoxels; i++)
                        {
                            byte x = stream.ReadByte();
                            byte y = stream.ReadByte();
                            byte z = stream.ReadByte();
                            byte color = stream.ReadByte();
                            result[x + y * MX + z * MX * MY] = color;
                            //Debug.Log($"adding voxel {x} {y} {z} of color {color}");
                        }
                    }
                }
                file.Close();
                return (result, MX, MY, MZ);
            }
            catch (Exception) { return (null, -1, -1, -1); }
        }

        private static readonly int[] DEFAULT_VOX_PALETTE = new int[] {
            0x000000, 0xffffff, 0xffffcc, 0xffff99, 0xffff66, 0xffff33, 0xffff00, 0xffccff, 
            0xffcccc, 0xffcc99, 0xffcc66, 0xffcc33, 0xffcc00, 0xff99ff, 0xff99cc, 0xff9999,
            0xff9966, 0xff9933, 0xff9900, 0xff66ff, 0xff66cc, 0xff6699, 0xff6666, 0xff6633,
            0xff6600, 0xff33ff, 0xff33cc, 0xff3399, 0xff3366, 0xff3333, 0xff3300, 0xff00ff,
            0xff00cc, 0xff0099, 0xff0066, 0xff0033, 0xff0000, 0xccffff, 0xccffcc, 0xccff99,
            0xccff66, 0xccff33, 0xccff00, 0xccccff, 0xcccccc, 0xcccc99, 0xcccc66, 0xcccc33,
            0xcccc00, 0xcc99ff, 0xcc99cc, 0xcc9999, 0xcc9966, 0xcc9933, 0xcc9900, 0xcc66ff,
            0xcc66cc, 0xcc6699, 0xcc6666, 0xcc6633, 0xcc6600, 0xcc33ff, 0xcc33cc, 0xcc3399,
            0xcc3366, 0xcc3333, 0xcc3300, 0xcc00ff, 0xcc00cc, 0xcc0099, 0xcc0066, 0xcc0033,
            0xcc0000, 0x99ffff, 0x99ffcc, 0x99ff99, 0x99ff66, 0x99ff33, 0x99ff00, 0x99ccff,
            0x99cccc, 0x99cc99, 0x99cc66, 0x99cc33, 0x99cc00, 0x9999ff, 0x9999cc, 0x999999,
            0x999966, 0x999933, 0x999900, 0x9966ff, 0x9966cc, 0x996699, 0x996666, 0x996633,
            0x996600, 0x9933ff, 0x9933cc, 0x993399, 0x993366, 0x993333, 0x993300, 0x9900ff,
            0x9900cc, 0x990099, 0x990066, 0x990033, 0x990000, 0x66ffff, 0x66ffcc, 0x66ff99,
            0x66ff66, 0x66ff33, 0x66ff00, 0x66ccff, 0x66cccc, 0x66cc99, 0x66cc66, 0x66cc33,
            0x66cc00, 0x6699ff, 0x6699cc, 0x669999, 0x669966, 0x669933, 0x669900, 0x6666ff,
            0x6666cc, 0x666699, 0x666666, 0x666633, 0x666600, 0x6633ff, 0x6633cc, 0x663399,
            0x663366, 0x663333, 0x663300, 0x6600ff, 0x6600cc, 0x660099, 0x660066, 0x660033,
            0x660000, 0x33ffff, 0x33ffcc, 0x33ff99, 0x33ff66, 0x33ff33, 0x33ff00, 0x33ccff,
            0x33cccc, 0x33cc99, 0x33cc66, 0x33cc33, 0x33cc00, 0x3399ff, 0x3399cc, 0x339999,
            0x339966, 0x339933, 0x339900, 0x3366ff, 0x3366cc, 0x336699, 0x336666, 0x336633,
            0x336600, 0x3333ff, 0x3333cc, 0x333399, 0x333366, 0x333333, 0x333300, 0x3300ff,
            0x3300cc, 0x330099, 0x330066, 0x330033, 0x330000, 0x00ffff, 0x00ffcc, 0x00ff99,
            0x00ff66, 0x00ff33, 0x00ff00, 0x00ccff, 0x00cccc, 0x00cc99, 0x00cc66, 0x00cc33,
            0x00cc00, 0x0099ff, 0x0099cc, 0x009999, 0x009966, 0x009933, 0x009900, 0x0066ff,
            0x0066cc, 0x006699, 0x006666, 0x006633, 0x006600, 0x0033ff, 0x0033cc, 0x003399,
            0x003366, 0x003333, 0x003300, 0x0000ff, 0x0000cc, 0x000099, 0x000066, 0x000033,
            0xee0000, 0xdd0000, 0xbb0000, 0xaa0000, 0x880000, 0x770000, 0x550000, 0x440000,
            0x220000, 0x110000, 0x00ee00, 0x00dd00, 0x00bb00, 0x00aa00, 0x008800, 0x007700,
            0x005500, 0x004400, 0x002200, 0x001100, 0x0000ee, 0x0000dd, 0x0000bb, 0x0000aa,
            0x000088, 0x000077, 0x000055, 0x000044, 0x000022, 0x000011, 0xeeeeee, 0xdddddd,
            0xbbbbbb, 0xaaaaaa, 0x888888, 0x777777, 0x555555, 0x444444, 0x222222, 0x111111
        };

        public record VoxPiece
        {
            public int SizeX;
            public int SizeY;
            public int SizeZ;
            public int[] BlockData;
        }

        public static (VoxPiece[], int[]) LoadFullVox(string filename)
        {
            try
            {
                using FileStream file = File.Open(filename, FileMode.Open);
                var stream = new BinaryReader(file);

                var result = new List<VoxPiece>();
                int[] rgbPalette = new int[256];

                int MX = -1, MY = -1, MZ = -1;

                string magic = new(stream.ReadChars(4));
                int version = stream.ReadInt32();
                bool colorPaletteSpecified = false;

                while (stream.BaseStream.Position < stream.BaseStream.Length)
                {
                    byte[] bt = stream.ReadBytes(1);
                    char head = Encoding.ASCII.GetChars(bt)[0];

                    if (head == 'S')
                    {
                        string tail = Encoding.ASCII.GetString(stream.ReadBytes(3));
                        if (tail != "IZE") continue;

                        int chunkSize = stream.ReadInt32();
                        stream.ReadBytes(4);
                        //Debug.Log("found SIZE chunk");
                        MX = stream.ReadInt32();
                        MY = stream.ReadInt32();
                        MZ = stream.ReadInt32();
                        stream.ReadBytes(chunkSize - 4 * 3);
                        //Debug.Log($"size = ({MX}, {MY}, {MZ})");
                    }
                    else if (head == 'X')
                    {
                        string tail = Encoding.ASCII.GetString(stream.ReadBytes(3));
                        if (tail != "YZI") continue;

                        if (MX <= 0 || MY <= 0 || MZ <= 0)
                        {
                            UnityEngine.Debug.LogWarning("Reading XYZI field before receiving a valid size!");
                            return (null, null);
                        }
                        var pieceBlockData = new int[MX * MY * MZ];
                        for (int i = 0; i < pieceBlockData.Length; i++) pieceBlockData[i] = -1;

                        //Debug.Log("found XYZI chunk");
                        stream.ReadBytes(8);
                        int numVoxels = stream.ReadInt32();
                        //Debug.Log($"number of voxels = {numVoxels}");
                        for (int i = 0; i < numVoxels; i++)
                        {
                            byte x = stream.ReadByte();
                            byte y = stream.ReadByte();
                            byte z = stream.ReadByte();
                            byte colorIndex = stream.ReadByte();
                            pieceBlockData[x + y * MX + z * MX * MY] = colorIndex;
                        }

                        result.Add(new VoxPiece { SizeX = MX, SizeY = MY, SizeZ = MZ, BlockData = pieceBlockData });

                        MX = -1;
                        MY = -1;
                        MZ = -1;
                    }
                    else if (head == 'R')
                    {
                        string tail = Encoding.ASCII.GetString(stream.ReadBytes(3));
                        if (tail != "GBA") continue;

                        int chunkSize = stream.ReadInt32(); // Should be 1024 (chunk data size)
                        //UnityEngine.Debug.Log($"RGBA Chunk size: {chunkSize}");
                        stream.ReadInt32(); // Should be 0 (children chunk size, unused)

                        for (int i = 1;i < rgbPalette.Length;i++)
                        {
                            byte r = stream.ReadByte();
                            byte g = stream.ReadByte();
                            byte b = stream.ReadByte();
                            byte a = stream.ReadByte();
                            rgbPalette[i] = (r << 16) + (g << 8) + b;
                        }

                        colorPaletteSpecified = true;
                    }
                }
                file.Close();

                if (!colorPaletteSpecified) // Color palette not specified, used vox default palette
                {
                    DEFAULT_VOX_PALETTE.CopyTo(rgbPalette, 0);
                }

                return (result.ToArray(), rgbPalette);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"An exception occurred: {e}");
                return (null, null);
            }
        }

        static void WriteString(this BinaryWriter stream, string s) { foreach (char c in s) stream.Write(c); }

        public static void SaveVox(byte[] blockData, byte MX, byte MY, byte MZ, int[] palette, HashSet<int> airIndices, string filename)
        {
            List<(byte, byte, byte, byte)> voxels = new();
            for (byte z = 0; z < MZ; z++) for (byte y = 0; y < MY; y++) for (byte x = 0; x < MX; x++)
            {
                int i = x + y * MX + z * MX * MY;
                byte v = blockData[i];

                if (!airIndices.Contains(v))
                {
                    voxels.Add((x, y, z, (byte)(v + 1)));
                }
            }

            FileStream file = File.Open(filename, FileMode.Create);
            using BinaryWriter stream = new(file);

            stream.WriteString("VOX ");
            stream.Write(150);

            stream.WriteString("MAIN");
            stream.Write(0);
            stream.Write(1092 + voxels.Count * 4);

            stream.WriteString("PACK");
            stream.Write(4);
            stream.Write(0);
            stream.Write(1);

            stream.WriteString("SIZE");
            stream.Write(12);
            stream.Write(0);
            stream.Write((int)MX);
            stream.Write((int)MY);
            stream.Write((int)MZ);

            stream.WriteString("XYZI");
            stream.Write(4 + voxels.Count * 4);
            stream.Write(0);
            stream.Write(voxels.Count);

            foreach (var (x, y, z, color) in voxels)
            {
                stream.Write(x);
                //stream.Write((byte)(size.y - v.y - 1));
                stream.Write(y);
                stream.Write(z);
                stream.Write(color);
            }

            stream.WriteString("RGBA");
            stream.Write(1024);
            stream.Write(0);

            foreach (int c in palette)
            {
                //(byte R, byte G, byte B) = c.ToTuple();
                stream.Write((byte)((c & 0xff0000) >> 16));
                stream.Write((byte)((c & 0xff00) >> 8));
                stream.Write((byte)(c & 0xff));
                stream.Write((byte)0);
            }
            for (int i = palette.Length; i < 255; i++)
            {
                stream.Write((byte)(0xff - i - 1));
                stream.Write((byte)(0xff - i - 1));
                stream.Write((byte)(0xff - i - 1));
                stream.Write((byte)(0xff));
            }
            stream.Write(0);
            file.Close();
        }
    }
}