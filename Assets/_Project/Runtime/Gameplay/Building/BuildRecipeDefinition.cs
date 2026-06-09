using System.Collections.Generic;
using UnityEngine;

namespace TLN.Gameplay.Building
{
	[CreateAssetMenu(fileName = "BuildRecipeDefinition", menuName = "TLN/Building/Build Recipe")]
	public sealed class BuildRecipeDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private string _id;
		[SerializeField] private string _displayName;

		[Header("Placement")]
		[SerializeField] private GameObject _placedPrefab;
		[SerializeField] private float _placeDistance = 2f;

		[Header("Requirements")]
		[SerializeField] private BuildRecipeIngredient[] _ingredients;

		public string Id => _id;
		public string DisplayName => _displayName;
		public GameObject PlacedPrefab => _placedPrefab;
		public float PlaceDistance => _placeDistance;
		public IReadOnlyList<BuildRecipeIngredient> Ingredients => _ingredients;
	}
}
