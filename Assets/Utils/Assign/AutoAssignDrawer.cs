#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Assign
{
	[CustomPropertyDrawer(typeof(AutoAssignAttribute))]
	public class AutoAssignDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label);
			if (property.objectReferenceValue != null)
				return;

			if (!(property.serializedObject.targetObject is Component component))
				return;

			var attribute = (AutoAssignAttribute)base.attribute;
			var requiredType = fieldInfo.FieldType;

			Object found = null;

			switch (attribute.mode)
			{
				case AutoAssignMode.Local:
					found = component.GetComponent(requiredType);
					break;

				case AutoAssignMode.Parent:
					if (component.transform.parent)
						found = component.transform.parent.GetComponentInParent(requiredType);
					break;

				case AutoAssignMode.Children:
					found = component.GetComponentInChildren(requiredType, true);
					break;

				case AutoAssignMode.Scene:
					var all = Object.FindObjectsOfType(requiredType, true);
					if (all.Length == 1)
						found = all[0];
					break;
			}

			if (!found)
				return;

			property.objectReferenceValue = found;
			property.serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
