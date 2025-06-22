using TheLongNight.UI.PauseMenu;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class UIInputHandler : MonoBehaviour
{
    [SerializeField] private GamePause _gamePause;

    public void OnPause(InputValue value)
    {
        if (value.isPressed)
            _gamePause.TogglePause();
    }
}