using UnityEngine;

namespace TLN.Gameplay.Time
{
	[CreateAssetMenu(fileName = "GameTimeConfig", menuName = "TLN/Time/Game Time Config")]
	public sealed class GameTimeConfig : ScriptableObject
	{
		[Header("Initial Time")]
		[SerializeField] private int _startDay = 1;
		[SerializeField] private int _startHour = 8;
		[SerializeField] private int _startMinute = 0;

		[Header("Random Start")]
		[SerializeField] private bool _randomStartTime = true;
		[SerializeField] private int _minRandomHour = 5;
		[SerializeField] private int _maxRandomHour = 22;

		[Header("Scale")]
		[SerializeField] private float _gameMinutesPerRealSecond = 0.2f;

		public int StartDay => _startDay;
		public int StartHour => _startHour;
		public int StartMinute => _startMinute;

		public bool RandomStartTime => _randomStartTime;
		public int MinRandomHour => _minRandomHour;
		public int MaxRandomHour => _maxRandomHour;

		public float GameMinutesPerRealSecond => _gameMinutesPerRealSecond;
	}
}
