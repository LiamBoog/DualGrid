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
            if (!(currentEvent.type == EventType.MouseDown || (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)))
                return;

            foreach (var module in DualGridModules)
            {
                GridBrush brush = (GridBrush) GridPaintingState.gridBrush;
                Vector3Int size = brush.size;
                Vector3Int pivot = brush.pivot;
                Vector3Int bottomLeft = new(Math.Min(-pivot.x, 0), Math.Min(-pivot.y, 0));
                Vector3Int topRight = new(size.x - 1 - pivot.x, size.y - 1 - pivot.y);
                Debug.DrawLine(module.DataTilemap.CellToWorld(GridPaintingState.lastSceneViewGridPosition + bottomLeft), module.DataTilemap.CellToWorld(GridPaintingState.lastSceneViewGridPosition + topRight), Color.red);
                
                HashSet<Vector3Int> affectedTiles = new();
                for (int x = bottomLeft.x; x <= topRight.x; x++)
                {
                    for (int y = bottomLeft.y; y <= topRight.y; y++)
                    {
                        Vector3Int cell = GridPaintingState.lastSceneViewGridPosition + new Vector3Int(x, y);
                        affectedTiles.UnionWith(DualGridUtils.GetRenderTilePositions(cell));
                    }
                }

                if (size == Vector3Int.one)
                {
                    module.UpdateEditorPreviewTiles(GridPaintingState.lastSceneViewGridPosition);
                    return;
                }
                
                module.UpdateEditorPreviewTiles(affectedTiles);
            }
        }
    }
}
