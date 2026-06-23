using System;
using Assign;
using Newtonsoft.Json;
using TLN.Core.Validation;
using TLN.Gameplay.Saves;
using UnityEngine;

namespace TLN.Gameplay.Wildlife
{
    [RequireComponent(typeof(PersistentWorldEntity))]
    public sealed class AnimalHealth : MonoBehaviour, IWorldSaveable
    {
        private const string SaveType = "animal_health";

        [Header("References")]
        [SerializeField][Required][Assign] private AnimalActor _animalActor;
        [SerializeField][Required][Assign(Mode.Children)] private AnimalAnimationController _animationController;

        [Header("Death")]
        [SerializeField] private Behaviour[] _behavioursToDisableOnDeath;

        private float _currentHealth;
        private bool _isDead;

        public string SaveTypeId => SaveType;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _animalActor.Definition.MaxHealth;

        public bool IsDead => _isDead;

        public event Action Died;

        private void Awake()
        {
            _currentHealth = MaxHealth;
        }

        public void Damage(float amount)
        {
            if (_isDead)
            {
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);

            if (_currentHealth > 0f)
            {
                _animationController?.PlayHit();
                return;
            }

            Die();
        }

        public void Kill()
        {
            if (_isDead)
            {
                return;
            }

            _currentHealth = 0f;
            Die();
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;

            _animalActor.SetDead();

            DisableDeathBehaviours();

            Died?.Invoke();
        }

        private void DisableDeathBehaviours()
        {
            if (_behavioursToDisableOnDeath == null)
            {
                return;
            }

            for (int i = 0; i < _behavioursToDisableOnDeath.Length; i++)
            {
                Behaviour behaviour = _behavioursToDisableOnDeath[i];

                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour == this)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        public string CaptureStateJson()
        {
            AnimalHealthSaveData data = new AnimalHealthSaveData
            {
                currentHealth = _currentHealth,
                isDead = _isDead
            };

            return JsonConvert.SerializeObject(data);
        }

        public void RestoreStateJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            AnimalHealthSaveData data = JsonConvert.DeserializeObject<AnimalHealthSaveData>(json);

            if (data == null)
            {
                return;
            }

            _currentHealth = Mathf.Clamp(data.currentHealth, 0f, MaxHealth);
            _isDead = data.isDead || _currentHealth <= 0f;

            if (_isDead)
            {
                _currentHealth = 0f;
                _animalActor.SetDead();
                DisableDeathBehaviours();
            }
        }

        private sealed class AnimalHealthSaveData
        {
            public float currentHealth;
            public bool isDead;
        }
    }
}
