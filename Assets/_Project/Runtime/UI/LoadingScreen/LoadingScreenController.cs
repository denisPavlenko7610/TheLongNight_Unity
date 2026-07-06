using System;
using TLN.Application.GameStates;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace TLN.UI.LoadingScreen
{
	public sealed class LoadingScreenController : IInitializable, IDisposable
	{
		private const string OverlayName = "loading-screen-overlay";

		private readonly IGameStateMachine _gameStateMachine;
		private readonly GameObject _loadingScreenPrefab;

		private GameObject _documentGameObject;
		private UIDocument _document;
		private VisualElement _overlay;

		public LoadingScreenController(IGameStateMachine gameStateMachine, GameObject loadingScreenPrefab)
		{
			_gameStateMachine = gameStateMachine;
			_loadingScreenPrefab = loadingScreenPrefab;
		}

		public void Initialize()
		{
			if (_loadingScreenPrefab == null)
			{
				Debug.LogError("Loading screen prefab is not assigned in ProjectLifetimeScope.");
				return;
			}

			_documentGameObject = UnityEngine.Object.Instantiate(_loadingScreenPrefab);
			_documentGameObject.name = _loadingScreenPrefab.name;
			UnityEngine.Object.DontDestroyOnLoad(_documentGameObject);

			_gameStateMachine.StateChanged += OnGameStateChanged;
			ApplyState(_gameStateMachine.CurrentState);
		}

		public void Dispose()
		{
			_gameStateMachine.StateChanged -= OnGameStateChanged;

			if (_documentGameObject != null)
			{
				UnityEngine.Object.Destroy(_documentGameObject);
				_documentGameObject = null;
				_document = null;
				_overlay = null;
			}
		}

		private void OnGameStateChanged(GameStateId _, GameStateId nextState)
		{
			ApplyState(nextState);
		}

		private void ApplyState(GameStateId state)
		{
			if (_documentGameObject == null)
			{
				return;
			}

			bool isLoading = state == GameStateId.Loading;

			if (!isLoading)
			{
				if (_overlay != null)
				{
					_overlay.style.display = DisplayStyle.None;
				}

				if (_documentGameObject.activeSelf)
				{
					_documentGameObject.SetActive(false);
					_document = null;
					_overlay = null;
				}

				return;
			}

			if (!_documentGameObject.activeSelf)
			{
				_documentGameObject.SetActive(true);
			}

			if (!TryResolveOverlay())
			{
				Debug.LogError("Loading screen prefab must contain a UIDocument with loading-screen-overlay.");
				return;
			}

			_overlay.style.display = DisplayStyle.Flex;
		}

		private bool TryResolveOverlay()
		{
			if (_overlay != null)
			{
				return true;
			}

			if (_document == null && !_documentGameObject.TryGetComponent(out _document))
			{
				return false;
			}

			VisualElement root = _document.rootVisualElement;
			if (root == null)
			{
				return false;
			}

			_overlay = root.Q<VisualElement>(OverlayName);
			return _overlay != null;
		}
	}
}
