﻿// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)

using System.Xml.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace MarkovJunior
{
    class Interpreter
    {
        public Branch root, current;
        public Grid grid;
        Grid startgrid;

        bool origin;
        public System.Random random;

        public List<(int, int, int)> changes;
        public List<int> first;
        public int counter;
        
        public bool animated;
        public int numId = 0;

        Interpreter() { }
        public static Interpreter Load(XElement xelem, int MX, int MY, int MZ)
        {
            Interpreter ip = new();
            ip.origin = xelem.Get("origin", false);
            ip.grid = Grid.Load(xelem, MX, MY, MZ);
            if (ip.grid == null)
            {
                Debug.Log("failed to load grid");
                return null;
            }
            ip.startgrid = ip.grid;

            string symmetryString = xelem.Get<string>("symmetry", null);
            bool[] symmetry = SymmetryHelper.GetSymmetry(ip.grid.MZ == 1, symmetryString, ArrayHelper.Array1D(ip.grid.MZ == 1 ? 8 : 48, true));
            if (symmetry == null)
            {
                WriteLine($"unknown symmetry {symmetryString} at line {xelem.LineNumber()}");
                return null;
            }

            Node topnode = Node.Factory(xelem, symmetry, ip, ip.grid);
            if (topnode == null) return null;
            ip.root = topnode is Branch ? topnode as Branch : new MarkovNode(topnode, ip);

            ip.changes = new List<(int, int, int)>();
            ip.first = new List<int>();
            return ip;
        }

        public IEnumerable<(byte[], char[], int, int, int, int)> Run(int seed, int steps, bool animated)
        {
            random = new System.Random(seed);
            grid = startgrid;
            grid.Clear();
            if (origin) grid.state[grid.MX / 2 + (grid.MY / 2) * grid.MX + (grid.MZ / 2) * grid.MX * grid.MY] = 1;

            changes.Clear();
            first.Clear();
            first.Add(0);

            root.Reset();
            current = root;

            this.animated = animated;
            counter = 0;
            while (current != null && (steps <= 0 || counter < steps))
            {
                if (animated)
                    yield return (grid.state, grid.characters, grid.MX, grid.MY, grid.MZ, counter);

                current.Go();
                counter++;
                first.Add(changes.Count);
            }

            yield return (grid.state, grid.characters, grid.MX, grid.MY, grid.MZ, counter);
        }

        public static void WriteLine(string s) => Debug.Log(s);
        public static void Write(string s) => Debug.Log(s); // TODO: Console.Write(s);
    }
}