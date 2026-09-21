using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    [RequireComponent(typeof(PlayerDeathContainer))]
    public sealed class PlayerDeathInteractable : MonoBehaviour, IInteractable
    {
        public InventoryTestBench inventoryBench;
        public GameObject prompt;
        private PlayerInputHandler playerInput;
        private Transform interactor;
        public float InteractDuration => 0.3f;

        public bool CanInteract(GameObject actor)
        {
            if (inventoryBench == null) inventoryBench = FindAnyObjectByType<InventoryTestBench>();
            return inventoryBench != null && inventoryBench.IsReady && !inventoryBench.IsExternalOpen &&
                GetComponent<InventoryWorldContainer>().kind == InventoryWorldKind.PlayerDeath;
        }

        public void OnInteractComplete(GameObject actor)
        {
            if (actor == null || !CanInteract(actor)) return;
            Unsubscribe();
            interactor = actor.transform;
            playerInput = actor.GetComponent<PlayerInputHandler>();
            if (playerInput != null) playerInput.InteractPressed += Close;
            GetComponent<InventoryWorldContainer>().Open(inventoryBench);
            HidePrompt();
        }

        private void Update()
        {
            if (interactor != null && Vector3.Distance(interactor.position, transform.position) > 3f) Close();
        }
        private void Close()
        {
            if (inventoryBench != null && inventoryBench.ExternalAnchor == transform) inventoryBench.CloseCurrent();
            Unsubscribe();
        }
        private void Unsubscribe()
        {
            if (playerInput != null) playerInput.InteractPressed -= Close;
            playerInput = null;
            interactor = null;
        }
        private void OnDisable() { Close(); }
        public void ShowPrompt() { if (prompt != null) prompt.SetActive(true); }
        public void HidePrompt() { if (prompt != null) prompt.SetActive(false); }
    }
}
