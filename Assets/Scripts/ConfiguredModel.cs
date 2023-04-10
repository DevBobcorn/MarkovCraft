#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

using MarkovJunior;

namespace MarkovBlocks
{
    [Serializable]
    public struct CustomCharRemap
    {
        public char Symbol;
        public string RemapTarget;
        public Color32 RemapColor;
    }

    [CreateAssetMenu(fileName = "MarkovJuniorModel", menuName = "MarkovBlocks/MarkovJuniorModel")]
    public class ConfiguredModel : ScriptableObject
    {
        [SerializeField] public string Model = "Apartemazements";
        [SerializeField] public int SizeX = 8;
        [SerializeField] public int SizeY = 8;
        [SerializeField] public int SizeZ = 8;
        [SerializeField] public int Amount = 1;
        [SerializeField] public int Steps = 1000;
        [SerializeField] public bool Animated = true;
        [SerializeField] public int[] Seeds = { };

        [SerializeField] public CustomCharRemap[] CustomRemapping = { };

        public static ConfiguredModel CreateFromXMLDoc(XDocument xdoc)
        {
            var model = ScriptableObject.CreateInstance(typeof (ConfiguredModel)) as ConfiguredModel;
            UpdateFromXMLDoc(ref model!, xdoc);

            return model;
        }

        public static void UpdateFromXMLDoc(ref ConfiguredModel model, XDocument xdoc)
        {
            var root = xdoc.Root;

            // Basic configurations
            model.Model = root.Get<string>("Model", model.Model);
            model.SizeX = root.Get<int>("SizeX", model.SizeX);
            model.SizeY = root.Get<int>("SizeY", model.SizeY);
            model.SizeZ = root.Get<int>("SizeZ", model.SizeZ);
            model.Amount = root.Get<int>("Amount", model.Amount);
            model.Steps = root.Get<int>("Steps", model.Steps);
            model.Animated = root.Get<bool>("Animated", model.Animated);

            // Generation seeds
            var xSeeds = root.Element("Seeds");
            if (xSeeds is not null)
            {
                var seeds = new List<int>();
                foreach (var xSeed in xSeeds.Elements("Seed"))
                    seeds.Add(xSeed.Get<int>("Value"));

                model.Seeds = seeds.ToArray();
            }
            else // No seeds specified
                model.Seeds = new int[] { };

            // Custom remapping items
            var xRemap = root.Element("CustomRemapping");
            if (xRemap is not null)
            {
                var remaps = new List<CustomCharRemap>();
                foreach (var remap in xRemap.Elements("Item"))
                    remaps.Add(new() {
                        Symbol = remap.Get<char>("Symbol"),
                        RemapColor = ColorConvert.GetColor32(remap.Get<int>("RemapColor")),
                        RemapTarget = remap.Get<string>("RemapTarget", string.Empty)
                    });

                model.CustomRemapping = remaps.ToArray();
            }
            else // No custom remapping specified
                model.CustomRemapping = new CustomCharRemap[] { };
        }

        public static XDocument GetXMLDoc(ConfiguredModel model)
        {
            var xdoc = new XDocument();

            var root = new XElement("ConfiguredModel",
                // Basic configurations
                new XAttribute("Model", model.Model),
                new XAttribute("SizeX", model.SizeX),
                new XAttribute("SizeY", model.SizeY),
                new XAttribute("SizeZ", model.SizeZ),
                new XAttribute("Amount", model.Amount),
                new XAttribute("Steps", model.Steps),
                new XAttribute("Animated", model.Animated)
            );
            
            // Generation seeds
            if (model.Seeds.Length > 0)
            {
                var xSeeds = new XElement("Seeds");
                var seeds = model.Seeds;

                for (int i = 0;i < seeds.Length;i++)
                {
                    xSeeds.Add(new XElement("Seed",
                        new XAttribute("Value", seeds[i])
                    ));
                }

                root.Add(xSeeds);
            }

            // Custom remapping items
            if (model.CustomRemapping.Length > 0)
            {
                var xRemap = new XElement("CustomRemapping");
                var remap = model.CustomRemapping;

                for (int i = 0;i < remap.Length;i++)
                {
                    var item = new XElement("Item",
                        new XAttribute("Symbol", remap[i].Symbol),
                        new XAttribute("RemapColor",
                            ColorConvert.GetRGBA(remap[i].RemapColor))
                    );

                    if (!string.IsNullOrWhiteSpace(remap[i].RemapTarget))
                        item.SetAttributeValue("RemapTarget", remap[i].RemapTarget);
                    
                    xRemap.Add(item);
                }

                root.Add(xRemap);
            }

            xdoc.Add(root);

            return xdoc;
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof (ConfiguredModel))]
    public class ConfiguredModelEditor : Editor
    {
        private const string PLACE_HOLDER = "<model/path/here>";
        private const string MODEL_FOLDER = "configured_models";

        private string XDocFileName = PLACE_HOLDER;

        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label($"Load from/Save to (file under {MODEL_FOLDER}):");
            XDocFileName = GUILayout.TextField(XDocFileName);

            if (XDocFileName.Equals(PLACE_HOLDER)) // Fill in default xml model path
            {
                var modelObj = this.target as ConfiguredModel;

                if (modelObj is not null)
                    XDocFileName = $"{modelObj.name}.xml";
                else
                    XDocFileName = "model.xml";
            }

            GUILayout.Space(10);

            // XDoc import button
            if (GUILayout.Button("Load from .xml file"))
            {
                var modelObj = this.target as ConfiguredModel;

                if (modelObj is not null)
                {
                    var path = PathHelper.GetExtraDataFile($"{MODEL_FOLDER}/{XDocFileName}");
                    
                    if (File.Exists(path))
                    {
                        var xdoc = XDocument.Load(path);
                        ConfiguredModel.UpdateFromXMLDoc(ref modelObj, xdoc);
                        Debug.Log($"Configured model updated from {path}");
                    }
                    else
                        Debug.LogWarning($"File not found at {path}");
                }
            }

            // XDoc export button
            if (GUILayout.Button("Save as .xml file"))
            {
                var modelObj = this.target as ConfiguredModel;

                if (modelObj is not null)
                {
                    var path = PathHelper.GetExtraDataFile($"{MODEL_FOLDER}/{XDocFileName}");
                    var xdoc = ConfiguredModel.GetXMLDoc(modelObj);
                    xdoc.Save(path);

                    Debug.Log($"Configured model exported to {path}");
                }
            }

            GUILayout.Space(10);

            // Configured model editor
            DrawDefaultInspector();
        }

    }

#endif
}