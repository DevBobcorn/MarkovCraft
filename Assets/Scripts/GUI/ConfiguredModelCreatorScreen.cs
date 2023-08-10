#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MarkovJunior;

namespace MarkovCraft
{
    public class ConfiguredModelCreatorScreen : BaseScreen
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        private const string CONFIGURED_MODEL_FOLDER = "configured_models";
        private const string DEFAULT_FILE_NAME_KEY = "creator.text.default_file_name";

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
        [SerializeField] public TMP_InputField? SaveNameInput;
        [SerializeField] public Button? SaveButton;
        // Models Panel
        [SerializeField] public GameObject? ModelItemPrefab;
        [SerializeField] public RectTransform? GridTransform;
        private readonly Dictionary<string, ModelItem> modelItems = new();
        private readonly Dictionary<string, (UnityEngine.Sprite, bool)> cachedModelPreviews = new();
        private string selectedModel = string.Empty;

        private bool working = false, properlyLoaded = false;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private void SelectModelItem(string modelName)
        {
            if (selectedModel == modelName)
            {
                // Target already selected
                return;
            }

            if (modelItems.ContainsKey(modelName))
            {
                // Select our target
                selectedModel = modelName;
                // Assign preset values
                (int px, int py, int pz, int ps, bool anim) = modelItems[modelName].PresetData;

                if (px > 0)
                    SizeXInput!.text = px.ToString();
                if (py > 0)
                    SizeYInput!.text = py.ToString();
                if (pz > 0)
                    SizeZInput!.text = pz.ToString();
                if (ps > 0)
                    StepsInput!.text = ps.ToString();
                
                AnimatedToggle!.isOn = anim;

                // Update text input
                ModelInput!.text = modelName;

                // Update conf model name
                //SaveNameInput!.text = $"{modelName}.xml";
            }
        }

        private IEnumerator InitializeScreen()
        {
            // Initialize settings panel
            //var modelDir = PathHelper.GetExtraDataFile("models");
            var modelsPath = PathHelper.GetExtraDataFile("models.xml");

            int index = 0;
            
            modelItems.Clear();
            foreach (Transform item in GridTransform!)
            {
                Destroy(item.gameObject);
            }

            int maxSteps = StepsInput!.GetComponent<IntegerInputValidator>().MaxValue;

            //foreach (var m in Directory.GetFiles(modelDir, "*.xml", SearchOption.TopDirectoryOnly))
            foreach (var melem in System.Xml.Linq.XDocument.Load(modelsPath).Root.Elements())
            {
                //var modelName = m[(modelDir.Length + 1)..^4];
                var modelName = melem.Get<string?>("Model", null);

                if (modelName == null)
                    continue;

                var modelItemObj = Instantiate(ModelItemPrefab)!;
                modelItemObj.transform.SetParent(GridTransform, false);

                var modelItem = modelItemObj.GetComponent<ModelItem>();
                int x = melem.Get<int>("SizeX");
                int y = melem.Get<int>("SizeY");
                int z = melem.Get("SizeZ", 1);
                int steps = melem.Get("Steps", maxSteps);
                bool animated = melem.Get("Animated", false);

                modelItem.SetModelData(modelName, x, y, z, steps, animated);
                modelItem.SetClickEvent(() => SelectModelItem(modelName));

                modelItems.Add(modelName, modelItem);
                
                index++;
            }

            if (index > 0) // If the model list is not empty
            {
                // Select first model
                var pair = modelItems.First();
                SelectModelItem(pair.Key);
                pair.Value.VisualSelect();
            }

            yield return null;

            SaveNameInput!.text = GameScene.GetL10nString(DEFAULT_FILE_NAME_KEY);
            SaveButton!.onClick.RemoveAllListeners();
            SaveButton.onClick.AddListener(SaveConfiguredModel);

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("creator.text.loaded");

            yield return new WaitForSecondsRealtime(0.3F);

            StartCoroutine(LoadPreviews());
        }

        private IEnumerator LoadPreviews()
        {
            var prevDir = PathHelper.GetExtraDataFile("model_previews");
            var prev3dDir = PathHelper.GetExtraDataFile($"model_previews{SP}3d");
            var pairs = modelItems.ToArray();

            var wait = new WaitForSecondsRealtime(0.03F);

            foreach (var pair in pairs)
            {
                var modelName = pair.Key;

                // See if preview is cached
                if (cachedModelPreviews.ContainsKey(modelName))
                {
                    (var sprite, var is3d) = cachedModelPreviews[modelName];
                    pair.Value.SetPreviewSprite(sprite, is3d);
                    continue;
                }

                var prevPath = prevDir + $"{SP}{modelName}.png";
                bool is3dPrev = false;

                bool prevFound = File.Exists(prevPath);

                if (!prevFound) // 2d preview not found, Check 3d preview
                {
                    prevPath = prev3dDir + $"{SP}{modelName}.png";
                    prevFound = File.Exists(prevPath);

                    if (prevFound) is3dPrev = true;
                }

                // See if preview is available
                if (prevFound)
                {
                    var tex = new Texture2D(2, 2);
                    //tex.filterMode = FilterMode.Point;
                    var bytes = File.ReadAllBytes(prevPath);
                    tex.LoadImage(bytes);
                    // Update sprite
                    var sprite = UnityEngine.Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));

                    if (pair.Value == null) // Item got destroyed, screen might be closed already
                    {
                        yield break;
                    }

                    pair.Value.SetPreviewSprite(sprite, is3dPrev);
                    cachedModelPreviews.Add(modelName, (sprite, is3dPrev));

                    yield return wait;
                }
            }
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = GameScene.GetL10nString("screen.text.loading");

            StartCoroutine(InitializeScreen());
        }

        private void SaveConfiguredModel()
        {
            if (working) return;

            // File name to save to, and the file to load after saving
            var saveFileName = GameScene.GetL10nString(DEFAULT_FILE_NAME_KEY);

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                var model = ScriptableObject.CreateInstance(typeof (ConfiguredModel)) as ConfiguredModel;
                if (model is not null)
                {
                    model.Model = ModelInput!.text;

                    int.TryParse(SizeXInput!.text, out model.SizeX);
                    int.TryParse(SizeYInput!.text, out model.SizeY);
                    int.TryParse(SizeZInput!.text, out model.SizeZ);
                    int.TryParse(AmountInput!.text, out model.Amount);
                    int.TryParse(StepsInput!.text, out model.Steps);
                    model.Animated = AnimatedToggle!.isOn;
                    int.TryParse(StepsPerRefreshInput!.text, out model.StepsPerRefresh);
                    model.Seeds = SeedsInput!.text.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => int.Parse(x)).ToArray();
                    
                    var savePath = PathHelper.GetExtraDataFile(CONFIGURED_MODEL_FOLDER);
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

                manager?.SetActiveScreenByType<GenerationScreen>();
                game.UpdateConfiguredModel(saveFileName);
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