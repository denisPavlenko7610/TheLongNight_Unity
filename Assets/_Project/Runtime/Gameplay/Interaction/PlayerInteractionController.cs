using TLN.Application.Input;
using TLN.Gameplay.Player;
using TLN.Gameplay.Player.Input;
using UnityEngine;
using VContainer;

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

        [Header("Debug")]
        [SerializeField] private bool _drawDebugRay = true;
        [SerializeField] private Color _debugHitColor = Color.green;
        [SerializeField] private Color _debugMissColor = Color.red;
        [SerializeField] private Color _debugNormalColor = Color.yellow;
        [SerializeField] private float _debugRayDuration;

        private IInputModeService _inputModeService;
        private IInteractionPromptView _promptView;
        private PlayerInteractionRaycaster _raycaster;

        private InteractionHit? _currentHit;

        [Inject]
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
            bool hasHit = _raycaster.TryRaycast(out InteractionHit hit);

            DrawDebugRay(hasHit, hit);

            if (hasHit)
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

        private void DrawDebugRay(bool hasHit, InteractionHit hit)
        {
            if (!_drawDebugRay || _camera == null)
            {
                return;
            }

            Vector3 origin = _camera.transform.position;
            Vector3 direction = _camera.transform.forward;

            if (hasHit)
            {
                Debug.DrawRay(origin, direction * hit.Distance, _debugHitColor, _debugRayDuration);
                Debug.DrawRay(hit.Point, hit.Normal * 0.35f, _debugNormalColor, _debugRayDuration);

                return;
            }

            Debug.DrawRay(origin, direction * _maxDistance, _debugMissColor, _debugRayDuration);
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
