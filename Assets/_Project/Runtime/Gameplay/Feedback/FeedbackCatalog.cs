using System.Collections.Generic;
using TLN.Application.Feedback;
using TLN.Core.Validation;
using UnityEngine;

namespace TLN.Gameplay.Feedback
{
	[CreateAssetMenu(fileName = "FeedbackCatalog", menuName = "TLN/Feedback/Feedback Catalog")]
	public sealed class FeedbackCatalog : ScriptableObject
	{
		[SerializeField] private FeedbackDefinition[] _definitions;

		private Dictionary<FeedbackEventId, FeedbackDefinition> _lookup;

		[Button]
		private void RebuildLookup()
		{
			_lookup = new Dictionary<FeedbackEventId, FeedbackDefinition>();

			if (_definitions == null)
			{
				return;
			}

			for (int i = 0; i < _definitions.Length; i++)
			{
				FeedbackDefinition definition = _definitions[i];

				if (definition == null || definition.EventId == FeedbackEventId.None)
				{
					continue;
				}

				_lookup[definition.EventId] = definition;
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
			_definitions = (FeedbackDefinition[])definitions?.Clone();
			RebuildLookup();
			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
