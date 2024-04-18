#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class PathGraphNodeV2 : BaseGraphNodeV2
    {
        public PathGraphNodeV2(VisualElement nodeElement) : base(nodeElement)
        {
        }

        public void SetPreviews(Color32[] froms, Color32[] tos, Color32[] ons, Color32 pathColor)
        {
            var container = m_NodeElement.Q(name: "path_container");

            var pathColorPreview = container.Q(name: "color_preview_frame").Q(name: "color_preview");

            var pathStarts = container.Q(name: "starts");
            var pathEnds = container.Q(name: "ends");
            var pathSubstrates = container.Q(name: "substrates");

            static void addPreview(VisualElement parent, Color color)
            {
                var v = new VisualElement();
                v.style.minWidth  = v.style.maxWidth  = v.style.width  = 15;
                v.style.minHeight = v.style.maxHeight = v.style.height = 15;

                v.style.backgroundColor = color;

                parent.Add(v);
            }

            if (pathStarts != null)
            {
                for (int i = 0;i < froms.Length;i++)
                {
                    addPreview(pathStarts, froms[i]);
                }
            }

            if (pathEnds != null)
            {
                for (int i = 0;i < tos.Length;i++)
                {
                    addPreview(pathEnds, tos[i]);
                }
            }

            if (pathSubstrates != null)
            {
                for (int i = 0;i < ons.Length;i++)
                {
                    addPreview(pathSubstrates, ons[i]);
                }
            }

            if (pathColorPreview != null)
            {
                pathColorPreview.style.backgroundColor = new(pathColor);
            }
        }
    }
}