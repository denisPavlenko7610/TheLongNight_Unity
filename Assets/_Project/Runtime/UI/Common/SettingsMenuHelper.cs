using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using UnityEngine.UIElements;

namespace TLN.UI.Common
{
	public static class SettingsMenuHelper
	{
		private const string EnglishLanguageName = "English";
		private const string UkrainianLanguageName = "Українська";
		private const string RussianLanguageName = "Русский";

		private static readonly List<string> SupportedLanguages = new()
		{
			EnglishLanguageName,
			UkrainianLanguageName,
			RussianLanguageName
		};

		public static void ConfigureLanguageDropdown(DropdownField dropdown)
		{
			if (dropdown == null)
			{
				return;
			}

			dropdown.choices = new List<string>(SupportedLanguages);
			dropdown.SetValueWithoutNotify(EnglishLanguageName);
		}

		public static void SyncLanguageDropdown(DropdownField dropdown)
		{
			if (dropdown == null)
			{
				return;
			}

			string languageName = GetLanguageName(LocaleCodes.Current);

			dropdown.SetValueWithoutNotify(languageName);
		}

		public static string GetLocaleCode(string languageName)
		{
			return languageName switch
			{
				EnglishLanguageName => LocaleCodes.English,
				UkrainianLanguageName => LocaleCodes.Ukrainian,
				RussianLanguageName => LocaleCodes.Russian,
				_ => string.Empty
			};
		}

		public static string GetLanguageName(string localeCode)
		{
			if (string.IsNullOrWhiteSpace(localeCode))
			{
				return EnglishLanguageName;
			}

			if (localeCode.StartsWith(LocaleCodes.Ukrainian, StringComparison.OrdinalIgnoreCase))
			{
				return UkrainianLanguageName;
			}

			if (localeCode.StartsWith(LocaleCodes.Russian, StringComparison.OrdinalIgnoreCase))
			{
				return RussianLanguageName;
			}

			return EnglishLanguageName;
		}
	}
}
