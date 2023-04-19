using System.Collections.Generic;
using UnityEngine;

namespace MarkovCraft
{
    public static class MaterialManager {
        private static Dictionary<RenderType, Material> blockMaterials = new();

        private static Material defaultMaterial;

        private static bool initialized = false;

        public static Material GetAtlasMaterial(RenderType renderType)
        {
            EnsureInitialized();
            return blockMaterials.GetValueOrDefault(renderType, defaultMaterial);
        }

        public static void EnsureInitialized()
        {
            if (!initialized) Initialize();
        }

        public static void ClearInitializedFlag()
        {
            initialized = false;
        }

        private static void Initialize()
        {
            blockMaterials.Clear();

            var material = Resources.Load<Material>("Materials/BlockMaterial");
            material.SetTexture("_BaseMap", AtlasManager.GetAtlasArray(RenderType.SOLID));

            blockMaterials.Add(RenderType.SOLID, material);
            blockMaterials.Add(RenderType.CUTOUT, material);
            blockMaterials.Add(RenderType.CUTOUT_MIPPED, material);
            blockMaterials.Add(RenderType.TRANSLUCENT, material);
            blockMaterials.Add(RenderType.WATER, material);

            defaultMaterial = material;

            initialized = true;

        }

    }

}