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
        private const string MODEL_FOLDER = "configured_models";

        [SerializeField] public TMP_Text? ScreenHeader;
        // Settings Panel
        [SerializeField] public TMP_Dropdown? ModelDropdown;
        [SerializeField] public TMP_InputField? SizeXInput;
        [SerializeField] public TMP_InputField? SizeYInput;
        [SerializeField] public TMP_InputField? SizeZInput;
        [SerializeField] public TMP_InputField? AmountInput;
        [SerializeField] public TMP_InputField? StepsInput;
        [SerializeField] public TMP_InputField? SeedsInput;
        [SerializeField] public Toggle? AnimatedToggle;
        [SerializeField] public Button? SaveButton;
        // Mapping Items Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;
        // BlockState Preview
        [SerializeField] public BlockStatePreview? BlockStatePreview;

        private readonly List<MappingEditorItem> mappingItems = new();
        private bool working = false, properlyLoaded = false;
        private string confModelFile = string.Empty;

        public override bool ShouldPause() => true;

        private IEnumerator InitializeScreen(string confModelFile)
        {
            // Load configured model file
            var xdoc = XDocument.Load($"{PathHelper.GetExtraDataFile("configured_models")}/{confModelFile}");
            var confModel = ConfiguredModel.CreateFromXMLDoc(xdoc);

            // Initialize settings panel
            var dir = PathHelper.GetExtraDataFile("models");
            int index = 0, selectedIndex = -1;
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                var confModelModel = m[(dir.Length + 1)..^4];
                options.Add(new(confModelModel));

                if (confModelModel.Equals(confModel.Model))
                    selectedIndex = index;
                
                index++;
            }
            
            ModelDropdown!.ClearOptions();
            ModelDropdown.AddOptions(options);

            ModelDropdown.onValueChanged.RemoveAllListeners();
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

            SaveButton!.onClick.RemoveAllListeners();
            SaveButton.onClick.AddListener(SaveConfiguredModel);

            // Initialize mappings panel
            XDocument? paletteDoc = null, confModelDoc = null;

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
                    confModelDoc = task2.Result;
                }
            }
            
            if (paletteDoc is null || confModelDoc is null)
            {
                Debug.LogWarning($"ERROR: Couldn't open xml file at {paletteFileName}");
                working = false;
                properlyLoaded = false;
                yield break;
            }

            yield return null;

            var basePalette = new Dictionary<char, int>();

            paletteDoc.Root.Elements("color").ToList().ForEach(x => basePalette.Add(
                    x.Get<char>("symbol"), ColorConvert.RGBFromHexString(x.Get<string>("value"))));
            
            var activeCharSet = new HashSet<char>();

            foreach (var vals in from node in confModelDoc.Descendants()
                    where node.Attribute("values") is not null
                    select node.Attribute("values").Value )
                vals.ToList().ForEach(ch => activeCharSet.Add(ch));
            
            var FullCharSet = basePalette.Keys.ToHashSet();

            var customMapping = confModel.CustomMapping.ToDictionary(x => x.Character, x => x);

            // Populate mapping item grid
            foreach (var ch in FullCharSet)
            {
                var newItemObj = GameObject.Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingEditorItem>();

                mappingItems.Add(newItem);

                var defoColor = basePalette[ch];

                var custom = customMapping.ContainsKey(ch);
                newItem.InitializeData(ch, defoColor, custom ? ColorConvert.GetRGB(customMapping[ch].Color)
                        : defoColor, custom ? customMapping[ch].BlockState : string.Empty, BlockStatePreview!);

                newItem.transform.SetParent(GridTransform);
                newItem.transform.localScale = Vector3.one;
            }

            ShowActiveCharSet(activeCharSet);

            working = false;
            properlyLoaded = true;

            if (ScreenHeader != null)
                ScreenHeader.text = $"Editing {confModelFile}";
        }

        private IEnumerator UpdateActiveCharSetFromModel(string modelFileName)
        {
            working = true;

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
                working = false;
                properlyLoaded = false;
                yield break;
            }

            var activeCharSet = new HashSet<char>();

            foreach (var vals in from node in modelDoc.Descendants()
                    where node.Attribute("values") is not null
                    select node.Attribute("values").Value )
                vals.ToList().ForEach(ch => activeCharSet.Add(ch));
            
            ShowActiveCharSet(activeCharSet);
            working = false;
        }

        private void UpdateDropdownOption(int newValue)
        {
            if (working || !properlyLoaded) return;

            var confModelModel = ModelDropdown?.options[newValue].text;
            if (confModelModel is null) return;

            string modelFileName = PathHelper.GetExtraDataFile($"models/{confModelModel}.xml");
            StartCoroutine(UpdateActiveCharSetFromModel(modelFileName));
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            if (ScreenHeader != null)
                ScreenHeader.text = "Loading...";
            
            confModelFile = Test.Instance.ConfiguredModelFile;
            
            if (ModelDropdown == null || SizeXInput == null || SizeYInput == null || SizeZInput == null ||SaveButton == null || AmountInput == null ||
                    StepsInput == null || SeedsInput == null || AnimatedToggle == null || GridTransform == null || BlockStatePreview == null)
            {
                Debug.LogWarning("Editor is not properly loaded!");

                if (ScreenHeader != null)
                    ScreenHeader.text = "0.0 Editor not loaded";

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen(Test.Instance.ConfiguredModelFile));
        }

        public override void OnHide(ScreenManager manager)
        {
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();

        }

        private void ShowActiveCharSet(HashSet<char> charSet)
        {
            foreach (var mappingItem in mappingItems)
            {
                if (charSet.Contains(mappingItem.Character)) // This item is used in the current model. Show it
                    mappingItem.gameObject.SetActive(true);
                else // Hide it
                    mappingItem.gameObject.SetActive(false);
            }
        }

        private void SaveConfiguredModel()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                var model = ScriptableObject.CreateInstance(typeof (ConfiguredModel)) as ConfiguredModel;

                if (model is not null)
                {
                    model.Model = ModelDropdown!.options[ModelDropdown.value].text;

                    int.TryParse(SizeXInput!.text, out model.SizeX);
                    int.TryParse(SizeYInput!.text, out model.SizeY);
                    int.TryParse(SizeZInput!.text, out model.SizeZ);
                    int.TryParse(AmountInput!.text, out model.Amount);
                    int.TryParse(StepsInput!.text, out model.Steps);

                    model.Animated = AnimatedToggle!.isOn;
                    model.Seeds = SeedsInput!.text.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => int.Parse(x)).ToArray();
                    model.CustomMapping = mappingItems.Where(x => x.ShouldBeSaved()).Select(x => new CustomMappingItem()
                    {
                        Character = x.Character,
                        Color = ColorConvert.OpaqueColor32FromHexString(x.GetColorCode()),
                        BlockState = x.GetBlockState()
                    }).ToArray();
                    
                    ConfiguredModel.GetXMLDoc(model).Save($"{PathHelper.GetExtraDataFile("configured_models")}/{confModelFile}");
                }
                
                working = false;

                manager?.SetActiveScreenByType<HUDScreen>();

                Test.Instance.SetConfiguredModel(confModelFile);
            }

        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                manager.SetActiveScreenByType<HUDScreen>();

        }
    }
}