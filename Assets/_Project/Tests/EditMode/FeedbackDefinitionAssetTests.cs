using System.Collections.Generic;
using NUnit.Framework;
using TLN.Gameplay.Feedback;
using UnityEditor;
using UnityEngine;

namespace TLN.Tests.EditMode
{
	public sealed class FeedbackDefinitionAssetTests
	{
		private const string ProjectRoot = "Assets/_Project";

		[Test]
		public void FeedbackDefinitions_DoNotUseInvalidOneShotVfx()
		{
			List<string> errors = new();
			string[] guids = AssetDatabase.FindAssets(
				"t:FeedbackDefinition",
				new[] { ProjectRoot }
			);

			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				FeedbackDefinition definition =
					AssetDatabase.LoadAssetAtPath<FeedbackDefinition>(path);

				if (definition == null || !definition.HasEffect)
				{
					continue;
				}

				int eventId = GetEventId(definition);

				if (IsPlayedWithoutPosition(eventId))
				{
					errors.Add(
						$"{path}: event {eventId} has VFX but is played through Play() without position."
					);
				}

				ParticleSystem[] particleSystems =
					definition.EffectPrefab.GetComponentsInChildren<ParticleSystem>(true);

				for (int j = 0; j < particleSystems.Length; j++)
				{
					ParticleSystem particleSystem = particleSystems[j];

					if (particleSystem != null && particleSystem.main.loop)
					{
						errors.Add(
							$"{path}: event {eventId} uses looping ParticleSystem '{particleSystem.name}'."
						);
					}
				}
			}

			Assert.That(errors, Is.Empty, string.Join("\n", errors));
		}

		private static int GetEventId(FeedbackDefinition definition)
		{
			return (int)definition.GetType()
				.GetProperty("EventId")
				.GetValue(definition);
		}

		private static bool IsPlayedWithoutPosition(int eventId)
		{
			return eventId == 100
			       || eventId == 110
			       || eventId == 200;
		}
	}
}
