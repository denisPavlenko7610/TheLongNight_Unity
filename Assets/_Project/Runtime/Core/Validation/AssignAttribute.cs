using System;
using UnityEngine;

namespace Assign
{
	public enum Mode
	{
		Local,
		Parent,
		Children,
		Scene
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class AssignAttribute : PropertyAttribute
	{
		public readonly Mode mode;

		public AssignAttribute(Mode mode = Mode.Local)
		{
			this.mode = mode;
		}
	}
}
