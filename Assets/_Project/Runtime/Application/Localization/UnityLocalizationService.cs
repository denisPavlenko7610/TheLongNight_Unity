using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace TLN.Application.Localization
{
	public sealed class UnityLocalizationService : ILocalizationService, IDisposable
	{
		public string CurrentLocaleCode => LocalizationSettings.SelectedLocale?.Identifier.Code ?? string.Empty;

		public event Action LocaleChanged;

		public UnityLocalizationService()
		{
			LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
		}

		public string Get(string tableName, string entryKey, params object[] arguments)
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				throw new ArgumentException("Localization table name cannot be empty.", nameof(tableName));
			}

			if (string.IsNullOrWhiteSpace(entryKey))
			{
				throw new ArgumentException("Localization entry key cannot be empty.", nameof(entryKey));
			}

			if (arguments == null || arguments.Length == 0)
			{
				return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, entryKey);
			}

			return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, entryKey, arguments);
		}

		public bool TrySetLocale(string localeCode)
		{
			if (string.IsNullOrWhiteSpace(localeCode))
			{
				return false;
			}

			Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
			if (locale == null)
			{
				return false;
			}

			if (LocalizationSettings.SelectedLocale == locale)
			{
				return true;
			}

			LocalizationSettings.SelectedLocale = locale;
			return true;
		}

		public void Dispose()
		{
			LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
		}

		private void OnSelectedLocaleChanged(Locale locale)
		{
			LocaleChanged?.Invoke();
		}
	}
}