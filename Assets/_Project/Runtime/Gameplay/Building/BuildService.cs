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

		public BuildService(
			IInventoryService inventoryService,
			PlacementService placementService,
			INotificationService notificationService
		)
		{
			_inventoryService = inventoryService;
			_placementService = placementService;
			_notificationService = notificationService;
		}

		public bool CanBuild(BuildRecipeDefinition recipe, out string failureReason)
		{
			if (recipe == null)
			{
				failureReason = Loc.RecipeMissing;
				return false;
			}

			if (recipe.PlacedPrefab == null)
			{
				failureReason = Loc.PrefabMissing;
				return false;
			}

			if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
			{
				failureReason = Loc.NoIngredients;
				return false;
			}

			for (int i = 0; i < recipe.Ingredients.Count; i++)
			{
				BuildRecipeIngredient ingredient = recipe.Ingredients[i];

				if (ingredient == null || ingredient.Item == null)
				{
					failureReason = Loc.InvalidIngredient;
					return false;
				}

				int availableAmount = CountItems(ingredient.Item);

				if (availableAmount < ingredient.Amount)
				{
					failureReason = Loc.NotEnoughItems(ingredient.Item.DisplayName, ingredient.Amount, availableAmount);

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
				return Fail(Loc.CannotBuildHere);
			}

			bool consumed = TryConsumeIngredients(recipe, out string consumeFailureReason);

			if (!consumed)
			{
				Object.Destroy(placedObject);
				return Fail(consumeFailureReason);
			}

			string message = Loc.Built(recipe.DisplayName);
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

				if (!_inventoryService.TryRemoveItem(
					ingredient.Item,
					ingredient.Amount,
					out failureReason
				))
				{
					return false;
				}
			}

			failureReason = string.Empty;
			return true;
		}
	}
}
