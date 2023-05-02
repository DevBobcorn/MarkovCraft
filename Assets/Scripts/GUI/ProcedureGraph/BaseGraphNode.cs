#nullable enable
using UnityEngine;
using TMPro;

namespace MarkovCraft
{
    public class BaseGraphNode : MonoBehaviour
    {
        [SerializeField] TMP_Text? NameText;
        [HideInInspector] public string NodeName = string.Empty;
        public string SourceText = string.Empty;

        public void SetNodeName(string name)
        {
            NameText!.text = name;
            NodeName = name;
        }


    }
}