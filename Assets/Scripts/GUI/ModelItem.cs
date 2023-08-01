#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace MarkovCraft
{
    public class ModelItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text? modelNameText;
        [SerializeField] private Image? modelPreviewImage;
        [SerializeField] private Image? modelPreviewFrame;
        [SerializeField] [Range(10F, 500F)] private float frameSize = 100F;

        //       X,   Y,   Z, Steps, Animated
        private (int, int, int, int, bool) presetData = new();
        public (int, int, int, int, bool) PresetData => presetData;

        private string modelName = string.Empty;
        public string ModelName => modelName;

        private string AddSpacesBeforeUppercase(string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) && 
                        i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public void SetModelData(string name, int x, int y, int z, int steps, bool animated)
        {
            modelNameText!.text = AddSpacesBeforeUppercase(name);
            modelName = name;

            presetData = (x, y, z, steps, animated);
        }

        public void VisualSelect()
        {
            GetComponentInChildren<Button>().Select();
        }

        public void SetClickEvent(UnityAction action)
        {
            GetComponentInChildren<Button>().onClick.AddListener(action);
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