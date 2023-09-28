#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CraftSharp;

namespace MarkovCraft
{
    public class MappingItem : MonoBehaviour
    {
        [SerializeField] private Color32 SpecialTagColor;
        [SerializeField] private Image? ColorPreviewImage, MarkCornerImage;
        [SerializeField] private TMP_Text? CharacterText;
        [SerializeField] private TMP_InputField? ColorCodeInput;
        [SerializeField] protected TMP_InputField? BlockStateInput;

        [SerializeField] private Button? EditColorButton;
        [SerializeField] private Button? PickBlockButton;
        [SerializeField] private Button? RevertOverrideButton;

        protected BlockStatePreview? blockStatePreview;
        private MappingItemColorPicker? colorPicker;
        private MappingItemBlockPicker? blockPicker;
        private bool overridesPaletteColor = false;

        // RGB color of this item in the base palette
        private int defaultRgb = 0;

        private char character;
        public char Character => character;

        public virtual void InitializeData(char character, int defoRgb, int rgb, string blockState, 
                MappingItemColorPicker colorPicker, MappingItemBlockPicker blockPicker, BlockStatePreview blockStatePreview)
        {
            this.character = character;
            defaultRgb = defoRgb & 0xFFFFFF; // Remove alpha channel if presents

            // BlockState Preview
            this.blockStatePreview = blockStatePreview;
            // Character display
            CharacterText!.text = character.ToString();
            // Color input
            ColorCodeInput!.text = ColorConvert.GetHexRGBString(rgb);
            ColorPreviewImage!.color = ColorConvert.GetOpaqueColor32(rgb);
            // Color picker
            this.colorPicker = colorPicker;
            // Black state input
            BlockStateInput!.text = blockState;
            // Block picker
            this.blockPicker = blockPicker;

            SetOverridesPaletteColor(defoRgb != rgb);

            // Assign control events (should get called only once)
            RevertOverrideButton!.onClick.AddListener(OnRevertOverrideButtonClick);
            ColorCodeInput.onValueChanged.AddListener(OnColorCodeInputValueChange);
            ColorCodeInput.onEndEdit.AddListener(OnColorCodeInputValidate);
            EditColorButton!.onClick.AddListener(OnEditColorButtonClick);

            BlockStateInput.onSelect.AddListener(OnSelectBlockStateInput);
            BlockStateInput.onValueChanged.AddListener(OnUpdateBlockStateInput);
            BlockStateInput.onEndEdit.AddListener(OnEndEditBlockStateInput);
            PickBlockButton!.onClick.AddListener(OnPickBlockButtonClick);
        }

        private void OnEditColorButtonClick()
        {
            var currentColor = (Color32) ColorPreviewImage!.color;
            currentColor.a = (byte) 255; // Fully opaque
            colorPicker?.OpenAndInitialize(this, currentColor);
        }

        private void OnPickBlockButtonClick()
        {
            var currentStateId = BlockStateHelper.GetStateIdFromString(GetBlockState());

            if (currentStateId != BlockStateHelper.INVALID_BLOCKSTATE) // Initialize with current blockstate
            {
                var currentState = BlockStatePalette.INSTANCE.StatesTable[currentStateId];
                blockPicker?.OpenAndInitialize(this, currentStateId, currentState);
            }
            else
            {
                blockPicker?.OpenAndInitialize(this, currentStateId, BlockState.AIR_STATE);
            }
        }

        public void SetCharacter(char character)
        {
            this.character = character;
            // Character display
            CharacterText!.text = character.ToString();
        }

        public void SetColorRGB(int newRgb)
        {
            // Update color sprite
            ColorPreviewImage!.color = ColorConvert.GetOpaqueColor32(newRgb);
            // Check if the color is overriden
            SetOverridesPaletteColor(newRgb != defaultRgb);
            // Update color code
            ColorCodeInput!.SetTextWithoutNotify(ColorConvert.GetHexRGBString(newRgb));
        }

        public void OnColorCodeInputValueChange(string colorHex)
        {
            int newRgb = ColorConvert.RGBFromHexString(colorHex.PadRight(6, '0'));
            // Update color sprite
            ColorPreviewImage!.color = ColorConvert.GetOpaqueColor32(newRgb);
            // Check if the color is overriden
            SetOverridesPaletteColor(newRgb != defaultRgb);
        }

        public void OnColorCodeInputValidate(string colorHex)
        {
            ColorCodeInput!.text = colorHex.PadRight(6, '0').ToUpper();
        }

        protected virtual void OnSelectBlockStateInput(string blockState)
        {
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);
            // Update and show preview
            blockStatePreview?.UpdatePreview(stateId);
        }

        protected virtual void OnEndEditBlockStateInput(string _)
        {
            // Hide preview
            blockStatePreview?.UpdatePreview(BlockStateHelper.INVALID_BLOCKSTATE);
        }

        protected virtual void OnUpdateBlockStateInput(string blockState)
        {
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);

            if (stateId != BlockStateHelper.INVALID_BLOCKSTATE) // Update and show preview
            {
                blockStatePreview?.UpdatePreview(stateId);
            }
            else // Hide preview
            {
                blockStatePreview?.UpdateHint(blockState);
            }
        }

        public string GetColorCode() => ColorCodeInput?.text ?? "000000";

        public string GetBlockState()
        {
            var blockState = BlockStateInput?.text;

            if (string.IsNullOrWhiteSpace(blockState))
                return string.Empty;
            
            return blockState;
        }

        public virtual void SetBlockState(string blockState)
        {
            if (BlockStateInput!.interactable) // The blockstate input is not locked
            {
                BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview
            }
        }

        public virtual void TagAsSpecial(string blockState)
        {
            BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview
            BlockStateInput.interactable = false;

            MarkCornerImage!.gameObject.SetActive(true);
            MarkCornerImage!.color = SpecialTagColor;
        }

        private void SetOverridesPaletteColor(bool o)
        {
            overridesPaletteColor = o;

            RevertOverrideButton?.gameObject.SetActive(o);
        }

        public bool ShouldBeSaved()
        {
            return overridesPaletteColor || !string.IsNullOrWhiteSpace(BlockStateInput?.text);
        }

        private void OnRevertOverrideButtonClick()
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