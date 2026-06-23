using System;
using System.Collections.Generic;
using System.Reflection;
using TLN.Application.Localization;
using TLN.Core.Logging;
using UnityEditor;
using UnityEditor.Localization;

namespace TLN.Editor.Validation
{
	[InitializeOnLoad]
	public static class LocalizationKeysValidator
	{
		private const bool BlockPlayModeOnError = true;

		private static readonly KeyTableBinding[] Bindings =
		{
			new(LocalizationTableNames.UI, typeof(LocalizationKeys))
		};

		static LocalizationKeysValidator()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		[MenuItem("Tools/TLN/Validate Localization Keys")]
		public static void ValidateFromMenu()
		{
			int errorCount = ValidateAll();

			if (errorCount <= 0)
			{
				UnityEngine.Debug.Log("[Localization] All localization keys are valid.");
				return;
			}

			TLNLogger.LogError($"[Localization] Found {errorCount} localization key issue(s). See detailed errors above.");
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.ExitingEditMode)
			{
				return;
			}

			int errorCount = ValidateAll();

			if (errorCount <= 0)
			{
				return;
			}

			TLNLogger.LogError($"[Localization] Found {errorCount} localization key issue(s). See detailed errors above.");

			if (BlockPlayModeOnError)
			{
				EditorApplication.isPlaying = false;
			}
		}

		private static int ValidateAll()
		{
			int errorCount = 0;

			foreach (KeyTableBinding binding in Bindings)
			{
				errorCount += ValidateBinding(binding);
			}

			return errorCount;
		}

		private static int ValidateBinding(KeyTableBinding binding)
		{
			int errorCount = 0;
			StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(binding.TableName);

			if (collection == null)
			{
				TLNLogger.LogError($"[Localization] Missing String Table Collection: {binding.TableName}");
				return 1;
			}

			if (collection.SharedData == null)
			{
				TLNLogger.LogError($"[Localization] String Table Collection has no shared data: {binding.TableName}", collection);
				return 1;
			}

			foreach (LocalizationKey key in CollectKeys(binding.KeysType))
			{
				if (string.IsNullOrWhiteSpace(key.Value))
				{
					TLNLogger.LogError(
						$"[Localization] Empty localization key.\n" +
						$"Table: {binding.TableName}\n" +
						$"Field: {key.FieldPath}",
						collection
					);

					errorCount++;
					continue;
				}

				if (collection.SharedData.GetEntry(key.Value) == null)
				{
					TLNLogger.LogError(
						$"[Localization] Missing shared localization key.\n" +
						$"Table: {binding.TableName}\n" +
						$"Field: {key.FieldPath}\n" +
						$"Key: {key.Value}",
						collection
					);

					errorCount++;
					continue;
				}

				foreach (UnityEngine.Localization.Tables.StringTable table in collection.StringTables)
				{
					if (table == null || table.GetEntry(key.Value) != null)
					{
						continue;
					}

					TLNLogger.LogError(
						$"[Localization] Missing localized table entry.\n" +
						$"Table: {binding.TableName}\n" +
						$"Locale: {table.LocaleIdentifier.Code}\n" +
						$"Field: {key.FieldPath}\n" +
						$"Key: {key.Value}",
						table
					);

					errorCount++;
				}
			}

			return errorCount;
		}

		private static IEnumerable<LocalizationKey> CollectKeys(Type type)
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.Public
				| BindingFlags.Static
				| BindingFlags.FlattenHierarchy);

			foreach (FieldInfo field in fields)
			{
				if (!field.IsLiteral || field.IsInitOnly || field.FieldType != typeof(string))
				{
					continue;
				}

				yield return new LocalizationKey(GetFieldPath(field), (string)field.GetRawConstantValue());
			}

			Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public);

			foreach (Type nestedType in nestedTypes)
			{
				foreach (LocalizationKey key in CollectKeys(nestedType))
				{
					yield return key;
				}
			}
		}

		private static string GetFieldPath(FieldInfo field)
		{
			return $"{GetTypePath(field.DeclaringType)}.{field.Name}";
		}

		private static string GetTypePath(Type type)
		{
			string typeName = type?.FullName ?? "<Unknown Type>";
			int rootIndex = typeName.IndexOf(nameof(LocalizationKeys), StringComparison.Ordinal);

			if (rootIndex >= 0)
			{
				typeName = typeName[rootIndex..];
			}

			return typeName.Replace('+', '.');
		}

		private readonly struct KeyTableBinding
		{
			public KeyTableBinding(string tableName, Type keysType)
			{
				TableName = tableName;
				KeysType = keysType;
			}

			public string TableName { get; }

			public Type KeysType { get; }
		}

		private readonly struct LocalizationKey
		{
			public LocalizationKey(string fieldPath, string value)
			{
				FieldPath = fieldPath;
				Value = value;
			}

			public string FieldPath { get; }

			public string Value { get; }
		}
	}
}
