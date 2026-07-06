using TLN.Core.Logging;
using TLN.Core.Validation;
using TLN.Infrastructure.Audio;
using TLN.UI.MainMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class MainMenuLifetimeScope : LifetimeScope
{
	[SerializeField, Required] private MainMenuView _mainMenuView;
	[SerializeField, Required] private AudioPlayer _musicPlayer;

	protected override void Configure(IContainerBuilder builder)
	{
		RegisterSceneComponentIfPresent(builder, _mainMenuView, nameof(_mainMenuView));
		RegisterSceneComponentIfPresent(builder, _musicPlayer, nameof(_musicPlayer));
	}

	private static void RegisterSceneComponentIfPresent<T>(
		IContainerBuilder builder,
		T component,
		string componentName
	) where T : Component
	{
		if (component == null)
		{
			TLNLogger.LogWarning($"{componentName} is missing in MainMenuLifetimeScope.");
			return;
		}

		builder.RegisterComponent(component);
	}
}
