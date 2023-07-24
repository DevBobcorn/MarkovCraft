#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class ResultDetailPanel : MonoBehaviour
    {
        [SerializeField] private Image? detailImage;

        private enum PreviewRotation
        {
            ZERO,
            NINETY,
            ONE_EIGHTY,
            TWO_SEVENTY
        }

        public void UpdateImage()
        {
            var exporter = GetComponentInParent<ExporterScreen>();
            
            if (exporter is not null)
            {
                var prev = exporter.GetPreviewData();
                // Update Preview Image
                var (pixels, sizeX, sizeY) = MarkovJunior.Graphics.Render(prev.state, prev.sizeX, prev.sizeY, prev.sizeZ, prev.colors, 6, 0);
                var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, sizeX, sizeY);
                //tex.filterMode = FilterMode.Point;
                // Update sprite
                var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
                detailImage!.sprite = sprite;
                detailImage!.SetNativeSize();
            }
        }

        public void Show()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            UpdateImage();
        }

        public void Hide()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}