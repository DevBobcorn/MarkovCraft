#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CraftSharp;

namespace MarkovCraft
{
    public class ResultExporterScreen : ResultManipulatorWithItemRemapScreen
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

        private int dataVersion = 0;

        // Disable pause for animated inventory
        public override bool ShouldPause() => false;

        private IEnumerator InitializeScreen()
        {
            if (result == null)
            {
                Debug.LogWarning($"ERROR: Export screen not correctly initialized!");
                working = false;
                properlyLoaded = false;
                yield break;
            }

            // Initialize settings panel
            var savedExportPath = PlayerPrefs.GetString(EXPORT_PATH_KEY, MarkovGlobal.GetDefaultExportPath());
            ExportFolderInput!.text = savedExportPath;
            if (CheckWindowsPlatform())
            {
                OpenExplorerButton!.onClick.RemoveAllListeners();
                OpenExplorerButton.onClick.AddListener(() => ShowExplorer(ExportFolderInput!.text.Replace("/", @"\")));
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

            // Update Info text
            InfoText!.text = GameScene.GetL10nString("screen.text.result_info", result.ConfiguredModelName,
                    result.GenerationSeed, result.SizeX, result.SizeY, result.SizeZ);
            var prev = result.GetPreviewData();

            // Update selected format (and also update default export file name)
            var lastUsedFormatIndex = PlayerPrefs.GetInt(EXPORT_FORMAT_KEY, 0);
            ExportFormatDropdown.value = lastUsedFormatIndex;
            UpdateExportFileName(lastUsedFormatIndex);

            // Update Preview Image
            var (pixels, sizeX, sizeY) = ResultDetailPanel.RenderPreview(prev.sizeX, prev.sizeY, prev.sizeZ,
                    prev.blockData, prev.colors, prev.airIndices, result.SizeZ == 1 ? ResultDetailPanel.PreviewRotation.ZERO : ResultDetailPanel.PreviewRotation.NINETY);
            var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, sizeX, sizeY);
            //tex.filterMode = FilterMode.Point;
            // Update sprite
            var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
            ResultPreviewImage!.sprite = sprite;
            ResultPreviewImage!.SetNativeSize();

            // Initialize remap logic
            InitializeRemap();

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("exporter.text.loaded", result.ConfiguredModelName);
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = GameScene.GetL10nString("screen.text.loading");
            
            if (GameScene.Instance is not GenerationScene game)
            {
                Debug.LogError("Wrong game scene!");
                working = false;
                return;
            }
            
            // Get selected result data
            result = game.GetSelectedResult();
            dataVersion = game.GetDataVersionInt();
            
            if (result == null)
            {
                Debug.LogWarning("Exporter is not properly loaded!");

                ScreenHeader!.text = GenerationScene.GetL10nString("screen.text.load_failure");

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen());
        }

        public override void OnHide(ScreenManager manager)
        {
            // Finalize remap logic
            FinalizeRemap();
        }

        public string? GetExportPath()
        {
            return ExportFolderInput!.text;
        }

        private void UpdateExportFileName(int selectedFormatIndex)
        {
            if (properlyLoaded)
            {
                var baseName = GetDefaultBaseName();
                var extName = EXPORT_FORMAT_EXT_NAMES[selectedFormatIndex];

                ExportNameInput!.text = $"{baseName}.{extName}";
            }
        }

        private void Export()
        {
            if (working) return;

            if (properlyLoaded && result != null) // The editor is properly loaded
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

                if (!GameScene.CheckFileName(fileName))
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

                var resultPalette = result.ResultPalette;
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
                result.RequestRebuildResultMesh(updatedEntries);

                int sizeX = result.SizeX;
                int sizeY = result.SizeY;
                int sizeZ = result.SizeZ;
                var blockData = result.BlockData;

                var filePath = $"{dirInfo.FullName}{SP}{fileName}";

                switch (formatIndex)
                {
                    case 0: // sponge schem
                        SpongeSchemExporter.Export(sizeX, sizeY, sizeZ, resultPalette, blockData, filePath, dataVersion);
                        break;
                    case 1: // nbt structure
                        NbtStructureExporter.Export(sizeX, sizeY, sizeZ, resultPalette, blockData, filePath, dataVersion);
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
                            var airIndices = result.AirIndices;

                            MarkovJunior.VoxHelper.SaveVox(voxBlockData, (byte) sizeX, (byte) sizeY, (byte) sizeZ, resultColorPalette, airIndices, filePath);
                        }

                        break;
                }
                
                working = false;

                manager!.SetActiveScreenByType<GenerationScreen>();
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