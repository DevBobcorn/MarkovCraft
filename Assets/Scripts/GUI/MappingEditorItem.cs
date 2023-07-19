#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MinecraftClient.Mapping;

namespace MarkovCraft
{
    public class MappingEditorItem : MonoBehaviour
    {
        [SerializeField] Color32 SpecialTagColor;
        [SerializeField] Image? ColorPreviewImage, MarkCornerImage;
        [SerializeField] TMP_Text? CharacterText;
        [SerializeField] TMP_InputField? ColorCodeInput;
        [SerializeField] TMP_InputField? BlockStateInput;

        [SerializeField] Button? RevertOverrideButton;

        private BlockStatePreview? blockStatePreview;
        private bool overridesPaletteColor = false;

        // RGB color of this item in the base palette
        private int defaultRgb = 0;

        private char character;
        public char Character => character;

        public void InitializeData(char character, int defoRgb, int rgb, string blockState, BlockStatePreview blockStatePreview)
        {
            if (ColorPreviewImage == null || CharacterText == null || ColorCodeInput == null || BlockStateInput == null
                    || MarkCornerImage == null || RevertOverrideButton == null)
            {
                Debug.LogError("Mapping Item missing components!");
                return;
            }

            this.character = character;
            defaultRgb = defoRgb & 0xFFFFFF; // Remove alpha channel if presents

            // BlockState Preview
            this.blockStatePreview = blockStatePreview;

            // Character input
            CharacterText.text = character.ToString();
            // Color input
            ColorCodeInput.text = ColorConvert.GetHexRGBString(rgb);
            ColorPreviewImage.color = ColorConvert.GetOpaqueColor32(rgb);
            // Black state input
            BlockStateInput.text = blockState;

            SetOverridesPaletteColor(defoRgb != rgb);

            // Assign control events (should get called only once)
            RevertOverrideButton.onClick.AddListener(RevertColorToBaseValue);
            ColorCodeInput.onValueChanged.AddListener(UpdateColorCode);
            ColorCodeInput.onEndEdit.AddListener(ValidateColorCode);

            BlockStateInput.onSelect.AddListener(ShowBlockStatePreview);
            BlockStateInput.onValueChanged.AddListener(UpdateBlockStateText);
            BlockStateInput.onEndEdit.AddListener(HideBlockStatePreview);
        }

        public void UpdateColorCode(string colorHex)
        {
            int newRgb = ColorConvert.RGBFromHexString(colorHex.PadRight(6, '0'));
            ColorPreviewImage!.color = ColorConvert.GetOpaqueColor32(newRgb);

            if (newRgb == defaultRgb)
                SetOverridesPaletteColor(false);
            else
                SetOverridesPaletteColor(true);
        }

        public void ValidateColorCode(string colorHex)
        {
            ColorCodeInput!.text = colorHex.PadRight(6, '0').ToUpper();

        }

        public void ShowBlockStatePreview(string blockState)
        {
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);

            blockStatePreview!.UpdatePreview(stateId);
        }

        public void UpdateBlockStateText(string blockState)
        {
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);

            if (stateId != BlockStateHelper.INVALID_BLOCKSTATE)
                blockStatePreview!.UpdatePreview(stateId);
            else
                blockStatePreview!.UpdateHint(blockState);
        }

        public void HideBlockStatePreview(string blockState)
        {
            // Hide preview
            blockStatePreview!.UpdatePreview(BlockStateHelper.INVALID_BLOCKSTATE);

        }

        public string GetColorCode() => ColorCodeInput?.text ?? "000000";

        public string GetBlockState()
        {
            var blockState = BlockStateInput?.text;

            if (string.IsNullOrWhiteSpace(blockState))
                return string.Empty;
            
            return blockState;
        }

        public void SetBlockState(string blockState)
        {
            if (BlockStateInput!.interactable) // The blockstate input is not locked
            {
                BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview
            }
        }

        public void TagAsSpecial(string blockState)
        {
            BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview
            BlockStateInput.interactable = false;

            MarkCornerImage!.gameObject.SetActive(true);
            MarkCornerImage!.color = SpecialTagColor;
        }

        public void SetOverridesPaletteColor(bool o)
        {
            overridesPaletteColor = o;

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