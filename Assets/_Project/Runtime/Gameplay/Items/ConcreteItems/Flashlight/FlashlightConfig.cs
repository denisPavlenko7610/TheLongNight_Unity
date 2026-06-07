using UnityEngine;

namespace _Project.Runtime.Gameplay.Items.ConcreteItems.Flashlight
{
	[CreateAssetMenu(fileName = "FlashlightConfig", menuName = "TLN/Gameplay/Flashlight Config")]
	public sealed class FlashlightConfig : ScriptableObject
	{
		[Header("Battery Settings")]
		[SerializeField] private float _maxBattery = 100f;
		[SerializeField] private float _drainPerSecond = 2f;

		public float MaxBattery => _maxBattery;
		public float DrainPerSecond => _drainPerSecond;
	}
}
