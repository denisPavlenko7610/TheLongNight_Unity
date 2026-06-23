using System;
using System.Collections.Generic;
using TLN.Application.Localization;
using UnityEngine.UIElements;

namespace TLN.UI.Common
{
	public static class SettingsMenuHelper
	{
		public const string EnglishLanguageName = "English";
		public const string UkrainianLanguageName = "Українська";
		public const string RussianLanguageName = "Русский";

		public static readonly List<string> SupportedLanguages = new()
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

			dropdown.choices = SupportedLanguages;
			dropdown.SetValueWithoutNotify(EnglishLanguageName);
		}

		public static void SyncLanguageDropdown(
			DropdownField dropdown,
			ILocalizationService localizationService
		)
		{
			if (dropdown == null || localizationService == null)
			{
				return;
			}

			string languageName = GetLanguageName(localizationService.CurrentLocaleCode);

			dropdown.SetValueWithoutNotify(languageName);
		}

		public static string GetLocaleCode(string languageName)
		{
			return languageName switch
			{
				EnglishLanguageName => "en",
				UkrainianLanguageName => "uk",
				RussianLanguageName => "ru",
				_ => string.Empty
			};
		}

		public static string GetLanguageName(string localeCode)
		{
			if (string.IsNullOrWhiteSpace(localeCode))
			{
				return EnglishLanguageName;
			}

			if (localeCode.StartsWith("uk", StringComparison.OrdinalIgnoreCase))
			{
				return UkrainianLanguageName;
			}

			if (localeCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
			{
				return RussianLanguageName;
			}

			return EnglishLanguageName;
		}
	}
}
