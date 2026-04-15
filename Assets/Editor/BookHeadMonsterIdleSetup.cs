#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-configura un idle simple para el BookHeadMonster colocado en la escena.
/// Se ejecuta al recompilar el proyecto o manualmente desde Tools/BookHeadMonster.
/// </summary>
[InitializeOnLoad]
public static class BookHeadMonsterIdleSetup
{
    private const string TargetObjectName = "BookHeadMonster (1)";
    private const string IdleClipPath = "Assets/BookHeadMonster/Y Bot@Old Man Idle.fbx";
    private const string MonsterModelPath = "Assets/BookHeadMonster/Built-in/Meshes/BookHeadMonster.fbx";
    private const string ControllerPath = "Assets/BookHeadMonster/BookHeadMonsterIdle.controller";

    static BookHeadMonsterIdleSetup()
    {
        EditorApplication.delayCall += ApplyToOpenScenes;
    }

    [MenuItem("Tools/BookHeadMonster/Reaplicar Idle")]
    public static void ReapplyFromMenu()
    {
        ApplyToOpenScenes();
    }

    private static void ApplyToOpenScenes()
    {
        if (!EnsureHumanoidAnimationImport())
        {
            return;
        }

        AnimationClip idleClip = LoadIdleClip();
        Avatar avatar = LoadMonsterAvatar();
        AnimatorController controller = EnsureIdleController(idleClip);

        if (idleClip == null || avatar == null || controller == null)
        {
            return;
        }

        bool changedScene = false;
        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject current = sceneObjects[i];
            if (current == null || current.name != TargetObjectName)
            {
                continue;
            }

            if (!current.scene.IsValid() || EditorUtility.IsPersistent(current))
            {
                continue;
            }

            Animator animator = current.GetComponent<Animator>();
            if (animator == null)
            {
                animator = current.AddComponent<Animator>();
            }

            bool changedAnimator = false;

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                changedAnimator = true;
            }

            if (animator.avatar != avatar)
            {
                animator.avatar = avatar;
                changedAnimator = true;
            }

            if (animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
                changedAnimator = true;
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                changedAnimator = true;
            }

            if (animator.updateMode != AnimatorUpdateMode.Normal)
            {
                animator.updateMode = AnimatorUpdateMode.Normal;
                changedAnimator = true;
            }

            if (changedAnimator)
            {
                EditorUtility.SetDirty(animator);
                EditorSceneManager.MarkSceneDirty(current.scene);
                changedScene = true;
            }
        }

        if (changedScene)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static bool EnsureHumanoidAnimationImport()
    {
        ModelImporter importer = AssetImporter.GetAtPath(IdleClipPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"No se encontro el importer del clip: {IdleClipPath}");
            return false;
        }

        bool needsReimport = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            needsReimport = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            needsReimport = true;
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
        }

        return true;
    }

    private static AnimationClip LoadIdleClip()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(IdleClipPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && clip.name == "Y Bot@Old Man Idle")
            {
                return clip;
            }
        }

        Debug.LogWarning("No se encontro el clip 'Y Bot@Old Man Idle' dentro del FBX.");
        return null;
    }

    private static Avatar LoadMonsterAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(MonsterModelPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar avatar && avatar.isValid && avatar.isHuman)
            {
                return avatar;
            }
        }

        Debug.LogWarning("No se encontro un Avatar humanoide valido para BookHeadMonster.");
        return null;
    }

    private static AnimatorController EnsureIdleController(AnimationClip idleClip)
    {
        if (idleClip == null)
        {
            return null;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        if (controller == null || controller.layers == null || controller.layers.Length == 0)
        {
            return controller;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine == null)
        {
            return controller;
        }

        AnimatorState idleState = stateMachine.states
            .Select(entry => entry.state)
            .FirstOrDefault(state => state != null && state.name == "Idle");

        if (idleState == null)
        {
            idleState = stateMachine.AddState("Idle", new Vector3(260f, 120f, 0f));
        }

        idleState.motion = idleClip;
        idleState.speed = 1f;
        idleState.writeDefaultValues = true;
        stateMachine.defaultState = idleState;

        foreach (ChildAnimatorState stateEntry in stateMachine.states)
        {
            if (stateEntry.state != idleState)
            {
                stateMachine.RemoveState(stateEntry.state);
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }
}
#endif
