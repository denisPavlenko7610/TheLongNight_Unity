using UnityEngine;

namespace Assign
{
	using UnityEngine;

	public class AutoAssignAttribute : PropertyAttribute
	{
		public readonly AutoAssignMode mode;

		public AutoAssignAttribute(AutoAssignMode mode = AutoAssignMode.Local)
		{
			this.mode = mode;
		}
	}

}