#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class PathGraphNode : BaseGraphNode
    {
        [SerializeField] public GameObject? ColorPreviewPrefab;

        [SerializeField] RectTransform? pathStarts;
        [SerializeField] RectTransform? pathEnds;
        [SerializeField] RectTransform? pathSubstrates;
        [SerializeField] Image? pathColorPreview;

        public void SetPreviews(Color32[] froms, Color32[] tos, Color32[] ons, Color32 pathColor)
        {
            if (pathStarts != null)
            {
                for (int i = 0;i < froms.Length;i++)
                {
                    var colorPreview = GameObject.Instantiate(ColorPreviewPrefab, pathStarts);
                    colorPreview!.GetComponent<Image>().color = froms[i];
                }
            }

            if (pathEnds != null)
            {
                for (int i = 0;i < tos.Length;i++)
                {
                    var colorPreview = GameObject.Instantiate(ColorPreviewPrefab, pathEnds);
                    colorPreview!.GetComponent<Image>().color = tos[i];
                }
            }

            if (pathSubstrates != null)
            {
                for (int i = 0;i < ons.Length;i++)
                {
                    var colorPreview = GameObject.Instantiate(ColorPreviewPrefab, pathSubstrates);
                    colorPreview!.GetComponent<Image>().color = ons[i];
                }
            }

            if (pathColorPreview != null)
            {
                pathColorPreview.color = pathColor;
            }
        }
    }
}