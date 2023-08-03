#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace MarkovCraft
{
    public class RuleGraphNode : BaseGraphNode
    {
        [SerializeField] public GameObject? RulePreviewPrefab;
        private readonly Dictionary<int, RulePreview> rulePreviews = new();

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

        public void AddRulePreview(int siblingId, Texture2D inPreview, Texture2D outPreview)
        {
            var rulePreview = Instantiate(RulePreviewPrefab, transform);
            rulePreview!.transform.SetAsLastSibling();

            var rulePrev = rulePreview.GetComponent<RulePreview>();
            rulePrev.SetRuleActive(false);
            rulePrev.SetPreviews(inPreview, outPreview);

            rulePreviews.Add(siblingId, rulePrev);
        }
    }
}