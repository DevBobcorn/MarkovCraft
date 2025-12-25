#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarkovCraft
{
    public class GenerationScreen : BaseScreen
    {
        private GameScene? game;

        void Start()
        {
            game = GameScene.Instance;
        }

        public override void OnShow(ScreenManager manager) 
        {
            base.OnShow(manager);

            // Show model graph if available (don't use game reference here as it might has not been set)
            GameScene.Instance.ShowSpecialGUI();
        }

        public override bool AllowsMovementInput() => true;

        public override void ScreenUpdate(ScreenManager manager)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.cKey.wasPressedThisFrame)
            {
                if (!game!.Loading)
                {
                    game!.HideSpecialGUI();
                    manager.SetActiveScreenByType<ConfiguredModelEditorScreen>();
                }
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (!game!.Loading)
                {
                    game!.HideSpecialGUI();
                    manager.SetActiveScreenByType<PauseScreen>();
                }
            }
        }
    }
}