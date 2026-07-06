using System;
using System.Collections.Generic;
using System.IO;
using TLN.Core.Logging;
using TLN.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLN.Gameplay.World.Streaming
{
	public sealed class WorldStreamingController : MonoBehaviour
	{
		private const float MinCheckIntervalSeconds = 0.05f;

		[SerializeField] private Transform _playerOverride;
		[SerializeField, Min(MinCheckIntervalSeconds)] private float _checkIntervalSeconds = 0.35f;
		[SerializeField] private bool _logTransitions = true;
		[SerializeField] private WorldStreamedLocation[] _locations = Array.Empty<WorldStreamedLocation>();

		private readonly Dictionary<string, StreamedSceneState> _statesBySceneName = new(StringComparer.Ordinal);

		private Transform _player;
		private float _nextEvaluationTime;

		private void Awake()
		{
			RebuildRuntimeStates();
		}

		private void OnEnable()
		{
			if (_statesBySceneName.Count == 0)
			{
				RebuildRuntimeStates();
			}

			_nextEvaluationTime = 0f;
		}

		private void OnValidate()
		{
			if (_checkIntervalSeconds < MinCheckIntervalSeconds)
			{
				_checkIntervalSeconds = MinCheckIntervalSeconds;
			}

			if (_locations == null)
			{
				return;
			}

			for (int i = 0; i < _locations.Length; i++)
			{
				_locations[i]?.Normalize();
			}
		}

		private void Update()
		{
			if (!TryGetPlayerPosition(out Vector3 playerPosition))
			{
				return;
			}

			if (UnityEngine.Time.unscaledTime < _nextEvaluationTime)
			{
				return;
			}

			_nextEvaluationTime = UnityEngine.Time.unscaledTime + Mathf.Max(MinCheckIntervalSeconds, _checkIntervalSeconds);
			EvaluateLocations(playerPosition);
		}

		private void OnDrawGizmosSelected()
		{
			if (_locations == null)
			{
				return;
			}

			for (int i = 0; i < _locations.Length; i++)
			{
				WorldStreamedLocation location = _locations[i];
				if (!IsLocationConfigured(location))
				{
					continue;
				}

				Gizmos.color = new Color(0.15f, 0.75f, 1f, 0.45f);
				Gizmos.DrawWireSphere(location.Center, location.LoadRadius);

				Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
				Gizmos.DrawWireSphere(location.Center, location.UnloadRadius);
			}
		}

		private void RebuildRuntimeStates()
		{
			_statesBySceneName.Clear();

			if (_locations == null)
			{
				return;
			}

			for (int i = 0; i < _locations.Length; i++)
			{
				WorldStreamedLocation location = _locations[i];
				if (!IsLocationConfigured(location))
				{
					continue;
				}

				string sceneName = location.SceneName;
				if (_statesBySceneName.ContainsKey(sceneName))
				{
					TLNLogger.LogWarning($"Duplicate streamed scene entry ignored: {sceneName}", this);
					continue;
				}

				_statesBySceneName.Add(
					sceneName,
					new StreamedSceneState(IsSceneLoaded(sceneName) ? StreamedSceneStatus.Loaded : StreamedSceneStatus.Unloaded)
				);
			}
		}

		private void EvaluateLocations(Vector3 playerPosition)
		{
			if (_locations == null)
			{
				return;
			}

			for (int i = 0; i < _locations.Length; i++)
			{
				WorldStreamedLocation location = _locations[i];
				if (!IsLocationConfigured(location))
				{
					continue;
				}

				EvaluateLocation(location, playerPosition);
			}
		}

		private void EvaluateLocation(WorldStreamedLocation location, Vector3 playerPosition)
		{
			StreamedSceneState state = GetOrCreateState(location.SceneName);

			if (state.Status == StreamedSceneStatus.Loading ||
			    state.Status == StreamedSceneStatus.Unloading)
			{
				return;
			}

			bool isLoadedNow = IsSceneLoaded(location.SceneName);
			if (isLoadedNow && state.Status != StreamedSceneStatus.Loaded)
			{
				state.Status = StreamedSceneStatus.Loaded;
			}
			else if (!isLoadedNow && state.Status != StreamedSceneStatus.Unloaded)
			{
				state.Status = StreamedSceneStatus.Unloaded;
			}

			float sqrDistance = (playerPosition - location.Center).sqrMagnitude;
			float loadRadiusSqr = location.LoadRadius * location.LoadRadius;
			float unloadRadiusSqr = location.UnloadRadius * location.UnloadRadius;

			if (state.Status == StreamedSceneStatus.Unloaded)
			{
				if (location.KeepLoaded || sqrDistance <= loadRadiusSqr)
				{
					BeginLoad(location, state);
				}

				return;
			}

			if (state.Status != StreamedSceneStatus.Loaded)
			{
				return;
			}

			if (!location.KeepLoaded && sqrDistance >= unloadRadiusSqr)
			{
				BeginUnload(location, state);
			}
		}

		private void BeginLoad(WorldStreamedLocation location, StreamedSceneState state)
		{
			string sceneName = location.SceneName;

			if (!IsSceneInBuildSettings(sceneName))
			{
				TLNLogger.LogError($"Cannot stream scene '{sceneName}' because it is not enabled in Build Settings.", this);
				return;
			}

			try
			{
				AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
				if (operation == null)
				{
					TLNLogger.LogError($"Unity did not start loading streamed scene '{sceneName}'.", this);
					return;
				}

				state.Status = StreamedSceneStatus.Loading;
				state.Operation = operation;
				operation.completed += _ => CompleteLoad(sceneName);

				LogTransition($"Loading streamed scene '{sceneName}'.");
			}
			catch (Exception exception)
			{
				state.Status = StreamedSceneStatus.Unloaded;
				state.Operation = null;
				TLNLogger.LogError($"Failed to start streaming scene '{sceneName}': {exception}", this);
			}
		}

		private void CompleteLoad(string sceneName)
		{
			StreamedSceneState state = GetOrCreateState(sceneName);
			state.Operation = null;
			state.Status = IsSceneLoaded(sceneName) ? StreamedSceneStatus.Loaded : StreamedSceneStatus.Unloaded;

			LogTransition($"Loaded streamed scene '{sceneName}'.");
		}

		private void BeginUnload(WorldStreamedLocation location, StreamedSceneState state)
		{
			string sceneName = location.SceneName;

			if (!TryGetLoadedScene(sceneName, out Scene scene))
			{
				state.Status = StreamedSceneStatus.Unloaded;
				state.Operation = null;
				return;
			}

			AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
			if (operation == null)
			{
				TLNLogger.LogError($"Unity did not start unloading streamed scene '{sceneName}'.", this);
				return;
			}

			state.Status = StreamedSceneStatus.Unloading;
			state.Operation = operation;
			operation.completed += _ => CompleteUnload(sceneName);

			LogTransition($"Unloading streamed scene '{sceneName}'.");
		}

		private void CompleteUnload(string sceneName)
		{
			StreamedSceneState state = GetOrCreateState(sceneName);
			state.Operation = null;
			state.Status = StreamedSceneStatus.Unloaded;

			LogTransition($"Unloaded streamed scene '{sceneName}'.");
		}

		private bool TryGetPlayerPosition(out Vector3 position)
		{
			if (_playerOverride != null)
			{
				_player = _playerOverride;
			}

			if (_player == null || !_player.gameObject.activeInHierarchy)
			{
				_player = ResolvePlayerTransform();
			}

			if (_player == null)
			{
				position = default;
				return false;
			}

			position = _player.position;
			return true;
		}

		private static Transform ResolvePlayerTransform()
		{
			PlayerRoot[] players = UnityEngine.Object.FindObjectsByType<PlayerRoot>(FindObjectsInactive.Exclude);

			if (players.Length == 1)
			{
				return players[0].transform;
			}

			for (int i = 0; i < players.Length; i++)
			{
				PlayerRoot player = players[i];
				if (player != null && player.Camera != null && player.Camera.enabled)
				{
					return player.transform;
				}
			}

			Camera mainCamera = Camera.main;
			if (mainCamera == null)
			{
				return null;
			}

			PlayerRoot cameraPlayer = mainCamera.GetComponentInParent<PlayerRoot>();
			return cameraPlayer != null ? cameraPlayer.transform : mainCamera.transform;
		}

		private StreamedSceneState GetOrCreateState(string sceneName)
		{
			if (_statesBySceneName.TryGetValue(sceneName, out StreamedSceneState state))
			{
				return state;
			}

			state = new StreamedSceneState(IsSceneLoaded(sceneName) ? StreamedSceneStatus.Loaded : StreamedSceneStatus.Unloaded);
			_statesBySceneName.Add(sceneName, state);
			return state;
		}

		private static bool IsLocationConfigured(WorldStreamedLocation location)
		{
			return location != null && !string.IsNullOrWhiteSpace(location.SceneName);
		}

		private static bool IsSceneLoaded(string sceneName)
		{
			return TryGetLoadedScene(sceneName, out _);
		}

		private static bool TryGetLoadedScene(string sceneName, out Scene loadedScene)
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
				{
					continue;
				}

				if (string.Equals(scene.name, sceneName, StringComparison.Ordinal) ||
				    string.Equals(scene.path, sceneName, StringComparison.OrdinalIgnoreCase))
				{
					loadedScene = scene;
					return true;
				}
			}

			loadedScene = default;
			return false;
		}

		private static bool IsSceneInBuildSettings(string sceneName)
		{
			for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
			{
				string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
				if (string.IsNullOrWhiteSpace(scenePath))
				{
					continue;
				}

				if (string.Equals(scenePath, sceneName, StringComparison.OrdinalIgnoreCase) ||
				    string.Equals(Path.GetFileNameWithoutExtension(scenePath), sceneName, StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		private void LogTransition(string message)
		{
			if (_logTransitions)
			{
				TLNLogger.Log($"[WorldStreaming] {message}", this);
			}
		}

		private enum StreamedSceneStatus
		{
			Unloaded,
			Loading,
			Loaded,
			Unloading
		}

		private sealed class StreamedSceneState
		{
			public StreamedSceneStatus Status { get; set; }
			public AsyncOperation Operation { get; set; }

			public StreamedSceneState(StreamedSceneStatus status)
			{
				Status = status;
			}
		}
	}

	[Serializable]
	public sealed class WorldStreamedLocation
	{
		[SerializeField] private string _sceneName;
		[SerializeField] private Vector3 _center;
		[SerializeField, Min(1f)] private float _loadRadius = 90f;
		[SerializeField, Min(1f)] private float _unloadRadius = 130f;
		[SerializeField] private bool _keepLoaded;

		public string SceneName => string.IsNullOrWhiteSpace(_sceneName) ? string.Empty : _sceneName.Trim();
		public Vector3 Center => _center;
		public float LoadRadius => Mathf.Max(1f, _loadRadius);
		public float UnloadRadius => Mathf.Max(LoadRadius + 1f, _unloadRadius);
		public bool KeepLoaded => _keepLoaded;

		internal void Normalize()
		{
			if (_loadRadius < 1f)
			{
				_loadRadius = 1f;
			}

			if (_unloadRadius < _loadRadius + 1f)
			{
				_unloadRadius = _loadRadius + 1f;
			}
		}
	}
}
