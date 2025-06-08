#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CraftSharp;

namespace MarkovCraft
{
    public class ConfiguredModelEditorScreen : BaseScreen
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        private const string CONFIGURED_MODEL_FOLDER = "configured_models";

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
        [SerializeField] public TMP_InputField? StepsPerRefreshInput;
        [SerializeField] public TMP_InputField? SaveNameInput;
        [SerializeField] public Button? SaveButton;
        // Mapping Items Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;
        // BlockState Preview
        [SerializeField] public BlockStatePreview? BlockStatePreview;
        // Color Picker
        [SerializeField] public MappingItemColorPicker? ColorPicker;
        // Block Picker
        [SerializeField] public MappingItemBlockPicker? BlockPicker;
        // Auto Mapping Panel
        [SerializeField] public AutoMappingPanel? AutoMappingPanel;

        private readonly List<MappingItem> mappingItems = new();
        // Items in this dictionary share references with generation scene's fullPaletteAsLoaded
        // Its CustomMappingItem objects SHOULD NOT be edited
        private Dictionary<char, CustomMappingItem> fullPaletteAsLoaded = new();
        // Base color palette, loaded in and taken from generation scene
        private Dictionary<char, int> baseColorPalette = new();
        private bool working = false, properlyLoaded = false;
        // Current configured model, loaded in and taken from generation scene
        private ConfiguredModel? confModel = null;
        private string confModelFile = string.Empty;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private IEnumerator InitializeScreen(string initConfModelFile)
        {
            // Initialize settings panel
            var dir = MarkovGlobal.GetDataFile("models");
            int index = 0, selectedIndex = -1;
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                var modelName = m[(dir.Length + 1)..^4];
                options.Add(new(modelName));

                if (modelName.Equals(confModel!.Model))
                    selectedIndex = index;
                
                index++;
            }
            
            ModelDropdown!.ClearOptions();
            ModelDropdown.AddOptions(options);

            ModelDropdown.onValueChanged.RemoveAllListeners();
            ModelDropdown.onValueChanged.AddListener(UpdateDropdownOption);

            if (selectedIndex != -1)
                ModelDropdown.value = selectedIndex;
            
            SizeXInput!.text = confModel!.SizeX.ToString();
            SizeYInput!.text = confModel.SizeY.ToString();
            SizeZInput!.text = confModel.SizeZ.ToString();

            AmountInput!.text = confModel.Amount.ToString();
            StepsInput!.text = confModel.Steps.ToString();

            SeedsInput!.text = confModel.Seeds.Length > 0 ? string.Join(' ', confModel.Seeds) : string.Empty;
            
            AnimatedToggle!.isOn = confModel.Animated;
            StepsPerRefreshInput!.text = confModel.StepsPerRefresh.ToString();

            SaveNameInput!.text = initConfModelFile;
            SaveButton!.onClick.RemoveAllListeners();
            SaveButton.onClick.AddListener(SaveConfiguredModel);

            // Initialize mappings panel
            
            // Populate mapping item grid

            // All mapping items, whether included in the currently selected model
            // or not, should be populated. This is because the selected MJ model
            // can be changed, and the active items should be updated accordingly.
            foreach (var pair in fullPaletteAsLoaded)
            {
                var newItemObj = Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingItem>();

                mappingItems.Add(newItem);

                int defaultRgb = baseColorPalette[pair.Key];
                int loadedRgb = ColorConvert.GetRGB(pair.Value.Color);

                newItem.InitializeData(pair.Key, defaultRgb, loadedRgb, pair.Value.BlockState,
                        ColorPicker!, BlockPicker!, BlockStatePreview!);

                newItem.transform.SetParent(GridTransform, false);
                newItem.transform.localScale = Vector3.one;
            }

