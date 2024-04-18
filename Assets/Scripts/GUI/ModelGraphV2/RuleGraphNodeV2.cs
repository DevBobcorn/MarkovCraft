#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class RuleGraphNodeV2 : BaseGraphNodeV2
    {
        private readonly Dictionary<int, RulePreviewV2> rulePreviews = new();

        public RuleGraphNodeV2(VisualElement nodeElement) : base(nodeElement)
        {
        }

        public override void SetNodeActive(bool active)
        {
            base.SetNodeActive(active);
            
            if (!active) // Mark all rule previews as inactive
                foreach (var item in rulePreviews)
                    item.Value.SetRuleActive(false);
        }

        public void SetActiveRules(List<int> ruleIsActive)
        {
            foreach (var item in rulePreviews)
                item.Value.SetRuleActive(ruleIsActive.Contains(item.Key));
            
        }

        public void AddRulePreview(VisualTreeAsset asset, int siblingId, Texture2D inPreview, Texture2D outPreview)
        {
            var rulePreview = asset.CloneTree();
            m_NodeElement!.Add(rulePreview);

            var rulePrev = new RulePreviewV2(rulePreview);
            rulePrev.SetRuleActive(false);
            rulePrev.SetPreviews(inPreview, outPreview);

            rulePreviews.Add(siblingId, rulePrev);
            
        }
    }
}