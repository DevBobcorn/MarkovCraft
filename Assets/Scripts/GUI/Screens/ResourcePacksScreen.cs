#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CraftSharp;
using System.Linq;

namespace MarkovCraft
{
    public class ResourcePacksScreen : BaseScreen
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        // Vanilla resource pack name prefix e.g. vanilla-1.16.5
        private static readonly string VANILLA_PREFIX = "vanilla-1.";

        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public TMP_InputField? PacksFolderInput;
        [SerializeField] public Button? ApplyPacksButton;
        [SerializeField] public Button? OpenExplorerButton;

        // Resource Packs Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? ResourcePackItemPrefab;

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

            // Collect available pack overrides (filter out vanilla resources for other versions)
            var selectedResVersion = WelcomeScene.Instance.GetResourceVersion();
            var selectedPackFormat = WelcomeScene.Instance.GetResPackFormatInt();
            //Debug.Log($"Base resource: {selectedResVersion}");

            var packNames = Directory.GetDirectories(PathHelper.GetPacksDirectory())
                    .Select(x => new DirectoryInfo(x).Name)
                    .Where(x => !x.StartsWith(VANILLA_PREFIX) || x == $"vanilla-{selectedResVersion}");
            
            // Clean up
            foreach(Transform child in GridTransform!)
            {
                Destroy(child.gameObject);
            }

            var packDict = new Dictionary<string, ResourcePackItem>();
            
            // Initialize resource packs panel
            foreach (var packName in packNames)
            {
                var newItemObj = Instantiate(ResourcePackItemPrefab);
                var newItem = newItemObj!.GetComponent<ResourcePackItem>();

                // Add item to dictionary and set data
                //mappingItems.Add(newItem);

                // Get pack info
                var packMetaPath = PathHelper.GetPackFile(packName, "pack.mcmeta");
                if (!File.Exists(packMetaPath))
                {
                    continue; // Not a valid resource pack
                }
                string meta = File.ReadAllText(packMetaPath);
                Json.JSONData metaData = Json.ParseJson(meta);

                var packFormat = "0";
                var description = "<No description>";

                if (metaData.Properties.TryGetValue("pack", out var packData))
                {
                    if (packData.Properties.TryGetValue("pack_format", out var packFormatData))
                    {
                        packFormat = packFormatData.StringValue;
                        description = string.Empty;

                        if (packData.Properties.TryGetValue("description", out var descriptionData))
                        {
                            description = descriptionData.StringValue;
                        }
                    }
                    else
                    {
                        continue; // Not a valid resource pack
                    }
                }

                // Get pack icon
                Sprite? sprite = null;
                var packPngPath = PathHelper.GetPackFile(packName, "pack.png");
                if (File.Exists(packPngPath))
                {
                    var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                    var bytes = File.ReadAllBytes(packPngPath);
                    tex.LoadImage(bytes);

                    sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2F, tex.height / 2F));
                }

                // Initialize item data
                newItem.SetPackData(packName, packFormat, description, sprite);
                
                // Add item to container
                newItem.transform.SetParent(GridTransform, false);
                newItem.transform.localScale = Vector3.one;
                packDict.Add(packName.StartsWith(VANILLA_PREFIX) ?
                        MarkovGlobal.VANILLA_RESPACK_SYMBOL : packName, newItem);
            }

            // Update selected packs in loading order [Base -> Overrides]
            var selectedResPacks = MarkovGlobal.LoadSelectedResPacks();
            foreach (var pack in selectedResPacks)
            {
                if (packDict.ContainsKey(pack))
                {
                    packDict[pack].SelectPack(false);
                    // Move this pack to top
                    packDict[pack].transform.SetAsFirstSibling();
                }
            }

            yield return null;

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = WelcomeScene.GetL10nString("res_packs_manager.text.loaded");
            
            // Update Info text
            InfoText!.text = WelcomeScene.GetL10nString("screen.text.resource_info", selectedResVersion, selectedPackFormat);
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = WelcomeScene.GetL10nString("screen.text.loading");

            StartCoroutine(InitializeScreen());
        }

        public override void OnHide(ScreenManager manager)
        {
            // Clean up
            foreach(Transform child in GridTransform!)
            {
                Destroy(child.gameObject);
            }
        }

        private void ApplyPacks()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                // Apply selected resource packs
                var updatedPacks = new List<string>();

                for (int i = GridTransform!.childCount - 1; i >= 0; i--)
                {
                    var item = GridTransform.GetChild(i).GetComponent<ResourcePackItem>();
                    if (item.Selected)
                    {
                        updatedPacks.Add(item.PackName.StartsWith(VANILLA_PREFIX) ?
                                MarkovGlobal.VANILLA_RESPACK_SYMBOL : item.PackName);
                    }
                }

                MarkovGlobal.SaveSelectedResPacks(updatedPacks.ToArray());

                working = false;

                manager!.SetActiveScreenByType<WelcomeScreen>();
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