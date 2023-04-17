#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    public class HUDScreen : BaseScreen
    {
        public override void ScreenUpdate(ScreenManager manager)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                manager.SetActiveScreenByType<ModelEditorScreen>();
            }

        }
    }
}