using UnityEngine;

namespace TLN.Core.Utilities
{
	public static class CameraUtility
	{
		private static Camera _mainCamera;

		public static Camera GetMainCamera()
		{
			if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
			{
				_mainCamera = Camera.main;
			}

			return _mainCamera;
		}

		public static void InvalidateCache()
		{
			_mainCamera = null;
		}
	}
}
