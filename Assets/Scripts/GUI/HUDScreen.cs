#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class HUDScreen : BaseScreen
    {
        private Markov? game;

        void Start()
        {
            game = Markov.Instance;
        }

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