#nullable enable
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

using MarkovJunior;
using UnityEditor;

namespace MarkovCraft
{
    [Serializable]
    public class CustomMappingItem : Json.JSONSerializable
    {
        public char Character;
        public string BlockState = string.Empty;
        public Color32 Color;

        public CustomMappingItem AsCopy()
        {
            return new CustomMappingItem
            {
                Character = this.Character,
                BlockState = this.BlockState,
                Color = this.Color
            };
        }

        public bool MapTargetIdentical(CustomMappingItem item)
        {
            return item.BlockState.Equals(BlockState) && item.Color.Equals(Color);
        }

        public string ToJson()
        {
            return "{\"color\":\"" + ColorConvert.GetRGB(Color) + "\",\"state\":\"" + BlockState + "\"}";
        }
    }

    [CreateAssetMenu(fileName = "ConfiguredModel", menuName = "MarkovCraft/Configured Model")]
    public class ConfiguredModel : ScriptableObject
    {
        [SerializeField] public string Model = "Apartemazements";
        [SerializeField] public int SizeX = 8; // [1, 256]
        [SerializeField] public int SizeY = 8; // [1, 256]
        [SerializeField] public int SizeZ = 8; // [1, 256]
        [SerializeField] public int Amount = 1; // [1, 100]
        [SerializeField] public int Steps = 1000; // [1000, 100000]
        [SerializeField] public bool Animated = true; // true or false
        [SerializeField] public int StepsPerRefresh = 1; // [1, 100]
        [SerializeField] public int[] Seeds = { };

        // These items themselves shouldn't be changed once they have been loaded/updated from file
        // Copies of these items should be created and used for editing purposes
        [SerializeField] public CustomMappingItem[] CustomMapping = { };

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
            model.StepsPerRefresh = root.Get<int>("StepsPerRefresh", model.StepsPerRefresh);

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

            // Custom mapping items
            var xMapping = root.Element("CustomMapping");
            if (xMapping is not null)
            {
                var mapping = new List<CustomMappingItem>();
                foreach (var item in xMapping.Elements("Item"))
                    mapping.Add(new() {
                        Character = item.Get<char>("Character"),
                        Color = ColorConvert.OpaqueColor32FromHexString(item.Get<string>("Color")),
                        BlockState = item.Get<string>("BlockState", string.Empty)
                    });

                model.CustomMapping = mapping.ToArray();
            }
            else // No custom mapping specified
                model.CustomMapping = new CustomMappingItem[] { };
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
                new XAttribute("Animated", model.Animated),
                new XAttribute("StepsPerRefresh", model.StepsPerRefresh)
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

            // Custom mapping items
            if (model.CustomMapping.Length > 0)
            {
                var xMapping = new XElement("CustomMapping");
                var mapping = model.CustomMapping;

                for (int i = 0;i < mapping.Length;i++)
                {
                    var item = new XElement("Item",
                        new XAttribute("Character", mapping[i].Character),
                        new XAttribute("Color",
                            ColorConvert.GetHexRGBString(mapping[i].Color))
                    );

                    if (!string.IsNullOrWhiteSpace(mapping[i].BlockState))
                        item.SetAttributeValue("BlockState", mapping[i].BlockState);
                    
                    xMapping.Add(item);
                }

                root.Add(xMapping);
            }

            xdoc.Add(root);

            return xdoc;
        }

    }
}