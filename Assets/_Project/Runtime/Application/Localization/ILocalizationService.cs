using System;
using TLN.Core.Lifetime;

namespace TLN.Application.Localization
{
	public interface ILocalizationService : IGameService
	{
		string CurrentLocaleCode { get; }

		event Action LocaleChanged;

		string Get(string entryKey, params object[] arguments);

		bool TrySetLocale(string localeCode);
	}
}
