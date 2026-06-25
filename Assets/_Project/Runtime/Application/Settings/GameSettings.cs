using System;
using TLN.Application.Localization;

namespace TLN.Application.Settings
{
	[Serializable]
	public sealed class GameSettings
	{
		public float MasterVolume = 1f;
		public float SfxVolume = 1f;
		public float MusicVolume = 0.8f;
		public float AmbientVolume = 1f;

		public int DisplayMode;
		public int ResolutionWidth = 1920;
		public int ResolutionHeight = 1080;
		public int RefreshRate = 60;
		public int QualityLevel = 2;
		public int TextureQuality = 2;
		public float FieldOfView = 75f;
		public float Brightness = 50f;
		public bool VSyncEnabled;

		public float MouseSensitivity = 0.5f;
		public float LookSmoothing = 0.3f;
		public bool InvertMouseY;

		public bool SubtitlesEnabled = true;
		public bool AutoWalkEnabled;
		public bool AutoHarvestEnabled;
		public string LocaleCode = LocaleCodes.English;
	}
}
