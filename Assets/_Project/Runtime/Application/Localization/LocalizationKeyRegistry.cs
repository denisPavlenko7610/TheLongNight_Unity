using System;

namespace TLN.Application.Localization
{
	public static class LocalizationKeyRegistry
	{
		public static string GetTableName(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Localization key cannot be empty.", nameof(key));
			}

			if (IsUiKey(key))
			{
				return LocalizationTableNames.UI;
			}

			if (IsGameplayKey(key))
			{
				return LocalizationTableNames.Gameplay;
			}

			throw new ArgumentException($"Localization key has no table mapping: {key}", nameof(key));
		}

		private static bool IsUiKey(string key)
		{
			return key.StartsWith("common.", StringComparison.Ordinal)
				|| key.StartsWith("main_menu.", StringComparison.Ordinal)
				|| key.StartsWith("pause.", StringComparison.Ordinal)
				|| key.StartsWith("settings.", StringComparison.Ordinal)
				|| key.StartsWith("save.", StringComparison.Ordinal)
				|| key.StartsWith("hud.", StringComparison.Ordinal)
				|| key.StartsWith("sleep_window.", StringComparison.Ordinal)
				|| key.StartsWith("survival.header.", StringComparison.Ordinal)
				|| key.StartsWith("survival.section.", StringComparison.Ordinal)
				|| key.StartsWith("survival.sort.", StringComparison.Ordinal)
				|| key.StartsWith("survival.category.", StringComparison.Ordinal)
				|| key.StartsWith("survival.empty.", StringComparison.Ordinal)
				|| key.StartsWith("survival.description.", StringComparison.Ordinal)
				|| key.StartsWith("survival.build.", StringComparison.Ordinal)
				|| key.StartsWith("survival.recipe.", StringComparison.Ordinal)
				|| key.StartsWith("survival.weight.", StringComparison.Ordinal)
				|| key.StartsWith("survival.service_missing.", StringComparison.Ordinal)
				|| key.StartsWith("item.interaction_", StringComparison.Ordinal)
				|| key.StartsWith("inventory.weight_", StringComparison.Ordinal)
				|| key.StartsWith("inventory.meta_", StringComparison.Ordinal)
				|| key is "campfire.state_label"
					or "campfire.fuel_label"
					or "build.service_missing"
					or "build.recipes_missing";
		}

		private static bool IsGameplayKey(string key)
		{
			return key.StartsWith("notification.", StringComparison.Ordinal)
				|| key.StartsWith("sleep.", StringComparison.Ordinal)
				|| key.StartsWith("item.", StringComparison.Ordinal)
				|| key.StartsWith("bedroll.", StringComparison.Ordinal)
				|| key.StartsWith("campfire.", StringComparison.Ordinal)
				|| key.StartsWith("build.", StringComparison.Ordinal)
				|| key.StartsWith("equip.", StringComparison.Ordinal)
				|| key.StartsWith("inventory.", StringComparison.Ordinal)
				|| key.StartsWith("survival.", StringComparison.Ordinal);
		}
	}
}
