#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml.Linq;

using MarkovJunior;
using MinecraftClient;

namespace MarkovCraft
{
    public class ConfiguredModelCreatorScreen : BaseScreen
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        private const string CONFIGURED_MODEL_FOLDER = "configured_models";

        [SerializeField] public TMP_Text? ScreenHeader;
        // Settings Panel
        [SerializeField] public TMP_InputField? ModelInput;
        [SerializeField] public TMP_InputField? SizeXInput;
        [SerializeField] public TMP_InputField? SizeYInput;
        [SerializeField] public TMP_InputField? SizeZInput;
        [SerializeField] public TMP_InputField? AmountInput;
        [SerializeField] public TMP_InputField? StepsInput;
        [SerializeField] public TMP_InputField? SeedsInput;
        [SerializeField] public Toggle? AnimatedToggle;
        [SerializeField] public TMP_InputField? StepsPerRefreshInput;
        [SerializeField] public Button? SaveButton;
        // Models Panel
        [SerializeField] public GameObject? ModelItemPrefab;
        [SerializeField] public RectTransform? GridTransform;
        private readonly Dictionary<string, ModelItem> modelItems = new();

        private bool working = false, properlyLoaded = false;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private string AddSpacesBeforeUppercase(string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) && 
                        i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        private IEnumerator InitializeScreen()
        {
            // Initialize settings panel
            var dir = PathHelper.GetExtraDataFile("models");
            int index = 0;
            
            modelItems.Clear();
            foreach (Transform item in GridTransform!)
            {
                Destroy(item.gameObject);
            }

            foreach (var m in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                var modelName = m[(dir.Length + 1)..^4];
                var modelItemObj = Instantiate(ModelItemPrefab)!;
                modelItemObj.transform.SetParent(GridTransform, false);

                var modelItem = modelItemObj.GetComponent<ModelItem>();
                modelItem.SetModelName(AddSpacesBeforeUppercase(AddSpacesBeforeUppercase(modelName)));

                modelItems.Add(modelName, modelItem);
                
                index++;
            }

            yield return null;

            //if (selectedIndex != -1)
            //    ModelDropdown.value = selectedIndex;
            
            /*
            SizeXInput!.text = confModel!.SizeX.ToString();
            SizeYInput!.text = confModel.SizeY.ToString();
            SizeZInput!.text = confModel.SizeZ.ToString();

            AmountInput!.text = confModel.Amount.ToString();
            StepsInput!.text = confModel.Steps.ToString();

            if (confModel.Seeds.Length > 0)
                SeedsInput!.text = string.Join(' ', confModel.Seeds);
            else
                SeedsInput!.text = string.Empty;
            
            AnimatedToggle!.isOn = confModel.Animated;
            StepsPerRefreshInput!.text = confModel.StepsPerRefresh.ToString();
            */

            SaveButton!.onClick.RemoveAllListeners();
            SaveButton.onClick.AddListener(SaveConfiguredModel);

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("creator.text.loaded");
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = GameScene.GetL10nString("creator.text.loading");

            StartCoroutine(InitializeScreen());
        }

        public override void OnHide(ScreenManager manager)
        {
            
        }

        private void SaveConfiguredModel()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                var model = ScriptableObject.CreateInstance(typeof (ConfiguredModel)) as ConfiguredModel;
                var confModelFile = "TODO.xml";

                if (model is not null)
                {
                    //model.Model = ModelDropdown!.options[ModelDropdown.value].text;
                    model.Model = ModelInput!.text;

                    int.TryParse(SizeXInput!.text, out model.SizeX);
                    int.TryParse(SizeYInput!.text, out model.SizeY);
                    int.TryParse(SizeZInput!.text, out model.SizeZ);
                    int.TryParse(AmountInput!.text, out model.Amount);
                    int.TryParse(StepsInput!.text, out model.Steps);
                    model.Animated = AnimatedToggle!.isOn;
                    int.TryParse(StepsPerRefreshInput!.text, out model.StepsPerRefresh);
                    model.Seeds = SeedsInput!.text.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => int.Parse(x)).ToArray();
                    // No custom mappings
                    model.CustomMapping = new CustomMappingItem[] { };
                    
                    ConfiguredModel.GetXMLDoc(model).Save($"{PathHelper.GetExtraDataFile(CONFIGURED_MODEL_FOLDER)}/{confModelFile}");
                }

                var game = GameScene.Instance as GenerationScene;

                if (game is null)
                {
                    Debug.LogError("Wrong game scene!");
                    working = false;
                    return;
                }
                
                working = false;

                manager?.SetActiveScreenByType<GenerationScreen>();

                game.SetConfiguredModel(confModelFile);
            }

        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                manager.SetActiveScreenByType<GenerationScreen>();

        }
    }
}