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

		private void Awake()
		{
			if (_config == null)
			{
				SetLightEnabled(false);
				enabled = false;
				return;
			}

			_currentBattery = _config.MaxBattery;
			SetLightEnabled(false);
		}

		private void Update()
		{
			if (!_isOn)
			{
				return;
			}

			_currentBattery = Mathf.Max(0f, _currentBattery - _config.DrainPerSecond * UnityEngine.Time.deltaTime);

			if (_currentBattery <= 0f)
			{
				TurnOff();
			}
		}

		public void Toggle()
		{
			if (_isOn)
			{
				TurnOff();
			}
			else
			{
				TurnOn();
			}
		}

		private void TurnOn()
		{
			if (_currentBattery <= 0f || _config == null)
			{
				return;
			}

			_isOn = true;
			SetLightEnabled(true);
		}

		private void TurnOff()
		{
			_isOn = false;
			SetLightEnabled(false);
		}

		private void SetLightEnabled(bool isEnabled)
		{
			if (_light != null)
			{
				_light.enabled = isEnabled;
			}
		}
	}
}
