using TLN.Application.Feedback;
using UnityEngine;

namespace TLN.Gameplay.Feedback
{
	[CreateAssetMenu(fileName = "FeedbackCatalog", menuName = "TLN/Feedback/Feedback Catalog")]
	public sealed class FeedbackCatalog : ScriptableObject
	{
		[SerializeField] private FeedbackDefinition[] _definitions;

		public bool TryGet(FeedbackEventId eventId, out FeedbackDefinition definition)
		{
			definition = null;

			if (eventId == FeedbackEventId.None)
			{
				return false;
			}

			if (_definitions == null)
			{
				return false;
			}

			for (int i = 0; i < _definitions.Length; i++)
			{
				FeedbackDefinition currentDefinition = _definitions[i];

				if (currentDefinition == null)
				{
					continue;
				}

				if (currentDefinition.EventId == eventId)
				{
					definition = currentDefinition;
					return true;
				}
			}

			return false;
		}

		#if UNITY_EDITOR
		public void EditorSetDefinitions(FeedbackDefinition[] definitions)
		{
			_definitions = (FeedbackDefinition[])definitions?.Clone();

			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
