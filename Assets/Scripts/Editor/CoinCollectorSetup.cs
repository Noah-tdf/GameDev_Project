using UnityEditor;
using UnityEngine;

public static class CoinCollectorSetup
{
    [MenuItem("Tools/Setup Coin Tilemap Collector")]
    public static void Setup()
    {
        string[] prefabPaths =
        {
            "Assets/Prefabs/Characters/Luca.prefab",
            "Assets/Prefabs/Level1/Luca.prefab"
        };

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"CoinCollectorSetup: missing prefab at {prefabPath}");
                continue;
            }

            CoinTilemapCollector collector = prefab.GetComponent<CoinTilemapCollector>();
            if (collector == null)
                collector = prefab.AddComponent<CoinTilemapCollector>();

            EditorUtility.SetDirty(collector);
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log($"CoinCollectorSetup: coin tilemap collector ready on {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
