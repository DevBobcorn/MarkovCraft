#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarkovCraft
{
    public class PauseScreen : BaseScreen
    {
        [SerializeField] private BaseScreen? normalScreen;
        [SerializeField] private GameScene? game;

        public override bool ShouldPause() => true;

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                BackToGame();
            }
        }

        public void ReturnToMenu()
        {
            game!.ReturnToMenu();
            
        }

        public void BackToGame()
        {
            if (normalScreen)
            {
                manager!.SetActiveScreen(normalScreen);
            }
        }
    }
}