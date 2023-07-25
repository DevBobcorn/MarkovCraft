#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MinecraftClient;
using MinecraftClient.Resource;

namespace MarkovCraft
{
    [Serializable]
    public class BlockGroupItemInfo
    {
        [SerializeField] public string BlockId = string.Empty;
        [SerializeField] public string TextureId = string.Empty;
    }

    public class BlockGroup : MonoBehaviour
    {
        [SerializeField] private TMP_Text? groupTitleText;
        [SerializeField] private RectTransform? groupItems;
        [SerializeField] private GameObject? groupItemPrefab;
        [SerializeField] private BlockGroupItemInfo[] itemSource = { };

        private (ResourceLocation blockId, Color32 color, Toggle toggle)[] itemInfo = { };

        private bool initialized = false;

        public void SetData(string groupName, BlockGroupItemInfo[] items)
        {
            groupTitleText!.text = groupName;
            itemSource = items;
        }

        public void SelectAll()
        {
            if (!initialized)
            {
                return;
            }

            foreach (var item in itemInfo)
            {
                item.toggle.isOn = true;
                item.toggle.gameObject.SetActive(true);
            }
        }

        public void SelectNone()
        {
            if (!initialized)
            {
                return;
            }

            foreach (var item in itemInfo)
            {
                item.toggle.isOn = false;
                item.toggle.gameObject.SetActive(false);
            }
        }

        public void AppendSelected(ref Dictionary<ResourceLocation, Color32> mapping)
        {
            foreach (var item in itemInfo)
            {
                if (item.toggle.isOn) // This item is selected
                {
                    mapping.Add(item.blockId, item.color);
                }
            }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            var packManager = ResourcePackManager.Instance;
            List<(ResourceLocation, Color32, Toggle)> infoList = new();

            // Populate group items
            foreach (var item in itemSource)
            {
                var itemObj = Instantiate(groupItemPrefab);
                itemObj!.transform.SetParent(groupItems, false);

                var blockId = ResourceLocation.fromString(item.BlockId);
                var itemText = itemObj.GetComponentInChildren<TMP_Text>();
                itemText.text = GameScene.GetL10nBlockName(blockId);

                ResourceLocation textureId;

                if (string.IsNullOrWhiteSpace(item.TextureId))
                {
                    textureId = new ResourceLocation($"block/{item.BlockId}");
                }
                else
                {
                    textureId = ResourceLocation.fromString(item.TextureId);
                }

                if (packManager.TextureFileTable.ContainsKey(textureId))
                {
                    var itemTexture = itemText.GetComponentInChildren<Image>();
                    // Load block texture
                    var tex = new Texture2D(2, 2);
                    tex.filterMode = FilterMode.Point;
                    var bytes = File.ReadAllBytes(packManager.TextureFileTable[textureId]);
                    tex.LoadImage(bytes);
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

                    infoList.Add(( blockId, averageColor, itemObj.GetComponent<Toggle>()));
                }
                else // Mark this item as unavailable
                {
                    itemText.color = Color.red;
                }
            }

            itemInfo = infoList.ToArray();
        }
    }
}