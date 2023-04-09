#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    public class ScreenManager : MonoBehaviour
    {
        private BaseScreen? activeScreen;
        public BaseScreen? ActiveScreen => activeScreen;

        public void SetActiveScreen(BaseScreen newScreen)
        {
            if (activeScreen != null)
                activeScreen.Hide(this);
            
            activeScreen = newScreen;

            newScreen.Show(this);
        }

        public void SetActiveScreenByType<T>() where T : BaseScreen
        {
            var screen = Component.FindObjectOfType<T>();

            SetActiveScreen(screen);
        }

        void Start()
        {
            SetActiveScreenByType<HUDScreen>();
        }

        void Update()
        {
            if (activeScreen != null) // Update screen
                activeScreen.ScreenUpdate(this);
            
        }
    }
}