#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MinecraftClient;

namespace MarkovCraft
{
    public class ExporterScreen : BaseScreen
    {
        private const string EXPORT_PATH_KEY = "ExportPath";
        private const string EXPORT_FORMAT_KEY = "ExportFormat";

        private static readonly string[] EXPORT_FORMAT_KEYS = {
            "exporter.format.nbt_structure",
            "exporter.format.mcfunction",
            "exporter.format.vox_model"
        };

        private static readonly string[] EXPORT_FORMAT_EXT_NAMES = {
            "nbt",
            "mcfunction",
            "vox"
        };

        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public TMP_InputField? ExportFolderInput, ExportNameInput;
        [SerializeField] public Button? ExportButton, ApplyMappingButton;
        [SerializeField] public Button? OpenExplorerButton;
        [SerializeField] public TMP_Dropdown? ExportFormatDropdown;
        // Result Preview
        [SerializeField] public Image? ResultPreviewImage;
        // Mapping Items Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;
        // BlockState Preview
        [SerializeField] public BlockStatePreview? BlockStatePreview;
        // Color Picker
        [SerializeField] public MappingItemColorPicker? ColorPicker;
        // Result Detail Panel
        [SerializeField] public ResultDetailPanel? ResultDetailPanel;
        // Auto Mapping Panel
        [SerializeField] public AutoMappingPanel? AutoMappingPanel;

        private (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ, int steps)? exportData;
        // Items in this dictionary share refereces with generation scene's fullPaletteForEditing
        // If items get changed, it'll also be reflected in other scenes
        private Dictionary<char, CustomMappingItem>? exportPalette;
        private readonly HashSet<char> minimumCharSet = new();
        private readonly List<MappingItem> mappingItems = new();
        private bool working = false, properlyLoaded = false;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private bool CheckWindows() => Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer;

        private IEnumerator InitializeScreen(HashSet<char> minimumCharSet)
        {
            if (exportData is null || exportPalette is null)
            {
                Debug.LogWarning($"ERROR: Export screen not correctly initialized!");
                working = false;
                properlyLoaded = false;
                yield break;
            }

            var data = exportData.Value;
            bool is2d = data.FZ == 1;

            // Initialize settings panel
            var savedExportPath = PlayerPrefs.GetString(EXPORT_PATH_KEY, PathHelper.GetDefaultExportPath());
            ExportFolderInput!.text = savedExportPath;
            if (CheckWindows())
            {
                OpenExplorerButton!.onClick.RemoveAllListeners();
                OpenExplorerButton.onClick.AddListener(ShowExplorer);
            }
            else // Hide this button
                OpenExplorerButton!.gameObject.SetActive(false);
            
            ExportButton!.onClick.RemoveAllListeners();
            ExportButton.onClick.AddListener(Export);
            ApplyMappingButton!.onClick.RemoveAllListeners();
            ApplyMappingButton.onClick.AddListener(ApplyMappings);

            ExportFormatDropdown!.ClearOptions();
            ExportFormatDropdown.AddOptions(EXPORT_FORMAT_KEYS.Select(x =>
                    new TMP_Dropdown.OptionData(GameScene.GetL10nString(x))).ToList());
            ExportFormatDropdown!.onValueChanged.RemoveAllListeners();
            ExportFormatDropdown!.onValueChanged.AddListener(UpdateExportFileName);

            ResultDetailPanel!.Hide();
            
            // Initialize mappings panel
            // Populate mapping item grid
            foreach (var ch in minimumCharSet)
            {
                var newItemObj = Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingItem>();

                mappingItems.Add(newItem);

                var itemVal = exportPalette[ch];
                var rgb = ColorConvert.GetRGB(itemVal.Color);

                newItem.InitializeData(ch, rgb, rgb, itemVal.BlockState, ColorPicker!, BlockStatePreview!);

                newItem.transform.SetParent(GridTransform, false);
                newItem.transform.localScale = Vector3.one;
            }

            yield return null;

            // The character chosen to be air block (not used when 'is2d' equals true)
            var airCharacter = data.legend[0];

            foreach (var item in mappingItems)
            {
                if (!is2d && item.Character == airCharacter)
                {
                    item.gameObject.transform.SetAsLastSibling();
                    item.TagAsSpecial("minecraft:air");
                }
            }

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("exporter.text.loaded", data.info[0]);
            
            // Update Info text
            InfoText!.text = GameScene.GetL10nString("export.text.result_info", data.info[0], data.info[1], data.FX, data.FY, data.FZ);
            var prev = GetPreviewData();

            // Update selected format (and also update default export file name)
            var lastUsedFormatIndex = PlayerPrefs.GetInt(EXPORT_FORMAT_KEY, 0);
            ExportFormatDropdown.value = lastUsedFormatIndex;
            UpdateExportFileName(lastUsedFormatIndex);

            // Update Preview Image
            var (pixels, sizeX, sizeY) = ResultDetailPanel.RenderPreview(prev.sizeX, prev.sizeY, prev.sizeZ,
                    prev.state, prev.colors, is2d ? ResultDetailPanel.PreviewRotation.ZERO : ResultDetailPanel.PreviewRotation.NINETY);
            var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, sizeX, sizeY);
            //tex.filterMode = FilterMode.Point;
            // Update sprite
            var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
            ResultPreviewImage!.sprite = sprite;
            ResultPreviewImage!.SetNativeSize();
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = GameScene.GetL10nString("exporter.text.loading");
            
