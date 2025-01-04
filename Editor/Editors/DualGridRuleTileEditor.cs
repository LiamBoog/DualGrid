using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace skner.DualGrid.Editor
{
    [CustomEditor(typeof(DualGridRuleTile), true)]
    public class DualGridRuleTileEditor : RuleTileEditor
    {
        private static class Styles
        {
            public static GUIContent tilingRules = 
                (GUIContent) typeof(RuleTileEditor).
                    Assembly.
                    GetType($"{typeof(RuleTileEditor).FullName}+{nameof(Styles)}").
                    GetField(nameof(tilingRules))!.GetValue(null); 
        }
        
        private ReorderableList m_ReorderableList;
        
        public override void OnEnable()
        {
            base.OnEnable();

            // Don't show 'Extend Neighbors' toggle 
            m_ReorderableList = (ReorderableList) typeof(RuleTileEditor).GetField(nameof(m_ReorderableList), BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this);
            m_ReorderableList.drawHeaderCallback = OnDrawHeader;

            extendNeighbor = false;
        }
        
        public void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, Styles.tilingRules);
        }
        
        public override BoundsInt GetRuleGUIBounds(BoundsInt bounds, RuleTile.TilingRule rule)
        {
            return new BoundsInt(-1, -1, 0, 2, 2, 0);
        }

        public override Vector2 GetMatrixSize(BoundsInt bounds)
        {
            float matrixCellSize = 27;
            return new Vector2(bounds.size.x * matrixCellSize, bounds.size.y * matrixCellSize);
        }

        public override void RuleMatrixOnGUI(RuleTile tile, Rect rect, BoundsInt bounds, RuleTile.TilingRule tilingRule)
        {
            // This code was copied from the base RuleTileEditor.RuleMatrixOnGUI, because there are no good ways to extend it.
            // The changes were marked with a comment

            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            float w = rect.width / bounds.size.x;
            float h = rect.height / bounds.size.y;

            for (int y = 0; y <= bounds.size.y; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }
            for (int x = 0; x <= bounds.size.x; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }
            Handles.color = Color.white;

            var neighbors = tilingRule.GetNeighbors();

            // Incremented for cycles by 1 to workaround new GetBounds(), while perserving corner behaviour
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    // Pos changed here to workaround for the new 2x2 matrix, only considering the corners, while not changing the Rect r
                    Vector3Int pos = new Vector3Int(x >= 0 ? x + 1 : x, y >= 0 ? y + 1 : y, 0);

                    Rect r = new Rect(rect.xMin + (x - bounds.xMin) * w, rect.yMin + (-y + bounds.yMax - 1) * h, w - 1, h - 1);
                    RuleMatrixIconOnGUI(tilingRule, neighbors, pos, r);
                }
            }
        }
    }

}

