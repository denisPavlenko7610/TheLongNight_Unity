using System;
using System.Collections.Generic;
using TLN.Application.Input;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Gameplay.Building;
using TLN.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TLN.UI.Building
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class BuildWindowView : MonoBehaviour, IBuildWindow
	{
		private const string VisibleClassName = "build-window-root-visible";

		private readonly List<BuildRecipeButtonView> _recipeButtonViews = new();

		private VisualElement _root;
		private ScrollView _recipesScrollView;
		private Button _closeButton;

		private BuildRecipeCatalog _recipeCatalog;
		private IBuildService _buildService;
		private IInputModeService _inputModeService;
		private INotificationService _notificationService;

		private IDisposable _inputModeScope;
		private bool _isVisible;

		[Inject]
		public void Construct(
			BuildRecipeCatalog recipeCatalog,
			IBuildService buildService,
			IInputModeService inputModeService,
			INotificationService notificationService
		)
		{
			_recipeCatalog = recipeCatalog;
			_buildService = buildService;
			_inputModeService = inputModeService;
			_notificationService = notificationService;

			Hide();
		}

		private void Awake()
		{
			UIDocument document = GetComponent<UIDocument>();
			VisualElement documentRoot = document.rootVisualElement;

			_root = documentRoot.RequiredQ<VisualElement>("build-window-root");
			_recipesScrollView = documentRoot.RequiredQ<ScrollView>("build-recipes-scroll-view");
			_closeButton = documentRoot.RequiredQ<Button>("build-close-button");

			_root.RemoveFromClassList(VisibleClassName);
			_root.SetVisible(false);

			_closeButton.clicked += Hide;
		}

		private void OnDestroy()
		{
			if (_closeButton != null)
			{
				_closeButton.clicked -= Hide;
			}

			ClearRecipes();
			ReleaseInputMode();
		}

		public void Toggle()
		{
			if (_isVisible)
			{
				Hide();
				return;
			}

			Show();
		}

		public void Hide()
		{
			_isVisible = false;

			_root?.RemoveFromClassList(VisibleClassName);
			_root?.SetVisible(false);

			ReleaseInputMode();
		}

		private void Show()
		{
			_isVisible = true;
			RefreshRecipes();

			_root?.SetVisible(true);
			_root?.AddToClassList(VisibleClassName);

			AcquireInputMode();
		}

		private void RefreshRecipes()
		{
			ClearRecipes();

			if (_recipeCatalog == null)
			{
				_notificationService?.Show(Loc.BuildRecipesMissing);
				return;
			}

			foreach (BuildRecipeDefinition recipe in _recipeCatalog.Recipes)
			{
				if (recipe == null)
				{
					continue;
				}

				CreateRecipeButton(recipe);
			}
		}

		private void CreateRecipeButton(BuildRecipeDefinition recipe)
		{
			BuildRecipeButtonView buttonView = new BuildRecipeButtonView(recipe, OnBuildClicked);

			_recipeButtonViews.Add(buttonView);
			_recipesScrollView.Add(buttonView.Root);
		}

		private void OnBuildClicked(BuildRecipeDefinition recipe)
		{
			if (_buildService == null)
			{
				_notificationService?.Show(Loc.BuildServiceMissing);
				return;
			}

			BuildResult result = _buildService.Build(recipe);

			if (result.IsSuccess)
			{
				Hide();
			}
		}

		private void ClearRecipes()
		{
			foreach (BuildRecipeButtonView buttonView in _recipeButtonViews)
			{
				buttonView.Dispose();
			}

			_recipeButtonViews.Clear();

			if (_recipesScrollView != null)
			{
				_recipesScrollView.Clear();
			}
		}

		private void AcquireInputMode()
		{
			_inputModeScope ??= _inputModeService?.AcquireUIMode();
		}

		private void ReleaseInputMode()
		{
			_inputModeScope?.Dispose();
			_inputModeScope = null;
		}
	}
}
