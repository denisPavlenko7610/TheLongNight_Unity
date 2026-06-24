using System;
using Assign;
using TLN.Application.GameStates;
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

        private bool _isDead;

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

            ApplyState(AnimalStateId.Idle);
        }

        private void Update()
        {
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

            if (!_useInternalDecisionLogic)
            {
                RefreshAnimation();
                return;
            }

            if (_agent != null && _agent.isStopped)
            {
                _agent.isStopped = false;
            }

            PlayerRoot player = GetPlayer();

            switch (_definition.Species)
            {
                case AnimalSpeciesId.Rabbit:
                    TickRabbit(player);
                    break;

                case AnimalSpeciesId.Wolf:
                    TickWolf(player);
                    break;
            }

            RefreshAnimation();
        }

        private PlayerRoot GetPlayer()
        {
			return _targetService?.PlayerRoot;
        }

        private void TickRabbit(PlayerRoot player)
        {
            if (player != null)
            {
                float distanceToPlayer =
                    Vector3.Distance(
                        transform.position,
                        player.transform.position);

                if (distanceToPlayer <= _definition.FleeRadius)
                {
                    FleeFrom(player.transform.position);
                    return;
                }
            }

            Wander();
        }

        private void TickWolf(PlayerRoot player)
        {
            if (player == null)
            {
                Wander();
                return;
            }

            float distanceToPlayer =
                Vector3.Distance(
                    transform.position,
                    player.transform.position);

            if (distanceToPlayer <= _definition.AttackDistance)
            {
                AttackPlayer();
                return;
            }

            if (distanceToPlayer <= _definition.DetectionRadius)
            {
                Chase(player.transform.position);
                return;
            }

            Wander();
        }

        private void Wander()
        {
            if (UnityEngine.Time.time < _nextDecisionTime)
            {
                return;
            }

            _nextDecisionTime =
                UnityEngine.Time.time + _definition.DecisionInterval;

            if (_agent == null)
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
            if (_agent == null)
            {
                return;
            }

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

            ApplyState(AnimalStateId.Flee);

            _agent.speed = _definition.RunSpeed;
            _agent.SetDestination(targetPosition);
        }

        private void Chase(Vector3 targetPosition)
        {
            if (_agent == null)
            {
                return;
            }

            ApplyState(AnimalStateId.Chase);

            _agent.speed = _definition.RunSpeed;
            _agent.SetDestination(targetPosition);
        }

        public void StopMovement()
        {
            if (_agent == null)
            {
                return;
            }

            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = true;

            ApplyState(AnimalStateId.Idle);
        }

        public void AttackPlayer()
        {
            if (_isDead)
            {
                return;
            }

            ApplyState(AnimalStateId.Attack);

            if (UnityEngine.Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = UnityEngine.Time.time + _definition.AttackCooldownSeconds;

            _animationController?.PlayAttack();

            if (_survivalService == null)
            {
                TLNLogger.LogWarning(
                    "Wolf cannot damage player because SurvivalService is missing.",
                    this);

                return;
            }

            _survivalService.DamageCondition(_definition.ConditionDamage);

            _notificationService?.Show($"Wolf attack! Condition -{_definition.ConditionDamage:0}.");
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
            if (_agent == null)
            {
                return;
            }

            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = true;
        }

        private void RefreshAnimation()
        {
            if (_animationController == null)
            {
                return;
            }

            bool isMoving =
                _agent != null &&
                _agent.enabled &&
                _agent.velocity.sqrMagnitude > MovingVelocityThreshold;

            _animationController.ApplyMovementState(_state, isMoving);
        }

        private float GetDistanceToPlayer()
        {
            PlayerRoot player = GetPlayer();

            if (player == null || _definition == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(transform.position, player.transform.position);
        }

        public bool IsPlayerInsideDetectionRadius()
        {
            return GetDistanceToPlayer() <= _definition.DetectionRadius;
        }

        public bool IsPlayerInsideFleeRadius()
        {
            return GetDistanceToPlayer() <= _definition.FleeRadius;
        }

        public bool IsPlayerInsideAttackDistance()
        {
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
