namespace TLN.Gameplay.Building
{
	public interface IBuildService
	{
		bool CanBuild(BuildRecipeDefinition recipe, out string failureReason);
		BuildResult Build(BuildRecipeDefinition recipe);
	}
}
