using UnityEngine;

namespace TLN.Gameplay.Wildlife
{
	[CreateAssetMenu(
		fileName = "AnimalDefinition",
		menuName = "TLN/Wildlife/Animal Definition")]
	public sealed class AnimalDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private AnimalSpeciesId _species = AnimalSpeciesId.Rabbit;

		[Header("Movement")]
		[SerializeField] private float _walkSpeed = 1.5f;
		[SerializeField] private float _runSpeed = 4.5f;
		[SerializeField] private float _wanderRadius = 8f;
		[SerializeField] private float _decisionInterval = 1f;

		[Header("Senses")]
		[SerializeField] private float _detectionRadius = 10f;
		[SerializeField] private float _fleeRadius = 8f;

		[Header("Predator")]
		[SerializeField] private float _chargeRadius = 6f;

		[Header("Combat")]
		[SerializeField] private float _attackDistance = 1.5f;
		[SerializeField] private float _attackCooldownSeconds = 2f;
		[SerializeField] private float _conditionDamage = 15f;

		[Header("Health")]
		[SerializeField] private float _maxHealth = 100f;

		public AnimalSpeciesId Species => _species;

		public float WalkSpeed => _walkSpeed;
		public float RunSpeed => _runSpeed;
		public float WanderRadius => _wanderRadius;
		public float DecisionInterval => _decisionInterval;

		public float DetectionRadius => _detectionRadius;
		public float FleeRadius => _fleeRadius;

		public float ChargeRadius => Mathf.Max(_attackDistance, _chargeRadius);

		public float AttackDistance => _attackDistance;
		public float AttackCooldownSeconds => _attackCooldownSeconds;
		public float ConditionDamage => _conditionDamage;
		public float MaxHealth => _maxHealth;
	}
}
