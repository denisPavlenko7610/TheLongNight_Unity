using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace TLN.Gameplay.Building
{
	[CreateAssetMenu(fileName = "BuildRecipeDefinition", menuName = "TLN/Building/Build Recipe")]
	public sealed class BuildRecipeDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private string _id;
		[SerializeField] private LocalizedString _displayName;
		[SerializeField] private LocalizedString _description;

		[Header("Placement")]
		[SerializeField] private GameObject _placedPrefab;
		[SerializeField] private float _placeDistance = 2f;

		[Header("Requirements")]
		[SerializeField] private BuildRecipeIngredient[] _ingredients;

		public string Id => _id;
		public string DisplayName => GetLocalizedStringOrFallback(_displayName, Id);
		public string Description => GetLocalizedStringOrFallback(_description, string.Empty);
		public GameObject PlacedPrefab => _placedPrefab;
		public float PlaceDistance => Mathf.Max(0f, _placeDistance);
		public IReadOnlyList<BuildRecipeIngredient> Ingredients => _ingredients ?? System.Array.Empty<BuildRecipeIngredient>();

		private static string GetLocalizedStringOrFallback(LocalizedString localizedString, string fallback)
		{
			if (localizedString == null || localizedString.IsEmpty)
			{
				return fallback;
			}

			string value = localizedString.GetLocalizedString();
			return string.IsNullOrWhiteSpace(value) ? fallback : value;
		}
	}
}
