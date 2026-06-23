using System;
using UnityEngine;

namespace TLN.Gameplay.World
{
	[Serializable]
	public sealed class RandomWorldSpawnEntry
	{
		[SerializeField] private GameObject _prefab;
		[SerializeField] private int _minCount;
		[SerializeField] private int _maxCount = 1;
		[SerializeField] private float _spawnRadius = 25f;
		[SerializeField] private float _minDistanceBetweenInstances = 3f;

		[Header("Ground")]
		[SerializeField] private bool _alignToGroundNormal = true;
		[SerializeField] private float _spawnYOffset;

		[Header("Navigation")]
		[SerializeField] private bool _requireNavMesh;
		[SerializeField] private float _navMeshSearchRadius = 3f;

		[Header("Debug")]
		[SerializeField] private Color _debugColor = Color.green;

		public GameObject Prefab => _prefab;
		public int MinCount => Mathf.Max(0, _minCount);
		public int MaxCount => Mathf.Max(MinCount, _maxCount);
		public float SpawnRadius => Mathf.Max(0.1f, _spawnRadius);
		public float MinDistanceBetweenInstances => Mathf.Max(0f, _minDistanceBetweenInstances);

		public bool AlignToGroundNormal => _alignToGroundNormal;
		public float SpawnYOffset => _spawnYOffset;

		public bool RequireNavMesh => _requireNavMesh;
		public float NavMeshSearchRadius => Mathf.Max(0.1f, _navMeshSearchRadius);

		public Color DebugColor => _debugColor;
	}
}
