#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    public class HUDScreen : BaseScreen
    {
        private Test? game;

        void Start()
        {
            game = Test.Instance;
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!game!.Loading)
                    manager.SetActiveScreenByType<ModelEditorScreen>();
            }

        }
    }
}