using System.Collections.Generic;
using TLN.Application.Feedback;
using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Gameplay.Feedback
{
	[CreateAssetMenu(fileName = "FeedbackCatalog", menuName = "TLN/Feedback/Feedback Catalog")]
	public sealed class FeedbackCatalog : ScriptableObject
	{
		[SerializeField] private FeedbackDefinition[] _definitions;

		private Dictionary<FeedbackEventId, FeedbackDefinition> _definitionsById;

		public bool TryGet(FeedbackEventId eventId, out FeedbackDefinition definition)
		{
			if (eventId == FeedbackEventId.None)
			{
				definition = null;
				return false;
			}

			EnsureCache();
			return _definitionsById.TryGetValue(eventId, out definition);
		}

		private void EnsureCache()
		{
			if (_definitionsById != null)
			{
				return;
			}

			int capacity = _definitions?.Length ?? 0;

			_definitionsById = new Dictionary<FeedbackEventId, FeedbackDefinition>(capacity);

			if (_definitions == null)
			{
				return;
			}

			for (int i = 0; i < _definitions.Length; i++)
			{
				FeedbackDefinition definition = _definitions[i];

				if (definition == null)
				{
					continue;
				}

				if (definition.EventId == FeedbackEventId.None)
				{
					TLNLogger.LogWarning(
						"FeedbackCatalog contains definition with None event id.",
						definition
					);

					continue;
				}

				if (!_definitionsById.TryAdd(definition.EventId, definition))
				{
					TLNLogger.LogWarning(
						$"Duplicate feedback event id: {definition.EventId}",
						definition
					);
				}
			}
		}

		#if UNITY_EDITOR
		public void EditorSetDefinitions(FeedbackDefinition[] definitions)
		{
			_definitions = definitions;
			_definitionsById = null;
			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
