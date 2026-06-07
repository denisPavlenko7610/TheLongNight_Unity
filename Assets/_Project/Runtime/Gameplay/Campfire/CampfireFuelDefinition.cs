using UnityEngine;

namespace TLN.Gameplay.Campfire
{
	[CreateAssetMenu(fileName = "CampfireFuelDefinition", menuName = "TLN/Campfire/Fuel Definition")]
	public sealed class CampfireFuelDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private string _id;
		[SerializeField] private string _displayName;

		[Header("Burning")]
		[SerializeField] private float _burnSeconds = 1800f; //30m

		[Header("Inventory Later")]
		[SerializeField] private float _weight = 0.5f;

		public string Id => _id;
		public string DisplayName => _displayName;
		public float BurnSeconds => _burnSeconds;
		public float Weight => _weight;
	}
}
