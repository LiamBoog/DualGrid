using skner.DualGrid.Editor.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static UnityEngine.Tilemaps.Tile;

namespace skner.DualGrid.Editor
{
    [CanEditMultipleObjects]
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

        private static class Styles
        {
            public static readonly GUIContent DefaultSprite = EditorGUIUtility.TrTextContent("Default Sprite", "The default sprite will be used as a last resort when no tiling rules are valid.");
            public static readonly GUIContent DefaultGameObject = EditorGUIUtility.TrTextContent("GameObject", "Depending on the configuration on the Dual Grid Tilemap Module, this GameObject will be used for every tile.");
            public static readonly GUIContent DefaultCollider = EditorGUIUtility.TrTextContent("Collider", "The collider type that will be used for this Dual Grid Rule Tile.");

            public static readonly GUIContent OriginalTexture = EditorGUIUtility.TrTextContent("Original Texture", "The original Texture2D associated with this Dual Grid Rule Tile. Only textures splitted in 16 pieces are considered valid.");

            public static readonly GUIContent TilingRules = EditorGUIUtility.TrTextContent("Tiling Rules List");
            public static readonly GUIContent TilingRulesGameObject = EditorGUIUtility.TrTextContent("GameObject", "Depending on the configuration on the Dual Grid Tilemap Module, this GameObject will be used for this specific Tiling Rule.");
            public static readonly GUIContent TilingRulesCollider = EditorGUIUtility.TrTextContent("Collider", "Colliders per Tiling Rule are not supported. They are set for the entire Dual Grid Rule Tile.");
            public static readonly GUIContent TilingRulesOutput = EditorGUIUtility.TrTextContent("Output", "The Output for the tile which fits this Rule. Each Output type has its own properties.");

            public static readonly GUIContent TilingRulesNoise = EditorGUIUtility.TrTextContent("Noise", "The Perlin noise factor when placing the tile.");
            public static readonly GUIContent TilingRulesShuffle = EditorGUIUtility.TrTextContent("Shuffle", "The randomized transform given to the tile when placing it.");
            public static readonly GUIContent TilingRulesRandomSize = EditorGUIUtility.TrTextContent("Size", "The number of Sprites to randomize from.");

            public static readonly GUIContent TilingRulesMinSpeed = EditorGUIUtility.TrTextContent("Min Speed", "The minimum speed at which the animation is played.");
            public static readonly GUIContent TilingRulesMaxSpeed = EditorGUIUtility.TrTextContent("Max Speed", "The maximum speed at which the animation is played.");
            public static readonly GUIContent TilingRulesAnimationSize = EditorGUIUtility.TrTextContent("Size", "The number of Sprites in the animation.");

            public static readonly GUIStyle extendNeighborsLightStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 10,
                normal = new GUIStyleState()
                {
                    textColor = Color.black
                }
            };

