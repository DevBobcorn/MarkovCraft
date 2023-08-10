#nullable enable
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class ResultSizeUpperScreen : ResultManipulatorScreen
    {
        private static readonly string[] SIZE_UPPER_KEYS = {
            "size_upper.type.scale_up",
            "size_upper.type.stack"
        };

        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public Button? ConfirmButton, CancelButton;
        [SerializeField] public TMP_Dropdown? SizeUpperDropdown;
        // Result Preview
        [SerializeField] public Image? ResultPreviewImage;
        // Result Detail Panel
        [SerializeField] public ResultDetailPanel? ResultDetailPanel;

        private GenerationResult? result = null;
        public override GenerationResult? GetResult() => result;

        private bool working = false, properlyLoaded = false;

        private IEnumerator InitializeScreen()
        {
            if (result is null)
            {
                Debug.LogWarning($"ERROR: Size upper screen not correctly initialized!");
                working = false;
                properlyLoaded = false;
                yield break;
            }

            // Initialize settings panel
            ConfirmButton!.onClick.RemoveAllListeners();
            ConfirmButton.onClick.AddListener(() => { });
            CancelButton!.onClick.RemoveAllListeners();
            CancelButton.onClick.AddListener(() => { });

            SizeUpperDropdown!.ClearOptions();
            SizeUpperDropdown.AddOptions(SIZE_UPPER_KEYS.Select(x =>
                    new TMP_Dropdown.OptionData(GameScene.GetL10nString(x))).ToList());
            SizeUpperDropdown!.onValueChanged.RemoveAllListeners();
            SizeUpperDropdown!.onValueChanged.AddListener((_) => { });

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("size_upper.text.loaded", result.ConfiguredModelName);
            
            // Update Info text
            InfoText!.text = GameScene.GetL10nString("screen.text.result_info", result.ConfiguredModelName,
                    result.GenerationSeed, result.SizeX, result.SizeY, result.SizeZ);
            var prev = result.GetPreviewData();

            // Update Preview Image
            var (pixels, sizeX, sizeY) = ResultDetailPanel.RenderPreview(prev.sizeX, prev.sizeY, prev.sizeZ,
                    prev.blockData, prev.colors, prev.airIndices, result.SizeZ == 1 ? ResultDetailPanel.PreviewRotation.ZERO : ResultDetailPanel.PreviewRotation.NINETY);
            var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, sizeX, sizeY);
            //tex.filterMode = FilterMode.Point;
            // Update sprite
            var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
            ResultPreviewImage!.sprite = sprite;
            ResultPreviewImage!.SetNativeSize();

            // Initialize result detail panel
            ResultDetailPanel!.Show();
        }

        public override void OnShow(ScreenManager manager)
        {
            if (working) return;
            working = true;
            properlyLoaded = false;

            ScreenHeader!.text = GameScene.GetL10nString("screen.text.loading");
            
            if (GameScene.Instance is not GenerationScene game)
            {
                Debug.LogError("Wrong game scene!");
                working = false;
                return;
            }
            
            // Get selected result data
            result = game.GetSelectedResult();
            
            if (result is null)
            {
                Debug.LogWarning("Size upper is not properly loaded!");

                ScreenHeader!.text = GenerationScene.GetL10nString("screen.text.load_failure");

                working = false;
                return;
            }

            StartCoroutine(InitializeScreen());
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
                manager.SetActiveScreenByType<GenerationScreen>();

        }
    }
}