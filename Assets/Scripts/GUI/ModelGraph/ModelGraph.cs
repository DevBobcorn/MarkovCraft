#nullable enable
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using TMPro;

using MarkovJunior;

namespace MarkovCraft
{
    [RequireComponent(typeof (RectTransform))]
    public class ModelGraph : MonoBehaviour
    {
        [SerializeField] public TMP_Text? ModelNameText;
        [SerializeField] public GameObject? ScopeGraphNodePrefab;
        [SerializeField] public GameObject? RuleGraphNodePrefab;

        [SerializeField] public RectTransform? GraphContentTransform;

        public readonly Dictionary<int, BaseGraphNode> GraphNodes = new();
        private BaseGraphNode? activeNode = null;
        public BaseGraphNode? ActiveNode => activeNode;

        private bool adjustingWidth = false, nodeNamesVisible = true;
        private RectTransform? ownTransform;

        public void AdjustWidth() => adjustingWidth = true;

        public void SetActiveNode(int nodeNumId)
        {
            if (activeNode != null) // Deselect previously selected node
            {
                activeNode.SetNodeActive(false);
                activeNode = null;
            }

            if (GraphNodes.TryGetValue(nodeNumId, out activeNode) && activeNode != null)
            {
                activeNode.SetNodeActive(true);

                // Perform auto scroll
                var aTransform = activeNode.GetComponent<RectTransform>()!;

                //var aPos = aTransform.anchoredPosition.y;
                var aPos = GraphContentTransform!.InverseTransformPoint(aTransform.position).y;
                var aHgt = aTransform.rect.height;

                var vPos = GraphContentTransform!.anchoredPosition.y;
                var vHgt = GraphContentTransform.parent.GetComponent<RectTransform>().rect.height;

                aHgt = Mathf.Min(aHgt, vHgt);

                if (vPos > -aPos)
                {
                    // Scroll to the active node (top aligned)
                    GraphContentTransform.anchoredPosition = new(0, -aPos);
                }
                else if (vPos + vHgt < -aPos + aHgt)
                {
                    // Scroll to the active node (bottom aligned)
                    GraphContentTransform.anchoredPosition = new(0, -aPos + aHgt - vHgt);
                }
                
            }
            
        }

        public void ToggleNodeNamesVisibility()
        {
            nodeNamesVisible = !nodeNamesVisible;

            foreach (var item in GraphNodes)
                item.Value.SetNodeNameVisible(nodeNamesVisible);
        }

        public void ClearUp()
        {
            GraphNodes.Clear();
            activeNode = null;

            foreach (Transform child in GraphContentTransform!)
                GameObject.Destroy(child.gameObject);
            
            GraphContentTransform!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0F);
            
            AdjustWidth();
        }

        void Start() => ownTransform = GetComponent<RectTransform>();

