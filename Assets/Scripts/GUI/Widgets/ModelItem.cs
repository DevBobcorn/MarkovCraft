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
        [SerializeField] [Range(10F, 500F)] private float frameSize = 100F;

        //       X,   Y,   Z, Steps, Animated
        private (int, int, int, int, bool) presetData = new();
        public (int, int, int, int, bool) PresetData => presetData;

        private string modelName = string.Empty;
        public string ModelName => modelName;

        public static string AddSpacesBeforeUppercase(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            static bool isUpperOrNum(char c)
            {
                return char.IsUpper(c) || char.IsDigit(c);
            };

            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (isUpperOrNum(text[i]))
                    if ((text[i - 1] != ' ' && !isUpperOrNum(text[i - 1])) ||
                        (isUpperOrNum(text[i - 1]) && 
                        // Preserve acronyms
                        i < text.Length - 1 && !isUpperOrNum(text[i + 1])))
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

        public void SetPreviewSprite(Sprite sprite, bool is3dPreview)
        {
            modelPreviewImage!.sprite = sprite;

            var shorterSide = Mathf.Min(sprite.rect.width, sprite.rect.height);
            var scale = frameSize / shorterSide;

            modelPreviewImage.SetNativeSize();
            // Stretch height if it is 3d, to make it look better
            modelPreviewImage.rectTransform.localScale = new(scale, is3dPreview ? scale * 1.1F : scale, 1F);
        }
    }
}