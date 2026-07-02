using System;
using System.Reflection;
using TLN.Core.Validation;
using UnityEditor;
using UnityEngine;

namespace TLN.Editor.Validation
{
	public abstract class ButtonEditorBase : UnityEditor.Editor
	{
		private const BindingFlags MethodFlags =
			BindingFlags.Instance |
			BindingFlags.Public |
			BindingFlags.NonPublic;

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			DrawButtons();
		}

		private void DrawButtons()
		{
			MethodInfo[] methods = target.GetType().GetMethods(MethodFlags);

			bool hasAnyButton = false;

			for (int i = 0; i < methods.Length; i++)
			{
				MethodInfo method = methods[i];
				ButtonAttribute button = method.GetCustomAttribute<ButtonAttribute>();

				if (button == null)
				{
					continue;
				}

				if (!IsValidButtonMethod(method))
				{
					DrawInvalidButtonWarning(method);
					continue;
				}

				if (!hasAnyButton)
				{
					EditorGUILayout.Space();
					hasAnyButton = true;
				}

				string label = string.IsNullOrWhiteSpace(button.Label)
					? ObjectNames.NicifyVariableName(method.Name)
					: button.Label;

				if (GUILayout.Button(label))
				{
					InvokeForTargets(method);
				}
			}
		}

		private static bool IsValidButtonMethod(MethodInfo method)
		{
			return method.GetParameters().Length == 0
				&& method.ReturnType == typeof(void);
		}

		private static void DrawInvalidButtonWarning(MethodInfo method)
		{
			EditorGUILayout.HelpBox(
				$"[Button] method '{method.Name}' must be parameterless and return void.",
				MessageType.Warning
			);
		}

		private void InvokeForTargets(MethodInfo method)
		{
			for (int i = 0; i < targets.Length; i++)
			{
				UnityEngine.Object currentTarget = targets[i];

				Undo.RecordObject(currentTarget, $"Invoke {method.Name}");

				try
				{
					method.Invoke(currentTarget, null);
					EditorUtility.SetDirty(currentTarget);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, currentTarget);
				}
			}
		}
	}
}
