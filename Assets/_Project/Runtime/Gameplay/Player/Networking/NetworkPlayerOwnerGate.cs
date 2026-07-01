using Assign;
using TLN.Core.Utilities;
using TLN.Gameplay.Interaction;
using TLN.Gameplay.Player.Input;
using TLN.Gameplay.Player.Look;
using TLN.Gameplay.Player.Movement;
using Unity.Netcode;
using UnityEngine;

namespace TLN.Gameplay.Player.Networking
{
	[RequireComponent(typeof(NetworkObject))]
	public sealed class NetworkPlayerOwnerGate : NetworkBehaviour
	{
		[SerializeField, Assign(Mode.Children)] private Camera _camera;
		[SerializeField, Assign(Mode.Children)] private AudioListener _audioListener;
		[SerializeField, Assign] private PlayerInputReader _inputReader;
		[SerializeField, Assign] private PlayerMotor _motor;
		[SerializeField, Assign] private PlayerLook _look;
		[SerializeField, Assign] private PlayerPauseController _pauseController;
		[SerializeField, Assign] private PlayerInteractionController _interactionController;
		[SerializeField, Assign] private PlayerInventoryController _inventoryController;
		[SerializeField, Assign] private PlayerBuildController _buildController;
		[SerializeField, Assign] private PlayerTimeOverlayController _timeOverlayController;
		[SerializeField, Assign] private PlayerWarmthController _warmthController;

		private void Awake()
		{
			if (IsNetworkSessionActive())
			{
				ApplyLocalOwnership(false);
			}
		}

		public override void OnNetworkSpawn()
		{
			ApplyLocalOwnership(IsOwner);
		}

		public override void OnNetworkDespawn()
		{
			ApplyLocalOwnership(false);
		}

		public override void OnGainedOwnership()
		{
			ApplyLocalOwnership(true);
		}

		public override void OnLostOwnership()
		{
			ApplyLocalOwnership(false);
		}

		private void ApplyLocalOwnership(bool isLocalPlayer)
		{
			SetLocalCamera(isLocalPlayer);

			SetBehaviour(_inputReader, isLocalPlayer);
			SetBehaviour(_motor, isLocalPlayer);
			SetBehaviour(_look, isLocalPlayer);
			SetBehaviour(_pauseController, isLocalPlayer);
			SetBehaviour(_interactionController, isLocalPlayer);
			SetBehaviour(_inventoryController, isLocalPlayer);
			SetBehaviour(_buildController, isLocalPlayer);
			SetBehaviour(_timeOverlayController, isLocalPlayer);

			SetBehaviour(_warmthController, isLocalPlayer);
		}

		private void SetLocalCamera(bool isLocalPlayer)
		{
			if (_audioListener != null)
			{
				_audioListener.enabled = isLocalPlayer;
			}

			if (_camera != null)
			{
				_camera.enabled = isLocalPlayer;
				_camera.gameObject.SetActive(isLocalPlayer);
			}

			CameraUtility.InvalidateCache();
		}

		private static void SetBehaviour(Behaviour behaviour, bool enabled)
		{
			if (behaviour != null)
			{
				behaviour.enabled = enabled;
			}
		}

		private static bool IsNetworkSessionActive()
		{
			NetworkManager networkManager = NetworkManager.Singleton;
			return networkManager != null && networkManager.IsListening;
		}
	}
}