            public static readonly GUIStyle extendNeighborsDarkStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 10,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            };
        }

        private DualGridRuleTile _targetDualGridRuleTile;

        private const string PreviewActiveStatusKey = "PreviewActiveStatusKey";
        private bool _isPreviewActive;
        private ReorderableList _tilingRulesReorderableList;

        private bool _hasMultipleTargets = false;
        private List<DualGridRuleTile> _targetDualGridRuleTiles = new();

        public override void OnEnable()
        {
            _targetDualGridRuleTile = (DualGridRuleTile)target;
            _hasMultipleTargets = targets.Length > 1;

            if (_hasMultipleTargets) _targetDualGridRuleTiles = targets.Cast<DualGridRuleTile>().ToList();
            else _targetDualGridRuleTiles = new List<DualGridRuleTile>() { target as DualGridRuleTile };

            _isPreviewActive = EditorPrefs.GetBool(PreviewActiveStatusKey);

            _tilingRulesReorderableList = new ReorderableList(tile != null ? tile.m_TilingRules : null, typeof(RuleTile.TilingRule), true, true, false, false);
            _tilingRulesReorderableList.drawHeaderCallback = OnDrawHeader;
            _tilingRulesReorderableList.drawElementCallback = OnDrawElement;
            _tilingRulesReorderableList.elementHeightCallback = GetElementHeight;
            _tilingRulesReorderableList.onChangedCallback = ListUpdated;

            base.OnEnable();
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

        public override BoundsInt GetRuleGUIBounds(BoundsInt bounds, RuleTile.TilingRule rule)
        {
            return new BoundsInt(-1, -1, 0, 2, 2, 0);
        }

        public override Vector2 GetMatrixSize(BoundsInt bounds)
        {
            float matrixCellSize = 27;
            return new Vector2(bounds.size.x * matrixCellSize, bounds.size.y * matrixCellSize);
        }

        protected override void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            // This code was copied from the base RuleTileEditor.OnDrawElement, because there are no good ways to extend it.
            // The changes were marked with a comment

            RuleTile.TilingRule rule = tile.m_TilingRules[index];
            BoundsInt bounds = GetRuleGUIBounds(rule.GetBounds(), rule);

            float yPos = rect.yMin + 2f;
            float height = rect.height - k_PaddingBetweenRules;
            Vector2 matrixSize = GetMatrixSize(bounds);

            Rect spriteRect = new Rect(rect.xMax - k_DefaultElementHeight - 5f, yPos, k_DefaultElementHeight, k_DefaultElementHeight);
            Rect matrixRect = new Rect(rect.xMax - matrixSize.x - spriteRect.width - 10f, yPos, matrixSize.x, matrixSize.y);
            Rect inspectorRect = new Rect(rect.xMin, yPos, rect.width - matrixSize.x - spriteRect.width - 20f, height);

            DualGridRuleInspectorOnGUI(inspectorRect, rule); // Used to call base.RuleInspectorOnGUI. But this method isn't virtual, so it can't be ovewritten. Thanks Unity!
            RuleMatrixOnGUI(tile, matrixRect, bounds, rule);
            SpriteOnGUI(spriteRect, rule);
        }

        protected virtual void DualGridRuleInspectorOnGUI(Rect rect, RuleTile.TilingRule tilingRule)
        {
            float y = rect.yMin;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesGameObject);
            tilingRule.m_GameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", tilingRule.m_GameObject, typeof(GameObject), false);
            y += k_SingleLineHeight;

            using (new EditorGUI.DisabledScope(true))
            {
                // Collider setting is disabled because it's not supported
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesCollider);
                tilingRule.m_ColliderType = (ColliderType)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_ColliderType);
                y += k_SingleLineHeight;
            }

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesOutput);
            tilingRule.m_Output = (RuleTile.TilingRuleOutput.OutputSprite)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_Output);
            y += k_SingleLineHeight;

            if (tilingRule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Animation)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesMinSpeed);
                tilingRule.m_MinAnimationSpeed = EditorGUI.FloatField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_MinAnimationSpeed);
                y += k_SingleLineHeight;
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesMaxSpeed);
                tilingRule.m_MaxAnimationSpeed = EditorGUI.FloatField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_MaxAnimationSpeed);
                y += k_SingleLineHeight;
            }
            if (tilingRule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Random)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesNoise);
                tilingRule.m_PerlinScale = EditorGUI.Slider(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_PerlinScale, 0.001f, 0.999f);
                y += k_SingleLineHeight;

                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), Styles.TilingRulesShuffle);
                tilingRule.m_RandomTransform = (RuleTile.TilingRuleOutput.Transform)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_RandomTransform);
                y += k_SingleLineHeight;
            }

            if (tilingRule.m_Output != RuleTile.TilingRuleOutput.OutputSprite.Single)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight)
                    , tilingRule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Animation ? Styles.TilingRulesAnimationSize : Styles.TilingRulesRandomSize);
                EditorGUI.BeginChangeCheck();
                int newLength = EditorGUI.DelayedIntField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_Sprites.Length);
                if (EditorGUI.EndChangeCheck())
                    Array.Resize(ref tilingRule.m_Sprites, Math.Max(newLength, 1));
                y += k_SingleLineHeight;
                for (int i = 0; i < tilingRule.m_Sprites.Length; i++)
                {
                    tilingRule.m_Sprites[i] = EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_Sprites[i], typeof(Sprite), false) as Sprite;
                    y += k_SingleLineHeight;
                }
            }
        }

        public new virtual void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, Styles.TilingRules);
        }

        private float GetElementHeight(int index)
        {
            RuleTile.TilingRule rule = tile.m_TilingRules[index];
            return base.GetElementHeight(rule);
        }

    }

}

