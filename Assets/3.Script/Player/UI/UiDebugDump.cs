using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 진단용 임시 스크립트. 화면에 실제로 보이는 UI 가 무엇인지 찾기 위해 씬의 Canvas 구조와
// 활성 상태를 콘솔에 찍는다. 원인을 찾으면 이 파일과 컴포넌트를 지우면 된다.
//  - 시작 1초 뒤 자동으로 한 번 찍는다 (벤치가 런타임에 UI 를 만든 뒤 상태를 보기 위해 늦춘다)
//  - F9 를 누르면 언제든 다시 찍는다 (인벤토리를 열고 닫은 뒤 비교용)
public class UiDebugDump : MonoBehaviour
{
    [Tooltip("시작 후 자동으로 찍을 때까지 기다리는 시간(초)")]
    [SerializeField] private float firstDumpDelay = 1f;
    [Tooltip("이 깊이까지만 자식을 따라 들어간다")]
    [SerializeField] private int maxDepth = 3;

    private bool didFirstDump;
    private float startTime;

    private void Start()
    {
        startTime = Time.time;
    }

    private void Update()
    {
        if (!didFirstDump && Time.time - startTime >= firstDumpDelay)
        {
            didFirstDump = true;
            Dump("자동 (시작 " + firstDumpDelay + "초 후)");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            Dump("F9 수동");
        }
    }

    private void Dump(string reason)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("===== UI 덤프 [" + reason + "] =====");

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        builder.AppendLine("Canvas 개수: " + canvases.Length);

        foreach (Canvas canvas in canvases)
        {
            if (canvas.transform.parent == null || canvas.transform.parent.GetComponentInParent<Canvas>() == null)
            {
                builder.AppendLine("");
                builder.AppendLine("[Canvas] " + GetPath(canvas.transform) +
                                   "  (활성 " + canvas.gameObject.activeInHierarchy +
                                   ", 모드 " + canvas.renderMode +
                                   ", sortingOrder " + canvas.sortingOrder + ")");
                AppendChildren(builder, canvas.transform, 1);
            }
        }

        Debug.Log(builder.ToString());
    }

    private void AppendChildren(StringBuilder builder, Transform parent, int depth)
    {
        if (depth > maxDepth)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            string mark = "  (꺼짐)";
            if (child.gameObject.activeInHierarchy)
            {
                mark = "  <<< 화면에 보임";
            }

            builder.AppendLine(new string(' ', depth * 2) + "- " + child.name + mark + GraphicInfo(child));
            AppendChildren(builder, child, depth + 1);
        }
    }

    // 실제로 그려지는 요소(Image/Text)인지, 글자 내용은 무엇인지 같이 찍는다
    private string GraphicInfo(Transform target)
    {
        string info = string.Empty;

        if (target.TryGetComponent(out Text text))
        {
            info = "  [Text: " + text.text + "]";
        }
        else if (target.TryGetComponent(out Image image))
        {
            info = "  [Image]";
            if (image.sprite != null)
            {
                info = "  [Image: " + image.sprite.name + "]";
            }
        }

        return info;
    }

    private string GetPath(Transform target)
    {
        string path = target.name;
        Transform current = target.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
