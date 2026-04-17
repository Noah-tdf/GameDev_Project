using UnityEngine;
using UnityEditor;

public static class WeaponSetup
{
    [MenuItem("Tools/Setup Weapons")]
    public static void Run()
    {
        var primarySprite   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Weapons/weapon-primary.png");
        var secondarySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Weapons/weapon-secondary.png");

        if (primarySprite == null || secondarySprite == null)
        {
            Debug.LogError("[WeaponSetup] Could not load weapon sprites.");
            return;
        }

        var player = GameObject.Find("Luca");
        if (player == null) { Debug.LogError("[WeaponSetup] 'Luca' not found."); return; }

        var playerSR   = player.GetComponent<SpriteRenderer>();
        string plLayer = playerSR != null ? playerSR.sortingLayerName : "Default";
        int    plOrder = playerSR != null ? playerSR.sortingOrder     : 0;

        // ── Primary gun ─────────────────────────────────────────────────────
        var gunT = player.transform.Find("Gun");
        if (gunT != null)
        {
            var sr = gunT.GetComponent<SpriteRenderer>() ?? gunT.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite           = primarySprite;
            sr.sortingLayerName = plLayer;
            sr.sortingOrder     = plOrder + 2;
            sr.color            = Color.white;

            gunT.localPosition = new Vector3(0.28f, -0.08f, 0f);
            gunT.localScale    = new Vector3(0.2f,  0.2f,  1f);
            EditorUtility.SetDirty(gunT.gameObject);
        }

        // ── Secondary gun ────────────────────────────────────────────────────
        var gun2T = player.transform.Find("GunSecondary");
        if (gun2T != null)
        {
            var sr2 = gun2T.GetComponent<SpriteRenderer>() ?? gun2T.gameObject.AddComponent<SpriteRenderer>();
            sr2.sprite           = secondarySprite;
            sr2.sortingLayerName = plLayer;
            sr2.sortingOrder     = plOrder + 3;
            sr2.color            = Color.white;

            gun2T.localPosition = new Vector3(0.25f, -0.1f, 0f);
            gun2T.localScale    = new Vector3(0.16f, 0.16f, 1f);
            EditorUtility.SetDirty(gun2T.gameObject);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[WeaponSetup] Done.");
    }
}
