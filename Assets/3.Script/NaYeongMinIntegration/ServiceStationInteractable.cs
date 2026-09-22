using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    public sealed class ServiceStationInteractable : MonoBehaviour, IInteractable
    {
        public InventoryTestBench inventoryBench;
        public bool crafting;
        public Birdkov.NaYeongMin.InventorySystem.MerchantKind merchantKind = Birdkov.NaYeongMin.InventorySystem.MerchantKind.Weapons;
        public GameObject prompt;
        public PlayerInputHandler playerInput;
        public Transform player;
        // 씬에 하나뿐인 것들은 비어 있으면 알아서 찾는다. 기획팀은 스크립트만 붙이면 된다.
        private void Awake()
        {
            if (inventoryBench == null) inventoryBench = FindAnyObjectByType<InventoryTestBench>();
            if (playerInput == null) playerInput = FindAnyObjectByType<PlayerInputHandler>();
            if (player == null && playerInput != null) player = playerInput.transform;
            if (prompt == null)
            {
                Transform found = transform.Find("InteractionLabel");
                if (found != null) prompt = found.gameObject;
            }

            // 이 둘은 코드가 대신 못 해 준다. 빠지면 F 로 열리지 않으니 바로 알려 준다.
            if (GetComponent<Collider>() == null) Debug.LogWarning("ServiceStationInteractable: Collider 가 있어야 상호작용됩니다.", this);
            if (gameObject.layer != LayerMask.NameToLayer("Interaction")) Debug.LogWarning("ServiceStationInteractable: 레이어를 Interaction 으로 바꿔야 F 로 열립니다.", this);
        }

        private void OnEnable() { if (playerInput != null) playerInput.InteractPressed += CloseStation; }
        private void OnDisable() { if (playerInput != null) playerInput.InteractPressed -= CloseStation; }
        private void CloseStation()
        {
            if (inventoryBench != null && inventoryBench.IsExternalOpen && inventoryBench.ExternalAnchor == transform)
                inventoryBench.CloseCurrent();
        }
        private void Update()
        {
            if (player != null && Vector3.Distance(player.position, transform.position) > 3f) CloseStation();
        }
        public float InteractDuration => 0.2f;
        public bool CanInteract(GameObject interactor) => inventoryBench != null && inventoryBench.IsReady && !inventoryBench.IsExternalOpen;
        public void OnInteractComplete(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (crafting) inventoryBench.OpenCrafting(transform);
            else inventoryBench.OpenShop(transform, merchantKind);
        }
        public void ShowPrompt() { if (prompt != null) prompt.SetActive(true); }
        public void HidePrompt() { if (prompt != null) prompt.SetActive(false); }
    }
}
