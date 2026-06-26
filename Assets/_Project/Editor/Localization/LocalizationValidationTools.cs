using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace TLN.Editor.Localization
{
	public static class LocalizationValidationTools
	{
		private const string SearchRoot = "Assets/_Project/Localization";

		// Not allowed:
		// MainMenu.NewGame
		// main menu new game
		// mainmenu..new_game
		// mainmenu.new_game.
		private static readonly Regex KeyPattern = new(
			"^[a-z0-9]+([._-][a-z0-9]+)*$",
			RegexOptions.Compiled
		);

		private sealed class ValidationResult
		{
			public int Errors;
			public int Warnings;
			public readonly List<string> Messages = new();
			public bool Success => Errors == 0;
		}

		[MenuItem("Tools/TLN/Localization/Validate Keys")]
		public static void ValidateKeys()
		{
			string[] guids = AssetDatabase.FindAssets("t:SharedTableData", new[] { SearchRoot });

			ValidationResult result = new();
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

				ValidationResult tableResult = ValidateSharedTable(sharedData, path, keyOwners);
				result.Errors += tableResult.Errors;
				result.Messages.AddRange(tableResult.Messages);
			}

			ValidationResult crossResult = ReportCrossTableDuplicateKeys(keyOwners);
			result.Warnings += crossResult.Warnings;
			result.Messages.AddRange(crossResult.Messages);

			if (result.Success)
			{
				Debug.Log(
					$"[TLN Localization] Key validation passed. Tables: {guids.Length}, entries: {entryCount}, warnings: {result.Warnings}"
				);
			}
			else
			{
				Debug.LogError(
					$"[TLN Localization] Key validation failed. Errors: {result.Errors}, warnings: {result.Warnings}, tables: {guids.Length}, entries: {entryCount}"
				);
			}
		}

		private static ValidationResult ValidateSharedTable(SharedTableData sharedData, string path, Dictionary<string, List<string>> keyOwners)
		{
			Dictionary<string, List<SharedTableData.SharedTableEntry>> entriesByKey = new();
			Dictionary<long, List<SharedTableData.SharedTableEntry>> entriesById = new();
			ValidationResult result = new();

			for (int i = 0; i < sharedData.Entries.Count; i++)
			{
				SharedTableData.SharedTableEntry entry = sharedData.Entries[i];

				if (entry == null)
				{
					Error(result, $"[TLN Localization] Null entry in {path} at index {i}.", sharedData);
					continue;
				}

				if (string.IsNullOrWhiteSpace(entry.Key))
				{
					Error(result, $"[TLN Localization] Empty key in '{sharedData.TableCollectionName}' at index {i}, id {entry.Id}.", sharedData);
				}
				else
				{
					if (!KeyPattern.IsMatch(entry.Key))
					{
						Error(result, $"[TLN Localization] Invalid key '{entry.Key}' in '{sharedData.TableCollectionName}' at index {i}, id {entry.Id}. Key must match pattern: {KeyPattern}", sharedData);
					}

					Add(entriesByKey, entry.Key, entry);
					Add(keyOwners, entry.Key, $"{sharedData.TableCollectionName} ({path})");
				}

				if (entry.Id == SharedTableData.EmptyId)
				{
					Error(result, $"[TLN Localization] Empty id for key '{entry.Key}' in '{sharedData.TableCollectionName}'.", sharedData);
				}
				else
				{
					Add(entriesById, entry.Id, entry);
				}
			}

			Merge(result, ReportDuplicates(sharedData, entriesByKey, "key"));
			Merge(result, ReportDuplicates(sharedData, entriesById, "id"));

			return result;
		}

		private static void Error(ValidationResult result, string message, Object context = null)
		{
			Debug.LogError(message, context);
			result.Errors++;
			result.Messages.Add(message);
		}

		private static void Warning(ValidationResult result, string message)
		{
			Debug.LogWarning(message);
			result.Warnings++;
			result.Messages.Add(message);
		}

		private static void Merge(ValidationResult target, ValidationResult source)
		{
			target.Errors += source.Errors;
			target.Warnings += source.Warnings;
			target.Messages.AddRange(source.Messages);
		}

		private static ValidationResult ReportDuplicates<T>(SharedTableData sharedData, Dictionary<T, List<SharedTableData.SharedTableEntry>> entries,
			string label)
		{
			ValidationResult result = new();

			foreach (KeyValuePair<T, List<SharedTableData.SharedTableEntry>> pair in entries)
			{
				if (pair.Value.Count <= 1)
				{
					continue;
				}

				string ids = string.Join(", ", pair.Value.Select(entry => $"{entry.Key}:{entry.Id}"));

				Error(result, $"[TLN Localization] Duplicate {label} '{pair.Key}' in '{sharedData.TableCollectionName}'." +
					$" Entries: {ids}", sharedData);
			}

			return result;
		}

		private static ValidationResult ReportCrossTableDuplicateKeys(Dictionary<string, List<string>> keyOwners)
		{
			ValidationResult result = new();

			foreach (KeyValuePair<string, List<string>> pair in keyOwners)
			{
				List<string> uniqueOwners = pair.Value.Distinct().ToList();

				if (uniqueOwners.Count <= 1)
				{
					continue;
				}

				Warning(result, $"[TLN Localization] Key '{pair.Key}'" +
					$" exists in multiple table collections: {string.Join(", ", uniqueOwners)}");
			}

			return result;
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
