using System;
using System.Collections.Generic;
using TLN.Gameplay.Feedback;
using UnityEditor;
using UnityEngine;

namespace TLN.Editor.Feedback
{
	public static class FeedbackCatalogTools
	{
		private const string ProjectRoot = "Assets/_Project";

		[MenuItem("Tools/TLN/Feedback/Rebuild Feedback Catalogs")]
		public static void RebuildFeedbackCatalogs()
		{
			FeedbackDefinition[] definitions = LoadAllDefinitions();
			FeedbackCatalog[] catalogs = LoadAllCatalogs();

			if (catalogs.Length == 0)
			{
				Debug.LogWarning("No FeedbackCatalog assets found.");
				return;
			}

			for (int i = 0; i < catalogs.Length; i++)
			{
				FeedbackCatalog catalog = catalogs[i];

				if (catalog == null)
				{
					continue;
				}

				catalog.EditorSetDefinitions(definitions);
			}

			AssetDatabase.SaveAssets();

			Debug.Log(
				$"Feedback catalogs rebuilt. Catalogs: {catalogs.Length}. Definitions: {definitions.Length}."
			);
		}

		private static FeedbackDefinition[] LoadAllDefinitions()
		{
			string[] guids = AssetDatabase.FindAssets(
				"t:FeedbackDefinition",
				new[] { ProjectRoot }
			);

			List<FeedbackDefinition> definitions = new List<FeedbackDefinition>();

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);

				FeedbackDefinition definition =
					AssetDatabase.LoadAssetAtPath<FeedbackDefinition>(path);

				if (definition != null)
				{
					definitions.Add(definition);
				}
			}

			definitions.Sort(CompareDefinitions);

			return definitions.ToArray();
		}

		private static FeedbackCatalog[] LoadAllCatalogs()
		{
			string[] guids = AssetDatabase.FindAssets(
				"t:FeedbackCatalog",
				new[] { ProjectRoot }
			);

			List<FeedbackCatalog> catalogs = new List<FeedbackCatalog>();

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);

				FeedbackCatalog catalog =
					AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(path);

				if (catalog != null)
				{
					catalogs.Add(catalog);
				}
			}

			return catalogs.ToArray();
		}

		private static int CompareDefinitions(
			FeedbackDefinition left,
			FeedbackDefinition right
		)
		{
			if (left == null && right == null)
			{
				return 0;
			}

			if (left == null)
			{
				return 1;
			}

			if (right == null)
			{
				return -1;
			}

			return left.EventId.CompareTo(right.EventId);
		}
	}
}
