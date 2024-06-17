#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace MarkovCraft
{
    public class ResourcePackItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text? packNameText;
        [SerializeField] private TMP_Text? packDescText;
        [SerializeField] private Image? packIconImage;
        [SerializeField] [Range(10F, 500F)] private float frameSize = 100F;

        private string packName = string.Empty;
        public string PackName => packName;
        private int packFormat = 0;
        public int PackFormat => packFormat;
        private string packDesc = string.Empty;
        public string PackDesc => packDesc;

        public void SetPackName(string name, int format, string desc, Sprite sprite)
        {
            // Store resource pack info
            packNameText!.text = name;
            packName = name;
            packFormat = format;
            packDesc = desc;

            // Set icon preview
            packIconImage!.sprite = sprite;
            var scale = frameSize / sprite.rect.width;
            packIconImage.SetNativeSize();
            // Scale sprite to proper size
            packIconImage.rectTransform.localScale = new(scale, scale, 1F);
        }

        public void VisualSelect()
        {
            GetComponentInChildren<Button>().Select();
        }

        public void SetClickEvent(UnityAction action)
        {
            GetComponentInChildren<Button>().onClick.AddListener(action);
        }
    }
}