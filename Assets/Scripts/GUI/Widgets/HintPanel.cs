#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class HintPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup? canvasGroup;

        void Start() => HidePanel();

        public void ShowPanel()
        {
            canvasGroup!.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void HidePanel()
        {
            canvasGroup!.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}