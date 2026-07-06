using System;
using UnityEngine.Localization.Settings;

namespace TLN.Application.Localization
{
	public static class LocaleCodes
	{
		public const string English = "en";
		public const string Ukrainian = "uk";
		public const string Russian = "ru";

		private static event Action LocaleChangedHandlers;

		static LocaleCodes()
		{
			LocalizationSettings.SelectedLocaleChanged += _ => LocaleChangedHandlers?.Invoke();
		}

		public static string Current =>
			LocalizationSettings.SelectedLocale?.Identifier.Code ?? string.Empty;

		public static event Action LocaleChanged
		{
			add => LocaleChangedHandlers += value;
			remove => LocaleChangedHandlers -= value;
		}

		public static bool TrySetLocale(string code)
		{
			var locale = LocalizationSettings.AvailableLocales.GetLocale(code);

			if (locale == null)
			{
				return false;
			}

			LocalizationSettings.SelectedLocale = locale;
			return true;
		}
	}
}
