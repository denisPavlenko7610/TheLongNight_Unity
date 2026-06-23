using TLN.Application.Input;
using UnityEngine;

namespace TLN.Infrastructure.Input
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
