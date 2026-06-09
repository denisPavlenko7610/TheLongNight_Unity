using UnityEngine;
using TLN.Gameplay.Items;

namespace TLN.Gameplay.Campfire
{
	[CreateAssetMenu(
		fileName = "FuelItemDefinition",
		menuName = "TLN/Items/Fuel Item Definition")]
	public sealed class FuelItemDefinition : ItemDefinition
	{
		[Header("Fuel")]
		[SerializeField] private int _burnMinutes = 10;

		public int BurnMinutes => Mathf.Max(0, _burnMinutes);
	}
}
