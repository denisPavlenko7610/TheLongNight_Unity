using System;
using System.Text;
using TLN.Gameplay.Building;
using UnityEngine.UIElements;

namespace TLN.UI.Building
{
    public sealed class BuildRecipeButtonView : IDisposable
    {
        private readonly Button _button;
        private readonly BuildRecipeDefinition _recipe;
        private readonly Action<BuildRecipeDefinition> _clicked;

        public VisualElement Root => _button;

        public BuildRecipeButtonView(
            BuildRecipeDefinition recipe,
            Action<BuildRecipeDefinition> clicked)
        {
            _recipe = recipe;
            _clicked = clicked;

            _button = new Button
            {
                text = CreateText(recipe)
            };

            _button.AddToClassList("build-recipe-button");
            _button.clicked += OnClicked;
        }

        public void Dispose()
        {
            _button.clicked -= OnClicked;
        }

        private void OnClicked()
        {
            _clicked?.Invoke(_recipe);
        }

        private static string CreateText(BuildRecipeDefinition recipe)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(recipe.DisplayName);

            if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                return builder.ToString();
            }

            builder.Append("  —  ");

            bool hasPreviousIngredient = false;

            foreach (BuildRecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null)
                {
                    continue;
                }

                if (hasPreviousIngredient)
                {
                    builder.Append(", ");
                }

                builder.Append(ingredient.Item.DisplayName);
                builder.Append(" x");
                builder.Append(ingredient.Amount);

                hasPreviousIngredient = true;
            }

            return builder.ToString();
        }
    }
}
