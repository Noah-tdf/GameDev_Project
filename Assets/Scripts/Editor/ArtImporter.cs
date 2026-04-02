using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ubuntu City → Apply Art Assets
/// Configures sprite importers, builds Luca's Animator, and wires all art into the Level1 scene.
/// </summary>
public static class ArtImporter
{
    [MenuItem("Ubuntu City/Apply Art Assets")]
    public static void ApplyArt()
    {
        // 1. Configure all sprite importers
        ConfigureSprites();

        // 2. Build Luca's AnimatorController
        AnimatorController lucaController = BuildLucaAnimator();

        // 3. Wire everything into Level1 scene
        string scenePath = "Assets/Scenes/Level1.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        WireScene(scene, lucaController);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Ubuntu City", "Art assets applied to Level1!", "OK");
    }

    // ── 1. SPRITE IMPORTER CONFIGURATION ────────────────────────────────────
    private static void ConfigureSprites()
    {
        string[] folders = {
            "Assets/Art/Characters/Luca",
            "Assets/Art/Characters/Enemy",
            "Assets/Art/Background",
            "Assets/Art/Vehicles",
        };

        foreach (string folder in folders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                ti.textureType       = TextureImporterType.Sprite;
                ti.spriteImportMode  = SpriteImportMode.Single;
                ti.filterMode        = FilterMode.Bilinear;
                ti.mipmapEnabled     = false;
                ti.alphaIsTransparency = true;

                if (path.Contains("/Background/"))
                {
                    ti.spritePixelsPerUnit = 16;
                    ti.textureCompression  = TextureImporterCompression.Uncompressed;
                    var bgS = new TextureImporterSettings();
                    ti.ReadTextureSettings(bgS);
                    bgS.spriteMeshType = SpriteMeshType.FullRect;
                    ti.SetTextureSettings(bgS);
                }
                else if (path.Contains("/Vehicles/"))
                {
                    ti.spritePixelsPerUnit = 120;
                }
                else if (path.Contains("/Characters/Enemy/"))
                {
                    // 427×496px sprite, want ~1.2 units tall → 400 PPU
                    ti.spritePixelsPerUnit = 400;
                }
                else
                {
                    // Luca (Red Hat Boy ~230px tall), want ~1.5 units → 150 PPU
                    ti.spritePixelsPerUnit = 150;
                }

                ti.SaveAndReimport();
            }
        }
    }

    // ── 2. BUILD LUCA ANIMATOR ───────────────────────────────────────────────
    private static AnimatorController BuildLucaAnimator()
    {
        string controllerPath = "Assets/Art/Characters/Luca/LucaAnimator.controller";
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters
        ac.AddParameter("Speed",      AnimatorControllerParameterType.Float);
        ac.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ac.AddParameter("IsHurt",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("IsDead",     AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        // States
        AnimatorState idle  = sm.AddState("Idle");
        AnimatorState run   = sm.AddState("Run");
        AnimatorState jump  = sm.AddState("Jump");
        AnimatorState hurt  = sm.AddState("Hurt");
        AnimatorState dead  = sm.AddState("Dead");

        idle.motion  = BuildClip("Idle",  "Assets/Art/Characters/Luca", "Idle",  10f);
        run.motion   = BuildClip("Run",   "Assets/Art/Characters/Luca", "Run",   12f);
        jump.motion  = BuildClip("Jump",  "Assets/Art/Characters/Luca", "Jump",  10f);
        hurt.motion  = BuildClip("Hurt",  "Assets/Art/Characters/Luca", "Hurt",  12f);
        dead.motion  = BuildClip("Dead",  "Assets/Art/Characters/Luca", "Dead",  8f);

        sm.defaultState = idle;

        // Transitions: Idle ↔ Run
        AddTransition(idle, run,  ac, "Speed",   AnimatorConditionMode.Greater, 0.1f);
        AddTransition(run,  idle, ac, "Speed",   AnimatorConditionMode.Less,    0.1f);

        // Idle/Run → Jump (when airborne)
        AddBoolTransition(idle, jump, ac, "IsGrounded", false);
        AddBoolTransition(run,  jump, ac, "IsGrounded", false);

        // Jump → Idle (when grounded)
        AddBoolTransition(jump, idle, ac, "IsGrounded", true);

        // Any → Hurt
        AnimatorStateTransition hurtT = sm.AddAnyStateTransition(hurt);
        hurtT.AddCondition(AnimatorConditionMode.If, 0f, "IsHurt");
        hurtT.duration = 0f;

        // Hurt → Idle (after clip ends)
        AnimatorStateTransition hurtExit = hurt.AddTransition(idle);
        hurtExit.hasExitTime = true;
        hurtExit.exitTime    = 1f;
        hurtExit.duration    = 0f;

        // Any → Dead
        AnimatorStateTransition deadT = sm.AddAnyStateTransition(dead);
        deadT.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");
        deadT.duration = 0f;

        AssetDatabase.SaveAssets();
        return ac;
    }

    /// <summary>Build an AnimationClip from numbered PNG frames in a folder.</summary>
    private static AnimationClip BuildClip(string clipName, string folder, string prefix, float fps)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        List<Sprite> frames = new List<Sprite>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.StartsWith(prefix))
                frames.Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
        }

        // Sort by the number inside the parentheses: "Run (1)" → 1
        frames = frames.OrderBy(s => {
            string n = s.name;
            int open = n.LastIndexOf('(');
            int close = n.LastIndexOf(')');
            if (open >= 0 && close > open)
                if (int.TryParse(n.Substring(open + 1, close - open - 1), out int num))
                    return num;
            return 0;
        }).ToList();

        if (frames.Count == 0)
        {
            Debug.LogWarning($"ArtImporter: No frames found for clip '{clipName}' with prefix '{prefix}'");
            return new AnimationClip();
        }

        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        float frameDuration = 1f / fps;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe {
                time  = i * frameDuration,
                value = frames[i]
            };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            "", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Loop idle and run; don't loop jump/hurt/dead
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = (clipName == "Idle" || clipName == "Run");
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string savePath = $"Assets/Art/Characters/Luca/{clipName}.anim";
        AssetDatabase.CreateAsset(clip, savePath);
        return clip;
    }

    // ── 3. WIRE SCENE ────────────────────────────────────────────────────────
    private static void WireScene(Scene scene, AnimatorController lucaController)
    {
        // --- LUCA ---
        GameObject luca = FindInScene(scene, "Luca");
        if (luca != null)
        {
            // Swap placeholder sprite → first idle frame
            Sprite idleSprite = LoadSprite("Assets/Art/Characters/Luca", "Idle (1)");
            if (idleSprite != null)
            {
                SpriteRenderer sr = luca.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sprite = idleSprite; sr.color = Color.white; }
                // At 100 PPU a ~150px tall sprite = 1.5 units — good player height
                luca.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // Add Animator with controller
            Animator anim = luca.GetComponent<Animator>();
            if (anim == null) anim = luca.AddComponent<Animator>();
            anim.runtimeAnimatorController = lucaController;
        }

        // --- ENEMIES ---
        Sprite enemyWalk1 = LoadSprite("Assets/Art/Characters/Enemy", "frame-1");
        for (int i = 1; i <= 6; i++)
        {
            GameObject enemy = FindInScene(scene, "UbuntyWalker_0" + i);
            if (enemy == null) continue;
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && enemyWalk1 != null)
            {
                sr.sprite = enemyWalk1;
                sr.color  = Color.white;
                // At 400 PPU the 427×496px sprite = ~1.07×1.24 units — correct enemy size
                enemy.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        // Background layers keep their procedural pastel blocks — they already look great.
        // The Warped City assets are too dark/cyberpunk to use as direct replacements.

        // --- CARS (update prefab) ---
        string carPrefabPath = "Assets/Prefabs/Level1/Car.prefab";
        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(carPrefabPath);
        if (carPrefab != null)
        {
            Sprite carSprite = LoadSpriteByPath("Assets/Art/Vehicles/v-red.png");
            if (carSprite != null)
            {
                SpriteRenderer carSr = carPrefab.GetComponent<SpriteRenderer>();
                if (carSr == null) carSr = carPrefab.GetComponentInChildren<SpriteRenderer>();
                if (carSr != null)
                {
                    using (var scope = new PrefabUtility.EditPrefabContentsScope(carPrefabPath))
                    {
                        SpriteRenderer prefabSr = scope.prefabContentsRoot.GetComponent<SpriteRenderer>();
                        if (prefabSr == null)
                            prefabSr = scope.prefabContentsRoot.GetComponentInChildren<SpriteRenderer>();
                        if (prefabSr != null)
                        {
                            prefabSr.sprite = carSprite;
                            prefabSr.color  = Color.white;
                            scope.prefabContentsRoot.transform.localScale = new Vector3(2.5f, 1f, 1f);
                        }
                    }
                }
            }
        }
    }

    private static void WireBackground(Scene scene, string objectName, string assetPathNoExt)
    {
        GameObject go = FindInScene(scene, objectName);
        if (go == null) return;

        // Try loading by exact path
        Sprite sp = LoadSpriteByPath(assetPathNoExt + ".png");
        if (sp == null) return;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite      = sp;
        sr.color       = Color.white;
        sr.drawMode    = SpriteDrawMode.Tiled;

        // Scale BG to cover the level width nicely
        float spriteW = sp.bounds.size.x;
        float spriteH = sp.bounds.size.y;
        float targetW = 70f;
        float scaleX  = spriteW > 0 ? targetW / spriteW : 1f;
        float scaleY  = scaleX;
        go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────
    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Sprite LoadSprite(string folder, string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite", new[] { folder });
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static Sprite LoadSpriteByPath(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AddTransition(AnimatorState from, AnimatorState to,
        AnimatorController ac, string param, AnimatorConditionMode mode, float threshold)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(mode, threshold, param);
        t.duration = 0.05f;
        t.hasExitTime = false;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to,
        AnimatorController ac, string param, bool value)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
        t.duration = 0f;
        t.hasExitTime = false;
    }
}
