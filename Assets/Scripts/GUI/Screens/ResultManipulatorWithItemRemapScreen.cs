#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarkovCraft
{
    public class ResultManipulatorWithItemRemapScreen : ResultManipulatorScreen
    {
        // Mapping Items Panel
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;
        // BlockState Preview
        [SerializeField] public BlockStatePreview? BlockStatePreview;
        // Color Picker
        [SerializeField] public MappingItemColorPicker? ColorPicker;
        // Block Picker
        [SerializeField] public MappingItemBlockPicker? BlockPicker;
        // Result Detail Panel
        [SerializeField] public ResultDetailPanel? ResultDetailPanel;
        // Auto Mapping Panel
        [SerializeField] public AutoMappingPanel? AutoMappingPanel;

        // Target generation result
        protected GenerationResult? result = null;
        public override GenerationResult? GetResult() => result;

        // Result palette index => mapping item
        protected readonly List<ExportItem> mappingItems = new();

        // Current screen status
        protected bool working = false, properlyLoaded = false;

        protected void InitializeRemap()
        {
            ResultDetailPanel!.Hide();
            
            // Initialize mappings panel
            for (var index = 0;index < result!.ResultPalette.Length;index++)
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
            }

            // Mark air items
            foreach (var airIndex in result.AirIndices)
            {
                var item = mappingItems[airIndex];
                item.gameObject.transform.SetAsLastSibling();
                item.TagAsSpecial("minecraft:air");
            }
        }

        protected void FinalizeRemap()
        {
            // The export palette is not destroyed. If exporter is screen is opened again
            // before the selected generation result is changed, the old export palette
            // containing cached mapping items will still be used
            
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();

            // Clear export result
            result = null;

            // Hide auto mapping panel
            AutoMappingPanel?.Hide();
            // Hide color picker
            ColorPicker?.CloseAndDiscard();
            // Hide block picker
            BlockPicker?.CloseAndDiscard();
        }

        public void AutoMap()
        {
            // Do auto mapping
            AutoMappingPanel!.AutoMap(mappingItems.Select(x => x as MappingItem).ToList());
            // Hide auto mapping panel
            AutoMappingPanel!.Hide();
        }

        protected void ApplyMappings()
        {
            if (working) return;

            if (properlyLoaded && result != null) // The editor is properly loaded
            {
                working = true;

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

                working = false;

                manager?.SetActiveScreenByType<GenerationScreen>();
            }
        }

        public override void ScreenUpdate(ScreenManager manager) { }
    }
}