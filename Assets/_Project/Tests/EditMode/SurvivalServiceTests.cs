using NUnit.Framework;
using TLN.Gameplay.Items;
using TLN.Gameplay.Survival;

namespace TLN.Tests.EditMode
{
	public sealed class SurvivalServiceTests
	{
		private const float FloatTolerance = 0.001f;

		[Test]
		public void Tick_WhenOneRealMinutePasses_IncreasesHungerThirstAndFatigue()
		{
			SurvivalConfig config =
				TestAssetFactory.CreateSurvivalConfig(
					hungerPerHour: 8f,
					thirstPerHour: 12f,
					fatiguePerHour: 6f,
					coldPerHour: 4f,
					gameHoursPerRealMinute: 1f
				);

			SurvivalService survival = new SurvivalService(config);

			survival.Tick(60f);

			Assert.That(survival.Hunger.Value, Is.EqualTo(8f).Within(FloatTolerance));
			Assert.That(survival.Thirst.Value, Is.EqualTo(12f).Within(FloatTolerance));
			Assert.That(survival.Fatigue.Value, Is.EqualTo(6f).Within(FloatTolerance));
			Assert.That(survival.Cold.Value, Is.EqualTo(0f).Within(FloatTolerance));
		}

		[Test]
		public void ApplyConsumable_WhenConsumableRestoresNeeds_ReducesNeedsAndAppliesConditionDamage()
		{
			SurvivalConfig config =
				TestAssetFactory.CreateSurvivalConfig(
					initialHunger: 50f,
					initialThirst: 40f,
					initialFatigue: 20f,
					initialCold: 10f,
					initialCondition: 80f
				);

			ConsumableItemDefinition food =
				TestAssetFactory.CreateConsumable(
					"canned_food",
					hungerRestore: 15f,
					thirstRestore: 5f,
					conditionDamage: 3f
				);

			SurvivalService survival = new SurvivalService(config);

			survival.ApplyConsumable(food);

			Assert.That(survival.Hunger.Value, Is.EqualTo(35f).Within(FloatTolerance));
			Assert.That(survival.Thirst.Value, Is.EqualTo(35f).Within(FloatTolerance));
			Assert.That(survival.Fatigue.Value, Is.EqualTo(20f).Within(FloatTolerance));
			Assert.That(survival.Cold.Value, Is.EqualTo(10f).Within(FloatTolerance));
			Assert.That(survival.Condition.Value, Is.EqualTo(77f).Within(FloatTolerance));
		}

		[Test]
		public void DamageCondition_WhenDamageIsGreaterThanCurrentCondition_ClampsConditionToZero()
		{
			SurvivalConfig config =
				TestAssetFactory.CreateSurvivalConfig(
					initialCondition: 10f
				);

			SurvivalService survival = new SurvivalService(config);

			survival.DamageCondition(100f);

			Assert.That(survival.Condition.Value, Is.EqualTo(0f).Within(FloatTolerance));
		}

		[Test]
		public void AddCold_WhenAmountIsPositive_IncreasesCold()
		{
			SurvivalConfig config =
				TestAssetFactory.CreateSurvivalConfig(
					initialCold: 10f
				);

			SurvivalService survival = new SurvivalService(config);

			survival.AddCold(5f);

			Assert.That(survival.Cold.Value, Is.EqualTo(15f).Within(FloatTolerance));
		}

		[Test]
		public void ReduceCold_WhenAmountIsGreaterThanCurrentCold_ClampsColdToZero()
		{
			SurvivalConfig config =
				TestAssetFactory.CreateSurvivalConfig(
					initialCold: 10f
				);

			SurvivalService survival = new SurvivalService(config);

			survival.ReduceCold(100f);

			Assert.That(survival.Cold.Value, Is.EqualTo(0f).Within(FloatTolerance));
		}
	}
}
