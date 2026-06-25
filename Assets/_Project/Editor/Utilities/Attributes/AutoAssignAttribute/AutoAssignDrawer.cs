#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assign
{
	[CustomPropertyDrawer(typeof(AssignAttribute))]
	public class AutoAssignDrawer : PropertyDrawer
	{
		// One-shot tracking: each [Assign] field is searched exactly once per domain reload.
		// Key packs (instanceId << 32) | pathHash — no string allocations, 8 bytes per entry.
		private static readonly HashSet<long> _searched = new();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label);

			if (Event.current.type != EventType.Layout)
			{
				return;
			}
			if (property.objectReferenceValue != null)
			{
				return;
			}
			if (property.serializedObject.targetObject is not Component component)
			{
				return;
			}

			long key = (long)component.GetHashCode() << 32
			         | (uint)property.propertyPath.GetHashCode();

			if (!_searched.Add(key))
			{
				return;
			}

			AssignAttribute assignAttribute = (AssignAttribute)attribute;
			UnityEngine.Object found = Find(component, fieldInfo.FieldType, assignAttribute.mode);

			if (found)
			{
				property.objectReferenceValue = found;
				property.serializedObject.ApplyModifiedProperties();
			}
		}

		private static UnityEngine.Object Find(Component component, Type type, Mode mode) => mode switch
		{
			Mode.Local => component.GetComponent(type),
			Mode.Parent => component.transform.parent
				? component.transform.parent.GetComponentInParent(type)
				: null,
			Mode.Children => component.GetComponentInChildren(type, true),
			Mode.Scene => UnityEngine.Object.FindAnyObjectByType(type, FindObjectsInactive.Include),
			_ => null
		};
	}
}
#endif
