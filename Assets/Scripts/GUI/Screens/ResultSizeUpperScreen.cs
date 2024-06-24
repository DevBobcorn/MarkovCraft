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
            "size_upper.method.scale_up",
            "size_upper.method.stack"
        };

        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public Button? ConfirmButton, CancelButton;
        [SerializeField] public TMP_Dropdown? SizeUpperDropdown;
        [SerializeField] public TMP_InputField? SizeXInput;
        [SerializeField] public TMP_InputField? SizeYInput;
        [SerializeField] public TMP_InputField? SizeZInput;
        private int scaleX = 0, scaleY = 0, scaleZ = 0;
        private int scaledX = 0, scaledY = 0, scaledZ = 0;
        int[] scaledState = { };
        // Result Preview
        [SerializeField] public Image? ResultPreviewImage;
        // Result Detail Panel
        [SerializeField] public ResultDetailPanel? ResultDetailPanel;

        private GenerationResult? result = null;
        public override GenerationResult? GetResult() => result;

        private bool working = false, properlyLoaded = false;

        private IEnumerator InitializeScreen()
        {
            if (result == null)
            {
                Debug.LogWarning($"ERROR: Size upper screen not correctly initialized!");
                working = false;
                properlyLoaded = false;
                yield break;
            }

            // Initialize settings panel
            ConfirmButton!.onClick.RemoveAllListeners();
            ConfirmButton.onClick.AddListener(ConfirmResize);
            CancelButton!.onClick.RemoveAllListeners();
            CancelButton.onClick.AddListener(() => manager?.SetActiveScreenByType<GenerationScreen>());

            SizeUpperDropdown!.ClearOptions();
            SizeUpperDropdown.AddOptions(SIZE_UPPER_KEYS.Select(x =>
                    new TMP_Dropdown.OptionData(GameScene.GetL10nString(x))).ToList());
            SizeUpperDropdown!.onValueChanged.RemoveAllListeners();
            SizeUpperDropdown!.onValueChanged.AddListener((_) => UpdateResizeData());

            // Reset scale size
            scaleX = scaleY = scaleZ = 1;
            SizeXInput!.SetTextWithoutNotify("1");
            SizeYInput!.SetTextWithoutNotify("1");
            SizeZInput!.SetTextWithoutNotify("1");

            SizeXInput!.GetComponent<IntegerInputValidator>().OnValidateValue!.AddListener(x => {
                if (scaleX != x)
                {
                    scaleX = x;
                    UpdateResizeData();
                }
            });

            SizeYInput!.GetComponent<IntegerInputValidator>().OnValidateValue!.AddListener(y => {
                if (scaleY != y)
                {
                    scaleY = y;
                    UpdateResizeData();
                }
            });

            SizeZInput!.GetComponent<IntegerInputValidator>().OnValidateValue!.AddListener(z => {
                if (scaleZ != z)
                {
                    scaleZ = z;
                    UpdateResizeData();
                }
            });

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("size_upper.text.loaded");
            
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

        public void ConfirmResize()
        {
            if (properlyLoaded)
            {
                result!.UpdateBlockData(scaledState, scaledX, scaledY, scaledZ);

                if (GameScene.Instance is not GenerationScene game)
                {
                    Debug.LogError("Wrong game scene!");
                    working = false;
                    return;
                }

                game.UpdateSelectedResult(null);
                manager?.SetActiveScreenByType<GenerationScreen>();
            }
        }

        public void UpdateResizeData()
        {
            if (result == null) return;
            //Debug.Log($"Updating resize preview: {scaleX}x{scaleY}x{scaleZ}");

            int sizeX = result.SizeX;
            int sizeY = result.SizeY;
            int sizeZ = result.SizeZ;
            scaledX = scaleX * sizeX;
            scaledY = scaleY * sizeY;
            scaledZ = scaleZ * sizeZ;

            scaledState = new int[scaledX * scaledY * scaledZ];
            int[] state = result.BlockData;

            if (SizeUpperDropdown!.value == 0) // Scale up
            {
                for (int ix = 0;ix < sizeX;ix++) for (int iy = 0;iy < sizeY;iy++) for (int iz = 0;iz < sizeZ;iz++)
                {
                    for (int sx = 0;sx < scaleX;sx++) for (int sy = 0;sy < scaleY;sy++) for (int sz = 0;sz < scaleZ;sz++)
                    {
                        int x = sx + ix * scaleX;
                        int y = sy + iy * scaleY;
                        int z = sz + iz * scaleZ;
                        
                        scaledState[x + y * scaledX + z * scaledX * scaledY] = state[ix + iy * sizeX + iz * sizeX * sizeY];
                    }
                }
            }
            else // Stack
            {
                for (int sx = 0;sx < scaleX;sx++) for (int sy = 0;sy < scaleY;sy++) for (int sz = 0;sz < scaleZ;sz++)
                {
                    for (int ix = 0;ix < sizeX;ix++) for (int iy = 0;iy < sizeY;iy++) for (int iz = 0;iz < sizeZ;iz++)
                    {
                        int x = sx * sizeX + ix;
                        int y = sy * sizeY + iy;
                        int z = sz * sizeZ + iz;
                        
                        scaledState[x + y * scaledX + z * scaledX * scaledY] = state[ix + iy * sizeX + iz * sizeX * sizeY];
                    }
                }
            }

            ResultDetailPanel!.UpdateSizeAndState(scaledX, scaledY, scaledZ, scaledState);
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
            if (result == null)
            {
                Debug.LogWarning("Size upper is not properly loaded!");

                ScreenHeader!.text = GenerationScene.GetL10nString("screen.text.load_failure");

                working = false;
                return;
            }

            scaledState = new int[result.BlockData.Length];
            result.BlockData.CopyTo(scaledState, 0);

            StartCoroutine(InitializeScreen());
        }

        public override void ScreenUpdate(ScreenManager manager)
        {
            if (working) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                manager.SetActiveScreenByType<GenerationScreen>();
            }
        }
    }
}