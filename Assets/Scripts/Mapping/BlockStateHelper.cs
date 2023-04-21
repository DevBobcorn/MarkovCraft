#nullable enable
using System.Linq;
using UnityEngine;

namespace MarkovCraft.Mapping
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
                var blockId = ResourceLocation.fromString(parts[0]);

                if (palette.StateListTable.ContainsKey(blockId)) // Get first in the hash set
                    return palette.StateListTable[blockId].First();
                else
                {
                    //Debug.LogWarning($"Block with id {blockId} is not present");
                    return INVALID_BLOCKSTATE;
                }
            }
            else if (parts.Length == 2 && parts[1].EndsWith(']')) // With predicates
            {
                var blockId = ResourceLocation.fromString(parts[0]);
                var filter = parts[1].Substring(0, parts[1].Length - 1); // Remove trailing ']'

                if (palette.StateListTable.ContainsKey(blockId)) // Get first in the hash set
                {
                    var predicate = BlockStatePredicate.fromString(filter);

                    foreach (var stateId in palette.StateListTable[blockId])
                    {
                        if (predicate.check(palette.StatesTable[stateId]))
                            return stateId;
                    }

                    //Debug.LogWarning($"Block with id {blockId} is present, but no state matches predicate [{filter}]. Using first state instead");
                    return palette.StateListTable[blockId].First();
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
                    x => x.Namespace == incompleteBlockId.Namespace && x.Path.StartsWith(incompleteBlockId.Path)).Take(3).ToArray();
        }
    }
}