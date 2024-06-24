#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace MarkovCraft
{
    public class ResultRCONScreen : ResultManipulatorWithItemRemapScreen
    {
        [SerializeField] public TMP_Text? ScreenHeader, InfoText;
        // Settings Panel
        [SerializeField] public Button? OutputButton, ApplyMappingButton;
        [SerializeField] public TMP_Text? PreviewButtonText;
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

        private RCONClient? client = null;
        private bool areaPreview = false;
        private float lastPreviewTime = 0F;
        private const float PREVIEW_INTERVAL = 0.8F;

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
            OutputButton!.onClick.RemoveAllListeners();
            OutputButton.onClick.AddListener(ConfirmOutput);
            ApplyMappingButton!.onClick.RemoveAllListeners();
            ApplyMappingButton.onClick.AddListener(ApplyMappings);

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

            // Initialize remap components
            InitializeRemap();

            working = false;
            properlyLoaded = true;

            ScreenHeader!.text = GameScene.GetL10nString("rcon.text.loaded");
        }

        private bool ConnectRCON()
        {
            if (client == null)
            {
                var host = HostInput!.text;
                var port = int.Parse(PortInput!.text);
                var password = PasswordInput!.text;
                PlayerPrefs.SetString(RCON_HOST_KEY, host);
                PlayerPrefs.SetInt(RCON_PORT_KEY, port);
                PlayerPrefs.SetString(RCON_PASSWORD_KEY, password);
                PlayerPrefs.Save();
                
                try
                {
                    client = new RCONClient(host, port);
                }
                catch
                {
                    ScreenHeader!.text = GameScene.GetL10nString("rcon.text.connection_error");
                    client?.Dispose();
                    client = null;
                    return false;
                }

                if (client.Authenticate(password))
                {
                    ScreenHeader!.text = GameScene.GetL10nString("rcon.text.loaded");
                    Debug.Log("RCON connected");
                    return true;
                }
                else
                {
                    ScreenHeader!.text = GameScene.GetL10nString("rcon.text.wrong_password");
                    client.Dispose();
                    client = null;
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public void ToggleAreaPreview()
        {
            areaPreview = !areaPreview;

            if (areaPreview) // Turned it on
            {
                PlayerPrefs.SetInt(OUTPUT_POSX_KEY, posX);
                PlayerPrefs.SetInt(OUTPUT_POSY_KEY, posY);
                PlayerPrefs.SetInt(OUTPUT_POSZ_KEY, posZ);
                PlayerPrefs.Save();

                if (!ConnectRCON()) // Turn it back off if connection failed
                {
                    areaPreview = false;
                }
            }

            if (areaPreview)
            {
                PreviewButtonText!.text = GameScene.GetL10nString("rcon.text.preview_stop");
            }
            else
            {
                PreviewButtonText!.text = GameScene.GetL10nString("rcon.text.preview_show");
            }
        }

        void Update()
        {
            if (areaPreview && Time.unscaledTime - lastPreviewTime > PREVIEW_INTERVAL)
            {
                lastPreviewTime = Time.unscaledTime;
                if (client != null && result != null)
                {
                    int sx = result.SizeY, sy = result.SizeZ, sz = result.SizeX;
                    float xi = posX, yi = posY + 0.2F, zi = posZ;
                    // Markov Y (Minecraft X)
                    for (int i = 0; i <= sx * 2; i++)
                    {
                        var x = xi + i * 0.5F;
                        client.SendCommand($"particle end_rod {x:0.00} {yi     :0.00} {zi     :0.00}", out Message _);
                        client.SendCommand($"particle end_rod {x:0.00} {yi + sy:0.00} {zi     :0.00}", out Message _);
                        client.SendCommand($"particle end_rod {x:0.00} {yi     :0.00} {zi + sz:0.00}", out Message _);
                        client.SendCommand($"particle end_rod {x:0.00} {yi + sy:0.00} {zi + sz:0.00}", out Message _);
                    }
                    // Markov Z (Minecraft Y)
                    for (int i = 0; i <= sy * 2; i++)
                    {
                        var y = yi + i * 0.5F;
                        client.SendCommand($"particle end_rod {xi     :0.00} {y:0.00} {zi     :0.00}", out Message _);
                        client.SendCommand($"particle end_rod {xi + sx:0.00} {y:0.00} {zi     :0.00}", out Message _);
                        client.SendCommand($"particle end_rod {xi     :0.00} {y:0.00} {zi + sz:0.00}", out Message _);
                        client.SendCommand($"particle end_rod {xi + sx:0.00} {y:0.00} {zi + sz:0.00}", out Message _);
                    }
                    // Markov X (Minecraft Z)
                    for (int i = 0; i <= sz * 2; i++)
                    {
                        var z = zi + i * 0.5F;
                        client.SendCommand($"particle end_rod {xi     :0.00} {yi     :0.00} {z:0.00}", out Message _);
                        client.SendCommand($"particle end_rod {xi + sx:0.00} {yi     :0.00} {z:0.00}", out Message _);
                        client.SendCommand($"particle end_rod {xi     :0.00} {yi + sy:0.00} {z:0.00}", out Message _);
                        client.SendCommand($"particle end_rod {xi + sx:0.00} {yi + sy:0.00} {z:0.00}", out Message _);
                    }
                }
            }
        }

        private string[] GetPrintCommands(int px, int py, int pz, int sx, int sy, int sz,
                CustomMappingItem[] resultPalette, HashSet<int> airIndicies, int[] blockData)
        {
            string[] commands = new string[sx * sy * sz];

            for (int mcy = 0; mcy < sy; mcy++) for (int mcx = 0; mcx < sx; mcx++) for (int mcz = 0; mcz < sz; mcz++)
            {
                int resultIndex = blockData[mcz + mcx * sz + mcy * sz * sx];
                var blockState = airIndicies.Contains(resultIndex) ? "air" : resultPalette[resultIndex].BlockState;
                
                commands[mcz + mcx * sz + mcy * sz * sx] = $"setblock {px + mcx} {py + mcy} {pz + mcz} {blockState}";
            }

            return commands;
        }

        public void ConfirmOutput()
        {
            if (properlyLoaded)
            {
                if (GameScene.Instance is not GenerationScene)
                {
                    Debug.LogError("Wrong game scene!");
                    working = false;
                    return;
                }

                PlayerPrefs.SetInt(OUTPUT_POSX_KEY, posX);
                PlayerPrefs.SetInt(OUTPUT_POSY_KEY, posY);
                PlayerPrefs.SetInt(OUTPUT_POSZ_KEY, posZ);
                PlayerPrefs.Save();

                //game.UpdateSelectedResult(null);
                //manager?.SetActiveScreenByType<GenerationScreen>();

                if (ConnectRCON() && client != null)
                {
                    // MarkovCraft => Minecraft
                    int sx = result!.SizeY, sy = result.SizeZ, sz = result.SizeX;
                    var commands = GetPrintCommands(posX, posY, posZ, sx, sy, sz,
                            result.ResultPalette, result.AirIndices, result.BlockData);

                    foreach (var command in commands)
                    {
                        client.SendCommand(command, out Message _);
                    }
                }
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
            if (result == null)
            {
                Debug.LogWarning("RCON is not properly loaded!");

                ScreenHeader!.text = GameScene.GetL10nString("screen.text.load_failure");

                working = false;
                return;
            }

            // Initialize button text
            PreviewButtonText!.text = GameScene.GetL10nString("rcon.text.preview_show");

            StartCoroutine(InitializeScreen());
        }

        public override void OnHide(ScreenManager manager)
        {
            areaPreview = false;

            // Dispose client if present
            if (client != null)
            {
                client?.Dispose();
                client = null;
                Debug.Log("RCON disconnected");
            }

            // Finalize remap logic
            FinalizeRemap();
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