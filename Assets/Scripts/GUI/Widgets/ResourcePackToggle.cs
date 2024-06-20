#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class ResourcePackToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color32 HIDDEN = new Color32(255, 255, 255, 0);
        private static readonly Color32 NORMAL = new Color32(50, 50, 50, 255);
        [SerializeField] private Image? spriteImage;
        [SerializeField] private Sprite? deselectIcon;

        private event Action? OnToggle;

        void Start()
        {
            if (spriteImage != null)
            {
                spriteImage.color = HIDDEN;
            }
        }

        public void AddToggleHandler(Action handler)
        {
            OnToggle += handler;
        }

        public void ClearToggleEvents()
        {
            OnToggle = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (spriteImage != null)
            {
                spriteImage.color = NORMAL;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (spriteImage != null)
            {
                spriteImage.color = HIDDEN;
            }
        }

        public void SetSelected(bool selected)
        {
            if (spriteImage != null)
            {
                spriteImage.overrideSprite = selected ? deselectIcon : null;
            }
        }

        public void ToggleClick()
        {
            OnToggle?.Invoke();
        }
    }
}