using System;
using System.Reflection;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Survival;
using UnityEngine;

namespace TLN.Tests.EditMode
{
	public static class TestAssetFactory
	{
		public static InventoryConfig CreateInventoryConfig(float maxCarryWeight)
		{
			InventoryConfig config = ScriptableObject.CreateInstance<InventoryConfig>();

			SetPrivateField(config, "_maxCarryWeight", maxCarryWeight);

			return config;
		}

		public static SurvivalConfig CreateSurvivalConfig(
			float initialHunger = 0f,
			float initialThirst = 0f,
			float initialFatigue = 0f,
			float initialCold = 0f,
			float initialCondition = 100f,
			float hungerPerHour = 8f,
			float thirstPerHour = 12f,
			float fatiguePerHour = 6f,
			float coldPerHour = 4f,
			float gameHoursPerRealMinute = 1f
		)
		{
			SurvivalConfig config = ScriptableObject.CreateInstance<SurvivalConfig>();

			SetPrivateField(config, "_initialHunger", initialHunger);
			SetPrivateField(config, "_initialThirst", initialThirst);
			SetPrivateField(config, "_initialFatigue", initialFatigue);
			SetPrivateField(config, "_initialCold", initialCold);
			SetPrivateField(config, "_initialCondition", initialCondition);

			SetPrivateField(config, "_hungerPerHour", hungerPerHour);
			SetPrivateField(config, "_thirstPerHour", thirstPerHour);
			SetPrivateField(config, "_fatiguePerHour", fatiguePerHour);
			SetPrivateField(config, "_coldPerHour", coldPerHour);

			SetPrivateField(config, "_gameHoursPerRealMinute", gameHoursPerRealMinute);

			return config;
		}

		public static T CreateItem<T>(
			string id,
			float weight = 1f,
			bool isStackable = true,
			int maxStackSize = 10,
			ItemUseKind useKind = ItemUseKind.None
		) where T : ItemDefinition
		{
			T item = ScriptableObject.CreateInstance<T>();

			SetPrivateField(item, "_id", id);
			SetPrivateField(item, "_weight", weight);
			SetPrivateField(item, "_isStackable", isStackable);
			SetPrivateField(item, "_maxStackSize", maxStackSize);
			SetPrivateField(item, "_useKind", useKind);

			return item;
		}

		public static ConsumableItemDefinition CreateConsumable(
			string id,
			float hungerRestore = 0f,
			float thirstRestore = 0f,
			float fatigueRestore = 0f,
			float coldRestore = 0f,
			float conditionRestore = 0f,
			float conditionDamage = 0f
		)
		{
			ConsumableItemDefinition item = CreateItem<ConsumableItemDefinition>(
				id,
				useKind: ItemUseKind.Consumable
			);

			SetPrivateField(item, "_hungerRestore", hungerRestore);
			SetPrivateField(item, "_thirstRestore", thirstRestore);
			SetPrivateField(item, "_fatigueRestore", fatigueRestore);
			SetPrivateField(item, "_coldRestore", coldRestore);
			SetPrivateField(item, "_conditionRestore", conditionRestore);
			SetPrivateField(item, "_conditionDamage", conditionDamage);

			return item;
		}

		private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
		{
			FieldInfo field = FindField(target.GetType(), fieldName);

			if (field == null)
			{
				throw new MissingFieldException(target.GetType().Name, fieldName);
			}

			field.SetValue(target, value);
		}

		private static FieldInfo FindField(Type type, string fieldName)
		{
			while (type != null)
			{
				FieldInfo field = type.GetField(
					fieldName,
					BindingFlags.Instance |
					BindingFlags.NonPublic |
					BindingFlags.DeclaredOnly
				);

				if (field != null)
				{
					return field;
				}

				type = type.BaseType;
			}

			return null;
		}
	}
}
