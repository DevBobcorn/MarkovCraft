#nullable enable
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class BaseGraphNodeV2
    {
        protected VisualElement? m_NodeElement;

        public string NodeName = string.Empty;
        public string SourceXml = string.Empty;

        public BaseGraphNodeV2(VisualElement nodeElement)
        {
            m_NodeElement = nodeElement;
        }

        public virtual void SetNodeActive(bool active)
        {
            /*
            activeHint!.gameObject.SetActive(active);
            backgroundImage!.color = active ? activeColor : inactiveColor;
            nameText!.color = active ? Color.black : Color.white;
            */
        }

        public void SetNodeNameVisible(bool visible)
        {
            //nameText!.gameObject.SetActive(visible);
        }

        public void SetNodeName(string nodeName)
        {
            (m_NodeElement.Q("node_name_text") as Label)!.text = nodeName;
            NodeName = nodeName;
        }

        public void SetSourceXml(string xml)
        {
            SourceXml = xml;
        }
    }
}