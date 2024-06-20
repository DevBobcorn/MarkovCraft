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

        [SerializeField] private Button? moveUpButton;
        [SerializeField] private Button? moveDownButton;

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
            DeselectPack(false);

            // Toggle event
            packToggle!.ClearToggleEvents();
            packToggle.AddToggleHandler(TogglePack);
        }

        public void TogglePack()
        {
            if (selected)
            {
                DeselectPack(true);
            }
            else
            {
                SelectPack(true);
            }
        }

        public void SelectPack(bool updateSlots)
        {
            packIconFrameImage!.color = Color.white;
            GetComponent<Image>().color = SELECTED_COLOR;

            selected = true;
            packToggle?.SetSelected(true);

            var parent = transform.parent;
            var curIndex = transform.GetSiblingIndex();

            // Enable move buttons
            moveUpButton!.interactable = true;
            moveDownButton!.interactable = true;

            if (!updateSlots) return;

            // Move up to below lowest selected pack or top
            while (curIndex > 0 && !parent.GetChild(curIndex - 1).GetComponent<ResourcePackItem>().Selected)
            {
                curIndex -= 1; // Move up 1 slot
            }
            transform.SetSiblingIndex(curIndex);
        }

        public void DeselectPack(bool updateSlots)
        {
            packIconFrameImage!.color = Color.gray;
            GetComponent<Image>().color = DESELECTED_COLOR;

            selected = false;
            packToggle?.SetSelected(false);

            var parent = transform.parent;
            var curIndex = transform.GetSiblingIndex();

            // Disable move buttons
            moveUpButton!.interactable = false;
            moveDownButton!.interactable = false;

            if (!updateSlots) return;

            // Move down to above highest unselected pack or bottom
            while (curIndex < parent.childCount - 1 && parent.GetChild(curIndex + 1).GetComponent<ResourcePackItem>().Selected)
            {
                curIndex += 1; // Move down 1 slot
            }
            transform.SetSiblingIndex(curIndex);
        }

        public void MoveUp()
        {
            if (Selected) // Only movable if selected
            {
                var curIndex = transform.GetSiblingIndex();
                if (curIndex > 0)
                {
                    transform.SetSiblingIndex(curIndex - 1);
                }
            }
        }

        public void MoveDown()
        {
            if (Selected) // Only movable if selected
            {
                var curIndex = transform.GetSiblingIndex();
                var parent = transform.parent;
                if (curIndex < parent.childCount - 1 && parent.GetChild(curIndex + 1).GetComponent<ResourcePackItem>().Selected)
                {
                    transform.SetSiblingIndex(curIndex + 1);
                }
            }
        }

        public void SetClickEvent(UnityAction action)
        {
            GetComponentInChildren<Button>().onClick.AddListener(action);
        }
    }
}