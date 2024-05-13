#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class PauseScreen : BaseScreen
    {
        [SerializeField] private BaseScreen? normalScreen;
        [SerializeField] private GameScene? game;

        public override bool ShouldPause() => true;

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
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
            if (normalScreen != null)
            {
                manager!.SetActiveScreen(normalScreen);
            }
        }
    }
}