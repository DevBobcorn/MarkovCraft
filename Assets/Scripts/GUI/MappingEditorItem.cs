#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovBlocks
{
    public class MappingEditorItem : MonoBehaviour
    {
        [SerializeField] Color32 MappingColor;

        [SerializeField] Image? ColorPreviewImage, MarkCornerImage;
        [SerializeField] TMP_Text? CharacterText;
        [SerializeField] TMP_InputField? ColorCodeInput;
        [SerializeField] TMP_InputField? BlockStateInput;

        [SerializeField] Button? RevertOverrideButton;

        private bool overridesPaletteColor = false;

        // RGB color of this item in the base palette
        private int defaultRgb = 0;

        private char character;
        public char Character => character;

        public void InitializeData(char character, int defoRgb, int rgb, string blockState)
        {
            if (ColorPreviewImage == null || CharacterText == null || ColorCodeInput == null || BlockStateInput == null
                    || MarkCornerImage == null || RevertOverrideButton == null)
            {
                Debug.LogError("Mapping Item missing components!");
                return;
            }

            this.character = character;
            defaultRgb = defoRgb & 0xFFFFFF; // Remove alpha channel if presents

            // Character input
            CharacterText.text = character.ToString();
            // Color input
            ColorCodeInput.text = ColorConvert.GetHexRGBString(rgb);
            ColorPreviewImage.color = ColorConvert.GetOpaqueColor32(rgb);
            // Black state input
            BlockStateInput.text = blockState;

            TagAsSpecial(false);

            SetOverridesPaletteColor(!blockState.Equals(string.Empty) || defoRgb != rgb);

            // Assign control events
            RevertOverrideButton.onClick.AddListener(RevertColorToBaseValue);
            ColorCodeInput.onValueChanged.AddListener(UpdateColorCode);
            ColorCodeInput.onEndEdit.AddListener(ValidateColorCode);
            
        }

        public void UpdateColorCode(string colorHex)
        {
            var padded = colorHex.PadRight(6, '0'); // Pad left with '0's

            int newRgb = ColorConvert.RGBFromHexString(colorHex);
            ColorPreviewImage!.color = ColorConvert.GetOpaqueColor32(newRgb);

            if (newRgb == defaultRgb)
                SetOverridesPaletteColor(false);
            else
                SetOverridesPaletteColor(true);
        }

        public void ValidateColorCode(string colorHex)
        {
            var paddedUpper = colorHex.PadRight(6, '0').ToUpper(); // Pad left with '0's
            ColorCodeInput!.text = paddedUpper;

            int newRgb = ColorConvert.RGBFromHexString(colorHex);
            ColorPreviewImage!.color = ColorConvert.GetOpaqueColor32(newRgb);

            if (newRgb == defaultRgb)
                SetOverridesPaletteColor(false);
            else
                SetOverridesPaletteColor(true);

        }

        public void TagAsSpecial(bool s)
        {
            MarkCornerImage?.gameObject.SetActive(s);
        }

        public void SetOverridesPaletteColor(bool o)
        {
            overridesPaletteColor = o;

            if (RevertOverrideButton?.gameObject.activeSelf != o)
                RevertOverrideButton?.gameObject.SetActive(o);
        }

        public bool ShouldBeSaved()
        {
            return overridesPaletteColor || !string.IsNullOrWhiteSpace(BlockStateInput?.text);
        }

        public void RevertColorToBaseValue()
        {
            if (ColorPreviewImage == null || CharacterText == null || ColorCodeInput == null || BlockStateInput == null)
            {
                Debug.LogError("Mapping Item missing components!");
                return;
            }

            ColorCodeInput.text = ColorConvert.GetHexRGBString(defaultRgb);
            ColorPreviewImage.color = ColorConvert.GetOpaqueColor32(defaultRgb);
        }

    }
}