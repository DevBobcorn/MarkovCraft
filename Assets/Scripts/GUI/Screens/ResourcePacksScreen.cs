#nullable enable
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CraftSharp;

namespace MarkovCraft
{
    public class ResourcePacksScreen : BaseScreen
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public TMP_InputField? PacksFolderInput;
        [SerializeField] public Button? ApplyPacksButton;
        [SerializeField] public Button? OpenExplorerButton;

        // Resource Packs Panel
        [SerializeField] public RectTransform? GridTransform;

        private int dataVersion = 0;

        private bool working = false, properlyLoaded = false;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private IEnumerator InitializeScreen()
        {
            // Initialize settings panel
            var resPacksPath = PathHelper.GetPacksDirectory();
            PacksFolderInput!.text = resPacksPath;
            PacksFolderInput.enabled = false;
            if (CheckWindowsPlatform())
            {
                OpenExplorerButton!.onClick.RemoveAllListeners();
                OpenExplorerButton.onClick.AddListener(() => ShowExplorer(PacksFolderInput!.text.Replace("/", @"\")));
            }
            else // Hide this button
                OpenExplorerButton!.gameObject.SetActive(false);
            
            ApplyPacksButton!.onClick.RemoveAllListeners();
            ApplyPacksButton.onClick.AddListener(ApplyPacks);
            
            // Initialize resource packs panel
            /*
            for (var index = 0;index < result.ResultPalette.Length;index++)
            {
                var newItemObj = Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<ExportItem>();
                var itemVal = result.ResultPalette[index];
                // Add item to dictionary and set data
                mappingItems.Add(newItem);
                var rgb = ColorConvert.GetRGB(itemVal.Color);
                newItem.InitializeData(' ', rgb, rgb, itemVal.BlockState, ColorPicker!, BlockPicker!, BlockStatePreview!);
                // Add item to container
                newItem.transform.SetParent(GridTransform, false);
                newItem.transform.localScale = Vector3.one;
            } */

            yield return null;

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = WelcomeScene.GetL10nString("res_packs_manager.text.loaded");
            
            // Update Info text
            InfoText!.text = WelcomeScene.GetL10nString("screen.text.resource_info", "1.100.2", 456);
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = WelcomeScene.GetL10nString("screen.text.loading");
            
            var welcomeScene = WelcomeScene.Instance;
            //dataVersion = welcomeScene.GetDataVersionInt();

            StartCoroutine(InitializeScreen());
        }

        public override void OnHide(ScreenManager manager)
        {
            // The export palette is not destroyed. If exporter is screen is opened again
            // before the selected generation result is changed, the old export palette
            // containing cached mapping items will still be used
            
            /*
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();
            */
        }

        private void ApplyPacks()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                // Apply selected resource packs

                working = false;

                manager?.SetActiveScreenByType<GenerationScreen>();
            }
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                manager.SetActiveScreenByType<WelcomeScreen>();
            }
        }
    }
}