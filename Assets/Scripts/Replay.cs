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
        
        // Palettes and resources
        public class World : AbstractWorld { }
        public readonly World DummyWorld = new();

        private Mesh[] blockMeshes = { };
        private BlockGeometry?[] blockGeometries = { };
        private float3[] blockTints = { };
        private int blockMeshCount = 0;
        // Character => (meshIndex, meshColor)
        private readonly Dictionary<char, int2> palette = new();

        [HideInInspector] public bool Loading = false;

        private static Replay? instance;
        public static Replay Instance
        {
            get {
                if (instance == null)
                    instance = Component.FindObjectOfType<Replay>();

                return instance!;
            }
        }

        void Start()
        {
            
        }

        void Update()
        {
            
        }

        public override void ReturnToMenu()
        {
            // Unpause game to restore time scale
            screenManager!.IsPaused = false;

            SceneManager.LoadScene("Scenes/Welcome", LoadSceneMode.Single);
        }
    }
}