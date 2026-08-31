#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// haneul_scene 에서 다듬은 HUD 캔버스를 실제 플레이 씬으로 옮긴다.
// 대상 씬에 이미 같은 이름의 빈 Canvas 가 있으므로 복사해서 얹지 않고 통째로 교체한다.
// 교체하면서 끊기는 씬 안의 참조는 새 오브젝트로 다시 연결한다.
// 메뉴: Tools/Haneul/Install HUD Into SampleScene
public static class HudInstaller
{
    const string SourceScene = "Assets/Haneul_Branch/haneul_scene.unity";
    const string TargetScene = "Assets/Scenes/SampleScene.unity";
    const string CanvasName = "Canvas";
    const string CrosshairPath = "DefaultGroup/Aim";

    [MenuItem("Tools/Haneul/Install HUD Into SampleScene")]
    public static void Install()
    {
        Install(true);
    }

    // confirm = false 는 확인 창 없이 바로 실행한다 (자동화용)
    public static void Install(bool confirm)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (confirm && !EditorUtility.DisplayDialog("HUD 설치",
                "SampleScene 의 기존 Canvas 를 지우고 haneul_scene 의 HUD 캔버스로 교체합니다.\n계속할까요?",
                "교체", "취소"))
            return;

        Scene target = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        Scene source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Additive);

        GameObject sourceCanvas = FindRoot(source, CanvasName);
        if (sourceCanvas == null)
        {
            Debug.LogError("[HudInstaller] haneul_scene 에서 '" + CanvasName + "' 를 찾지 못했습니다.");
            EditorSceneManager.CloseScene(source, true);
            return;
        }

        // 기존 캔버스 제거
        int removed = 0;
        foreach (GameObject root in target.GetRootGameObjects())
        {
            if (root.name != CanvasName) continue;
            Object.DestroyImmediate(root);
            removed++;
        }

        // 새 캔버스 복사
        GameObject copy = Object.Instantiate(sourceCanvas);
        copy.name = CanvasName;
        SceneManager.MoveGameObjectToScene(copy, target);

        EditorSceneManager.CloseScene(source, true);

        int relinked = RelinkCrosshair(copy);
        int extraEventSystems = TrimEventSystems(target);

        EditorSceneManager.MarkSceneDirty(target);
        EditorSceneManager.SaveScene(target);

        Debug.Log("[HudInstaller] HUD 설치 완료 — 기존 캔버스 " + removed + "개 교체, "
            + "crosshair 참조 " + relinked + "개 재연결, 중복 EventSystem " + extraEventSystems + "개 정리");
    }

    // PlayerADS 는 정조준 중에 크로스헤어를 껐다 켠다. 캔버스를 갈아끼우면 이 참조가 끊긴다.
    static int RelinkCrosshair(GameObject canvas)
    {
        Transform aim = canvas.transform.Find(CrosshairPath);
        if (aim == null)
        {
            Debug.LogWarning("[HudInstaller] '" + CrosshairPath + "' 를 찾지 못해 crosshair 를 연결하지 못했습니다.");
            return 0;
        }

        int count = 0;
        PlayerADS[] all = Object.FindObjectsByType<PlayerADS>(FindObjectsInactive.Include);
        foreach (PlayerADS ads in all)
        {
            ads.crosshair = aim.gameObject;
            EditorUtility.SetDirty(ads);
            count++;
        }
        return count;
    }

    // 캔버스와 함께 EventSystem 이 딸려오면 둘이 되어 입력이 꼬인다.
    static int TrimEventSystems(Scene scene)
    {
        List<EventSystem> found = new List<EventSystem>();
        foreach (GameObject root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<EventSystem>(true));

        int removed = 0;
        for (int i = 1; i < found.Count; i++)
        {
            Object.DestroyImmediate(found[i].gameObject);
            removed++;
        }
        return removed;
    }

    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }
}
#endif
