#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class ReplayScreen : BaseScreen
    {
        private Replay? game;

        void Start()
        {
            game = Replay.Instance;
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!game!.Loading)
                    manager.SetActiveScreenByType<PauseScreen>();
            }
        }
    }
}