using System;
using _Project.Runtime.Gameplay.Items.ConcreteItems.Flashlight;
using UnityEngine;

namespace TLN.Gameplay.Flashlight
{
	public sealed class FlashlightController : MonoBehaviour
	{
		[SerializeField] private FlashlightConfig _config;
		[SerializeField] private Light _light;

		private float _currentBattery;
		private bool _isOn;

		public bool IsOn => _isOn;
		public float BatteryNormalized => _currentBattery / _config.MaxBattery;

		public event Action<bool> OnStateChanged;
		public event Action OnBatteryDepleted;

		private void Awake()
		{
			_currentBattery = _config.MaxBattery;
			if (_light != null)
			{
				_light.enabled = false;
			}
		}

		private void Update()
		{
			if (_isOn && _currentBattery > 0f)
			{
				_currentBattery -= _config.DrainPerSecond * UnityEngine.Time.deltaTime;

				if (_currentBattery <= 0f)
				{
					_currentBattery = 0f;
					TurnOff();
					OnBatteryDepleted?.Invoke();
				}
			}
		}

		public void Toggle()
		{
			if (_isOn) {
				TurnOff();
			}
			else {
				TurnOn();
			}
		}

		private void TurnOn()
		{
			if (_currentBattery <= 0f) {
				return;
			}

			_isOn = true;
			if (_light != null) {
				_light.enabled = true;
			}
			OnStateChanged?.Invoke(true);
		}

		private void TurnOff()
		{
			_isOn = false;
			if (_light != null) {
				_light.enabled = false;
			}
			OnStateChanged?.Invoke(false);
		}

		public void Recharge(float amount)
		{
			_currentBattery = Mathf.Min(_config.MaxBattery, _currentBattery + amount);
		}
	}
}
