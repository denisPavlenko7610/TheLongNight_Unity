using TLN.Application.Feedback;
using UnityEngine;

namespace TLN.Gameplay.Feedback
{
	[CreateAssetMenu(fileName = "FeedbackDefinition", menuName = "TLN/Feedback/Feedback Definition")]
	public sealed class FeedbackDefinition : ScriptableObject
	{
		[Header("Identity")]
		[SerializeField] private FeedbackEventId _eventId;

		[Header("Audio")]
		[SerializeField] private AudioClip[] _audioClips;
		[SerializeField, Range(0f, 1f)] private float _volume = 1f;
		[SerializeField] private float _minPitch = 0.95f;
		[SerializeField] private float _maxPitch = 1.05f;
		[SerializeField, Range(0f, 1f)] private float _spatialBlend = 1f;
		[SerializeField] private float _minDistance = 1f;
		[SerializeField] private float _maxDistance = 20f;

		[Header("VFX")]
		[SerializeField] private GameObject _effectPrefab;
		[SerializeField] private float _effectLifetimeSeconds = 5f;

		public FeedbackEventId EventId => _eventId;
		public AudioClip[] AudioClips => _audioClips;
		public float Volume => _volume;
		public float MinPitch => _minPitch;
		public float MaxPitch => _maxPitch;
		public float SpatialBlend => _spatialBlend;
		public float MinDistance => _minDistance;
		public float MaxDistance => _maxDistance;
		public GameObject EffectPrefab => _effectPrefab;
		public float EffectLifetimeSeconds => _effectLifetimeSeconds;
		public bool HasAudio => _audioClips != null && _audioClips.Length > 0;
		public bool HasEffect => _effectPrefab != null;
	}
}
