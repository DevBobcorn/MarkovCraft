#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarkovCraft
{
    public class ReplayScreen : BaseScreen
    {
        private GameScene? game;

        private void Start()
        {
            game = GameScene.Instance;
        }

        public override bool AllowsMovementInput() => true;

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (!game!.Loading)
                    manager.SetActiveScreenByType<PauseScreen>();
            }
        }
    }
}