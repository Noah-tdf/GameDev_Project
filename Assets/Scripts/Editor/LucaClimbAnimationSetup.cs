using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class LucaClimbAnimationSetup
{
    private const string ControllerPath = "Assets/Art/Characters/LucaV2/Animations/LucaV2Animator.controller";
    private const string ClimbClipPath = "Assets/Art/Characters/LucaV2/Animations/Climb.anim";
    private const string ClimbSpriteFolder = "Assets/Art/Characters/LucaV2/04_playerClimb";

    [MenuItem("Tools/Ubuntu City/Setup Luca Climb Animation")]
    public static void Setup()
    {
        AnimationClip climbClip = CreateOrUpdateClimbClip();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"LucaClimbAnimationSetup: Missing animator controller at {ControllerPath}.");
            return;
        }

        EnsureParameter(controller, "IsClimbing", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "ClimbSpeed", AnimatorControllerParameterType.Float);
        EnsureClimbState(controller, climbClip);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LucaClimbAnimationSetup: Luca climb animation and animator transitions are ready.");
    }

    private static AnimationClip CreateOrUpdateClimbClip()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClimbClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, ClimbClipPath);
        }

        Sprite[] sprites = Enumerable.Range(1, 8)
            .Select(index => LoadSprite($"{ClimbSpriteFolder}/playerClimb{index}.png"))
            .Where(sprite => sprite != null)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"LucaClimbAnimationSetup: No climb sprites found in {ClimbSpriteFolder}.");
            return clip;
        }

        clip.frameRate = 12f;
        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (controller.parameters.Any(parameter => parameter.name == name))
            return;

        controller.AddParameter(name, type);
    }

    private static void EnsureClimbState(AnimatorController controller, AnimationClip climbClip)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState climbState = stateMachine.states
            .FirstOrDefault(childState => childState.state.name == "Climb")
            .state;

        if (climbState == null)
            climbState = stateMachine.AddState("Climb", new Vector3(340f, 260f, 0f));

        climbState.motion = climbClip;
        climbState.writeDefaultValues = true;

        bool hasEnterTransition = stateMachine.anyStateTransitions.Any(transition =>
            transition.destinationState == climbState && HasCondition(transition, "IsClimbing", AnimatorConditionMode.If));
        if (!hasEnterTransition)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(climbState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsClimbing");
        }

        AnimatorState defaultState = stateMachine.defaultState;
        bool hasExitTransition = climbState.transitions.Any(transition =>
            transition.destinationState == defaultState && HasCondition(transition, "IsClimbing", AnimatorConditionMode.IfNot));
        if (!hasExitTransition)
        {
            AnimatorStateTransition transition = climbState.AddTransition(defaultState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsClimbing");
        }
    }

    private static bool HasCondition(AnimatorStateTransition transition, string parameter, AnimatorConditionMode mode)
    {
        return transition.conditions.Any(condition =>
            condition.parameter == parameter && condition.mode == mode);
    }
}
