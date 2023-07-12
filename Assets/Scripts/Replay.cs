#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using TMPro;

using MinecraftClient;
using MinecraftClient.Resource;
using MinecraftClient.Mapping;

namespace MarkovCraft
{
    public class Replay : GameScene
    {
        [SerializeField] private ScreenManager? screenManager;
        [SerializeField] public TMP_Text? PlaybackSpeedText, ReplayText, FPSText;
        
        private float playbackSpeed = 1F;
        private bool replaying = false;

        void Start()
        {
            // First load Minecraft data & resources
            var ver = VersionHolder!.Versions[VersionHolder.SelectedVersion];

            StartCoroutine(LoadMCBlockData(ver.DataVersion, ver.ResourceVersion,
                () => {
                    //ExecuteButton!.interactable = false;
                    //ExecuteButton.GetComponentInChildren<TMP_Text>().text = GetL10nString("hud.text.load_resource");
                },
                (status) => ReplayText!.text = GetL10nString(status),
                () => {
                    ReplayText!.text = "UwU";
                })
            );
        }

        void Update()
        {
            if (FPSText != null)
                FPSText.text = $"FPS:{((int)(1 / Time.unscaledDeltaTime)).ToString().PadLeft(4, ' ')}";
            
        }

        public void StartReplay()
        {

        }

        public void StopReplay()
        {

        }

        private void ClearUpScene()
        {
            // Clear up persistent entities
            BlockInstanceSpawner.ClearUpPersistentState();
        }

        public override void ReturnToMenu()
        {
            if (replaying)
                StopReplay();
            
            ClearUpScene();

            // Unpause game to restore time scale
            screenManager!.IsPaused = false;

            SceneManager.LoadScene("Scenes/Welcome", LoadSceneMode.Single);
        }
    }
}