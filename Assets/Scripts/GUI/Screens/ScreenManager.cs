#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField] private BaseScreen? initialScreen;
        private BaseScreen? ActiveScreen { get; set; }

        private bool isPaused = true;
        public bool IsPaused
        {
            get => isPaused;

            set {
                isPaused = value;
                Time.timeScale = isPaused ? 0F : 1F;
            }
        }

        public bool AllowsMovementInput = false;

        public void SetActiveScreen(BaseScreen newScreen)
        {
            if (ActiveScreen)
            {
                ActiveScreen.Hide(this);
            }
            
            ActiveScreen = newScreen;
            IsPaused = newScreen.ShouldPause();
            AllowsMovementInput = newScreen.AllowsMovementInput();

            newScreen.Show(this);
        }

        public void SetActiveScreenByType<T>() where T : BaseScreen
        {
            var screen = Component.FindFirstObjectByType<T>();

            SetActiveScreen(screen);
        }

        private void Start()
        {
            if (initialScreen) // Initialize screen
            {
                SetActiveScreen(initialScreen);
            }
        }

        private void Update()
        {
            if (ActiveScreen)
            {
                ActiveScreen.ScreenUpdate(this);
            }
        }
    }
}