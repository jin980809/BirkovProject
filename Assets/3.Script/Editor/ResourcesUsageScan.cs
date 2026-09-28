using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Resources 폴더 안에서 실제로 빌드에 쓰이는 파일이 뭔지 정확히 찾는 일회성 도구.
// 텍스트 검색(guid grep)으로는 바이너리로 직렬화된 에셋(TerrainData 등) 안의 참조를 못 찾아서
// 대신 유니티 자체의 AssetDatabase.GetDependencies() 를 쓴다 - 바이너리/텍스트 상관없이 정확하다.
// 다 쓰면 이 스크립트는 지워도 된다.
public static class ResourcesUsageScan
{
    private static readonly string[] BuildScenes =
    {
        "Assets/1.Scene/StartScene.unity",
        "Assets/1.Scene/LoadingScene.unity",
        "Assets/1.Scene/LobbyScene.unity",
        "Assets/1.Scene/BattleScene.unity",
    };

    [MenuItem("Tools/NaYeongMin/Resources 사용 여부 스캔")]
    public static void Scan()
    {
        HashSet<string> used = new HashSet<string>();

        // 1) 빌드 씬 4개가 의존하는 모든 에셋(재귀) - 씬 안에서 직접 쓰는 것 전부
        foreach (string scene in BuildScenes)
        {
            foreach (string dep in AssetDatabase.GetDependencies(scene, true))
            {
                used.Add(dep);
            }
        }

        // 2) Graphics/Quality 세팅이 물고 있는 항상-포함 에셋(셰이더 등)도 같이 확인
        string[] extraRoots =
        {
            "ProjectSettings/GraphicsSettings.asset",
            "ProjectSettings/QualitySettings.asset",
        };
        foreach (string root in extraRoots)
        {
            if (File.Exists(root))
            {
                foreach (string dep in AssetDatabase.GetDependencies(root, true))
                {
                    used.Add(dep);
                }
            }
        }

        // Resources 안의 전체 파일 중 위 사용 목록에 없는 것을 뽑는다
        string[] allResourceFiles = Directory.GetFiles("Assets/Resources", "*", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(".meta"))
            .Select(p => p.Replace('\\', '/'))
            .ToArray();

        List<string> unusedList = new List<string>();
        List<string> usedList = new List<string>();
        foreach (string f in allResourceFiles)
        {
            if (used.Contains(f))
            {
                usedList.Add(f);
            }
            else
            {
                unusedList.Add(f);
            }
        }

        unusedList.Sort();
        usedList.Sort();

        string outDir = "Assets/../ResourcesUsageScanResult";
        Directory.CreateDirectory(outDir);
        File.WriteAllLines(Path.Combine(outDir, "unused.txt"), unusedList);
        File.WriteAllLines(Path.Combine(outDir, "used.txt"), usedList);

        Debug.Log($"[ResourcesUsageScan] 전체 {allResourceFiles.Length}개 중 사용중 {usedList.Count}개, 미사용(추정) {unusedList.Count}개.\n" +
                  $"결과: {Path.GetFullPath(outDir)}\\unused.txt / used.txt");
    }
}
