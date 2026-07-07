using System.Collections.Generic;
using UnityEngine;

namespace TLN.Gameplay.Building
{
	[CreateAssetMenu(fileName = "BuildRecipeCatalog", menuName = "TLN/Building/Build Recipe Catalog")]
	public sealed class BuildRecipeCatalog : ScriptableObject
	{
		[SerializeField] private BuildRecipeDefinition[] _recipes;

		public IReadOnlyList<BuildRecipeDefinition> Recipes => _recipes ?? System.Array.Empty<BuildRecipeDefinition>();
	}
}