        void Update()
        {
            if (!adjustingWidth) return;

            if (GraphContentTransform != null && ownTransform != null)
            {
                var target = GraphContentTransform.rect.width;

                //var ownWidth = Mathf.MoveTowards(ownTransform.rect.width, target, Time.unscaledDeltaTime * 500F);
                var ownWidth = Mathf.Lerp(ownTransform.rect.width, target, 0.2F);

                ownTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ownWidth);

                if (Mathf.Abs(target - ownWidth) < 1F) adjustingWidth = false;
            }
            else
                adjustingWidth = false;
        }
    }

    static class ModelGraphGenerator
    {
        static readonly bool D3;
        public static readonly Color32 INACTIVE, ACTIVE;

        static ModelGraphGenerator()
        {
            XElement settings = XDocument.Load(PathHelper.GetExtraDataFile("settings.xml")).Root;
            D3 = settings.Get("d3", true);
            INACTIVE = ColorConvert.OpaqueColor32FromHexString(settings.Get("inactive", "666666"));
            ACTIVE = ColorConvert.OpaqueColor32FromHexString(settings.Get("active", "FFFFFF"));
        }

        public static void GenerateGraph(ModelGraph graph, string modelName, Branch root, Dictionary<char, Color32> palette)
        {
            const int ZSHIFT = 4;
            const int TILE_SIZE = 10;

            // Clear up model graph first
            graph.ClearUp();
            
            graph.ModelNameText!.text = modelName;
            Color32 BACKGROUND = new(0, 0, 0, 0);

            void drawRectangle(Color32[] bitmap, int bitmapWidth, int bitmapHeight, int x, int y, int width, int height, Color32 color)
            {
                for (int dy = 0; dy < height; dy++) for (int dx = 0; dx < width; dx++) bitmap[x + dx + (bitmapHeight - 1 - y - dy) * bitmapWidth] = color;
            };
            void drawShadedSquare(Color32[] bitmap, int bitmapWidth, int bitmapHeight, int x, int y, int S, Color32 color)
            {
                drawRectangle(bitmap, bitmapWidth, bitmapHeight, x, y, S, S, color);
                drawRectangle(bitmap, bitmapWidth, bitmapHeight, x + S, y, 1, S + 1, BACKGROUND);
                drawRectangle(bitmap, bitmapWidth, bitmapHeight, x, y + S, S + 1, 1, BACKGROUND);
            };
            Texture2D getPreview(byte[] a, int MX, int MY, int MZ, char[] characters, int S)
            {
                int texWidth  = MX * S + MZ * ZSHIFT;
                int texHeight = MY * S + MZ * ZSHIFT;

                var bitmap = new Color32[texWidth * texHeight];

                for (int dz = 0; dz < MZ; dz++) for (int dy = 0; dy < MY; dy++) for (int dx = 0; dx < MX; dx++)
                        {
                            byte i = a[dx + dy * MX + dz * MX * MY];
                            Color32 color = i != 0xff ? palette[characters[i]] : (D3 ? INACTIVE : BACKGROUND);

                            drawShadedSquare(bitmap, texWidth, texHeight, dx * S + (MZ - dz - 1) * ZSHIFT, dy * S + (MZ - dz - 1) * ZSHIFT, S, color);
                        }
                
                var texture = new Texture2D(texWidth, texHeight);
                texture.SetPixels32(bitmap);
                texture.filterMode = FilterMode.Point;
                texture.Apply();

                return texture;
            };
            void generate(ModelGraph graph, Node node, Transform transform)
            {
                var nodeName = GameScene.GetL10nString(GetNodeNameKey(node));
                var nodeNumId = node.numId;
                char[] characters = node.grid.characters;

                BaseGraphNode nodeCmp;

                if (node is Branch branch)
                {
                    var nodeObj = GameObject.Instantiate(graph.ScopeGraphNodePrefab, transform);
                    nodeCmp = nodeObj!.GetComponent<ScopeGraphNode>();

                    foreach (var child in branch.nodes) // Generate child nodes
                        generate(graph, child, nodeObj.transform);
                }
                else if (node is RuleNode ruleNode)
                {
                    var nodeObj = GameObject.Instantiate(graph.RuleGraphNodePrefab, transform);
                    nodeCmp = nodeObj!.GetComponent<RuleGraphNode>();

                    for (int r = 0; r < ruleNode.rules.Length; r++)
                    {
                        Rule rule = ruleNode.rules[r];
                        rule.ruleIndex = r;
                        if (!rule.original) continue;

                        var inPreview  = getPreview(rule.binput, rule.IMX, rule.IMY, rule.IMZ, characters, TILE_SIZE);
                        var outPreview = getPreview(rule.output, rule.OMX, rule.OMY, rule.OMZ, characters, TILE_SIZE);

                        (nodeCmp as RuleGraphNode)!.AddRulePreview(r, inPreview, outPreview);
                    }

                    // Assign this graph node
                    graph.GraphNodes.TryAdd(nodeNumId, nodeCmp);
                    
                }
                else
                {
                    var nodeObj = GameObject.Instantiate(graph.RuleGraphNodePrefab, transform);
                    nodeCmp = nodeObj!.GetComponent<BaseGraphNode>();
                }

                // Set node data
                nodeCmp.SetNodeName($"{nodeName} <color=#888888>#{node.numId}</color>");
                nodeCmp.SetNodeActive(false);

                // Assign this graph node
                graph.GraphNodes.TryAdd(nodeNumId, nodeCmp);
            };
            
            // Start generation
            generate(graph, root, graph.GraphContentTransform!);

            // Adjust own width to fit graph content
            graph.AdjustWidth();
        }

        public static void UpdateGraph(ModelGraph graph, Branch? current)
        {
            if (current is null)
            {
                graph.SetActiveNode(-1);
                return;
            }
            
            if (current.n < 0) // Set the whole branch as active
            {
                graph.SetActiveNode(current.numId);
            }
            else // Set the current child node as active
            {
                var currentNode = current.nodes[current.n];
                graph.SetActiveNode(currentNode.numId);

                if (currentNode is RuleNode ruleNode && graph.ActiveNode is RuleGraphNode ruleGraphNode) // Set active branch
                    ruleGraphNode.SetActiveRules(GetActiveRules(ruleNode));
                
            }
        }

        private static Dictionary<Type, string> NodeNameDict = new()
        {
            [typeof (OneNode)] =           "one",
            [typeof (AllNode)] =           "all",
            [typeof (ParallelNode)] =      "prl",
            [typeof (MarkovNode)] =        "markov",
            [typeof (SequenceNode)] =      "sequence",
            [typeof (PathNode)] =          "path",
            [typeof (MapNode)] =           "map",
            [typeof (ConvolutionNode)] =   "convolution",
            [typeof (ConvChainNode)] =     "convchain",
            [typeof (OverlapNode)] =       "wfc_overlap",
            [typeof (TileNode)] =          "wfc_tile",
        };

        private static string GetNodeNameKey(Node node)
        {
            //return node.GetType().ToString().Split('.')[^1];
            return "node.name." + NodeNameDict.GetValueOrDefault(node.GetType(), "unknown");
        }

        private static List<int> GetActiveRules(RuleNode node)
        {
            var result = new List<int>();
            for (int index = 0;index < node.rules.Length;index++)
            {
                if (node.last[index])
                {
                    result.Add(index);
                    break;
                }

                for (int r = index + 1; r < node.rules.Length; r++)
                {
                    Rule rule = node.rules[r];
                    if (rule.original) break;
                    if (node.last[r])
                    {
                        result.Add(index);
                        break;
                    }
                }
            }

            return result;
        }
    }
}