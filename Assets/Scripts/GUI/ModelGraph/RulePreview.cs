#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class RulePreview : MonoBehaviour
    {
        [SerializeField] GameObject? activeHint;
        [SerializeField] RawImage? ruleInPreview;
        [SerializeField] RawImage? ruleOutPreview;

        public void SetRuleActive(bool active) => activeHint?.SetActive(active);

        public void SetPreviews(Texture2D inPrev, Texture2D outPrev)
        {
            if (ruleInPreview != null)
            {
                ruleInPreview.GetComponent<LayoutElement>().minWidth = inPrev.width;
                ruleInPreview.GetComponent<LayoutElement>().minHeight = inPrev.height;

                ruleInPreview.texture = inPrev;
                ruleInPreview.SetNativeSize();
            }
            
            if (ruleOutPreview != null)
            {
                ruleOutPreview.GetComponent<LayoutElement>().minWidth = outPrev.width;
                ruleOutPreview.GetComponent<LayoutElement>().minHeight = outPrev.height;

                ruleOutPreview.texture = outPrev;
                ruleOutPreview.SetNativeSize();
            }
            
        }

    }
}