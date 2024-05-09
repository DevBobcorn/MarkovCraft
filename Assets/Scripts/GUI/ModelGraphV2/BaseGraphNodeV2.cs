#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class BaseGraphNodeV2
    {
        private static readonly StyleColor ACTIVE_COLOR   = new(new Color(200, 255, 255, 255));
        private static readonly StyleColor INACTIVE_COLOR = new(new Color(100,   0,   0,   0));
        private static readonly StyleColor BLACK_COLOR = new(Color.black);
        private static readonly StyleColor WHITE_COLOR = new(Color.white);

        protected readonly VisualElement m_NodeElement;

        // Cache frequently accessed children elements
        // to eliminate redundant queries
        private VisualElement? m_ActiveHint;
        private VisualElement? m_NameText;

        public string NodeName = string.Empty;
        public string SourceXml = string.Empty;

        public BaseGraphNodeV2(VisualElement nodeElement)
        {
            m_NodeElement = nodeElement;
        }

        public virtual void SetNodeActive(bool active)
        {
            (m_ActiveHint ??= m_NodeElement.Q(name: "active_hint")).style.display =
                    active ? DisplayStyle.Flex : DisplayStyle.None;
            m_NodeElement!.style.backgroundColor = active ? ACTIVE_COLOR : INACTIVE_COLOR;
            (m_NameText ??= m_NodeElement.Q(name: "node_name_text"))!.style.color = active ? BLACK_COLOR : WHITE_COLOR;
        }

        public void SetNodeNameVisible(bool visible)
        {
            //m_NameText!.visible = visible;
            m_NameText!.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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