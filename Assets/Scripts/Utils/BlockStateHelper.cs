#nullable enable
using System.Linq;
using CraftSharp;

namespace MarkovCraft
{
    public static class BlockStateHelper
    {
        public const int INVALID_BLOCKSTATE = -1;

        public static int GetStateIdFromString(string state)
        {
            var palette = BlockStatePalette.INSTANCE;
            var parts = state.Trim().Split('[');

            if (parts.Length == 1) // No predicate specified
            {
                var blockId = ResourceLocation.FromString(parts[0]);

                if (palette.DefaultStateTable.ContainsKey(blockId))
                {
                    return palette.DefaultStateTable[blockId];
                }
                else
                {
                    //Debug.LogWarning($"Block with id {blockId} is not present");
                    return INVALID_BLOCKSTATE;
                }
            }
            else if (parts.Length == 2 && parts[1].EndsWith(']')) // With predicates
            {
                var blockId = ResourceLocation.FromString(parts[0]);
                var filter = parts[1].Substring(0, parts[1].Length - 1); // Remove trailing ']'

                if (palette.StateListTable.ContainsKey(blockId)) // StateListTable should have the same keys as DefaultStateTable
                {
                    var predicate = BlockStatePredicate.fromString(filter);

                    foreach (var stateId in palette.StateListTable[blockId])
                    {
                        if (predicate.check(palette.StatesTable[stateId]))
                            return stateId;
                    }

                    //Debug.LogWarning($"Block with id {blockId} is present, but no state matches predicate [{filter}]. Using default state instead");
                    return palette.DefaultStateTable[blockId];
                }
                else
                {
                    //Debug.LogWarning($"Block with id {blockId} is not present");
                    return INVALID_BLOCKSTATE;
                }
            }
            else
            {
                //Debug.LogWarning($"Malformed block state string: {state}");
                return INVALID_BLOCKSTATE;
            }
            
        }

        public static ResourceLocation[] GetBlockIdCandidates(ResourceLocation incompleteBlockId)
        {
            return BlockStatePalette.INSTANCE.StateListTable.Keys.Where(
                    x => x.Namespace == incompleteBlockId.Namespace &&
                            x.Path.StartsWith(incompleteBlockId.Path)).Take(3).ToArray();
        }
    }
}