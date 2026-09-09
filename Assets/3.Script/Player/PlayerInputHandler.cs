using System;
using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInput 컴포넌트(Behavior = Invoke Unity Events)가 이 스크립트의
// On... 메서드로 InputAction.CallbackContext 를 넘겨준다.
// PlayerController 는 TryGetComponent 로 이 컴포넌트를 가져와서
// 연속 입력 값(프로퍼티)과 단발 입력(이벤트)을 사용한다.
public class PlayerInputHandler : MonoBehaviour
{
    // ---------- 연속 입력 상태 ----------

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookScreenPosition { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool FireHeld { get; private set; }

    // ---------- 단발 입력 이벤트 ----------

    public event Action FirePressed;
    public event Action FireReleased;
    public event Action DodgePressed;
    public event Action ReloadPressed;
    public event Action InteractPressed;
    public event Action InventoryToggled;
    public event Action<int> WeaponSelected; // 0 = 1번, 1 = 2번
    public event Action<int> QuickSlotUsed;  // 0~2 = 3, 4, 5번 키

    // ---------- 입력 콜백 ----------

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookScreenPosition = context.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            FireHeld = true;
            if (FirePressed != null)
            {
                FirePressed();
            }
        }
        else if (context.canceled)
        {
            FireHeld = false;
            if (FireReleased != null)
            {
                FireReleased();
            }
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SprintHeld = true;
        }
        else if (context.canceled)
        {
            SprintHeld = false;
        }
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed && DodgePressed != null)
        {
            DodgePressed();
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed && ReloadPressed != null)
        {
            ReloadPressed();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && InteractPressed != null)
        {
            InteractPressed();
        }
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.performed && InventoryToggled != null)
        {
            InventoryToggled();
        }
    }

    public void OnWeapon1(InputAction.CallbackContext context)
    {
        if (context.performed && WeaponSelected != null)
        {
            WeaponSelected(0);
        }
    }

    public void OnWeapon2(InputAction.CallbackContext context)
    {
        if (context.performed && WeaponSelected != null)
        {
            WeaponSelected(1);
        }
    }

    public void OnQuickSlot1(InputAction.CallbackContext context)
    {
        if (context.performed && QuickSlotUsed != null)
        {
            QuickSlotUsed(0);
        }
    }

    public void OnQuickSlot2(InputAction.CallbackContext context)
    {
        if (context.performed && QuickSlotUsed != null)
        {
            QuickSlotUsed(1);
        }
    }

    public void OnQuickSlot3(InputAction.CallbackContext context)
    {
        if (context.performed && QuickSlotUsed != null)
        {
            QuickSlotUsed(2);
        }
    }
}
