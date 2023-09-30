#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

using CraftSharp;
using CraftSharp.Resource;
using System.Text;

namespace MarkovCraft
{
    [Serializable]
    public class BlockGroupItemInfo
    {
        [SerializeField] public string BlockId = string.Empty;
        [SerializeField] public string TextureId = string.Empty;
    }

    public class BlockGroup : MonoBehaviour, Json.IJSONSerializable
    {
        [SerializeField] private TMP_Text? groupTitleText;
        [SerializeField] private TMP_Text? selectedCountText;
        [SerializeField] private RectTransform? visibilityArrow;
        [SerializeField] private RectTransform? groupItems;
        [SerializeField] private GameObject? groupItemPrefab;
        [SerializeField] private BlockGroupItemInfo[] itemSource = { };
        private bool groupShown = false;
        private bool defaultSelected;

        private (ResourceLocation blockId, Color32 color, Toggle toggle)[] itemInfo = { };



        public void SetData(string groupName, BlockGroupItemInfo[] items, bool defaultSelected)
        {
            groupTitleText!.text = groupName;
            var blockTable = BlockStatePalette.INSTANCE.StateListTable;

            this.defaultSelected = defaultSelected;

            // Select only blocks which are present in currently loaded data
            itemSource = items.Where(x => blockTable.ContainsKey(ResourceLocation.FromString(x.BlockId))).ToArray();

            if (defaultSelected)
            {
                // Items are shown by default
                groupShown = true;
                // Arrow pointing up, meaning "click to hide"
                visibilityArrow!.localEulerAngles = new float3(0F, 0F, 180F);
            }
            else
            {
                // Items are hidden by default
                groupShown = false;
                // Arrow pointing down, meaning "click to show"
                visibilityArrow!.localEulerAngles = float3.zero;
            }

            var packManager = ResourcePackManager.Instance;
            List<(ResourceLocation, Color32, Toggle)> infoList = new();

            // Populate group items
            foreach (var item in itemSource)
            {
                var itemObj = Instantiate(groupItemPrefab);
                itemObj!.transform.SetParent(groupItems, false);

                var blockId = ResourceLocation.FromString(item.BlockId);
                var itemText = itemObj.GetComponentInChildren<TMP_Text>();
                itemText.text = GameScene.GetL10nBlockName(blockId);

                ResourceLocation textureId;

                if (string.IsNullOrWhiteSpace(item.TextureId))
                {
                    textureId = new ResourceLocation(blockId.Namespace, $"block/{blockId.Path}");
                }
                else
                {
                    textureId = ResourceLocation.FromString(item.TextureId);
                }

                if (packManager.TextureFileTable.ContainsKey(textureId))
                {
                    var itemTexture = itemText.GetComponentInChildren<Image>();
                    // Load block texture
                    var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                    var bytes = File.ReadAllBytes(packManager.TextureFileTable[textureId]);
                    tex.LoadImage(bytes);
                    // Crop texture if necessary
                    if (tex.width != tex.height)
                    {
                        var shortSide = Mathf.Min(tex.width, tex.height);
                        var px = new Color32[shortSide * shortSide];
                        for (int i = 0;i < shortSide;i++) for (int j = 0;j < shortSide;j++)
                        {
                            px[i * shortSide + j] = tex.GetPixel(j, i);
                        }

                        tex = new Texture2D(shortSide, shortSide) { filterMode = FilterMode.Point };
                        tex.SetPixels32(px);
                        tex.Apply();
                    }
                    // Update sprite
                    var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
                    itemTexture.sprite = sprite;
                    // Calculate average color of this texture
                    var pixels = tex.GetPixels32();
                    int rSum = 0, gSum = 0, bSum = 0;
                    for (int pix = 0;pix < pixels.Length;pix++)
                    {
                        rSum += pixels[pix].r;
                        gSum += pixels[pix].g;
                        bSum += pixels[pix].b;
                    }
                    float tot = 255F * pixels.Length;

                    var averageColor = new Color(rSum / tot, gSum / tot, bSum / tot, 1F);
                    //itemText.color = averageColor;
                    var toggle = itemObj.GetComponent<Toggle>();

                    if (!defaultSelected)
                    {
                        toggle.isOn = false;
                        toggle.gameObject.SetActive(false);
                    }

                    // Register toggle value callback
                    toggle.onValueChanged.AddListener(v => UpdateSelectedCount());

                    infoList.Add(( blockId, averageColor, toggle));
                }
                else // Mark this item as unavailable
                {
                    itemText.color = Color.red;
                }
            }

            itemInfo = infoList.ToArray();

            // Update selected count
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            int selected = itemInfo.Where(x => x.toggle.isOn).Count();

            selectedCountText!.text = $"{selected}/{itemInfo.Length}";
        }

        public void SelectAll()
        {
            groupShown = true;
            // Arrow pointing up, meaning "click to hide"
            visibilityArrow!.localEulerAngles = new float3(0F, 0F, 180F);

            foreach (var (_, _, toggle) in itemInfo)
            {
                toggle.isOn = true;
                // Show all items in group
                toggle.gameObject.SetActive(true);
            }
        }

        public void SelectNone()
        {
            foreach (var (_, _, toggle) in itemInfo)
            {
                toggle.isOn = false;
            }
        }

        public void ToggleVisibility()
        {
            groupShown = !groupShown;
            if (groupShown) // Arrow pointing up, meaning "click to hide"
            {
                visibilityArrow!.localEulerAngles = new float3(0F, 0F, 180F);
            }
            else // Arrow pointing down, meaning "click to show"
            {
                visibilityArrow!.localEulerAngles = float3.zero;
            }

            foreach (var (_, _, toggle) in itemInfo)
            {
                // Toggle all items in group
                toggle.gameObject.SetActive(groupShown);
            }
        }

        public void AppendSelected(ref Dictionary<ResourceLocation, Color32> mapping)
        {
            foreach (var (blockId, color, toggle) in itemInfo)
            {
                if (toggle.isOn) // This item is selected
                {
                    mapping.Add(blockId, color);
                }
            }
        }

        public string ToJson()
        {
            var sb = new StringBuilder("{");

            sb.Append("\"default_selected\":");
            sb.Append(defaultSelected.ToString().ToLower());

            sb.Append(",\"blocks\":{");
            sb.Append(string.Join(", ", itemSource.Select(x => $"\"{x.BlockId}\":\"{x.TextureId}\"")));
            sb.Append("}");

            sb.Append(",\"names\":{");
            sb.Append("}");

            sb.Append("}");

            return sb.ToString();
        }
    }
}