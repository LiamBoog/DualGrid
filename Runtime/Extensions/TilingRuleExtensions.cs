using skner.DualGrid.Utils;
using UnityEngine;
using static UnityEngine.RuleTile;

namespace skner.DualGrid.Extensions
{
    public static class TilingRuleExtensions
    {

        /// <summary>
        /// Calculates the relative neighbor offset from the <paramref name="dataTileOffset"/> and returns the correct index of the neighbor.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="dataTileOffset"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static int GetNeighborIndex(this TilingRule rule, Vector3Int dataTileOffset)
        {
            Vector3Int neighborOffsetPosition = DualGridUtils.ConvertDataTileOffsetToNeighborOffset(dataTileOffset);

            var neightborIndex = rule.m_NeighborPositions.IndexOf(neighborOffsetPosition);

            if (neightborIndex == -1) throw new System.ArgumentException($"Could not find a valid neighbor for tile id {rule.m_Id} with the data tile offset of {dataTileOffset}.");
            return neightborIndex;
        }

        public static BoundsInt GetBoundsDualGrid(this TilingRule rule)
        {
            BoundsInt bounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);
            foreach (var (position, _) in rule.GetNeighbors())
            {
                bounds.xMin = Mathf.Min(bounds.xMin, position.x);
                bounds.yMin = Mathf.Min(bounds.yMin, position.y);
                bounds.xMax = Mathf.Max(bounds.xMax, position.x);
                bounds.yMax = Mathf.Max(bounds.yMax, position.y);
            }
            return bounds;
        }
    }
}
