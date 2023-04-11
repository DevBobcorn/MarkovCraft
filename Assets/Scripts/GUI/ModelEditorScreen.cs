#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml.Linq;

using MarkovJunior;

namespace MarkovBlocks
{
    public class ModelEditorScreen : BaseScreen
    {
        [SerializeField] public TMP_Text? ScreenHeader;
        // Settings panel
        [SerializeField] public TMP_Dropdown? ModelDropdown;
        [SerializeField] public TMP_InputField? SizeXInput;
        [SerializeField] public TMP_InputField? SizeYInput;
        [SerializeField] public TMP_InputField? SizeZInput;
        [SerializeField] public TMP_InputField? AmountInput;
        [SerializeField] public TMP_InputField? StepsInput;
        [SerializeField] public TMP_InputField? SeedsInput;
        [SerializeField] public Toggle? AnimatedToggle;

        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;

        private readonly Dictionary<int, string> loadedModels = new();
        private readonly Dictionary<char, int> basePalette = new();
        private readonly List<MappingItem> mappingItems = new();
        private bool properlyLoaded = false;

        public override bool ShouldPause() => true;

        public override void OnShow(ScreenManager manager)
        {
            // Just to make sure things are cleared up
            ClearItems();

            properlyLoaded = false;
            
            var game = Test.Instance;
            var currentConfModel = game.CurrentConfiguredModel;
            var currentConfModelName = game.ConfiguredModelName;
            
            if (currentConfModel is null || ScreenHeader == null || ModelDropdown == null || SizeXInput == null || SizeYInput == null || SizeZInput == null ||
                    AmountInput == null || StepsInput == null || SeedsInput == null || AnimatedToggle == null || GridTransform == null)
            {
                Debug.LogWarning("The editor is not properly loaded!");

                if (ScreenHeader != null)
                    ScreenHeader.text = "0.0 Editor not loaded";

                return;
            }

            ScreenHeader.text = game.ConfiguredModelName;

            // Initialize settings panel
            var dir = PathHelper.GetExtraDataFile("models");
            int index = 0, selectedIndex = -1;
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                var confModelModel = m[(dir.Length + 1)..^4];
                options.Add(new(confModelModel));

                if (m.Equals(currentConfModel.Model))
                    selectedIndex = index;

                loadedModels.Add(index++, confModelModel);
            }
            
            ModelDropdown.AddOptions(options);

            if (selectedIndex != -1)
                ModelDropdown.value = selectedIndex;
            
            SizeXInput.text = currentConfModel.SizeX.ToString();
            SizeYInput.text = currentConfModel.SizeY.ToString();
            SizeZInput.text = currentConfModel.SizeZ.ToString();

            AmountInput.text = currentConfModel.Amount.ToString();
            StepsInput.text = currentConfModel.Steps.ToString();

            if (currentConfModel.Seeds.Length > 0)
                SeedsInput.text = string.Join(' ', currentConfModel.Seeds);
            else
                SeedsInput.text = string.Empty;
            
            AnimatedToggle.isOn = currentConfModel.Animated;

            // Initialize mappings panel
            XDocument.Load(PathHelper.GetExtraDataFile("palette.xml")).Root.Elements("color")
                    .ToList().ForEach(x => basePalette.Add(x.Get<char>("symbol"),
                            // RGB without alpha channel
                            ColorConvert.RGBFromHexString(x.Get<string>("value"))));
            
            var charSet = basePalette.Keys.ToHashSet();

            foreach (var item in currentConfModel.CustomRemapping)
            {
                if (charSet.Contains(item.Symbol))
                    charSet.Remove(item.Symbol);

                var newItemObj = GameObject.Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingItem>();

                mappingItems.Add(newItem);

                var defoColor = basePalette[item.Symbol];
                var overrideColor = ColorConvert.GetRGB(item.RemapColor);

                newItem.InitializeData(item.Symbol, defoColor, overrideColor, item.RemapTarget);

                newItem.transform.SetParent(GridTransform);
                newItem.transform.localScale = Vector3.one;
            }
            
            foreach (var ch in charSet)
            {
                var newItemObj = GameObject.Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingItem>();

                mappingItems.Add(newItem);

                var defoColor = basePalette[ch];

                newItem.InitializeData(ch, defoColor, defoColor, string.Empty);

                newItem.transform.SetParent(GridTransform);
                newItem.transform.localScale = Vector3.one;
            }

            properlyLoaded = true;

            MarkCharSetAsUsedInModel(new char[] { 'a', 'b' });
        }

        public override void OnHide(ScreenManager manager)
        {
            ClearItems();

        }

        public void MarkCharSetAsUsedInModel(char[] charSet)
        {
            foreach (var mappingItem in mappingItems)
            {
                if (charSet.Contains(mappingItem.Character)) // This item is used in the current model
                {
                    mappingItem.TagAsUsed(true);
                }
                else
                {
                    mappingItem.TagAsUsed(false);
                    // Move it to the end
                    mappingItem.transform.SetAsLastSibling();
                }
            }
        }

        private void ClearItems()
        {
            loadedModels.Clear();
            basePalette.Clear();

            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                manager.SetActiveScreenByType<HUDScreen>();
            }

        }
    }
}