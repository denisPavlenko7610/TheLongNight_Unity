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

        private readonly HashSet<int> _parameterHashes = new();

        private void Awake()
        {
            CacheParameters();
        }

        public void ApplyMovementState(
            AnimalStateId state,
            bool isMoving)
        {
            bool hasWalk = HasParameter(_isWalkParameter);
            bool hasRun = HasParameter(_isRunParameter);

            bool shouldWalk =
                isMoving &&
                state == AnimalStateId.Wander &&
                hasWalk;

            bool shouldRun =
                isMoving &&
                (
                    state == AnimalStateId.Flee ||
                    state == AnimalStateId.Chase ||
                    state == AnimalStateId.Attack ||
                    state == AnimalStateId.Wander && !hasWalk
                );

            SetBoolIfExists(_isWalkParameter, shouldWalk);
            SetBoolIfExists(_isRunParameter, shouldRun && hasRun);
        }

        public void PlayAttack()
        {
            bool canPlayLeft = HasParameter(_attackLeftParameter);
            bool canPlayRight = HasParameter(_attackRightParameter);

            if (canPlayLeft && canPlayRight)
            {
                if (Random.value < 0.5f)
                {
                    SetTriggerIfExists(_attackLeftParameter);
                }
                else
                {
                    SetTriggerIfExists(_attackRightParameter);
                }

                return;
            }

            if (canPlayLeft)
            {
                SetTriggerIfExists(_attackLeftParameter);
                return;
            }

            if (canPlayRight)
            {
                SetTriggerIfExists(_attackRightParameter);
            }
        }

        public void PlayHit()
        {
            SetTriggerIfExists(_hitParameter);
        }

        public void PlayDeath()
        {
            SetBoolIfExists(_isWalkParameter, false);
            SetBoolIfExists(_isRunParameter, false);

            if (HasParameter(_deadParameter))
            {
                SetTriggerIfExists(_deadParameter);
                return;
            }

            SetTriggerIfExists(_isDeadParameter);
        }

        private void CacheParameters()
        {
            _parameterHashes.Clear();

            if (_animator == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                _parameterHashes.Add(parameters[i].nameHash);
            }
        }

        private bool HasParameter(string parameterName)
        {
            if (_animator == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            return _parameterHashes.Contains(
                Animator.StringToHash(parameterName));
        }

        private void SetBoolIfExists(
            string parameterName,
            bool value)
        {
            if (!HasParameter(parameterName))
            {
                return;
            }

            _animator.SetBool(parameterName, value);
        }

        private void SetTriggerIfExists(string parameterName)
        {
            if (!HasParameter(parameterName))
            {
                return;
            }

            _animator.SetTrigger(parameterName);
        }
    }
}
