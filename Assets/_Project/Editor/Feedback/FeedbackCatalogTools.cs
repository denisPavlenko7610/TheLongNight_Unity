using System.Collections.Generic;
using TLN.Application.Feedback;
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
			FeedbackDefinition[] definitions = LoadDefinitions();
			FeedbackCatalog[] catalogs = LoadAssets<FeedbackCatalog>();

			if (definitions.Length == 0)
			{
				Debug.LogWarning("No FeedbackDefinition assets found.");
			}

			if (catalogs.Length == 0)
			{
				Debug.LogWarning("No FeedbackCatalog assets found.");
				return;
			}

			FeedbackDefinition[] catalogDefinitions = BuildCatalogDefinitions(definitions);

			for (int i = 0; i < catalogs.Length; i++)
			{
				catalogs[i].EditorSetDefinitions(catalogDefinitions);
			}

			AssetDatabase.SaveAssets();

			Debug.Log(
				$"Feedback catalogs rebuilt. Catalogs: {catalogs.Length}. Definitions: {catalogDefinitions.Length}."
			);
		}

		private static FeedbackDefinition[] LoadDefinitions()
		{
			FeedbackDefinition[] definitions = LoadAssets<FeedbackDefinition>();
			System.Array.Sort(definitions, (left, right) => left.EventId.CompareTo(right.EventId));

			return definitions;
		}

		private static TAsset[] LoadAssets<TAsset>() where TAsset : Object
		{
			string[] guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new[] { ProjectRoot });

			List<TAsset> assets = new();

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);

				if (asset != null)
				{
					assets.Add(asset);
				}
			}

			return assets.ToArray();
		}

		private static FeedbackDefinition[] BuildCatalogDefinitions(FeedbackDefinition[] definitions)
		{
			Dictionary<FeedbackEventId, FeedbackDefinition> definitionsById = new();
			List<FeedbackDefinition> catalogDefinitions = new();

			for (int i = 0; i < definitions.Length; i++)
			{
				FeedbackDefinition definition = definitions[i];

				if (definition.EventId == FeedbackEventId.None)
				{
					Debug.LogWarning(
						"FeedbackDefinition has None event id.",
						definition
					);

					continue;
				}

				if (definitionsById.TryGetValue(definition.EventId, out FeedbackDefinition existingDefinition))
				{
					Debug.LogWarning(
						$"Duplicate FeedbackEventId: {definition.EventId}. Existing: {existingDefinition.name}. Duplicate: {definition.name}.",
						definition
					);

					continue;
				}

				definitionsById.Add(definition.EventId, definition);
				catalogDefinitions.Add(definition);
			}

			return catalogDefinitions.ToArray();
		}
	}
}
