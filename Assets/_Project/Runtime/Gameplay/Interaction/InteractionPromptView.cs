using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public class InteractionPromptView : MonoBehaviour, IInteractionPromptView
	{
		public void Show(string text)
		{
			TLNLogger.Log($"[InteractionPrompt] Show: {text}");
		}

		public void Hide()
		{
			TLNLogger.Log("[InteractionPrompt] Hide");
		}
	}
}
