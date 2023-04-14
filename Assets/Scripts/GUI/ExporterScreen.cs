#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovBlocks
{
    public class ExporterScreen : BaseScreen
    {
        [SerializeField] public TMP_Text? ScreenHeader;
        // Settings Panel
        [SerializeField] public Button? ExportButton;
        // Mapping Items Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;

        private readonly List<MappingEditorItem> mappingItems = new();
        private bool working = false, properlyLoaded = false;
        private string confModelFile = string.Empty;

        public override bool ShouldPause() => true;

        private IEnumerator InitializeScreen((byte[] state, char[] legend, int FX, int FY, int FZ) data, Dictionary<char, CustomMappingItem> exportPalette, HashSet<char> minimumCharSet)
        {
            bool is2d = data.FZ == 1;

            // Initialize settings panel
            ExportButton!.onClick.RemoveAllListeners();
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
                ScreenHeader.text = $"Editing {confModelFile}";
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
            var data = game.GetSelectedResultData();
            // Get export palette
            Dictionary<char, CustomMappingItem>? exportPalette = null;
            var minimumCharSet = new HashSet<char>();
            
            if (data is not null)
            {
                // Find out characters that appeared in the final result
                var finalLegend = data.Value.legend;
                var byteVals = data.Value.state.ToHashSet();

                foreach (var v in byteVals)
                    minimumCharSet.Add(finalLegend[v]);

                exportPalette = game.GetExportPalette(finalLegend.ToHashSet());
            }
            
            if (data is null || exportPalette is null || ExportButton == null || GridTransform == null)
            {
                Debug.LogWarning("Exporter is not properly loaded!");

                if (ScreenHeader != null)
                    ScreenHeader.text = "0.0 Exporter not loaded";

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen(data.Value, exportPalette, minimumCharSet));
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

                // TODO: Do work
                
                working = false;

                //manager?.SetActiveScreenByType<HUDScreen>();
            }

        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                manager.SetActiveScreenByType<HUDScreen>();

        }
    }
}