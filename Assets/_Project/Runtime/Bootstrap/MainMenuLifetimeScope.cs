using TLN.Bootstrap.MainMenu;
using VContainer;
using VContainer.Unity;

public sealed class MainMenuLifetimeScope : LifetimeScope
{
	protected override void Configure(IContainerBuilder builder)
	{
		builder.RegisterComponentInHierarchy<MainMenuEntryPoint>();
	}
}