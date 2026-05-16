using UnityEngine;
using UnityEngine.UIElements;

namespace TLN.UI.Common
{
	public readonly struct UIFillIcon
	{
		private readonly VisualElement _fillMask;
		private readonly float _height;

		public UIFillIcon(VisualElement fillMask, float height)
		{
			_fillMask = fillMask;
			_height = height;
		}

		public void SetValue(float normalizedValue)
		{
			if (_fillMask == null)
			{
				return;
			}

			float clampedValue = Mathf.Clamp01(normalizedValue);
			_fillMask.style.height = _height * clampedValue;
		}
	}
}
