using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace TLN.Editor.Localization
{
	public static class LocalizationValidationTools
	{
		private const string SearchRoot = "Assets/_Project/Localization";

		[MenuItem("Tools/TLN/Localization/Validate Keys")]
		public static void ValidateKeys()
		{
			string[] guids = AssetDatabase.FindAssets("t:SharedTableData", new[] { SearchRoot });

			int errorCount = 0;
			int warningCount = 0;
			int entryCount = 0;

			Dictionary<string, List<string>> keyOwners = new();

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				SharedTableData sharedData = AssetDatabase.LoadAssetAtPath<SharedTableData>(path);

				if (sharedData == null)
				{
					continue;
				}

				entryCount += sharedData.Entries.Count;
				errorCount += ValidateSharedTable(sharedData, path, keyOwners);
			}

			warningCount += ReportCrossTableDuplicateKeys(keyOwners);

			if (errorCount == 0)
			{
				Debug.Log(
					$"[TLN Localization] Key validation passed. Tables: {guids.Length}, entries: {entryCount}, warnings: {warningCount}"
				);
			}
			else
			{
				Debug.LogError(
					$"[TLN Localization] Key validation failed. Errors: {errorCount}, warnings: {warningCount}, tables: {guids.Length}, entries: {entryCount}"
				);
			}
		}

		private static int ValidateSharedTable(
			SharedTableData sharedData,
			string path,
			Dictionary<string, List<string>> keyOwners
		)
		{
			Dictionary<string, List<SharedTableData.SharedTableEntry>> entriesByKey = new();
			Dictionary<long, List<SharedTableData.SharedTableEntry>> entriesById = new();
			int errorCount = 0;

			for (int i = 0; i < sharedData.Entries.Count; i++)
			{
				SharedTableData.SharedTableEntry entry = sharedData.Entries[i];

				if (entry == null)
				{
					Debug.LogError($"[TLN Localization] Null entry in {path} at index {i}.", sharedData);
					errorCount++;
					continue;
				}

				if (string.IsNullOrWhiteSpace(entry.Key))
				{
					Debug.LogError(
						$"[TLN Localization] Empty key in '{sharedData.TableCollectionName}' at index {i}, id {entry.Id}.",
						sharedData
					);
					errorCount++;
				}
				else
				{
					Add(entriesByKey, entry.Key, entry);
					Add(keyOwners, entry.Key, $"{sharedData.TableCollectionName} ({path})");
				}

				if (entry.Id == SharedTableData.EmptyId)
				{
					Debug.LogError(
						$"[TLN Localization] Empty id for key '{entry.Key}' in '{sharedData.TableCollectionName}'.",
						sharedData
					);
					errorCount++;
				}
				else
				{
					Add(entriesById, entry.Id, entry);
				}
			}

			errorCount += ReportDuplicates(sharedData, entriesByKey, "key");
			errorCount += ReportDuplicates(sharedData, entriesById, "id");

			return errorCount;
		}

		private static int ReportDuplicates<T>(
			SharedTableData sharedData,
			Dictionary<T, List<SharedTableData.SharedTableEntry>> entries,
			string label
		)
		{
			int errorCount = 0;

			foreach (KeyValuePair<T, List<SharedTableData.SharedTableEntry>> pair in entries)
			{
				if (pair.Value.Count <= 1)
				{
					continue;
				}

				string ids = string.Join(", ", pair.Value.Select(entry => $"{entry.Key}:{entry.Id}"));

				Debug.LogError(
					$"[TLN Localization] Duplicate {label} '{pair.Key}' in '{sharedData.TableCollectionName}'. Entries: {ids}",
					sharedData
				);

				errorCount++;
			}

			return errorCount;
		}

		private static int ReportCrossTableDuplicateKeys(Dictionary<string, List<string>> keyOwners)
		{
			int warningCount = 0;

			foreach (KeyValuePair<string, List<string>> pair in keyOwners)
			{
				List<string> uniqueOwners = pair.Value.Distinct().ToList();

				if (uniqueOwners.Count <= 1)
				{
					continue;
				}

				Debug.LogWarning(
					$"[TLN Localization] Key '{pair.Key}' exists in multiple table collections: {string.Join(", ", uniqueOwners)}"
				);

				warningCount++;
			}

			return warningCount;
		}

		private static void Add<TKey, TValue>(Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
		{
			if (!dictionary.TryGetValue(key, out List<TValue> values))
			{
				values = new List<TValue>();
				dictionary.Add(key, values);
			}

			values.Add(value);
		}
	}
}
