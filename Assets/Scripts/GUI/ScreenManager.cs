#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField] private BaseScreen? initialScreen;
        private BaseScreen? activeScreen;
        public BaseScreen? ActiveScreen => activeScreen;
        
        private bool isPaused = true;
        public bool IsPaused
        {
            get => isPaused;

            set {
                isPaused = value;
                Time.timeScale = isPaused ? 0F : 1F;
            }
        }

        public void SetActiveScreen(BaseScreen newScreen)
        {
            if (activeScreen != null)
                activeScreen.Hide(this);
            
            activeScreen = newScreen;
            IsPaused = newScreen.ShouldPause();

            newScreen.Show(this);
        }

        public void SetActiveScreenByType<T>() where T : BaseScreen
        {
            var screen = Component.FindObjectOfType<T>();

            SetActiveScreen(screen);
        }

        void Start()
        {
            if (initialScreen != null) // Initialize screen
            {
                SetActiveScreen(initialScreen);
            }
        }

        void Update()
        {
            if (activeScreen != null) // Update screen
                activeScreen.ScreenUpdate(this);
            
        }
    }
}