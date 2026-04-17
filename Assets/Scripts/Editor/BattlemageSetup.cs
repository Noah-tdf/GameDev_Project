using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// One-time setup: imports Battlemage sprites, creates animation clips,
/// builds an AnimatorController, and wires it onto the player.
/// Run via Tools > Setup Battlemage Character.
/// </summary>
public static class BattlemageSetup
{
    private const string BasePath  = "Assets/Art/Characters/Battlemage";
    private const string AnimsPath = "Assets/Art/Characters/Battlemage/Animations";
    private const float  FPS       = 12f;

    [MenuItem("Tools/Setup Battlemage Character")]
    public static void Run()
    {
        // 1. Ensure Animations folder exists
        if (!AssetDatabase.IsValidFolder(AnimsPath))
            AssetDatabase.CreateFolder(BasePath, "Animations");

        AssetDatabase.Refresh();

        // 2. Build sprite import settings for every PNG
        SetSpriteImportSettings();

        // 3. Create animation clips
        var idle   = MakeClip("Idle",   GetSprites(BasePath + "/Idle"),              loop: true);
        var run    = MakeClip("Run",    GetSprites(BasePath + "/Running"),            loop: true);
        var crouch = MakeClip("Crouch", GetSprites(BasePath + "/Crouch"),             loop: true);
        var dead   = MakeClip("Dead",   GetSprites(BasePath + "/Death"),              loop: false);
        var hurt   = MakeClip("Hurt",   GetSprites(BasePath + "/Attack 1").Take(5).ToArray(), loop: false);

        // Jump = Up + Down + Grounded frames combined
        var jumpSprites = GetSprites(BasePath + "/Jump Neutral/Going Up")
            .Concat(GetSprites(BasePath + "/Jump Neutral/Going Down"))
            .Concat(GetSprites(BasePath + "/Jump Neutral/Grounded"))
            .ToArray();
        var jump = MakeClip("Jump", jumpSprites, loop: false);

        // 4. Create AnimatorController
        string controllerPath = AnimsPath + "/BattlemageAnimator.controller";
        // Delete old one if re-running
        AssetDatabase.DeleteAsset(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded",  AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsHurt",      AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead",      AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        var sIdle   = AddState(sm, "Idle",   idle);
        var sRun    = AddState(sm, "Run",    run);
        var sJump   = AddState(sm, "Jump",   jump);
        var sHurt   = AddState(sm, "Hurt",   hurt);
        var sDead   = AddState(sm, "Dead",   dead);
        var sCrouch = AddState(sm, "Crouch", crouch);

        sm.defaultState = sIdle;

        // Idle <-> Run
        Transition(sIdle, sRun,   "Speed",      AnimatorConditionMode.Greater, 0.1f,  0.05f);
        Transition(sRun,  sIdle,  "Speed",      AnimatorConditionMode.Less,    0.1f,  0.05f);

        // Idle/Run -> Jump (not grounded)
        Transition(sIdle, sJump, "IsGrounded", AnimatorConditionMode.IfNot, 0, 0);
        Transition(sRun,  sJump, "IsGrounded", AnimatorConditionMode.IfNot, 0, 0);

        // Jump -> Idle (grounded)
        Transition(sJump, sIdle, "IsGrounded", AnimatorConditionMode.If, 0, 0);

        // Any -> Hurt
        var anyHurt = sm.AddAnyStateTransition(sHurt);
        anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
        anyHurt.duration = 0; anyHurt.hasExitTime = false;

        // Hurt -> Idle on finish
        var hurtExit = sHurt.AddTransition(sIdle);
        hurtExit.hasExitTime = true; hurtExit.exitTime = 1f; hurtExit.duration = 0;

        // Any -> Dead
        var anyDead = sm.AddAnyStateTransition(sDead);
        anyDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        anyDead.duration = 0; anyDead.hasExitTime = false;

        // Any -> Crouch / Crouch -> Idle
        var anyCrouch = sm.AddAnyStateTransition(sCrouch);
        anyCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");
        anyCrouch.duration = 0; anyCrouch.hasExitTime = false;
        Transition(sCrouch, sIdle, "IsCrouching", AnimatorConditionMode.IfNot, 0, 0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 5. Swap animator on the player
        var player = GameObject.Find("Luca");
        if (player != null)
        {
            var animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                // Set the SpriteRenderer sprite to the first idle frame
                var sr = player.GetComponent<SpriteRenderer>();
                if (sr != null && idle != null)
                {
                    var firstSprite = GetSprites(BasePath + "/Idle").FirstOrDefault();
                    if (firstSprite != null) sr.sprite = firstSprite;
                }
                EditorUtility.SetDirty(player);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
            }
        }

        Debug.Log("[BattlemageSetup] Done! Controller: " + controllerPath);
        EditorUtility.DisplayDialog("Battlemage Setup", "Character set up successfully!\nAnimator Controller: " + controllerPath, "OK");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static void SetSpriteImportSettings()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BasePath });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType        = TextureImporterType.Sprite;
            importer.spriteImportMode   = SpriteImportMode.Single;
            importer.filterMode         = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
        }
    }

    static Sprite[] GetSprites(string folder)
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        return guids
            .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .OrderBy(s => ExtractNumber(s.name))
            .ToArray();
    }

    static int ExtractNumber(string name)
    {
        var m = Regex.Match(name, @"(\d+)\s*$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    static AnimationClip MakeClip(string clipName, Sprite[] sprites, bool loop)
    {
        string path = AnimsPath + "/" + clipName + ".anim";
        AssetDatabase.DeleteAsset(path); // allow re-run

        var clip = new AnimationClip { frameRate = FPS };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe
        {
            time  = i / FPS,
            value = s
        }).ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        var s = sm.AddState(name);
        s.motion = clip;
        return s;
    }

    static void Transition(AnimatorState from, AnimatorState to,
        string param, AnimatorConditionMode mode, float threshold, float duration)
    {
        var t = from.AddTransition(to);
        t.AddCondition(mode, threshold, param);
        t.duration = duration;
        t.hasExitTime = false;
    }
}
