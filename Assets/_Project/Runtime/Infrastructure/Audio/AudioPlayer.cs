using TLN.Application.Audio;
using TLN.Core.Validation;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace TLN.Infrastructure.Audio
{
	[RequireComponent(typeof(AudioSource))]
	public sealed class AudioPlayer : MonoBehaviour
	{
		[SerializeField, Required, FormerlySerializedAs("_mainTheme")] private AudioClip _clip;
		[SerializeField] private AudioBusId _audioBusId = AudioBusId.Music;
		[SerializeField, Range(0f, 1f)] private float _volume = 1f;
		[SerializeField] private bool _loop = true;
		[SerializeField] private bool _playOnEnable = true;
		[SerializeField] private bool _stopOnDisable = true;
		[SerializeField, Range(0f, 1f)] private float _spatialBlend = 0f;

		private IAudioMixerService _audioMixerService;
		private AudioSource _source;

		public AudioSource Source
		{
			get
			{
				EnsureSource();
				return _source;
			}
		}

		public bool IsPlaying => Source != null && Source.isPlaying;

		[Inject]
		public void Construct(IAudioMixerService audioMixerService)
		{
			_audioMixerService = audioMixerService;

			ConfigureSource();

			if (isActiveAndEnabled && _playOnEnable)
			{
				Play();
			}
		}

		private void Awake()
		{
			EnsureSource();
			ConfigureSource();
		}

		private void OnEnable()
		{
			if (_playOnEnable)
			{
				Play();
			}
		}

		private void OnDisable()
		{
			if (_stopOnDisable)
			{
				Stop();
			}
		}

		public void Play()
		{
			EnsureSource();

			if (_source == null || _clip == null)
			{
				return;
			}

			bool isPlayingRequestedClip = _source.isPlaying && _source.clip == _clip;
			ConfigureSource();

			if (isPlayingRequestedClip)
			{
				return;
			}

			_source.Play();
		}

		public void Play(AudioClip clip)
		{
			_clip = clip;
			Play();
		}

		public void Pause()
		{
			_source?.Pause();
		}

		public void Resume()
		{
			ConfigureSource();
			_source?.UnPause();
		}

		public void Stop()
		{
			if (_source != null && _source.isPlaying)
			{
				_source.Stop();
			}
		}

		private void ConfigureSource()
		{
			EnsureSource();

			if (_source == null)
			{
				return;
			}

			_source.playOnAwake = false;
			_source.clip = _clip;
			_source.volume = _volume;
			_source.loop = _loop;
			_source.spatialBlend = _spatialBlend;

			_audioMixerService?.AssignMixerGroup(_source, _audioBusId);
		}

		private void EnsureSource()
		{
			if (_source == null)
			{
				_source = GetComponent<AudioSource>();
			}
		}

		#if UNITY_EDITOR
		private void OnValidate()
		{
			EnsureSource();
			ConfigureSource();
		}
		#endif
	}
}
