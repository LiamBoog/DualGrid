using System;
using System.Collections.Generic;
using skner.DualGrid.Utils;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace skner.DualGrid.Editor
{
    [InitializeOnLoad]
    public static class DualGridTilemapPersistentListener
    {
        private static DualGridTilemapModule[] DualGridModules => Object.FindObjectsByType<DualGridTilemapModule>(FindObjectsSortMode.None);
        
        static DualGridTilemapPersistentListener()
        {
            Tilemap.tilemapTileChanged -= HandleTilemapChange;
            Tilemap.tilemapTileChanged += HandleTilemapChange;
            SceneView.duringSceneGui -= UpdateDualGridTilemapPreviewTiles;
            SceneView.duringSceneGui += UpdateDualGridTilemapPreviewTiles;
            SceneView.duringSceneGui -= OnMouseEnterExitWindow;
            SceneView.duringSceneGui += OnMouseEnterExitWindow;
            
            RefreshRenderTiles();
        }

        private static void HandleTilemapChange(Tilemap tilemap, Tilemap.SyncTile[] tiles)
        {
            foreach (var module in DualGridModules)
            {
                module.HandleTilemapChange(tilemap, tiles);
            }
        }
        
        private static void UpdateDualGridTilemapPreviewTiles(SceneView _)
        {
            // Only update preview tiles when painting or erasing
            Type activeToolType = ToolManager.activeToolType;
            if (activeToolType != typeof(PaintTool) && activeToolType != typeof(EraseTool))
                return;
            
            // Only update preview tiles or click or drag
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown)
            {
                // Update on MouseDown doesn't work, so we delay it
                EditorApplication.delayCall += UpdatePreviewTiles;
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
            {
                UpdatePreviewTiles();
            }
            
            void UpdatePreviewTiles()
            {
                foreach (var module in DualGridModules)
                {
                    if (((GridBrush) GridPaintingState.gridBrush).size == Vector3Int.one)
                    {
                        module.UpdateEditorPreviewTiles(GridPaintingState.lastSceneViewGridPosition);
                        continue;
                    }
                    
                    module.UpdateEditorPreviewTiles(GetBrushAffectedRenderTilePositions());
                }
            }
        }

        /// <summary>
        /// Get a set of unique render tile positions affected by the current grid brush.
        /// </summary>
        private static IEnumerable<Vector3Int> GetBrushAffectedRenderTilePositions()
        {
            GridBrush brush = (GridBrush) GridPaintingState.gridBrush;

            Vector3Int size = brush.size;
            Vector3Int pivot = brush.pivot;
            Vector3Int bottomLeft = new(Math.Min(-pivot.x, 0), Math.Min(-pivot.y, 0));
            Vector3Int topRight = new(size.x - 1 - pivot.x, size.y - 1 - pivot.y);
                
            HashSet<Vector3Int> output = new();
            Vector3Int brushPosition = GridPaintingState.lastSceneViewGridPosition;
            for (int x = bottomLeft.x; x <= topRight.x; x++)
            {
                for (int y = bottomLeft.y; y <= topRight.y; y++)
                {
                    Vector3Int cell = brushPosition + new Vector3Int(x, y);
                    output.UnionWith(DualGridUtils.GetRenderTilePositions(cell));
                }
            }

            return output;
        }

        private static void OnMouseEnterExitWindow(SceneView _)
        {
            EventType currentEventType = Event.current.type;
            if (currentEventType != EventType.MouseEnterWindow && currentEventType != EventType.MouseLeaveWindow)
                return;
            
            RefreshRenderTiles();
        }
        
        private static void RefreshRenderTiles()
        {
            foreach (var module in DualGridModules)
            {
                module.RefreshRenderTiles();
            }
        }
    }
}
