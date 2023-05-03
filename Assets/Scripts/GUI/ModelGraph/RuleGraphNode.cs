#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class RuleGraphNode : BaseGraphNode
    {
        [SerializeField] public GameObject? RulePreviewPrefab;

        public void AddRulePreview(Texture2D inPreview, Texture2D outPreview)
        {
            var rulePreview = GameObject.Instantiate(RulePreviewPrefab, transform);
            rulePreview!.transform.SetAsLastSibling();

            var rulePrev = rulePreview.GetComponent<RulePreview>();
            rulePrev.SetPreviews(inPreview, outPreview);            

        }
    }
}