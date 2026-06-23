using UnityEngine.UIElements;

namespace TLN.UI.Common
{
	public static class VisualElementExtensions
	{
		public static T RequiredQ<T>(this VisualElement root, string name) where T : VisualElement
		{
			T element = root.Q<T>(name);

			if (element == null)
			{
				throw new System.InvalidOperationException(
					$"Required UI element was not found. Name: {name}, Type: {typeof(T).Name}"
				);
			}

			return element;
		}

		public static void SetVisible(this VisualElement element, bool isVisible)
		{
			if (element == null)
			{
				return;
			}

			element.style.display = isVisible
				? DisplayStyle.Flex
				: DisplayStyle.None;
		}

		public static void SetText(this Label label, string text)
		{
			if (label != null)
			{
				label.text = text;
			}
		}

		public static UIFillIcon RequiredFillIcon(this VisualElement root, string maskName, float height)
		{
			return new UIFillIcon(root.RequiredQ<VisualElement>(maskName), height);
		}
	}
}
