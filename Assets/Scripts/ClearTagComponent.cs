#nullable enable
using Unity.Entities;

namespace MarkovBlocks
{
    // Global tag component used for indicating the scene needs clearing up
    // See https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/components-tag.html
    public struct ClearTagComponent : IComponentData
    {

    }
}
