using UnityEngine;

namespace TLN.Gameplay.Interaction
{
	public class InteractionPromptView : MonoBehaviour, IInteractionPromptView
	{
		public void Show(string text)
		{
			Debug.Log($"[InteractionPrompt] Show: {text}");
		}

		public void Hide()
		{
			Debug.Log("[InteractionPrompt] Hide");
		}
	}
}
