using System;
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
                module.UpdateEditorPreviewTiles(GridPaintingState.lastSceneViewGridPosition);
            }
        }
    }
}
