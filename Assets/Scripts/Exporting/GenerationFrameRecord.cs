#nullable enable
using Unity.Mathematics;

namespace MarkovCraft
{
    public class GenerationFrameRecord
    {
        public int3 Size;
        public char[] States;

        public GenerationFrameRecord(int3 size, char[] states)
        {
            Size = size;
            States = states;
        }
    }
}