using System;

namespace TLN.Core.Validation
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class ButtonAttribute : Attribute
	{
		public string Label { get; }

		public ButtonAttribute()
		{
		}

		public ButtonAttribute(string label)
		{
			Label = label;
		}
	}
}
