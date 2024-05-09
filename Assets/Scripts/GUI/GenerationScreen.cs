#nullable enable
using UnityEngine;

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

            // Show model graph if available
            game!.ShowSpecialGUI();
        }

        public override bool AllowsMovementInput() => true;

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (!game!.Loading)
                {
                    game!.HideSpecialGUI();
                    manager.SetActiveScreenByType<ConfiguredModelEditorScreen>();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
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