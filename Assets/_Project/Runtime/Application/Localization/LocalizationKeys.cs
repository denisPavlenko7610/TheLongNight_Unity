namespace TLN.Application.Localization
{
	public static class LocalizationKeys
	{
		public static class Common
		{
			public const string Back = "common.back";
			public const string Use = "common.use";
			public const string Build = "common.build";
		}

		public static class SurvivalMenu
		{
			public const string HeaderBackpack = "survival.header.backpack";
			public const string HeaderCrafting = "survival.header.crafting";
			public const string HeaderFoodWater = "survival.header.food_water";
			public const string HeaderMedicine = "survival.header.medicine";
			public const string HeaderTools = "survival.header.tools";
			public const string HeaderClothing = "survival.header.clothing";
			public const string HeaderFire = "survival.header.fire";
			public const string SectionItems = "survival.section.items";
			public const string SortAlphabetically = "survival.sort.alphabetically";
			public const string CategoryAll = "survival.category.all";
			public const string CategoryFoodWater = "survival.category.food_water";
			public const string CategoryMedicine = "survival.category.medicine";
			public const string CategoryTools = "survival.category.tools";
			public const string CategoryClothing = "survival.category.clothing";
			public const string CategoryFire = "survival.category.fire";
			public const string CategoryCrafting = "survival.category.crafting";
			public const string EmptyBackpack = "survival.empty.backpack";
			public const string EmptyCategory = "survival.empty.category";
			public const string NothingSelected = "survival.empty.nothing_selected";
			public const string NoRecipeSelected = "survival.empty.no_recipe_selected";
			public const string DescriptionConsumable = "survival.description.consumable";
			public const string DescriptionPlaceable = "survival.description.placeable";
			public const string DescriptionClothing = "survival.description.clothing";
			public const string DescriptionNoAction = "survival.description.no_action";
			public const string BuildReady = "survival.build.ready";
			public const string BuildServiceMissing = "survival.build.service_missing";
			public const string RequirementsNone = "survival.recipe.requirements_none";
			public const string RequirementsFormat = "survival.recipe.requirements_format";
			public const string WeightFormat = "survival.weight.format";
			public const string ServiceMissingItemUse = "survival.service_missing.item_use";
			public const string ServiceMissingBuild = "survival.service_missing.build";
		}

		public static class Notifications
		{
			public const string Hunger = "notification.hunger";
			public const string Thirst = "notification.thirst";
			public const string Exhausted = "notification.exhausted";
			public const string Freezing = "notification.freezing";
			public const string ConditionCritical = "notification.condition_critical";
		}

		public static class Sleep
		{
			public const string MinHours = "sleep.min_hours";
			public const string MaxHours = "sleep.max_hours";
			public const string Result = "sleep.result";
		}

		public static class Items
		{
			public const string InvalidSlot = "item.invalid_slot";
			public const string CannotUse = "item.cannot_use";
			public const string CannotConsume = "item.cannot_consume";
			public const string CannotPlace = "item.cannot_place";
			public const string CannotEquip = "item.cannot_equip";
			public const string Placing = "item.placing";
			public const string Placed = "item.placed";
			public const string Used = "item.used";
			public const string CannotPlaceHere = "item.cannot_place_here";
			public const string AddressableServiceMissing = "item.addressable_service_missing";
			public const string PrefabReferenceMissing = "item.prefab_reference_missing";
			public const string PrefabLoadFailed = "item.prefab_load_failed";
			public const string EquipmentServiceMissing = "item.equipment_service_missing";
			public const string PickedUp = "item.picked_up";
			public const string InteractionPickup = "item.interaction_pickup";
			public const string InteractionPickupFormat = "item.interaction_pickup_format";
		}

		public static class Bedroll
		{
			public const string PickupFailed = "bedroll.pickup_failed";
			public const string InventoryMissing = "bedroll.inventory_service_missing";
			public const string PickedUp = "bedroll.picked_up";
		}

		public static class Campfire
		{
			public const string FuelMissing = "campfire.fuel_missing";
			public const string FuelAmountZero = "campfire.fuel_amount_zero";
			public const string CannotBurn = "campfire.cannot_burn";
			public const string Full = "campfire.full";
			public const string AlreadyBurning = "campfire.already_burning";
			public const string NotEnoughFuel = "campfire.not_enough_fuel";
			public const string NotBurning = "campfire.not_burning";
			public const string NoFuelInInventory = "campfire.no_fuel_in_inventory";
			public const string FuelAdded = "campfire.fuel_added";
			public const string FireStarted = "campfire.fire_started";
			public const string FireExtinguished = "campfire.fire_extinguished";
			public const string StateLabel = "campfire.state_label";
			public const string FuelLabel = "campfire.fuel_label";
		}

		public static class Build
		{
			public const string RecipeMissing = "build.recipe_missing";
			public const string PrefabMissing = "build.prefab_missing";
			public const string NoIngredients = "build.no_ingredients";
			public const string InvalidIngredient = "build.invalid_ingredient";
			public const string NotEnoughItems = "build.not_enough_items";
			public const string NotEnoughItem = "build.not_enough_item";
			public const string CannotBuildHere = "build.cannot_build_here";
			public const string Built = "build.built";
			public const string ServiceMissing = "build.service_missing";
			public const string RecipesMissing = "build.recipes_missing";
		}

		public static class Equipment
		{
			public const string ItemMissing = "equip.item_missing";
			public const string SlotMissing = "equip.slot_missing";
			public const string Unequipped = "equip.unequipped";
			public const string NoFreeSlot = "equip.no_free_slot";
			public const string Equipped = "equip.equipped";
		}

		public static class Survival
		{
			public const string WolfAttack = "survival.wolf_attack";
		}

		public static class Saves
		{
			public const string ManualUnavailable = "save.manual_unavailable";
			public const string Failed = "save.failed";
			public const string Saved = "save.saved";
			public const string SlotEmpty = "save.slot_empty";
			public const string Loaded = "save.loaded";
			public const string NewGame = "save.new_game";
			public const string NewGameDescription = "save.new_game_description";
			public const string LoadGame = "save.load_game";
			public const string LoadGameDescription = "save.load_game_description";
			public const string Empty = "save.empty";
			public const string SlotFormat = "save.slot_format";
			public const string DefaultReason = "save.default_reason";
			public const string OverwriteLabel = "save.overwrite_label";
			public const string DetailsFormat = "save.details_format";
			public const string DetailsFormatWithDate = "save.details_format_with_date";
			public const string DayTimeFormat = "save.day_time_format";
		}

		public static class Inventory
		{
			public const string InvalidItem = "inventory.invalid_item";
			public const string InvalidAmount = "inventory.invalid_amount";
			public const string TooHeavy = "inventory.too_heavy";
			public const string InvalidSlot = "inventory.invalid_slot";
			public const string NotEnough = "inventory.not_enough";
			public const string NotEnoughItem = "inventory.not_enough_item";
			public const string WeightKg = "inventory.weight_kg";
			public const string MetaDefault = "inventory.meta_default";
			public const string MetaClothing = "inventory.meta_clothing";
		}

		public static class HUD
		{
			public const string Day = "hud.day";
			public const string Morning = "hud.morning";
			public const string Afternoon = "hud.afternoon";
			public const string Evening = "hud.evening";
			public const string Night = "hud.night";
			public const string InteractionPrompt = "hud.interaction_prompt";
		}
	}
}