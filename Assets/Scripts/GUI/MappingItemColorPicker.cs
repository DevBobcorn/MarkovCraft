#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class MappingItemColorPicker : FlexibleColorPicker
    {
        [SerializeField] private Image? initialColorPreview;
        private Color32 initialColor = Color.black;
        private MappingItem? activeItem = null;

        private void Open()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void Close()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void OpenAndInitialize(MappingItem item, Color32 initColor)
        {
            if (activeItem != null) // Opened by an item while editing another one
            {
                // Apply current color to active item
                ApplyToItem(activeItem);
            }

            // Update active item
            activeItem = item;

            // Store initial color (of the new active item)
            initialColor = initColor;
            initialColorPreview!.color = initColor;

            // Set current color
            color = initialColor;

            Open();
        }

        public void RevertToInitialColor()
        {
            // Set current color
            color = initialColor;
        }

        public void CloseAndDiscard()
        {
            Close();
        }

        private void ApplyToItem(MappingItem item)
        {
            int curRgb = ColorConvert.GetRGB(color);
            item.SetColorRGB(curRgb);
        }

        public void CloseAndApply()
        {
            if (activeItem != null) // Active item is available
            {
                // Apply current color to active item
                ApplyToItem(activeItem);
            }

            Close();
        }
    }
}