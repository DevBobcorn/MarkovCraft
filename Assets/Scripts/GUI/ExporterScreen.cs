#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovBlocks
{
    public class ExporterScreen : BaseScreen
    {
        private const string EXPORT_PATH_KEY = "ExportPath";
        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public TMP_InputField? ExportFolderInput;
        [SerializeField] public Button? ExportButton, OpenExplorerButton;
        // Mapping Items Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;

        private (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ)? exportData;
        private Dictionary<char, CustomMappingItem>? exportPalette;

        private readonly List<MappingEditorItem> mappingItems = new();
        private bool working = false, properlyLoaded = false;

        public override bool ShouldPause() => true;

        private bool CheckWindows() => Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer;

        private IEnumerator InitializeScreen(HashSet<char> minimumCharSet)
        {
            if (exportData is null || exportPalette is null || ExportFolderInput == null || ExportButton == null || OpenExplorerButton == null)
            {
                Debug.LogWarning($"ERROR: Export data is not complete!");
                working = false;
                properlyLoaded = false;
                yield break;
            }

            var data = exportData.Value;
            bool is2d = data.FZ == 1;

            // Initialize settings panel
            var savedExportPath = PlayerPrefs.GetString(EXPORT_PATH_KEY, Directory.GetParent(Application.dataPath).ToString());
            ExportFolderInput.text = savedExportPath;
            if (CheckWindows())
            {
                OpenExplorerButton.onClick.RemoveAllListeners();
                OpenExplorerButton.onClick.AddListener(ShowExplorer);
            }
            else // Hide this button
                OpenExplorerButton.gameObject.SetActive(false);
            ExportButton.onClick.RemoveAllListeners();
            ExportButton.onClick.AddListener(Export);
            // Initialize mappings panel
            // Populate mapping item grid
            foreach (var ch in exportPalette.Keys)
            {
                var newItemObj = GameObject.Instantiate(MappingItemPrefab);
                var newItem = newItemObj!.GetComponent<MappingEditorItem>();

                mappingItems.Add(newItem);

                var itemVal = exportPalette[ch];
                var rgb = ColorConvert.GetRGB(itemVal.Color);

                newItem.InitializeData(ch, rgb, rgb, itemVal.BlockState);

                newItem.transform.SetParent(GridTransform);
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
                    item.TagAsLocked("minecraft:air");
                }
                else if (minimumCharSet.Contains(item.Character))
                    item.TagAsActive();
                else // Move it to the end
                    item.gameObject.transform.SetAsLastSibling();
            }

            working = false;
            properlyLoaded = true;

            if (ScreenHeader != null)
                ScreenHeader.text = $"Exporting generation result of {data.info[0]}";
            
            if (InfoText != null)
                InfoText.text = $"Configured Model:\n<u>{data.info[0]}</u>\n\nSeed:\n<u>{data.info[1]}</u>\n\nSize:\n<u>{data.FX}x{data.FZ}x{data.FY}</u>";
            
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            if (ScreenHeader != null)
                ScreenHeader.text = "Loading...";
            
            var game = Test.Instance;
            
            // Get selected result data
            exportData = game.GetSelectedResultData();
            // Get export palette
            var minimumCharSet = new HashSet<char>();
            
            if (exportData is not null)
            {
                // Find out characters that appeared in the final result
                var finalLegend = exportData.Value.legend;
                var byteVals = exportData.Value.state.ToHashSet();

                foreach (var v in byteVals)
                    minimumCharSet.Add(finalLegend[v]);

                exportPalette = game.GetExportPalette(finalLegend.ToHashSet());
            }
            
            if (exportData is null || exportPalette is null || ExportButton == null || GridTransform == null)
            {
                Debug.LogWarning("Exporter is not properly loaded!");

                if (ScreenHeader != null)
                    ScreenHeader.text = "0.0 Exporter not loaded";

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen(minimumCharSet));
        }

        public override void OnHide(ScreenManager manager)
        {
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();

        }

        private void Export()
        {
            if (working) return;

            if (properlyLoaded) // The editor is properly loaded
            {
                working = true;

                var path = ExportFolderInput!.text;
                var data = exportData!.Value;

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
                
                // SavePath is vaild, save it
                PlayerPrefs.SetString(EXPORT_PATH_KEY, dirInfo.FullName);

                // Both field shouldn't be null if exporter laods properly
                McFuncExporter.Export(data.info, data.state, data.legend, data.FX, data.FY, data.FZ, exportPalette!, dirInfo);
                
                working = false;

                manager?.SetActiveScreenByType<HUDScreen>();
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
                manager.SetActiveScreenByType<HUDScreen>();

        }
    }
}