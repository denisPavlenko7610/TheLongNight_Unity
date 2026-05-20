using UnityEngine;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "PlaceableItemDefinition", menuName = "TLN/Items/Placeable Item Definition")]
	public sealed class PlaceableItemDefinition : ItemDefinition
	{
		[Header("Placement")]
		[SerializeField] private GameObject _placedPrefab;
		[SerializeField] private float _placeDistance = 1.6f;

		public GameObject PlacedPrefab => _placedPrefab;
		public float PlaceDistance => _placeDistance;
	}
}
