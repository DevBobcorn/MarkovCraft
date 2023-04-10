#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    [RequireComponent(typeof (CanvasGroup))]
    public abstract class BaseScreen : MonoBehaviour
    {
        public void Show(ScreenManager manager)
        {
            var canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            canvasGroup.alpha = 1F;

            OnShow(manager);
        }

        public void Hide(ScreenManager manager)
        {
            var canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            canvasGroup.alpha = 0F;

            OnHide(manager);
        }

        public virtual void OnShow(ScreenManager manager) { }

        public virtual void OnHide(ScreenManager manager) { }

        public virtual bool ShouldPause() => false;

        public abstract void ScreenUpdate(ScreenManager manager);
    }
}