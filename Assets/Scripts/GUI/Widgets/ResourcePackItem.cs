#nullable enable
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using CraftSharp;

namespace MarkovCraft
{
    public class ResourcePackItem : MonoBehaviour
    {
        private static readonly Color32 SELECTED_COLOR = Color.white * 0.5F;
        private static readonly Color32 DESELECTED_COLOR = Color.white * 0.25F;

        [SerializeField] private TMP_Text? packNameText;
        [SerializeField] private TMP_Text? packDescText;
        [SerializeField] private TMP_Text? packFormatText;
        [SerializeField] private Image? packIconImage;
        [SerializeField] private Image? packIconFrameImage;
        [SerializeField] private ResourcePackToggle? packToggle;

        private string packName = string.Empty;
        public string PackName => packName;
        private string packFormat = "0";
        public string PackFormat => packFormat;
        private string packDesc = string.Empty;
        public string PackDesc => packDesc;

        private bool selected = false;
        public bool Selected => selected;

        public void SetPackData(string name, string format, string desc, Sprite? sprite)
        {
            // Store resource pack info
            packNameText!.text = name;
            packName = name;
            packDescText!.text = TMPConverter.MC2TMP(desc);
            packDesc = desc;
            packFormatText!.text = format;
            packFormat = format;

            if (sprite != null)
            {
                // Set icon preview
                packIconImage!.sprite = sprite;
                var scale = 1F; // frameSize / sprite.rect.width;
                //packIconImage.SetNativeSize();
                // Scale sprite to proper size
                packIconImage.rectTransform.localScale = new(scale, scale, 1F);
            }

            // Deselect on start
            DeselectPack();

            // Toggle event
            packToggle!.ClearToggleEvents();
            packToggle.AddToggleHandler(TogglePack);
        }

        public void TogglePack()
        {
            if (selected)
            {
                DeselectPack();
            }
            else
            {
                SelectPack();
            }
        }

        public void SelectPack()
        {
            packIconFrameImage!.color = Color.white;
            GetComponent<Image>().color = SELECTED_COLOR;

            selected = true;
            packToggle?.SetEnabled(true);
        }

        public void DeselectPack()
        {
            packIconFrameImage!.color = Color.gray;
            GetComponent<Image>().color = DESELECTED_COLOR;

            selected = false;
            packToggle?.SetEnabled(false);
        }

        public void SetClickEvent(UnityAction action)
        {
            GetComponentInChildren<Button>().onClick.AddListener(action);
        }
    }
}