using System.Diagnostics;
using UnityEngine;

namespace TLN.Core.Logging
{
	public static class TLNLogger
	{
		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void Info(string message)
		{
			UnityEngine.Debug.Log(message);
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void Info(string message, Object context)
		{
			UnityEngine.Debug.Log(message, context);
		}

		public static void Warning(string message)
		{
			UnityEngine.Debug.LogWarning(message);
		}

		public static void Warning(string message, Object context)
		{
			UnityEngine.Debug.LogWarning(message, context);
		}

		public static void Error(string message)
		{
			UnityEngine.Debug.LogError(message);
		}

		public static void Error(string message, Object context)
		{
			UnityEngine.Debug.LogError(message, context);
		}
	}
}
