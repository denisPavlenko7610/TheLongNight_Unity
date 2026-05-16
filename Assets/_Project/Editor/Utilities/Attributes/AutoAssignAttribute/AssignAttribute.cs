using UnityEngine;

namespace Assign
{
	using UnityEngine;

	public class AssignAttribute : PropertyAttribute
	{
		public readonly Mode mode;

		public AssignAttribute(Mode mode = Mode.Local)
		{
			this.mode = mode;
		}
	}

}
