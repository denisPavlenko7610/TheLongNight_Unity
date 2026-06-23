using System.Collections.Generic;
using TLN.Application.GameStates;
using TLN.Application.Saves;
using TLN.Core.Logging;
using TLN.Core.Validation;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace TLN.Gameplay.World
{
    public sealed class RandomWorldSpawner : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] [Required] private RandomWorldSpawnEntry[] _entries;
        [SerializeField] private bool _spawnOnStart;
        [SerializeField] private bool _spawnOnLoadedGame;

        [Header("Ground")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _rayStartHeight = 20f;
        [SerializeField] private float _rayDistance = 60f;

        [Header("Random")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed = 12345;

        [Header("Safety")]
        [SerializeField] private int _maxAttemptsPerInstance = 30;


        private readonly List<Vector3> _spawnedPositions = new();

        private IWorldObjectFactory _worldObjectFactory;
        private IGameStateMachine _gameStateMachine;
        private SaveSessionService _saveSessionService;
        private bool _hasSpawned;

        [Inject]
        public void Construct(
            IWorldObjectFactory worldObjectFactory,
            IGameStateMachine gameStateMachine,
            SaveSessionService saveSessionService
            )
        {
            _worldObjectFactory = worldObjectFactory;
            _gameStateMachine = gameStateMachine;
            _saveSessionService = saveSessionService;
        }

        private void Start()
        {
            if (_spawnOnStart)
            {
                TrySpawn();
            }
        }

        public void TrySpawn()
        {
            if (_hasSpawned)
            {
                return;
            }

            if (_gameStateMachine != null &&
                _gameStateMachine.CurrentState == GameStateId.Loading)
            {
                return;
            }

            if (_worldObjectFactory == null)
            {
                TLNLogger.LogWarning(
                    "RandomWorldSpawner cannot spawn because IWorldObjectFactory is missing.",
                    this);

                return;
            }

            if (_saveSessionService != null && _saveSessionService.ShouldLoadActiveSlot)
            {
                return;
            }

            _hasSpawned = true;

            Random.State previousState = Random.state;

            if (_useFixedSeed)
            {
                Random.InitState(_seed);
            }

            SpawnEntries();

            Random.state = previousState;
        }

        public void TrySpawnForWorldStart(bool wasSaveLoaded)
        {
            if (wasSaveLoaded && !_spawnOnLoadedGame)
            {
                return;
            }

            TrySpawn();
        }

        private void SpawnEntries()
        {
            if (_entries == null)
            {
                return;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                SpawnEntry(_entries[i]);
            }
        }

        private void SpawnEntry(RandomWorldSpawnEntry entry)
        {
            if (entry == null || entry.Prefab == null)
            {
                return;
            }

            int count = Random.Range(
                entry.MinCount,
                entry.MaxCount + 1);

            for (int i = 0; i < count; i++)
            {
                if (!TryFindSpawnPose(
                        entry,
                        out Vector3 position,
                        out Quaternion rotation))
                {
                    TLNLogger.LogWarning(
                        $"RandomWorldSpawner failed to find spawn position for prefab: {entry.Prefab.name}",
                        this);

                    continue;
                }

                GameObject instance =
                    _worldObjectFactory.Create(
                        entry.Prefab,
                        position,
                        rotation);

                if (instance == null)
                {
                    continue;
                }

                _spawnedPositions.Add(position);
            }
        }

        private bool TryFindSpawnPose(
            RandomWorldSpawnEntry entry,
            out Vector3 position,
            out Quaternion rotation)
        {
            for (int attempt = 0; attempt < _maxAttemptsPerInstance; attempt++)
            {
                Vector2 randomCircle =
                    Random.insideUnitCircle * entry.SpawnRadius;

                Vector3 candidate =
                    transform.position +
                    new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (!TryProjectToGround(
                        candidate,
                        out Vector3 groundPosition,
                        out Vector3 groundNormal))
                {
                    continue;
                }

                if (entry.RequireNavMesh &&
                    !TryProjectToNavMesh(
                        groundPosition,
                        entry.NavMeshSearchRadius,
                        out groundPosition))
                {
                    continue;
                }

                if (!IsFarEnoughFromSpawnedPositions(
                        groundPosition,
                        entry.MinDistanceBetweenInstances))
                {
                    continue;
                }

                position = groundPosition + Vector3.up * entry.SpawnYOffset;
                rotation = CreateRotation(entry, groundNormal);

                return true;
            }

            position = default;
            rotation = default;
            return false;
        }

        private bool TryProjectToGround(
            Vector3 candidate,
            out Vector3 position,
            out Vector3 normal)
        {
            Vector3 rayOrigin =
                candidate + Vector3.up * _rayStartHeight;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    _rayDistance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                normal = hit.normal;
                return true;
            }

            position = default;
            normal = Vector3.up;
            return false;
        }

        private static bool TryProjectToNavMesh(Vector3 candidate, float searchRadius, out Vector3 position)
        {
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            position = default;
            return false;
        }

        private bool IsFarEnoughFromSpawnedPositions(
            Vector3 position,
            float minDistance)
        {
            if (minDistance <= 0f)
            {
                return true;
            }

            float minSqrDistance = minDistance * minDistance;

            for (int i = 0; i < _spawnedPositions.Count; i++)
            {
                if ((position - _spawnedPositions[i]).sqrMagnitude < minSqrDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static Quaternion CreateRotation(
            RandomWorldSpawnEntry entry,
            Vector3 groundNormal)
        {
            Quaternion yaw =
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (!entry.AlignToGroundNormal)
            {
                return yaw;
            }

            Quaternion groundAlignment =
                Quaternion.FromToRotation(Vector3.up, groundNormal);

            return groundAlignment * yaw;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_entries == null)
            {
                return;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                RandomWorldSpawnEntry entry = _entries[i];

                if (entry == null)
                {
                    continue;
                }

                Gizmos.color = entry.DebugColor;

                Gizmos.DrawWireSphere(transform.position, entry.SpawnRadius);
            }
        }
#endif
    }
}
