using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TLN.Tests.PlayMode
{
	public sealed class SmokeTests
	{
		[UnityTest]
		public IEnumerator PlayMode_SpawnsGameObject_SurvivesOneFrame()
		{
			GameObject go = new GameObject("PlayModeSmokeTest");
			yield return null;

			Assert.That(go, Is.Not.Null);
			Assert.That(go.name, Is.EqualTo("PlayModeSmokeTest"));

			if (Application.isPlaying)
			{
				Object.Destroy(go);
			}
			else
			{
				Object.DestroyImmediate(go);
			}
		}
	}
}
