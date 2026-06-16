using UnityEngine;

namespace TLN.Gameplay.Weather
{
	[CreateAssetMenu(fileName = "SnowConfig", menuName = "TLN/Snow Config")]
	public sealed class SnowConfig : ScriptableObject
	{
		[SerializeField, Range(1000, 50000)] private int _maxParticles = 10000;
		[SerializeField, Range(10f, 80f)] private float _radius = 35f;
		[SerializeField, Range(5f, 50f)] private float _height = 25f;
		[SerializeField, Range(1f, 20f)] private float _fallDepth = 5f;
		[SerializeField, Range(0.1f, 3f)] private float _gravity = 0.8f;
		[SerializeField, Range(0f, 1f)] private float _windInfluence = 0.6f;
		[SerializeField, Range(0f, 2f)] private float _turbulence = 0.7f;
		[SerializeField] private Color _snowColor = new Color(0.92f, 0.94f, 1f, 1f);
		[SerializeField, Range(0.005f, 0.1f)] private float _minSize = 0.015f;
		[SerializeField, Range(0.01f, 0.15f)] private float _maxSize = 0.06f;
		[SerializeField, Range(0f, 1f)] private float _softness = 0.6f;
		[SerializeField, Range(0f, 1f)] private float _sparkleIntensity = 0.25f;
		[SerializeField, Range(10f, 100f)] private float _fadeDistance = 45f;

		public int MaxParticles => _maxParticles;
		public float Radius => _radius;
		public float Height => _height;
		public float FallDepth => _fallDepth;
		public float Gravity => _gravity;
		public float WindInfluence => _windInfluence;
		public float Turbulence => _turbulence;
		public Color SnowColor => _snowColor;
		public float MinSize => _minSize;
		public float MaxSize => _maxSize;
		public float Softness => _softness;
		public float SparkleIntensity => _sparkleIntensity;
		public float FadeDistance => _fadeDistance;
	}
}
