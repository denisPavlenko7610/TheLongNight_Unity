using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using UnityEngine;

namespace TLN.Gameplay.Building
{
	public sealed class BuildService : IBuildService
	{
		private readonly IInventoryService _inventoryService;
		private readonly PlacementService _placementService;
		private readonly INotificationService _notificationService;
		private readonly ILocalizationService _localizationService;

		public BuildService(
			IInventoryService inventoryService,
			PlacementService placementService,
			INotificationService notificationService,
			ILocalizationService localizationService
		)
		{
			_inventoryService = inventoryService;
			_placementService = placementService;
			_notificationService = notificationService;
			_localizationService = localizationService;
		}

		public bool CanBuild(BuildRecipeDefinition recipe, out string failureReason)
		{
			if (recipe == null)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Build.RecipeMissing);
				return false;
			}

			if (recipe.PlacedPrefab == null)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Build.PrefabMissing);
				return false;
			}

			if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Build.NoIngredients);
				return false;
			}

			for (int i = 0; i < recipe.Ingredients.Count; i++)
			{
				BuildRecipeIngredient ingredient = recipe.Ingredients[i];

				if (ingredient == null || ingredient.Item == null)
				{
					failureReason = _localizationService.Get(LocalizationKeys.Build.InvalidIngredient);
					return false;
				}

				int availableAmount = CountItems(ingredient.Item);

				if (availableAmount < ingredient.Amount)
				{
					failureReason = _localizationService.Get(LocalizationKeys.Build.NotEnoughItems,
						ingredient.Item.DisplayName, ingredient.Amount, availableAmount);

					return false;
				}
			}

			failureReason = string.Empty;
			return true;
		}

		public BuildResult Build(BuildRecipeDefinition recipe)
		{
			if (!CanBuild(recipe, out string failureReason))
			{
				return Fail(failureReason);
			}

			bool wasPlaced = _placementService.TryPlace(recipe.PlacedPrefab, recipe.PlaceDistance, out GameObject placedObject);
			if (!wasPlaced)
			{
				return Fail(_localizationService.Get(LocalizationKeys.Build.CannotBuildHere));
			}

			bool consumed = TryConsumeIngredients(recipe, out string consumeFailureReason);

			if (!consumed)
			{
				Object.Destroy(placedObject);
				return Fail(consumeFailureReason);
			}

			string message = _localizationService.Get(LocalizationKeys.Build.Built, recipe.DisplayName);
			_notificationService.Show(message);
			return BuildResult.Success(message);
		}

		private BuildResult Fail(string message)
		{
			_notificationService.Show(message);
			return BuildResult.Failure(message);
		}

		private int CountItems(ItemDefinition item)
		{
			int amount = 0;

			for (int i = 0; i < _inventoryService.Items.Count; i++)
			{
				ItemStack stack = _inventoryService.Items[i];

				if (stack.Definition.Id == item.Id)
				{
					amount += stack.Amount;
				}
			}

			return amount;
		}

		private bool TryConsumeIngredients(BuildRecipeDefinition recipe, out string failureReason)
		{
			for (int i = 0; i < recipe.Ingredients.Count; i++)
			{
				BuildRecipeIngredient ingredient = recipe.Ingredients[i];

				bool consumed = TryConsumeItem(
					ingredient.Item,
					ingredient.Amount,
					out failureReason
				);

				if (!consumed)
				{
					return false;
				}
			}

			failureReason = string.Empty;
			return true;
		}

		private bool TryConsumeItem(ItemDefinition item, int amount, out string failureReason)
		{
			int remainingAmount = amount;

			for (int i = _inventoryService.Items.Count - 1; i >= 0; i--)
			{
				if (remainingAmount <= 0)
				{
					break;
				}

				ItemStack stack = _inventoryService.Items[i];

				if (stack.Definition.Id != item.Id)
				{
					continue;
				}

				int amountToRemove = Mathf.Min(remainingAmount, stack.Amount);

				bool removed = _inventoryService.TryRemoveItemAt(
					i,
					amountToRemove,
					out failureReason
				);

				if (!removed)
				{
					return false;
				}

				remainingAmount -= amountToRemove;
			}

			if (remainingAmount > 0)
			{
				failureReason = _localizationService.Get(LocalizationKeys.Build.NotEnoughItem, item.DisplayName);
				return false;
			}

			failureReason = string.Empty;
			return true;
		}
	}
}
