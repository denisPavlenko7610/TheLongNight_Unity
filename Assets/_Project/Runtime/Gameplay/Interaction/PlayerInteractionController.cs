using TLN.Application.Input;
using TLN.Gameplay.Player;
using TLN.Gameplay.Player.Input;
using UnityEngine;

namespace TLN.Gameplay.Interaction
{
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerRoot _playerRoot;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private Camera _camera;

        [Header("Raycast")]
        [SerializeField] private float _maxDistance = 3f;
        [SerializeField] private LayerMask _interactableLayerMask;

        private IInputModeService _inputModeService;
        private IInteractionPromptView _promptView;
        private PlayerInteractionRaycaster _raycaster;

        private InteractionHit? _currentHit;

        public void Construct(IInputModeService inputModeService, IInteractionPromptView promptView)
        {
            _inputModeService = inputModeService;
            _promptView = promptView;
        }

        private void Awake()
        {
            if (_playerRoot == null)
            {
                _playerRoot = GetComponent<PlayerRoot>();
            }

            if (_inputReader == null)
            {
                _inputReader = GetComponent<PlayerInputReader>();
            }

            if (_camera == null && _playerRoot != null)
            {
                _camera = _playerRoot.Camera;
            }

            _raycaster = new PlayerInteractionRaycaster(_camera, _maxDistance, _interactableLayerMask);
        }

        private void Update()
        {
            if (_inputModeService != null && !_inputModeService.CanUseGameplayInput)
            {
                ClearCurrentTarget();
                return;
            }

            UpdateCurrentTarget();
            TryInteract();
        }

        private void UpdateCurrentTarget()
        {
            if (_raycaster.TryRaycast(out InteractionHit hit))
            {
                InteractionContext context = CreateContext();

                if (hit.Interactable.CanInteract(context))
                {
                    _currentHit = hit;
                    _promptView?.Show(hit.Interactable.InteractionText);
                    return;
                }
            }

            ClearCurrentTarget();
        }

        private void TryInteract()
        {
            if (!_inputReader.WasInteractPressedThisFrame)
            {
                return;
            }

            if (!_currentHit.HasValue)
            {
                return;
            }

            InteractionContext context = CreateContext();

            IInteractable interactable = _currentHit.Value.Interactable;

            if (!interactable.CanInteract(context))
            {
                ClearCurrentTarget();
                return;
            }

            interactable.Interact(context);
        }

        private InteractionContext CreateContext()
        {
            return new InteractionContext(_playerRoot, _camera);
        }

        private void ClearCurrentTarget()
        {
            _currentHit = null;
            _promptView?.Hide();
        }
    }
}
