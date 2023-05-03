#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class RulePreview : MonoBehaviour
    {
        [SerializeField] public RawImage? RuleInPreview;
        [SerializeField] public RawImage? RuleOutPreview;

        public void SetPreviews(Texture2D inPrev, Texture2D outPrev)
        {
            if (RuleInPreview != null)
            {
                RuleInPreview.GetComponent<LayoutElement>().minWidth = inPrev.width;
                RuleInPreview.GetComponent<LayoutElement>().minHeight = inPrev.height;

                RuleInPreview.texture = inPrev;
            }
            
            if (RuleOutPreview != null)
            {
                RuleOutPreview.GetComponent<LayoutElement>().minWidth = outPrev.width;
                RuleOutPreview.GetComponent<LayoutElement>().minHeight = outPrev.height;

                RuleOutPreview.texture = outPrev;
                RuleOutPreview.SetNativeSize();
            }
            
        }

    }
}