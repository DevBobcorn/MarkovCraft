#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class ResultExporterScreen : BaseScreen
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        private const string EXPORT_PATH_KEY = "ExportPath";
        private const string EXPORT_FORMAT_KEY = "ExportFormat";

        private static readonly string[] EXPORT_FORMAT_KEYS = {
            "exporter.format.sponge_schem",
            "exporter.format.nbt_structure",
            "exporter.format.mcfunction",
            "exporter.format.vox_model"
        };

        private static readonly string[] EXPORT_FORMAT_EXT_NAMES = {
            "schem",      "nbt",
            "mcfunction", "vox"
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

        private GenerationResult? exportResult = null;
        private int exportDataVersion = 0;
        // Result palette index => mapping item
        private readonly List<ExportItem> mappingItems = new();
        private bool working = false, properlyLoaded = false;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private bool CheckWindows() => Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer;

        private IEnumerator InitializeScreen()
        {
            if (exportResult is null)
            {
                Debug.LogWarning($"ERROR: Export screen not correctly initialized!");
                working = false;
                properlyLoaded = false;
                yield break;
            }

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
            for (var index = 0;index < exportResult.ResultPalette.Length;index++)
            {
                var newItemObj = Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<ExportItem>();
                var itemVal = exportResult.ResultPalette[index];
                // Add item to dictionary and set data
                mappingItems.Add(newItem);
                var rgb = ColorConvert.GetRGB(itemVal.Color);
                newItem.InitializeData(' ', rgb, rgb, itemVal.BlockState, ColorPicker!, BlockStatePreview!);
                // Add item to container
                newItem.transform.SetParent(GridTransform, false);
                newItem.transform.localScale = Vector3.one;
            }

            yield return null;
            // Mark air items
            foreach (var airIndex in exportResult.AirIndices)
            {
                var item = mappingItems[airIndex];
                item.gameObject.transform.SetAsLastSibling();
                item.TagAsSpecial("minecraft:air");
            }

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("exporter.text.loaded", exportResult.ConfiguredModelName);
            
            // Update Info text
            InfoText!.text = GameScene.GetL10nString("export.text.result_info", exportResult.ConfiguredModelName,
                    exportResult.GenerationSeed, exportResult.SizeX, exportResult.SizeY, exportResult.SizeZ);
            var prev = GetResultPreviewData();

            // Update selected format (and also update default export file name)
            var lastUsedFormatIndex = PlayerPrefs.GetInt(EXPORT_FORMAT_KEY, 0);
            ExportFormatDropdown.value = lastUsedFormatIndex;
            UpdateExportFileName(lastUsedFormatIndex);

            // Update Preview Image
            var (pixels, sizeX, sizeY) = ResultDetailPanel.RenderPreview(prev.sizeX, prev.sizeY, prev.sizeZ,
                    prev.blockData, prev.colors, prev.airIndices, exportResult.SizeZ == 1 ? ResultDetailPanel.PreviewRotation.ZERO : ResultDetailPanel.PreviewRotation.NINETY);
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
            
            if (GameScene.Instance is not GenerationScene game)
            {
                Debug.LogError("Wrong game scene!");
                working = false;
                return;
            }
            
            // Get selected result data
            exportResult = game.GetSelectedResult();
            exportDataVersion = game.GetDataVersionInt();
            
            if (exportResult is null)
            {
                Debug.LogWarning("Exporter is not properly loaded!");

                ScreenHeader!.text = GenerationScene.GetL10nString("exporter.text.load_failure");

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen());
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

            // Clear export result
            exportResult = null;
        }

        public void AutoMap()
        {
            // Do auto mapping
            AutoMappingPanel!.AutoMap(mappingItems.Select(x => x as MappingItem).ToList());
            // Hide auto mapping panel
            AutoMappingPanel!.Hide();
        }

        public (int sizeX, int sizeY, int sizeZ, int[] blockData, int[] colors, HashSet<int> airIndices) GetResultPreviewData()
        {
            return exportResult!.GetPreviewData();
        }

        private void ApplyMappings()
        {
            if (working) return;

            if (properlyLoaded && exportResult != null) // The editor is properly loaded
            {
                working = true;

                var resultPalette = exportResult.ResultPalette;
                var updatedEntries = new HashSet<int>();
                // Apply export palette overrides
                for (int index = 0;index < resultPalette.Length;index++)
                {
                    var item = mappingItems[index];
                    var itemVal = resultPalette[index];

                    var newColor = ColorConvert.OpaqueRGBFromHexString(item.GetColorCode());
                    var newBlockState = item.GetBlockState();

                    // Color32s are directly comparable so we have to convert them to rgb int first
                    if (ColorConvert.GetOpaqueRGB(itemVal.Color) != newColor || itemVal.BlockState != newBlockState)
                    {
                        itemVal.Color = ColorConvert.GetOpaqueColor32(newColor);
                        itemVal.BlockState = newBlockState;

                        updatedEntries.Add(index);
                    }
                }

                // Rebuild result mesh to reflect mapping changes
                exportResult.RequestRebuildResultMesh(updatedEntries);

                working = false;

                manager?.SetActiveScreenByType<GenerationScreen>();
            }
        }

        public string GetDefaultExportBaseName()
        {
            if (properlyLoaded)
            {
                return $"{exportResult!.ConfiguredModelName[0..^4]}_{exportResult!.GenerationSeed}";
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
                var baseName = $"{exportResult!.ConfiguredModelName[0..^4]}_{exportResult!.GenerationSeed}";
                var extName = EXPORT_FORMAT_EXT_NAMES[selectedFormatIndex];

                ExportNameInput!.text = $"{baseName}.{extName}";
            }
        }

        private void Export()
        {
            if (working) return;

            if (properlyLoaded && exportResult != null) // The editor is properly loaded
            {
                working = true;

                var path = ExportFolderInput!.text;
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

                var resultPalette = exportResult.ResultPalette;
                var updatedEntries = new HashSet<int>();
                // Apply export palette overrides
                for (int index = 0;index < resultPalette.Length;index++)
                {
                    var item = mappingItems[index];
                    var itemVal = resultPalette[index];

                    var newColor = ColorConvert.OpaqueRGBFromHexString(item.GetColorCode());
                    var newBlockState = item.GetBlockState();

                    // Color32s are directly comparable so we have to convert them to rgb int first
                    if (ColorConvert.GetOpaqueRGB(itemVal.Color) != newColor || itemVal.BlockState != newBlockState)
                    {
                        itemVal.Color = ColorConvert.GetOpaqueColor32(newColor);
                        itemVal.BlockState = newBlockState;

                        updatedEntries.Add(index);
                    }
                }

                // Rebuild result mesh to reflect mapping changes
                exportResult.RequestRebuildResultMesh(updatedEntries);

                int sizeX = exportResult.SizeX;
                int sizeY = exportResult.SizeY;
                int sizeZ = exportResult.SizeZ;
                var blockData = exportResult.BlockData;

                var filePath = $"{dirInfo.FullName}{SP}{fileName}";

                switch (formatIndex)
                {
                    case 0: // sponge schem
                        SpongeSchemExporter.Export(sizeX, sizeY, sizeZ, resultPalette, blockData, filePath, exportDataVersion);
                        break;
                    case 1: // nbt structure
                        NbtStructureExporter.Export(sizeX, sizeY, sizeZ, resultPalette, blockData, filePath, exportDataVersion);
                        break;
                    case 2: // mcfunction
                        McFuncExporter.Export(sizeX, sizeY, sizeZ, resultPalette, blockData, filePath);
                        break;
                    case 3: // vox model
                        // Vox use a byte value for x, y, z position and block index
                        if (sizeX <= 255 && sizeY <= 255 && sizeZ <= 255 && resultPalette.Length <= 255)
                        {
                            var voxBlockData = blockData.Select(x => (byte) x).ToArray();
                            var zeroAsAir = sizeZ != 1;
                            var resultColorPalette = resultPalette.Select(x => ColorConvert.GetOpaqueRGB(x.Color)).ToArray();
                            var airIndices = exportResult.AirIndices;

                            MarkovJunior.VoxHelper.SaveVox(voxBlockData, (byte) sizeX, (byte) sizeY, (byte) sizeZ, resultColorPalette, airIndices, filePath);
                        }

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