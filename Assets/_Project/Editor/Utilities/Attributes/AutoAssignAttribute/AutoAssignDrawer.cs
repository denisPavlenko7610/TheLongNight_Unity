#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Assign
{
    [CustomPropertyDrawer(typeof(AssignAttribute))]
    public class AutoAssignDrawer : PropertyDrawer
    {
       public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
       {
          EditorGUI.PropertyField(position, property, label);

          if (Event.current.type != EventType.Layout) {
             return;
          }

          if (property.objectReferenceValue != null) {
             return;
          }

          if (!(property.serializedObject.targetObject is Component component)) {
             return;
          }

          AssignAttribute attribute = (AssignAttribute)base.attribute;
          Type requiredType = fieldInfo.FieldType;

          UnityEngine.Object found = null;

          switch (attribute.mode)
          {
             case Mode.Local:
                found = component.GetComponent(requiredType);
                break;

             case Mode.Parent:
                if (component.transform.parent) {
                   found = component.transform.parent.GetComponentInParent(requiredType);
                }
                break;

             case Mode.Children:
                found = component.GetComponentInChildren(requiredType, true);
                break;

             case Mode.Scene:
                found = UnityEngine.Object.FindAnyObjectByType(requiredType, FindObjectsInactive.Include);
                break;
          }

          if (found != null)
          {
             property.objectReferenceValue = found;
             property.serializedObject.ApplyModifiedProperties();
          }
       }
    }
}
#endif
