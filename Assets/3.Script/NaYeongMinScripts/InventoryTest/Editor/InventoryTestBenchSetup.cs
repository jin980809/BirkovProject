using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Birdkov.NaYeongMin.InventoryTest.EditorTools
{
    // NaYeongMin 씬에 테스트 벤치를 만들고 CSV와 아이콘을 연결한다.
    // 팀원 씬과 파일은 건드리지 않는다.
    public static class InventoryTestBenchSetup
    {
        // 씬 위치는 정리 과정에서 바뀔 수 있으므로 경로를 박아두지 않고 이름으로 찾는다.
        private const string SceneName = "NaYeongMin";

        private static string FindScenePath()
        {
            foreach (string guid in AssetDatabase.FindAssets(SceneName + " t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == SceneName)
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private const string ItemCsvPath = "Assets/DataTeble/NaYeongMinCsvData/ItemData.csv";
        private const string DropCsvPath = "Assets/DataTeble/NaYeongMinCsvData/DropTable.csv";

        [MenuItem("Tools/NaYeongMin/테스트 벤치 구성")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Play를 종료한 뒤 테스트 벤치를 구성하세요.");
                return;
            }
            string scenePath = FindScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError(SceneName + " 씬을 찾지 못했습니다. 씬 이름이 바뀌었는지 확인하세요.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (!EditorUtility.DisplayDialog(
                        "테스트 벤치 구성",
                        scenePath + " 을(를) 열고 진행할까요? 저장하지 않은 변경은 확인 후 처리됩니다.",
                        "열고 진행", "취소"))
                {
                    return;
                }

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EditorSceneManager.OpenScene(scenePath);
            }

            var scene = EditorSceneManager.GetActiveScene();
            InventoryTestBench bench = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bench = root.GetComponentInChildren<InventoryTestBench>(true);
                if (bench != null) break;
            }
            if (bench == null)
            {
                GameObject canvasObject = new GameObject(
                    "InventoryTestBench",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryTestBench));
                bench = canvasObject.GetComponent<InventoryTestBench>();
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            bench.itemCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(ItemCsvPath);
            bench.dropCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(DropCsvPath);

            if (bench.itemCsv == null)
            {
                Debug.LogError("ItemData.csv 를 찾지 못했습니다: " + ItemCsvPath);
                return;
            }

            bench.icons = BuildIconBindings(bench.itemCsv.text);

            EditorUtility.SetDirty(bench);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            if (PrefabUtility.IsPartOfPrefabInstance(bench))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(bench);
            }
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"테스트 벤치 구성 완료. 아이콘 {bench.icons.Length}개 연결. Play 를 눌러 확인하세요.");
        }

        // CSV 의 iconKey 경로를 읽어 Sprite 참조를 직렬화한다.
        // 아이콘 에셋이 교체되면 이 메뉴만 다시 실행하면 된다.
        private static TestIconBinding[] BuildIconBindings(string csvText)
        {
            List<TestIconBinding> bindings = new List<TestIconBinding>();
            foreach (ItemData item in ItemCsvLoader.Parse(csvText))
            {
                if (string.IsNullOrEmpty(item.iconKey))
                {
                    continue;
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.iconKey);
                if (sprite == null)
                {
                    Debug.LogWarning($"아이콘을 찾지 못했습니다. itemId {item.itemId} : {item.iconKey}");
                    continue;
                }

                bindings.Add(new TestIconBinding { itemId = item.itemId, sprite = sprite });
            }

            return bindings.ToArray();
        }
    }
}
