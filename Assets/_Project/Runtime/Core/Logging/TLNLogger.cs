using System.Diagnostics;
using UnityEngine;

namespace TLN.Core.Logging
{
	public static class TLNLogger
	{
		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void Log(string message)
		{
			UnityEngine.Debug.Log(message);
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void Log(string message, Object context)
		{
			UnityEngine.Debug.Log(message, context);
		}

		public static void LogWarning(string message)
		{
			UnityEngine.Debug.LogWarning(message);
		}

		public static void LogWarning(string message, Object context)
		{
			UnityEngine.Debug.LogWarning(message, context);
		}

		public static void LogError(string message)
		{
			UnityEngine.Debug.LogError(message);
		}

		public static void LogError(string message, Object context)
		{
			UnityEngine.Debug.LogError(message, context);
		}
	}
}
