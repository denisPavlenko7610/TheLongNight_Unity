using System.Collections.Generic;
using TLN.Application.Feedback;
using TLN.Core.Logging;
using TLN.Core.Validation;
using UnityEngine;

namespace TLN.Gameplay.Feedback
{
	[CreateAssetMenu(fileName = "FeedbackCatalog", menuName = "TLN/Feedback/Feedback Catalog")]
	public sealed class FeedbackCatalog : ScriptableObject
	{
		[SerializeField] private FeedbackDefinition[] _definitions;

		private Dictionary<FeedbackEventId, FeedbackDefinition> _lookup;

		public IReadOnlyList<FeedbackDefinition> Definitions =>
			_definitions ?? System.Array.Empty<FeedbackDefinition>();

		[Button]
		private void RebuildLookup()
		{
			_lookup = new Dictionary<FeedbackEventId, FeedbackDefinition>();

			IReadOnlyList<FeedbackDefinition> definitions = Definitions;
			for (int i = 0; i < definitions.Count; i++)
			{
				FeedbackDefinition definition = definitions[i];

				if (definition == null || definition.EventId == FeedbackEventId.None)
				{
					continue;
				}

				if (!_lookup.TryAdd(definition.EventId, definition))
				{
					TLNLogger.LogWarning("Duplicate event: " + definition.EventId);
				}
			}
		}

		public bool TryGet(FeedbackEventId eventId, out FeedbackDefinition definition)
		{
			if (_lookup == null)
			{
				RebuildLookup();
			}

			if (_lookup.Count == 0 || eventId == FeedbackEventId.None)
			{
				definition = null;
				return false;
			}

			return _lookup.TryGetValue(eventId, out definition);
		}

		#if UNITY_EDITOR
		public void EditorSetDefinitions(FeedbackDefinition[] definitions)
		{
			_definitions = definitions == null
				? System.Array.Empty<FeedbackDefinition>()
				: (FeedbackDefinition[])definitions.Clone();
			RebuildLookup();
			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
