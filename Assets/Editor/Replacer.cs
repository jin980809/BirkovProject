using UnityEditor;
using UnityEngine;

// 사용법:
// 1) 이 파일을 프로젝트의 Assets/Editor 폴더 안에 넣기 (Editor 폴더가 없으면 새로 만들면 됨)
// 2) Unity 메뉴에서 Tools > Replace Selected With Prefab 클릭
// 3) 열린 창에 New Prefab 칸에 교체할 새 프리팹을 드래그해서 넣기
// 4) Hierarchy에서 바꿀 오브젝트들을 전부 선택
// 5) 창의 "Replace Selected" 버튼 클릭

public class ReplacePrefabWindow : EditorWindow
{
    private GameObject newPrefab;
    private bool keepOriginalName = false;

    [MenuItem("Tools/Replace Selected With Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ReplacePrefabWindow>("Replace With Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("선택된 오브젝트들을 아래 프리팹으로 교체합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        newPrefab = (GameObject)EditorGUILayout.ObjectField(
            "New Prefab", newPrefab, typeof(GameObject), false);

        keepOriginalName = EditorGUILayout.Toggle("기존 이름 유지", keepOriginalName);

        EditorGUILayout.Space();
        GUILayout.Label($"현재 선택된 오브젝트 수: {Selection.gameObjects.Length}");

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(newPrefab == null || Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Replace Selected"))
            {
                ReplaceSelected();
            }
        }
    }

    private void ReplaceSelected()
    {
        GameObject[] targets = Selection.gameObjects;
        int count = 0;

        Undo.SetCurrentGroupName("Replace With Prefab");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject original in targets)
        {
            if (original == null) continue;

            Transform originalTransform = original.transform;
            Transform parent = originalTransform.parent;
            string originalName = original.name;

            // 프리팹 연결을 유지한 채로 인스턴스 생성
            GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
            if (newInstance == null)
            {
                // newPrefab이 프리팹 에셋이 아니면 일반 복제로 대체
                newInstance = Object.Instantiate(newPrefab);
            }

            Undo.RegisterCreatedObjectUndo(newInstance, "Replace With Prefab");

            newInstance.transform.SetParent(parent, false);
            newInstance.transform.localPosition = originalTransform.localPosition;
            newInstance.transform.localRotation = originalTransform.localRotation;
            newInstance.transform.localScale = originalTransform.localScale;

            if (keepOriginalName)
            {
                newInstance.name = originalName;
            }

            Undo.DestroyObjectImmediate(original);
            count++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ReplaceWithPrefab] 총 {count}개 오브젝트를 '{newPrefab.name}'으로 교체했습니다.");
    }
}