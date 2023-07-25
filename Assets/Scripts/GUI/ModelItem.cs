#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class ModelItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text? modelNameText;
        [SerializeField] private Image? modelPreviewImage;
        [SerializeField] [Range(10F, 500F)] private float frameSize = 100F;

        public void SetModelName(string name)
        {
            modelNameText!.text = name;
        }

        public void SetPreviewSprite(Sprite sprite)
        {
            modelPreviewImage!.sprite = sprite;

            var shorterSide = Mathf.Min(sprite.rect.width, sprite.rect.height);
            var scale = frameSize / shorterSide;

            modelPreviewImage.SetNativeSize();
            modelPreviewImage.rectTransform.localScale = new(scale, scale, 1F);

            //modelPreviewImage!.rectTransform.sizeDelta
            //        = new(sprite.rect.width / 2F, sprite.rect.height / 2F);

        }
    }
}