using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TLN.Gameplay.Items
{
	[CreateAssetMenu(fileName = "PlaceableItemDefinition", menuName = "TLN/Items/Placeable Item Definition")]
	public sealed class PlaceableItemDefinition : ItemDefinition
	{
		[Header("Placement")]
		[SerializeField] private AssetReferenceGameObject _placedPrefabReference;
		[SerializeField] private float _placeDistance = 1.6f;

		public AssetReferenceGameObject PlacedPrefabReference => _placedPrefabReference;
		public float PlaceDistance => Mathf.Max(0f, _placeDistance);
	}
}
