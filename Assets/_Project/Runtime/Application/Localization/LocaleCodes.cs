using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace TLN.Application.Localization
{
	public static class LocaleCodes
	{
		public const string English = "en";
		public const string Ukrainian = "uk";
		public const string Russian = "ru";

		private static readonly Dictionary<Action, Action<Locale>> _localeChangedWrappers = new();

		public static string Current =>
			LocalizationSettings.SelectedLocale?.Identifier.Code ?? string.Empty;

		public static event Action LocaleChanged
		{
			add
			{
				Action<Locale> wrapper = _ => value();
				_localeChangedWrappers[value] = wrapper;
				LocalizationSettings.SelectedLocaleChanged += wrapper;
			}
			remove
			{
				if (_localeChangedWrappers.TryGetValue(value, out Action<Locale> wrapper))
				{
					LocalizationSettings.SelectedLocaleChanged -= wrapper;
					_localeChangedWrappers.Remove(value);
				}
			}
		}

		public static bool TrySetLocale(string code)
		{
			Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
			if (locale == null)
			{
				return false;
			}
			LocalizationSettings.SelectedLocale = locale;
			return true;
		}
	}
}
