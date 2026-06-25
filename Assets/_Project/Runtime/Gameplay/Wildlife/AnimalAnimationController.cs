using System.Collections.Generic;
using Assign;
using TLN.Core.Validation;
using UnityEngine;

namespace TLN.Gameplay.Wildlife
{
    [DisallowMultipleComponent]
    public sealed class AnimalAnimationController : MonoBehaviour
    {
        [SerializeField][Required][Assign] private Animator _animator;

        [Header("Bool Parameters")]
        [SerializeField] private string _isWalkParameter = "IsWalk";
        [SerializeField] private string _isRunParameter = "IsRun";

        [Header("Trigger Parameters")]
        [SerializeField] private string _attackLeftParameter = "AttackL";
        [SerializeField] private string _attackRightParameter = "AttackR";
        [SerializeField] private string _hitParameter = "Hit";
        [SerializeField] private string _deadParameter = "Dead";
        [SerializeField] private string _isDeadParameter = "IsDead";

        private int _isWalkHash;
        private int _isRunHash;
        private int _attackLeftHash;
        private int _attackRightHash;
        private int _hitHash;
        private int _deadHash;
        private int _isDeadHash;

        private readonly Dictionary<int, AnimatorControllerParameterType> _parametersByHash = new();

        private void Awake()
        {
            _isWalkHash = Animator.StringToHash(_isWalkParameter);
            _isRunHash = Animator.StringToHash(_isRunParameter);
            _attackLeftHash = Animator.StringToHash(_attackLeftParameter);
            _attackRightHash = Animator.StringToHash(_attackRightParameter);
            _hitHash = Animator.StringToHash(_hitParameter);
            _deadHash = Animator.StringToHash(_deadParameter);
            _isDeadHash = Animator.StringToHash(_isDeadParameter);
            CacheParameters();
        }

        public void ApplyMovementState(
            AnimalStateId state,
            bool isMoving)
        {
            bool hasWalk = IsBool(_isWalkHash);
            bool hasRun = IsBool(_isRunHash);

            bool shouldWalk =
                isMoving &&
                state == AnimalStateId.Wander &&
                hasWalk;

            bool shouldRun =
                isMoving &&
                (
                    state == AnimalStateId.Flee ||
                    state == AnimalStateId.Chase ||
                    (state == AnimalStateId.Wander && !hasWalk)
                );

            if (hasWalk)
            {
                _animator.SetBool(_isWalkHash, shouldWalk);
            }

            if (hasRun)
            {
                _animator.SetBool(_isRunHash, shouldRun);
            }
        }

        public void PlayAttack()
        {
            ResetMovementState();

            bool canPlayLeft = IsTrigger(_attackLeftHash);
            bool canPlayRight = IsTrigger(_attackRightHash);

            if (canPlayLeft && canPlayRight)
            {
                if (Random.value < 0.5f)
                {
                    _animator.SetTrigger(_attackLeftHash);
                }
                else
                {
                    _animator.SetTrigger(_attackRightHash);
                }

                return;
            }

            if (canPlayLeft)
            {
                _animator.SetTrigger(_attackLeftHash);
                return;
            }

            if (canPlayRight)
            {
                _animator.SetTrigger(_attackRightHash);
            }
        }

        public void PlayHit()
        {
            if (IsTrigger(_hitHash))
            {
                _animator.SetTrigger(_hitHash);
            }
        }

        public void PlayDeath()
        {
            ResetMovementState();

            if (IsTrigger(_deadHash))
            {
                _animator.SetTrigger(_deadHash);
                return;
            }

            if (IsTrigger(_isDeadHash))
            {
                _animator.SetTrigger(_isDeadHash);
                return;
            }

            if (IsBool(_isDeadHash))
            {
                _animator.SetBool(_isDeadHash, true);
            }
        }

        private void CacheParameters()
        {
            _parametersByHash.Clear();

            if (_animator == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                _parametersByHash[parameter.nameHash] = parameter.type;
            }
        }

        private void ResetMovementState()
        {
            if (IsBool(_isWalkHash))
            {
                _animator.SetBool(_isWalkHash, false);
            }

            if (IsBool(_isRunHash))
            {
                _animator.SetBool(_isRunHash, false);
            }
        }

        private bool IsTrigger(int hash)
        {
            return _parametersByHash.TryGetValue(hash, out AnimatorControllerParameterType type) &&
                   type == AnimatorControllerParameterType.Trigger;
        }

        private bool IsBool(int hash)
        {
            return _parametersByHash.TryGetValue(hash, out AnimatorControllerParameterType type) &&
                   type == AnimatorControllerParameterType.Bool;
        }
    }
}
