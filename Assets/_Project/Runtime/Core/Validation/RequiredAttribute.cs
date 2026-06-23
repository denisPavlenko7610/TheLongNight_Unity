using System;
using UnityEngine;

namespace TLN.Core.Validation
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class RequiredAttribute : PropertyAttribute { }
}
