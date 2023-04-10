#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
                    AmountInput == null || StepsInput == null || SeedsInput == null || AnimatedToggle == null)
            {
                Debug.LogWarning("The editor is not properly loaded!");
                return;
            }

            ScreenHeader.text = game.ConfiguredModelName;

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

            foreach (var item in currentConfModel.CustomRemapping)
            {
                var newItemObj = GameObject.Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingItem>();

                mappingItems.Add(newItem);

                newItem.InitializeData(item.Symbol, ColorConvert.GetRGB(item.RemapColor), item.RemapTarget);

                newItem.transform.SetParent(GridTransform);
                newItem.transform.localScale = Vector3.one;
            }
            

            properlyLoaded = true;
        }

        public override void OnHide(ScreenManager manager)
        {
            ClearItems();

        }

        private void ClearItems()
        {
            loadedModels.Clear();

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