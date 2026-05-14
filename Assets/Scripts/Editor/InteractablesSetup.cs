using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class InteractablesSetup
{
    private const string Level1ScenePath = "Assets/Scenes/Level1.unity";
    private const string InteractablesRootName = "=== INTERACTABLES ===";
    private const string CoinGridName = "CoinTilemapGrid";
    private const string CoinTilemapName = "CoinTilemap";

    [MenuItem("Tools/Setup Level1 Interactables")]
    public static void SetupLevel1Interactables()
    {
        Scene scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);

        GameObject root = FindRoot(scene, InteractablesRootName, "INTERACTABLES", "Interactables");
        if (root == null)
            root = new GameObject(InteractablesRootName);

        Grid grid = root.GetComponent<Grid>();
        if (grid == null)
            grid = root.AddComponent<Grid>();

        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSize = Vector3.one;

        GameObject oldCoinGrid = FindChild(root.transform, CoinGridName);
        GameObject coinTilemapObject = FindChild(root.transform, CoinTilemapName);
        if (coinTilemapObject == null && oldCoinGrid != null)
            coinTilemapObject = FindChild(oldCoinGrid.transform, CoinTilemapName);

        if (coinTilemapObject == null)
        {
            coinTilemapObject = new GameObject(CoinTilemapName);
        }

        coinTilemapObject.transform.SetParent(root.transform, true);

        if (oldCoinGrid != null)
            Object.DestroyImmediate(oldCoinGrid);

        if (coinTilemapObject.GetComponent<Tilemap>() == null)
            coinTilemapObject.AddComponent<Tilemap>();

        TilemapRenderer renderer = coinTilemapObject.GetComponent<TilemapRenderer>();
        if (renderer == null)
            renderer = coinTilemapObject.AddComponent<TilemapRenderer>();

        renderer.sortingOrder = 20;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("InteractablesSetup: ensured Level1 has === INTERACTABLES === with CoinTilemap.");
    }

    private static GameObject FindRoot(Scene scene, params string[] names)
    {
        return scene.GetRootGameObjects()
            .FirstOrDefault(root => names.Any(name => root.name == name));
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.gameObject : null;
    }
}
