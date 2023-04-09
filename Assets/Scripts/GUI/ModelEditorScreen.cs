#nullable enable
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MarkovBlocks
{
    public class ModelEditorScreen : BaseScreen
    {
        [SerializeField] public TMP_Text? ScreenHeader;
        [SerializeField] public RectTransform? GridTransform;
        [SerializeField] public GameObject? MappingItemPrefab;

        private readonly List<MappingItem> mappingItems = new();

        public override void OnShow(ScreenManager manager)
        {
            if (mappingItems.Count > 0)
                ClearItems();
            
            var game = Test.Instance;
            var model = game.CurrentModel;

            if (model != null)
            {
                if (ScreenHeader != null)
                {
                    ScreenHeader.text = game.ConfiguredModelName;
                }

                foreach (var item in model.CustomRemapping)
                {
                    var newItemObj = GameObject.Instantiate(MappingItemPrefab);
                    var newItem = newItemObj!.GetComponent<MappingItem>();

                    mappingItems.Add(newItem);

                    newItem.InitializeData(item.Symbol, ColorConvert.GetRGB(item.RemapColor), item.RemapTarget);

                    newItem.transform.SetParent(GridTransform);
                    newItem.transform.localScale = Vector3.one;
                }
            }


        }

        public override void OnHide(ScreenManager manager)
        {
            ClearItems();

        }

        private void ClearItems()
        {
            var array = mappingItems.ToArray();

            for (int i = 0;i < array.Length;i++)
                Destroy(array[i].gameObject);
            
            mappingItems.Clear();
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                manager.SetActiveScreenByType<HUDScreen>();
            }

        }
    }
}