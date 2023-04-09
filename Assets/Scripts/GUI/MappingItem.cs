#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovBlocks
{
    public class MappingItem : MonoBehaviour
    {
        [SerializeField] Color32 MappingColor;

        [SerializeField] Image? ColorPreviewImage;
        [SerializeField] TMP_InputField? CharacterInput;
        [SerializeField] TMP_InputField? ColorCodeInput;
        [SerializeField] TMP_InputField? BlockStateInput;

        public void InitializeData(char character, int rbga, string blockState)
        {
            if (ColorPreviewImage == null || CharacterInput == null || ColorCodeInput == null || BlockStateInput == null)
            {
                Debug.LogError("Mapping Item missing components!");
                return;
            }

            // Character input
            CharacterInput.text = character.ToString();
            // Color input
            ColorCodeInput.text = ColorConvert.GetHexRGBString(rbga);
            ColorPreviewImage.color = ColorConvert.GetOpaqueColor32(rbga);
            // Black state input
            BlockStateInput.text = blockState;
            
        }

    }
}