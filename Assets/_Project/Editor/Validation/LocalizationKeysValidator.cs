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
		private const string MenuPath = "Tools/TLN/Validation/Localization Keys";

		static LocalizationKeysValidator()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		[MenuItem(MenuPath)]
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
			Dictionary<string, List<LocalizationKey>> keysByTable = new();

			foreach (LocalizationKey key in CollectKeys(typeof(LocalizationKeys)))
			{
				if (string.IsNullOrWhiteSpace(key.Value))
				{
					TLNLogger.LogError(
						$"[Localization] Empty localization key.\n" +
						$"Field: {key.FieldPath}"
					);

					errorCount++;
					continue;
				}

				if (string.IsNullOrWhiteSpace(key.TableName))
				{
					TLNLogger.LogError(
						$"[Localization] Localization key has no table mapping.\n" +
						$"Field: {key.FieldPath}\n" +
						$"Key: {key.Value}"
					);

					errorCount++;
					continue;
				}

				if (!keysByTable.TryGetValue(key.TableName, out List<LocalizationKey> keys))
				{
					keys = new List<LocalizationKey>();
					keysByTable.Add(key.TableName, keys);
				}

				keys.Add(key);
			}

			foreach (KeyValuePair<string, List<LocalizationKey>> entry in keysByTable)
			{
				errorCount += ValidateBinding(entry.Key, entry.Value);
			}

			return errorCount;
		}

		private static int ValidateBinding(string tableName, IEnumerable<LocalizationKey> keys)
		{
			int errorCount = 0;
			StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);

			if (collection == null)
			{
				TLNLogger.LogError($"[Localization] Missing String Table Collection: {tableName}");
				return 1;
			}

			if (collection.SharedData == null)
			{
				TLNLogger.LogError($"[Localization] String Table Collection has no shared data: {tableName}", collection);
				return 1;
			}

			foreach (LocalizationKey key in keys)
			{
				if (collection.SharedData.GetEntry(key.Value) == null)
				{
					TLNLogger.LogError(
						$"[Localization] Missing shared localization key.\n" +
						$"Table: {tableName}\n" +
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
						$"Table: {tableName}\n" +
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

				string value = (string)field.GetRawConstantValue();
				string tableName;
				try
				{
					tableName = LocalizationKeyRegistry.GetTableName(value);
				}
				catch (ArgumentException)
				{
					tableName = string.Empty;
				}

				yield return new LocalizationKey(GetFieldPath(field), value, tableName);
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

		private readonly struct LocalizationKey
		{
			public LocalizationKey(string fieldPath, string value, string tableName)
			{
				FieldPath = fieldPath;
				Value = value;
				TableName = tableName;
			}

			public string FieldPath { get; }

			public string Value { get; }

			public string TableName { get; }
		}
	}
}
