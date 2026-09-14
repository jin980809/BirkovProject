using UnityEngine;

// 플레이어 인벤토리 UI. 지금은 열림/닫힘만 하는 빈 패널이다.
// 실제 아이템 슬롯 표시/드래그 등 인벤토리 내용물 연동은 나중에 NaYeongMin 쪽에서 붙인다
// (지금은 InventoryService/PlayerInventoryService 등 데이터 계층만 있고 UI 패널이 없어서 임시로 만든다).
//
// 상자를 열 때 ChestUI 가 이 패널도 같이 열고 닫는다 (룻팅 게임에서 상자+내 인벤토리를 같이 보여주는 것과 동일).
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    public bool IsOpen
    {
        get { return panelRoot != null && panelRoot.activeSelf; }
    }

    private void Awake()
    {
        Close();
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}