            // Update which items should be displayed (included in selected model)
            yield return StartCoroutine(UpdateActiveCharSetFromModel(confModel.Model));

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("editor.text.loaded", initConfModelFile);
        }

        private IEnumerator UpdateActiveCharSetFromModel(string modelName)
        {
            working = true;

            string modelFileName = MarkovGlobal.GetDataFile($"models{SP}{modelName}.xml");
            XDocument? modelDoc = null;

            if (File.Exists(modelFileName))
            {
                var fs2 = new FileStream(modelFileName, FileMode.Open);
                var task2 = XDocument.LoadAsync(fs2, LoadOptions.SetLineInfo, CancellationToken.None);

                while (!task2.IsCompleted)
                    yield return null;
                
                fs2.Close();
                
                if (task2.IsCompletedSuccessfully)
                    modelDoc = task2.Result;
            }
            
            if (modelDoc == null)
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

            var modelName = ModelDropdown!.options[newValue].text;
            if (modelName == null) return;

            // Update which items should be displayed (included in selected model)
            StartCoroutine(UpdateActiveCharSetFromModel(modelName));
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = GameScene.GetL10nString("editor.text.loading");


            if (GameScene.Instance is not GenerationScene game)
            {
                Debug.LogError("Wrong game scene!");
                working = false;
                return;
            }

            confModel = game.ConfiguredModel;
            confModelFile = game.ConfiguredModelFile;
            baseColorPalette = game.GetBaseColorPalette();
            fullPaletteAsLoaded = game.GetFullPaletteAsLoaded();
            
            if (!confModel)
            {
                Debug.LogWarning("Editor is not properly loaded!");

                ScreenHeader!.text = GameScene.GetL10nString("screen.text.load_failure");

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen(game.ConfiguredModelFile));
        }

        public override void OnHide(ScreenManager manager)
        {
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();

            // Hide auto mapping panel
            AutoMappingPanel!.Hide();
            // Hide color picker
            ColorPicker!.CloseAndDiscard();
            // Hide block picker
            BlockPicker!.CloseAndDiscard();
        }

        private void ShowActiveCharSet(HashSet<char> charSet)
        {
            foreach (var mappingItem in mappingItems)
            {
                // Show item if it is used in the current model
                mappingItem.gameObject.SetActive(charSet.Contains(mappingItem.Character));
            }
        }

        public void AutoMap()
        {
            // Select only active mapping items
            var activeMappingItems = mappingItems.Where(x => x.gameObject.activeSelf).ToList();
            // Do auto mapping
            AutoMappingPanel!.AutoMap(activeMappingItems);
            // Hide auto mapping panel
            AutoMappingPanel!.Hide();
        }

        private void SaveConfiguredModel()
        {
            if (working) return;

            // File name to save to, and the file to load after saving
            var saveFileName = confModelFile;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                if (ScriptableObject.CreateInstance(typeof (ConfiguredModel)) is ConfiguredModel model)
                {
                    model.Model = ModelDropdown!.options[ModelDropdown.value].text;

                    int.TryParse(SizeXInput!.text, out model.SizeX);
                    int.TryParse(SizeYInput!.text, out model.SizeY);
                    int.TryParse(SizeZInput!.text, out model.SizeZ);
                    int.TryParse(AmountInput!.text, out model.Amount);
                    int.TryParse(StepsInput!.text, out model.Steps);
                    model.Animated = AnimatedToggle!.isOn;
                    int.TryParse(StepsPerRefreshInput!.text, out model.StepsPerRefresh);
                    model.Seeds = SeedsInput!.text.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).Select(int.Parse).ToArray();
                    
                    model.CustomMapping = mappingItems.Where(x => x.ShouldBeSaved()).Select(x => new CustomMappingItem()
                    {
                        Symbol = x.Character,
                        Color = ColorConvert.OpaqueColor32FromHexString(x.GetColorCode()),
                        BlockState = x.GetBlockState()
                    }).ToArray();
                    
                    var savePath = MarkovGlobal.GetDataFile(CONFIGURED_MODEL_FOLDER);
                    var specifiedName = SaveNameInput!.text;

                    if (GameScene.CheckFileName(specifiedName))
                    {
                        saveFileName = specifiedName;
                    }

                    ConfiguredModel.GetXMLDoc(model).Save($"{savePath}{SP}{saveFileName}");
                }

                if (GameScene.Instance is not GenerationScene game)
                {
                    Debug.LogError("Wrong game scene!");
                    working = false;
                    return;
                }

                working = false;

                manager!.SetActiveScreenByType<GenerationScreen>();
                game.UpdateConfiguredModel(saveFileName);
            }
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                manager.SetActiveScreenByType<GenerationScreen>();
            }
        }
    }
}