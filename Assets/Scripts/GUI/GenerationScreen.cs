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

        public override bool AllowsMovementInput() => true;

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!game!.Loading)
                    manager.SetActiveScreenByType<ModelEditorScreen>();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!game!.Loading)
                    manager.SetActiveScreenByType<PauseScreen>();
            }
        }
    }
}