#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class RulePreviewV2
    {
        //[SerializeField] GameObject? activeHint;
        //[SerializeField] RawImage? ruleInPreview;
        //[SerializeField] RawImage? ruleOutPreview;

        protected VisualElement? m_RuleElement;

        public RulePreviewV2(VisualElement ruleElement)
        {
            m_RuleElement = ruleElement;
        }

        public void SetRuleActive(bool active)
        {
            //activeHint?.SetActive(active);
            
        }

        public void SetPreviews(Texture2D inPrev, Texture2D outPrev)
        {
            var ruleInPreview = m_RuleElement.Q(name: "in_preview") as Image;
            var ruleOutPreview = m_RuleElement.Q(name: "out_preview") as Image;

            if (ruleInPreview != null)
            {
                ruleInPreview.image = inPrev;

                ruleInPreview.style.top = StyleKeyword.Auto;
                ruleInPreview.style.bottom = StyleKeyword.Auto;
                ruleInPreview.style.left = StyleKeyword.Auto;
                ruleInPreview.style.right = StyleKeyword.Auto;
            }

            if (ruleOutPreview != null)
            {
                ruleOutPreview.image = outPrev;

                ruleOutPreview.style.top = StyleKeyword.Auto;
                ruleOutPreview.style.bottom = StyleKeyword.Auto;
                ruleOutPreview.style.left = StyleKeyword.Auto;
                ruleOutPreview.style.right = StyleKeyword.Auto;
            }
        }

    }
}