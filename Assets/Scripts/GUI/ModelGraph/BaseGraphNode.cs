#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class BaseGraphNode : MonoBehaviour
    {
        [SerializeField] TMP_Text? nameText, activeHint;
        [SerializeField] Image? backgroundImage;
        [SerializeField] Color inactiveColor, activeColor;

        [HideInInspector] public string NodeName = string.Empty;
        public string SourceText = string.Empty;

        public virtual void SetNodeActive(bool active)
        {
            activeHint!.gameObject.SetActive(active);
            backgroundImage!.color = active ? activeColor : inactiveColor;
            nameText!.color = active ? Color.black : Color.white;
        }

        public void SetNodeNameVisible(bool visible)
        {
            nameText!.gameObject.SetActive(visible);
        }

        public void SetNodeName(string name)
        {
            nameText!.text = name;
            NodeName = name;
        }


    }
}