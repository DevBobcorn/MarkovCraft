#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class WelcomeScreen : BaseScreen
    {
        [SerializeField] Button? ResourcePacksButton;

        public override void OnShow(ScreenManager manager)
        {
            if (ResourcePacksButton != null)
            {
                ResourcePacksButton.onClick.RemoveAllListeners();
                ResourcePacksButton.onClick.AddListener(() =>
                        manager.SetActiveScreenByType<ResourcePacksScreen>());
            }
            
        }

        public void SetEnterGameTrigger()
        {
            GetComponent<Animator>().SetTrigger("Enter");
        }

        public override bool AllowsMovementInput() => false;

        public override void ScreenUpdate(ScreenManager manager)
        {
            // Do nothing...
        }
    }
}