            var game = GameScene.Instance as GenerationScene;

            if (game is null)
            {
                Debug.LogError("Wrong game scene!");
                working = false;
                return;
            }
            
            // Get selected result data
            exportData = game.GetSelectedResultData();
            // Get export palette
            minimumCharSet.Clear();
            
            if (exportData is not null)
            {
                // Find out characters that appeared in the final result
                var finalLegend = exportData.Value.legend;
                var byteVals = exportData.Value.state.ToHashSet();

                foreach (var v in byteVals)
                    minimumCharSet.Add(finalLegend[v]);

                // Final legend and export palette can contain a few unused entries
                exportPalette = game.GetPartialPaletteForEditing(finalLegend.ToHashSet());
            }
            
            if (exportData is null || exportPalette is null)
            {
                Debug.LogWarning("Exporter is not properly loaded!");

                ScreenHeader!.text = GenerationScene.GetL10nString("exporter.text.load_failure");

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen(minimumCharSet));
        }

        public override void OnHide(ScreenManager manager)
        {
            // The export palette is not destroyed. If exporter is screen is opened again
            // before the selected generation result is changed, the old export palette
            // containing cached mapping items will still be used
            
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();
        }

        public void AutoMap()
        {
            // Do auto mapping
            AutoMappingPanel!.AutoMap(mappingItems);
            // Hide auto mapping panel
            AutoMappingPanel!.Hide();
        }

        public (int sizeX, int sizeY, int sizeZ, byte[] state, int[] colors) GetPreviewData()
        {
            var data = exportData!.Value;
            return (data.FX, data.FY, data.FZ, data.state, data.legend.Select(
                    x => ColorConvert.GetOpaqueRGB(exportPalette![x].Color)).ToArray());
        }

        private void ApplyMappings()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                // Apply export palette overrides
                mappingItems.ForEach(x => {
                    if (exportPalette!.ContainsKey(x.Character))
                    {
                        var item = exportPalette[x.Character];
                        item.Color = ColorConvert.OpaqueColor32FromHexString(x.GetColorCode());
                        item.BlockState = x.GetBlockState();
                    }
                });

                working = false;

                manager?.SetActiveScreenByType<GenerationScreen>();
            }
        }

        public string GetDefaultExportBaseName()
        {
            if (properlyLoaded)
            {
                var info = exportData!.Value.info;
                return $"{info[0][0..^4]}_{info[1]}";
            }

            return "exported";
        }

        public string? GetExportPath()
        {
            return ExportFolderInput?.text;
        }

        public static bool CheckFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            
            return true;
        }

        private void UpdateExportFileName(int selectedFormatIndex)
        {
            if (properlyLoaded)
            {
                var info = exportData!.Value.info;

                var baseName = $"{info[0][0..^4]}_{info[1]}";
                var extName = EXPORT_FORMAT_EXT_NAMES[selectedFormatIndex];

                ExportNameInput!.text = $"{baseName}.{extName}";
            }
        }

        private void Export()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                var path = ExportFolderInput!.text;
                var data = exportData!.Value;
                var fileName = ExportNameInput!.text;

                var dirInfo = new DirectoryInfo(path);

                if (!dirInfo.Exists)
                {
                    try {
                        dirInfo.Create();
                    }
                    catch (IOException e)
                    {
                        Debug.LogWarning($"ERROR: Failed to create export folder: {e}");
                        working = false;
                        return;
                    }
                }

                if (!CheckFileName(fileName))
                {
                    Debug.LogWarning($"ERROR: Invailid file name: {fileName}");
                    working = false;
                    return;
                }
                
                // SavePath is vaild, save it
                PlayerPrefs.SetString(EXPORT_PATH_KEY, dirInfo.FullName);

                var formatIndex = ExportFormatDropdown!.value;

                // Save last used export format
                PlayerPrefs.SetInt(EXPORT_FORMAT_KEY, formatIndex);

                // Apply export palette overrides
                mappingItems.ForEach(x => {
                    if (exportPalette!.ContainsKey(x.Character))
                    {
                        var item = exportPalette[x.Character];
                        item.Color = ColorConvert.OpaqueColor32FromHexString(x.GetColorCode());
                        item.BlockState = x.GetBlockState();
                    }
                });

                switch (formatIndex)
                {
                    case 0: // nbt structure
                        NbtStructureExporter.Export(data.state, data.legend, data.FX, data.FY, data.FZ, exportPalette!, dirInfo, fileName, minimumCharSet, 2586);
                        break;
                    case 1: // mcfunction
                        McFuncExporter.Export(data.state, data.legend, data.FX, data.FY, data.FZ, exportPalette!, dirInfo, fileName);
                        break;
                    case 2: // vox model
                        VoxModelExporter.Export(data.state, data.legend, data.FX, data.FY, data.FZ, exportPalette!, dirInfo, fileName);
                        break;
                }
                
                working = false;

                manager?.SetActiveScreenByType<GenerationScreen>();
            }
        }

        public void ShowExplorer()
        {
            if (CheckWindows())
                System.Diagnostics.Process.Start("explorer.exe", $"/select,{ExportFolderInput!.text.Replace("/", @"\")}");
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                manager.SetActiveScreenByType<GenerationScreen>();

        }
    }
}