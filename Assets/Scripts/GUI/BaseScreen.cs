#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    [RequireComponent(typeof (CanvasGroup), typeof (Animator))]
    public abstract class BaseScreen : MonoBehaviour
    {
        protected ScreenManager? manager;

        public void Show(ScreenManager manager)
        {
            GetComponent<Animator>().SetBool("Hidden", false);
            OnShow(this.manager = manager);

        }

        public void Hide(ScreenManager manager)
        {
            GetComponent<Animator>().SetBool("Hidden", true);
            OnHide(manager);

        }

        public virtual void OnShow(ScreenManager manager) { }

        public virtual void OnHide(ScreenManager manager) { }

        public virtual bool ShouldPause() => false;

        public abstract void ScreenUpdate(ScreenManager manager);
    }
}