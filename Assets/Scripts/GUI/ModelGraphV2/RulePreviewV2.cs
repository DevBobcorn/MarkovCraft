#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class RulePreviewV2
    {
        protected VisualElement? m_RuleElement;

        // Cache frequently accessed children elements
        // to eliminate redundant queries
        private VisualElement? m_ActiveHint;

        public RulePreviewV2(VisualElement ruleElement)
        {
            m_RuleElement = ruleElement;
        }

        public void SetRuleActive(bool active)
        {
            (m_ActiveHint ??= m_RuleElement.Q(name: "active_hint")).style.display =
                    active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetPreviews(Texture2D inPrev, Texture2D outPrev)
        {
            var ruleInPreview = m_RuleElement.Q(name: "in_preview") as Image;
            var ruleOutPreview = m_RuleElement.Q(name: "out_preview") as Image;

            if (ruleInPreview != null)
            {
                ruleInPreview.image = inPrev;
            }

            if (ruleOutPreview != null)
            {
                ruleOutPreview.image = outPrev;
            }
        }

    }
}