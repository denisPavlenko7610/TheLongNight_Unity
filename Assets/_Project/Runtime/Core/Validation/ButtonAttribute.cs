using System;
using UnityEngine;

namespace TLN.Core.Validation
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class ButtonAttribute : PropertyAttribute
	{
		public string Label { get; }

		public ButtonAttribute(string label = null)
		{
			Label = label;
		}
	}
}
