using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using TLN.Application.Notifications;
using TLN.Gameplay.Inventory;
using TLN.Gameplay.Items;
using TLN.Gameplay.Placement;
using UnityEngine;
using Object = UnityEngine.Object;

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

			if (recipe.Ingredients.Count == 0)
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

				long requiredAmount = CountRequiredAmount(recipe, ingredient.Item);

				int availableAmount = _inventoryService.CountItems(ingredient.Item);

				if (availableAmount < requiredAmount)
				{
					failureReason = Loc.NotEnoughItems(ingredient.Item.DisplayName, (int)Math.Min(requiredAmount, int.MaxValue), availableAmount);

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

		private static long CountRequiredAmount(BuildRecipeDefinition recipe, ItemDefinition item)
		{
			long amount = 0;
			for (int i = 0; i < recipe.Ingredients.Count; i++)
			{
				BuildRecipeIngredient ingredient = recipe.Ingredients[i];
				if (ingredient?.Item != null && ingredient.Item.Id == item.Id)
					amount += ingredient.Amount;
			}

			return amount;
		}

		private BuildResult Fail(string message)
		{
			_notificationService.Show(message);
			return BuildResult.Failure(message);
		}

		private bool TryConsumeIngredients(BuildRecipeDefinition recipe, out string failureReason)
		{
			// Placement callbacks may have changed the inventory since the initial check.
			if (!CanBuild(recipe, out failureReason))
				return false;

			List<ItemStack> remaining = new(_inventoryService.Items);
			foreach (BuildRecipeIngredient ingredient in recipe.Ingredients)
			{
				int amount = ingredient.Amount;
				for (int i = remaining.Count - 1; i >= 0 && amount > 0; i--)
				{
					ItemStack stack = remaining[i];
					if (stack.Definition.Id != ingredient.Item.Id)
						continue;

					int removed = Math.Min(amount, stack.Amount);
					stack.RemoveAmount(removed);
					amount -= removed;
					if (stack.Amount == 0)
						remaining.RemoveAt(i);
					else
						remaining[i] = stack;
				}
			}

			_inventoryService.ReplaceItems(remaining);
			failureReason = string.Empty;
			return true;
		}
	}
}
