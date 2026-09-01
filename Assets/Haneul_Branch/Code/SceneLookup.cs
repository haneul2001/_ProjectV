using System;
using System.IO;
using UnityEngine.SceneManagement;

// 씬 이름이 Build Settings 에 들어 있는지 확인한다.
// Application.CanStreamedLevelBeLoaded 는 에디터에서 빌드 목록을 제대로 못 봐서
// (등록돼 있어도 false) 빌드 목록을 직접 훑는다.
public static class SceneLookup
{
    public static bool IsInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                    StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
