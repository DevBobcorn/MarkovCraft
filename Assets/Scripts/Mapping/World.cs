using Unity.Mathematics;

namespace MarkovCraft.Mapping
{
    /// <summary>
    /// Represents a Minecraft World
    /// Placeholder in this project
    /// </summary>
    public class World
    {
        public static readonly float3 DEFAULT_FOLIAGE = new float3(119, 255,  47) / 255F;
        public static readonly float3 DEFAULT_GRASS   = new float3( 95, 255,  39) / 255F;
        public static readonly float3 DEFAULT_WATER   = new float3( 63, 118, 228) / 255F;

        public float3 GetFoliageColor(Location loc) => DEFAULT_FOLIAGE;

        public float3 GetGrassColor(Location loc) => DEFAULT_GRASS;

        public float3 GetWaterColor(Location loc) => DEFAULT_WATER;

    }
}