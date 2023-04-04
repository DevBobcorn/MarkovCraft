using System.Collections.Generic;

namespace MarkovBlocks
{
    public class BlockStateModel
    {
        public readonly BlockGeometry[] Geometries;
        public readonly RenderType RenderType;

        public BlockStateModel(List<BlockGeometry> geometries, RenderType renderType)
        {
            Geometries = geometries.ToArray();
            RenderType = renderType;
        }

    }

}