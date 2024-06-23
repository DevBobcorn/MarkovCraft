#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class ResultRCONScreen : ResultManipulatorScreen
    {
        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public Button? ConfirmButton, CancelButton;
        [SerializeField] public TMP_InputField? PosXInput;
        [SerializeField] public TMP_InputField? PosYInput;
        [SerializeField] public TMP_InputField? PosZInput;
        [SerializeField] public TMP_InputField? HostInput, PortInput;
        [SerializeField] public TMP_InputField? PasswordInput;
        private int posX = 0, posY = 0, posZ = 0;
        private const string OUTPUT_POSX_KEY = "OutputPosX";
        private const string OUTPUT_POSY_KEY = "OutputPosY";
        private const string OUTPUT_POSZ_KEY = "OutputPosZ";
        private const string RCON_HOST_KEY = "RCONHost";
        private const string RCON_PORT_KEY = "RCONPort";
        private const string RCON_PASSWORD_KEY = "RCONPassword";
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
            ConfirmButton.onClick.AddListener(ConfirmRCON);
            CancelButton!.onClick.RemoveAllListeners();
            CancelButton.onClick.AddListener(() => manager?.SetActiveScreenByType<GenerationScreen>());

            // Load and set output position
            posX = PlayerPrefs.GetInt(OUTPUT_POSX_KEY, 0);
            posY = PlayerPrefs.GetInt(OUTPUT_POSY_KEY, 0);
            posZ = PlayerPrefs.GetInt(OUTPUT_POSZ_KEY, 0);
            PosXInput!.SetTextWithoutNotify(posX.ToString());
            PosYInput!.SetTextWithoutNotify(posY.ToString());
            PosZInput!.SetTextWithoutNotify(posZ.ToString());

            // Load and set RCON info
            var host = PlayerPrefs.GetString(RCON_HOST_KEY, "127.0.0.1");
            var port = PlayerPrefs.GetInt(RCON_PORT_KEY, 25575);
            var password = PlayerPrefs.GetString(RCON_PASSWORD_KEY, string.Empty);
            HostInput!.SetTextWithoutNotify(host);
            PortInput!.SetTextWithoutNotify(port.ToString());
            PasswordInput!.SetTextWithoutNotify(password);

            // Register position input events
            PosXInput!.GetComponent<IntegerInputValidator>().OnValidateValue!.AddListener(x => posX = x);
            PosYInput!.GetComponent<IntegerInputValidator>().OnValidateValue!.AddListener(y => posY = y);
            PosZInput!.GetComponent<IntegerInputValidator>().OnValidateValue!.AddListener(z => posZ = z);

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("rcon.text.loaded");
            
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

        public void ConfirmRCON()
        {
            if (properlyLoaded)
            {
                if (GameScene.Instance is not GenerationScene)
                {
                    Debug.LogError("Wrong game scene!");
                    working = false;
                    return;
                }

                var host = HostInput!.text;
                var port = int.Parse(PortInput!.text);
                var password = PasswordInput!.text;

                PlayerPrefs.SetInt(OUTPUT_POSX_KEY, posX);
                PlayerPrefs.SetInt(OUTPUT_POSY_KEY, posY);
                PlayerPrefs.SetInt(OUTPUT_POSZ_KEY, posZ);
                PlayerPrefs.SetString(RCON_HOST_KEY, host);
                PlayerPrefs.SetInt(RCON_PORT_KEY, port);
                PlayerPrefs.SetString(RCON_PASSWORD_KEY, password);
                PlayerPrefs.Save();

                using var client = new RCONClient(host, port);
                var accepted = client.Authenticate(password);

                if (accepted)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        client.SendCommand($"particle end_rod {posX} {posY + i * 0.1F:0.00} {posZ}", out Message _);
                    }
                }
                else
                {
                    Debug.LogWarning("RCON Authenication failed.");
                }
                //game.UpdateSelectedResult(null);
                //manager?.SetActiveScreenByType<GenerationScreen>();
            }
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
                Debug.LogWarning("RCON is not properly loaded!");

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
            {
                manager.SetActiveScreenByType<GenerationScreen>();
            }
        }
    }
}