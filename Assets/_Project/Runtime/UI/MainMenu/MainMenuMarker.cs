using TLN.Core.Logging;
using UnityEngine;

namespace TLN.UI.MainMenu
{
	public sealed class MainMenuMarker : MonoBehaviour
	{
		private void Start()
		{
			TLNLogger.Log("MainMenu scene loaded.");
		}
	}
}
