using UnityEngine;
using UnityEngine.UI;

// 로딩 씬에 붙인다. 목적지 씬을 읽어들이는 건 GameSession 이 하고, 여기서는 그 진행률만 보여준다.
// 로딩 화면의 그림/문구/바 배치는 로딩 씬에서 자유롭게 꾸미면 된다. 연결은 전부 선택 사항이다.
//
// 씬 전환은 GameSession 이 이어서 처리하므로, 이 스크립트가 씬을 바꾸지는 않는다.
public class LoadingSceneController : MonoBehaviour
{
    [Tooltip("진행률 바 (0~1). 없어도 된다")]
    [SerializeField] private Slider progressBar;
    [Tooltip("진행률을 채워서 보여줄 이미지 (Image Type = Filled). 없어도 된다")]
    [SerializeField] private Image progressFill;
    [Tooltip("진행률 숫자 표시 (예: 73%). 없어도 된다")]
    [SerializeField] private Text progressText;
    [Tooltip("어디로 가는 중인지 보여줄 문구. 없어도 된다")]
    [SerializeField] private Text destinationText;
    [Tooltip("목적지 문구 형식. {0} 자리에 씬 이름이 들어간다")]
    [SerializeField] private string destinationFormat = "{0} (으)로 이동 중...";

    private void Start()
    {
        if (destinationText != null)
        {
            destinationText.text = string.Format(destinationFormat, GameSession.Instance.PendingSceneName);
        }
    }

    private void Update()
    {
        float progress = GameSession.Instance.LoadProgress;

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = progress;
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = progress;
        }

        if (progressText != null)
        {
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
        }
    }
}
