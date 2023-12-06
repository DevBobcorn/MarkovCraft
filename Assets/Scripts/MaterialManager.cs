#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CraftSharp.Resource;

namespace CraftSharp
{
    public class MaterialManager : MonoBehaviour
    {
        [SerializeField] public Material? AtlasSolid;
        [SerializeField] public Material? AtlasCutout;
        [SerializeField] public Material? AtlasCutoutMipped;
        [SerializeField] public Material? AtlasTranslucent;
        [SerializeField] public Material? AtlasWater;

        private readonly Dictionary<RenderType, Material> atlasMaterials = new();
        private Material? defaultAtlasMaterial;

        private bool initialized = false;

        public Material GetAtlasMaterial(RenderType renderType)
        {
            EnsureInitialized();
            return atlasMaterials.GetValueOrDefault(renderType, defaultAtlasMaterial!);
        }

        public Material[] GetMaterialArray(RenderType[] renderTypes)
        {
            EnsureInitialized();
            return renderTypes.Select(x => atlasMaterials.GetValueOrDefault(x, defaultAtlasMaterial!)).ToArray();
        }

        public void EnsureInitialized()
        {
            if (!initialized) Initialize();
        }

        // Used when textures are reloaded and materials need to be regenerated
        public void ClearInitializeFlag()
        {
            initialized = false;
        }

        private void Initialize()
        {
            atlasMaterials.Clear();
            var packManager = ResourcePackManager.Instance;

            // Solid
            var solid = new Material(AtlasSolid!);
            solid.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.SOLID, solid);

            defaultAtlasMaterial = solid;

            // Cutout & Cutout Mipped
            var cutout = new Material(AtlasCutout!);
            cutout.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.CUTOUT, cutout);

            var cutoutMipped = new Material(AtlasCutoutMipped!);
            cutoutMipped.SetTexture("_BaseMap", packManager.GetAtlasArray(true));
            atlasMaterials.Add(RenderType.CUTOUT_MIPPED, cutoutMipped);

            // Translucent
            var translucent = new Material(AtlasTranslucent!);
            translucent.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            atlasMaterials.Add(RenderType.TRANSLUCENT, translucent);

            // Water
            atlasMaterials.Add(RenderType.WATER, translucent);

            initialized = true;
        }
    }
}