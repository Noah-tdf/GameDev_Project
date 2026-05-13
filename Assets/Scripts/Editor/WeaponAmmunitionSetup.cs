using UnityEditor;
using UnityEngine;

public static class WeaponAmmunitionSetup
{
    [MenuItem("Tools/Assign Weapon Ammunition")]
    public static void AssignWeaponAmmunition()
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
                Debug.LogWarning($"WeaponAmmunitionSetup: missing prefab at {prefabPath}");
                continue;
            }

            PlayerShooting[] shooters = prefab.GetComponentsInChildren<PlayerShooting>(true);
            foreach (PlayerShooting shooter in shooters)
            {
                shooter.EditorEnsureDefaultAmmunitionProfiles();
                EditorUtility.SetDirty(shooter);
            }

            if (shooters.Length > 0)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"WeaponAmmunitionSetup: assigned ammunition profiles on {prefabPath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
