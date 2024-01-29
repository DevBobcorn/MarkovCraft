using Unity.Entities;
using Unity.Mathematics;

namespace MarkovCraft
{
    // Set the internal capacity to 0 to store it outside the chunk from the start.
    // See https://docs.unity3d.com/Packages/com.unity.entities@1.1/manual/components-buffer-introducing.html#capacity
    [InternalBufferCapacity(0)]
    public struct BlockMeshBufferElement : IBufferElementData
    {
        public int3 Value;
    }
}