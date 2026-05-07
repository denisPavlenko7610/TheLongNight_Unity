using UnityEngine;

namespace TLN.Application.Input
{
	public sealed class CursorService : ICursorService
	{
		public void LockGameplayCursor()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		public void UnlockUICursor()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
