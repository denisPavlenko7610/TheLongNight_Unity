using System;
using Assign;
using TLN.Application.GameStates;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Core.Logging;
using TLN.Core.Validation;
using TLN.Gameplay.Player;
using TLN.Gameplay.Survival;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace TLN.Gameplay.Wildlife
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class AnimalActor : MonoBehaviour
    {
        private const float RemainingDistanceThreshold = 0.5f;
        private const float DirectionEpsilon = 0.001f;
        private const float MovingVelocityThreshold = 0.02f;
        private const float AnimationRefreshInterval = 0.1f;
        private const float FleeRepathInterval = 0.3f;
        private const float AttackAnimationLockSeconds = 0.95f;
        private const string FurNameFragment = "fur";
        private const string HairNameFragment = "hair";
        private const int MaxNavMeshAttempts = 12;

        [Header("Definition")]
        [SerializeField][Required] private AnimalDefinition _definition;

        [Header("Behavior")]
        [SerializeField] private bool _useInternalDecisionLogic = true;

        [Header("Animation")]
        [SerializeField][Assign] private AnimalAnimationController _animationController;

        [Header("Debug")]
        [SerializeField] private bool _drawDebug;

        [Header("Navigation")]
        [SerializeField][Assign] private NavMeshAgent _agent;

		private IGameStateMachine _gameStateMachine;
		private ISurvivalService _survivalService;
		private INotificationService _notificationService;
		private WildlifeTargetService _targetService;

		private Vector3 _homePosition;
        private AnimalStateId _state = AnimalStateId.None;
        private float _nextDecisionTime;
        private float _nextAttackTime;
        private float _nextFleeTime;
        private float _nextAnimationTime;
        private float _attackAnimationLockUntil;

        private bool _isDead;
		private Transform _playerTransform;
		private float _sqrFleeRadius;
		private float _sqrDetectionRadius;
		private float _sqrAttackDistance;

        public AnimalDefinition Definition => _definition;
        public AnimalStateId State => _state;
        public bool IsDead => _isDead;

        public bool HasPlayerTarget => GetPlayer() != null;

		[Inject]
		public void Construct(
			IGameStateMachine gameStateMachine,
			ISurvivalService survivalService,
			INotificationService notificationService,
			WildlifeTargetService targetService)
		{
			_gameStateMachine = gameStateMachine;
			_survivalService = survivalService;
			_notificationService = notificationService;
			_targetService = targetService;
		}

        private void Awake()
        {
            _homePosition = transform.position;

            if (_definition != null)
            {
                _sqrFleeRadius = _definition.FleeRadius * _definition.FleeRadius;
                _sqrDetectionRadius = _definition.DetectionRadius * _definition.DetectionRadius;
                _sqrAttackDistance = _definition.AttackDistance * _definition.AttackDistance;

                if (_definition.Species == AnimalSpeciesId.Wolf)
                {
                    DisableWolfFurRenderers();
                }
            }

            ApplyDefinitionToAgent();
            ApplyState(AnimalStateId.Idle);
        }

        private void Update()
        {
            float currentTime = UnityEngine.Time.time;

            if (_isDead)
            {
                StopMovement();
                RefreshAnimation();
                return;
            }

            if (_gameStateMachine != null && _gameStateMachine.CurrentState != GameStateId.Playing)
            {
                StopMovement();
                return;
            }

            if (_definition == null)
            {
                StopMovement();
                return;
            }

            if (!_useInternalDecisionLogic)
            {
                RefreshAnimationIfNeeded(currentTime);
                return;
            }

            PlayerRoot player = GetPlayer();
            _playerTransform = player != null ? player.transform : null;

            if (IsAttackAnimationLocked(currentTime))
            {
                HoldAttackPose();
                RefreshAnimationIfNeeded(currentTime);
                return;
            }

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.isStopped)
            {
                _agent.isStopped = false;
            }

            switch (_definition.Species)
            {
                case AnimalSpeciesId.Rabbit:
                    TickRabbit(player);
                    break;

                case AnimalSpeciesId.Wolf:
                    TickWolf(player);
                    break;
            }

            RefreshAnimationIfNeeded(currentTime);
        }

        private PlayerRoot GetPlayer()
        {
			return _targetService?.PlayerRoot;
        }

        private void TickRabbit(PlayerRoot player)
        {
            if (player != null && _playerTransform != null)
            {
                float sqrDist = (transform.position - _playerTransform.position).sqrMagnitude;

                if (sqrDist <= _sqrFleeRadius)
                {
                    FleeFrom(_playerTransform.position);
                    return;
                }
            }

            Wander();
        }

        private void TickWolf(PlayerRoot player)
        {
            if (player == null || _playerTransform == null)
            {
                Wander();
                return;
            }

            float sqrDist = (transform.position - _playerTransform.position).sqrMagnitude;

            if (sqrDist <= _sqrAttackDistance)
            {
                AttackPlayer();
                return;
            }

            if (sqrDist <= _sqrDetectionRadius)
            {
                Chase(_playerTransform.position);
                return;
            }

            Wander();
        }

        private void Wander()
        {
            float currentTime = UnityEngine.Time.time;

            if (IsAttackAnimationLocked(currentTime))
            {
                HoldAttackPose();
                return;
            }

            if (currentTime < _nextDecisionTime)
            {
                return;
            }

            _nextDecisionTime =
                currentTime + _definition.DecisionInterval;

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                return;
            }

            if (_agent.hasPath &&
                _agent.remainingDistance > RemainingDistanceThreshold)
            {
                return;
            }

            Vector3 targetPosition =
                FindRandomNavMeshPoint(
                    _homePosition,
                    _definition.WanderRadius);

            ApplyState(AnimalStateId.Wander);

            _agent.speed = _definition.WalkSpeed;
            _agent.SetDestination(targetPosition);
        }

        private void FleeFrom(Vector3 threatPosition)
        {
            float currentTime = UnityEngine.Time.time;

            if (IsAttackAnimationLocked(currentTime))
            {
                HoldAttackPose();
                return;
            }

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                return;
            }

            ApplyState(AnimalStateId.Flee);
            _agent.speed = _definition.RunSpeed;

            if (currentTime < _nextFleeTime &&
                _agent.hasPath &&
                _agent.remainingDistance > RemainingDistanceThreshold)
            {
                return;
            }

            _nextFleeTime = currentTime + FleeRepathInterval;

            Vector3 direction =
                transform.position - threatPosition;

            direction.y = 0f;

            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                direction = -transform.forward;
            }

            Vector3 targetPosition =
                transform.position + direction.normalized * _definition.FleeRadius;

            targetPosition =
                FindNearestNavMeshPoint(targetPosition, _definition.FleeRadius);

            _agent.SetDestination(targetPosition);
        }

        private void Chase(Vector3 targetPosition)
        {
            float currentTime = UnityEngine.Time.time;

            if (IsAttackAnimationLocked(currentTime))
            {
                HoldAttackPose();
                return;
            }

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                return;
            }

            ApplyState(AnimalStateId.Chase);

            _agent.speed = _definition.RunSpeed;
            _agent.SetDestination(targetPosition);
        }

        public void StopMovement()
        {
            if (_agent == null || !_agent.enabled)
            {
                ApplyState(AnimalStateId.Idle);
                return;
            }

            if (_agent.isOnNavMesh)
            {
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                _agent.isStopped = true;
            }

            ApplyState(AnimalStateId.Idle);
        }

        public void AttackPlayer()
        {
            if (_isDead || _definition == null)
            {
                return;
            }

            float currentTime = UnityEngine.Time.time;

            ApplyState(AnimalStateId.Attack);
            StopAgent();
            FacePlayer();
            RefreshAnimation();

            if (currentTime < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = currentTime + _definition.AttackCooldownSeconds;
            _attackAnimationLockUntil = currentTime + AttackAnimationLockSeconds;

            _animationController?.PlayAttack();

			ISurvivalService targetSurvivalService = GetTargetSurvivalService();

            if (targetSurvivalService == null)
            {
                TLNLogger.LogWarning(
                    "Wolf cannot damage player because SurvivalService is missing.",
                    this);

                return;
            }

            targetSurvivalService.DamageCondition(_definition.ConditionDamage);

            _notificationService?.Show(Loc.WolfAttack(_definition.ConditionDamage));
        }

		private ISurvivalService GetTargetSurvivalService()
		{
			PlayerRoot player = GetPlayer();

			if (player != null &&
			    player.TryGetComponent(out NetworkPlayerSurvival networkSurvival))
			{
				return networkSurvival;
			}

			return _survivalService;
		}

        private void ApplyState(AnimalStateId state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
        }

        private Vector3 FindRandomNavMeshPoint(
            Vector3 center,
            float radius)
        {
            for (int i = 0; i < MaxNavMeshAttempts; i++)
            {
                Vector3 randomDirection =
                    UnityEngine.Random.insideUnitSphere * radius;

                randomDirection.y = 0f;

                Vector3 candidate =
                    center + randomDirection;

                if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    radius,
                    NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return transform.position;
        }

        private Vector3 FindNearestNavMeshPoint(
            Vector3 position,
            float searchRadius)
        {
            if (NavMesh.SamplePosition(
                position,
                out NavMeshHit hit,
                searchRadius,
                NavMesh.AllAreas))
            {
                return hit.position;
            }

            return transform.position;
        }

        private void StopAgent()
        {
            if (_agent == null || !_agent.enabled)
            {
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                return;
            }

            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = true;
        }

        private void HoldAttackPose()
        {
            ApplyState(AnimalStateId.Attack);
            StopAgent();
            FacePlayer();
        }

        private bool IsAttackAnimationLocked(float currentTime)
        {
            return currentTime < _attackAnimationLockUntil;
        }

        private void FacePlayer()
        {
            Transform playerTransform = GetPlayerTransform();

            if (playerTransform == null)
            {
                return;
            }

            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void RefreshAnimationIfNeeded(float currentTime)
        {
            if (currentTime < _nextAnimationTime)
            {
                return;
            }

            RefreshAnimation();
            _nextAnimationTime = currentTime + AnimationRefreshInterval;
        }

        private void RefreshAnimation()
        {
            if (_animationController == null)
            {
                return;
            }

            bool isMoving =
                _state != AnimalStateId.Attack &&
                _agent != null &&
                _agent.enabled &&
                (
                    _agent.velocity.sqrMagnitude > MovingVelocityThreshold ||
                    _agent.desiredVelocity.sqrMagnitude > MovingVelocityThreshold
                );

            _animationController.ApplyMovementState(_state, isMoving);
        }

        private void ApplyDefinitionToAgent()
        {
            if (_agent == null || _definition == null)
            {
                return;
            }

            _agent.speed = _definition.WalkSpeed;
            _agent.baseOffset = 0f;
        }

        private void DisableWolfFurRenderers()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null || !IsWolfFurRenderer(renderer))
                {
                    continue;
                }

                renderer.enabled = false;
            }
        }

        private static bool IsWolfFurRenderer(Renderer renderer)
        {
            string objectName = renderer.gameObject.name;

            return objectName.IndexOf(FurNameFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf(HairNameFragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private float GetDistanceToPlayer()
        {
            Transform playerTransform = GetPlayerTransform();

            if (playerTransform == null || _definition == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(transform.position, playerTransform.position);
        }

        private Transform GetPlayerTransform()
        {
            PlayerRoot player = GetPlayer();

            if (player != null)
            {
                _playerTransform = player.transform;
                return _playerTransform;
            }

            _playerTransform = null;
            return null;
        }

        public bool IsPlayerInsideDetectionRadius()
        {
            if (_definition == null)
            {
                return false;
            }

            return GetDistanceToPlayer() <= _definition.DetectionRadius;
        }

        public bool IsPlayerInsideFleeRadius()
        {
            if (_definition == null)
            {
                return false;
            }

            return GetDistanceToPlayer() <= _definition.FleeRadius;
        }

        public bool IsPlayerInsideAttackDistance()
        {
            if (_definition == null)
            {
                return false;
            }

            return GetDistanceToPlayer() <= _definition.AttackDistance;
        }

        public void WanderAroundHome()
        {
            Wander();
        }

        public void FleeFromPlayer()
        {
            PlayerRoot player = GetPlayer();

            if (player == null)
            {
                Wander();
                return;
            }

            FleeFrom(player.transform.position);
        }

        public void ChasePlayer()
        {
            PlayerRoot player = GetPlayer();

            if (player == null)
            {
                Wander();
                return;
            }

            Chase(player.transform.position);
        }

        public void SetDead()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;

            StopMovement();

            if (_agent != null)
            {
                _agent.enabled = false;
            }

            _animationController?.PlayDeath();
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_drawDebug || _definition == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _definition.DetectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _definition.AttackDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _definition.WanderRadius);
        }
        #endif
    }
}
