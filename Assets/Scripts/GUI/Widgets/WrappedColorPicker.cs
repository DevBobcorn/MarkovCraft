#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class WrappedColorPicker : FlexibleColorPicker
    {
        [SerializeField] private Image? initialColorPreview;
        private Color32 initialColor = Color.black;

        public void Initialize(Color32 initColor)
        {
            // Store initial color (of the new active item)
            initialColor = initColor;
            initialColorPreview!.color = initColor;

            // Set current color
            color = initialColor;
        }

        public void RevertToInitialColor()
        {
            // Set current color
            color = initialColor;
        }
    }
}