#nullable enable
using System.Collections;
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
        private bool loading = false, properlyLoaded = false;

        public override bool ShouldPause() => true;

        private IEnumerator InitializePanels(ConfiguredModel confModel)
        {
            // Initialize settings panel
            var dir = PathHelper.GetExtraDataFile("models");
            int index = 0, selectedIndex = -1;
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                var confModelModel = m[(dir.Length + 1)..^4];
                options.Add(new(confModelModel));

                if (confModelModel.Equals(confModel.Model))
                {
                    selectedIndex = index;
                    Debug.Log($"Selected [{index}] {confModel.Model}");
                }

                loadedModels.Add(index++, confModelModel);
            }
            
            ModelDropdown!.AddOptions(options);
            ModelDropdown.onValueChanged.AddListener(UpdateDropdownOption);

            if (selectedIndex != -1)
                ModelDropdown.value = selectedIndex;
            
            SizeXInput!.text = confModel.SizeX.ToString();
            SizeYInput!.text = confModel.SizeY.ToString();
            SizeZInput!.text = confModel.SizeZ.ToString();

            AmountInput!.text = confModel.Amount.ToString();
            StepsInput!.text = confModel.Steps.ToString();

            if (confModel.Seeds.Length > 0)
                SeedsInput!.text = string.Join(' ', confModel.Seeds);
            else
                SeedsInput!.text = string.Empty;
            
            AnimatedToggle!.isOn = confModel.Animated;

            // Initialize mappings panel
            XDocument? paletteDoc = null, modelDoc = null;

            string paletteFileName = PathHelper.GetExtraDataFile($"palette.xml");
            string modelFileName = PathHelper.GetExtraDataFile($"models/{confModel.Model}.xml");

            if (File.Exists(paletteFileName) && File.Exists(modelFileName))
            {
                var fs1 = new FileStream(paletteFileName, FileMode.Open);
                var fs2 = new FileStream(modelFileName, FileMode.Open);

                var task1 = XDocument.LoadAsync(fs1, LoadOptions.SetLineInfo, new());
                var task2 = XDocument.LoadAsync(fs2, LoadOptions.SetLineInfo, new());

                while (!task1.IsCompleted || !task2.IsCompleted)
                    yield return null;
                
                fs1.Close();
                fs2.Close();
                
                if (task1.IsCompletedSuccessfully && task2.IsCompletedSuccessfully)
                {
                    paletteDoc = task1.Result;
                    modelDoc = task2.Result;
                }
            }
            
            if (paletteDoc is null || modelDoc is null)
            {
                Debug.LogWarning($"ERROR: Couldn't open xml file at {paletteFileName}");
                loading = false;
                properlyLoaded = false;
                yield break;
            }

            yield return null;

            paletteDoc.Root.Elements("color").ToList().ForEach(x => basePalette.Add(
                    x.Get<char>("symbol"), ColorConvert.RGBFromHexString(x.Get<string>("value"))));
            
            var activeCharSet = new HashSet<char>();

            foreach (var vals in from node in modelDoc.Descendants()
                    where node.Attribute("values") is not null
                    select node.Attribute("values").Value )
                vals.ToList().ForEach(ch => activeCharSet.Add(ch));
            
            var charSet = basePalette.Keys.ToHashSet();

            foreach (var item in confModel.CustomRemapping)
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
            
            yield return null;

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

            ShowActiveCharSet(activeCharSet);

            loading = false;
            properlyLoaded = true;
        }

        public IEnumerator UpdateActiveCharSetFromModel(string modelFileName)
        {
            loading = true;

            XDocument? modelDoc = null;

            if (File.Exists(modelFileName))
            {
                var fs2 = new FileStream(modelFileName, FileMode.Open);
                var task2 = XDocument.LoadAsync(fs2, LoadOptions.SetLineInfo, new());

                while (!task2.IsCompleted)
                    yield return null;
                
                fs2.Close();
                
                if (task2.IsCompletedSuccessfully)
                    modelDoc = task2.Result;
            }
            
            if (modelDoc is null)
            {
                Debug.LogWarning($"ERROR: Couldn't open xml file at {modelFileName}");
                loading = false;
                properlyLoaded = false;
                yield break;
            }

            var activeCharSet = new HashSet<char>();

            foreach (var vals in from node in modelDoc.Descendants()
                    where node.Attribute("values") is not null
                    select node.Attribute("values").Value )
                vals.ToList().ForEach(ch => activeCharSet.Add(ch));
            
            ShowActiveCharSet(activeCharSet);
            loading = false;
        }

        public void UpdateDropdownOption(int newValue)
        {
            if (loading || !properlyLoaded) return;

            if (loadedModels.ContainsKey(newValue))
            {
                var confModelModel = loadedModels[newValue];

                string modelFileName = PathHelper.GetExtraDataFile($"models/{confModelModel}.xml");
                StartCoroutine(UpdateActiveCharSetFromModel(modelFileName));
            }
        }

        public override void OnShow(ScreenManager manager)
        {
            if (loading) return;
            loading = true;

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

                loading = false;
                return;
            }

            ScreenHeader.text = game.ConfiguredModelName;

            StartCoroutine(InitializePanels(currentConfModel));
        }

        public override void OnHide(ScreenManager manager)
        {
            ClearItems();

        }

        public void ShowActiveCharSet(HashSet<char> charSet)
        {
            foreach (var mappingItem in mappingItems)
            {
                if (charSet.Contains(mappingItem.Character)) // This item is used in the current model. Show it
                    mappingItem.gameObject.SetActive(true);
                else // Hide it
                    mappingItem.gameObject.SetActive(false);
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

        private void SaveChanges()
        {
            if (properlyLoaded) // The configured model is properly opened
            {

            }
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (loading) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {

                manager.SetActiveScreenByType<HUDScreen>();
            }

        }
    }
}