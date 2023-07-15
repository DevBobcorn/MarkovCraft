using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace MarkovCraft
{
    [MaterialProperty("_BaseColor")]
    public struct InstanceBlockColorComponent : IComponentData
    {
        public float4 Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class InstanceBlockColorAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(InstanceBlockColorComponent), nameof(InstanceBlockColorComponent.Value))]
        public UnityEngine.Color color;

        class InstanceBlockColorBaker : Unity.Entities.Baker<InstanceBlockColorAuthoring>
        {
            public override void Bake(InstanceBlockColorAuthoring authoring)
            {
                InstanceBlockColorComponent component = default(InstanceBlockColorComponent);
                float4 colorValues;
                colorValues.x = authoring.color.linear.r;
                colorValues.y = authoring.color.linear.g;
                colorValues.z = authoring.color.linear.b;
                colorValues.w = authoring.color.linear.a;
                component.Value = colorValues;
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}