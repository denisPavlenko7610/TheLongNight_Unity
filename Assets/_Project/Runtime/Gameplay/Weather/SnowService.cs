using System.Runtime.InteropServices;
using TLN.Core.Logging;
using TLN.Gameplay.Player;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;

namespace TLN.Gameplay.Weather
{
	public sealed class SnowService : MonoBehaviour
	{
		private const float DefaultRadius = 40f;
		private const float DefaultHeight = 30f;
		private const float DefaultFallDepth = 8f;
		private const float DefaultGravity = 0.6f;
		private const float DefaultWindInfluence = 0.7f;
		private const float DefaultTurbulence = 1.2f;
		private const float DefaultMinSize = 0.01f;
		private const float DefaultMaxSize = 0.083f;
		private const float DefaultSoftness = 0.7f;
		private const float DefaultSparkleIntensity = 0.15f;
		private const float DefaultFadeDistance = 45f;
		private const float DefaultDepthFade = 0.5f;
		private const int DefaultMaxParticles = 25000;
		private static readonly Color DefaultSnowColor = new Color(0.88f, 0.91f, 0.98f);

		private const float MaxDeltaTime = 0.1f;
		private const int ComputeThreadGroupSize = 64;
		private const float WindForceMultiplier = 2f;
		private const float SparkleFrequency = 2f;
		private const float DrawBoundsPadding = 2.2f;
		private const float MaxSizePadding = 4f;
		private const float FollowSearchCooldown = 0.5f;
		private const float SeedOffsetMultiplier = 997.3f;
		private const int FillSeed = 42;
		private const float MaxRandomSeed = 10000f;

		private const float GustFrequency1 = 0.7f;
		private const float GustAmplitude1 = 0.3f;
		private const float GustFrequency2 = 1.3f;
		private const float GustPhase2 = 1.5f;
		private const float GustAmplitude2 = 0.2f;

		private const float InitVelocityRangeXZ = 0.5f;
		private const float InitVelocityMinY = -2f;
		private const float InitVelocityMaxY = -0.2f;

		private const float SnowflakeSizePow = 1.35f;
		private const float SnowflakeLargeChanceThreshold = 0.92f;
		private const float SnowflakeLargeBlend = 0.65f;
		private const float SnowflakeSmallChanceThreshold = 0.18f;
		private const float SnowflakeSmallMultiplier = 0.35f;

		[SerializeField] private SnowConfig _config;
		[SerializeField] private Shader _snowShader;
		[SerializeField] private Transform _followTarget;
		[SerializeField] private bool _autoFollowPlayerCamera = true;
		[SerializeField] [Range(0, 1)] private float _intensity = 0.5f;
		[SerializeField] private Vector3 _windDirection = new Vector3(0.5f, 0, 0.3f);
		[SerializeField] [Range(0, 1)] private float _windStrength = 0.5f;
		[SerializeField] private bool _active = true;

		[StructLayout(LayoutKind.Sequential)]
		private struct Particle
		{
			public Vector3 position;
			public float seed;
			public Vector3 velocity;
			public float size;
		}

		private ComputeShader _computeShader;
		private Material _material;
		private Mesh _quadMesh;
		private ComputeBuffer _particleBuffer;
		private ComputeBuffer _argsBuffer;
		private int _kernelIndex;
		private int _maxParticles;
		private bool _initialized;

		private float _radius, _height, _fallDepth, _gravity, _windInfluence, _turbulence;
		private float _minSize, _maxSize, _softness, _sparkleIntensity, _depthFade, _fadeDistance;
		private Color _color;
		private float _seedOffset;
		private Vector3 _lastVolumeCenter;
		private float _nextFollowSearchTime;
		private PlayerRoot _playerRoot;
		private readonly uint[] _argsData = new uint[5];

		private Vector3 _cachedWindForce;
		private Vector3 _cachedWindDirectionNormalized;
		private Bounds _cachedDrawBounds;
		private uint _cachedIndexCount;
		private Particle[] _particles;
		private MaterialPropertyBlock _mpb;
		private int _lastArgsActiveCount = -1;

		private static readonly int PropSnowParticles = Shader.PropertyToID("_SnowParticles");
		private static readonly int PropVolumeCenter = Shader.PropertyToID("_VolumeCenter");
		private static readonly int PropVolumeHalfExtents = Shader.PropertyToID("_VolumeHalfExtents");
		private static readonly int PropWind = Shader.PropertyToID("_Wind");
		private static readonly int PropGravity = Shader.PropertyToID("_Gravity");
		private static readonly int PropTurbulence = Shader.PropertyToID("_Turbulence");
		private static readonly int PropDeltaTime = Shader.PropertyToID("_DeltaTime");
		private static readonly int PropParticleCount = Shader.PropertyToID("_ParticleCount");
		private static readonly int PropTime = Shader.PropertyToID("_Time");
		private static readonly int PropSeedOffset = Shader.PropertyToID("_SeedOffset");
		private static readonly int PropMinSize = Shader.PropertyToID("_MinSize");
		private static readonly int PropMaxSize = Shader.PropertyToID("_MaxSize");
		private static readonly int PropColor = Shader.PropertyToID("_Color");
		private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
		private static readonly int PropSparkleIntensity = Shader.PropertyToID("_SparkleIntensity");
		private static readonly int PropSparkleFrequency = Shader.PropertyToID("_SparkleFrequency");
		private static readonly int PropTimeGlobal = Shader.PropertyToID("_TimeGlobal");
		private static readonly int PropDepthFade = Shader.PropertyToID("_DepthFade");
		private static readonly int PropFadeDistance = Shader.PropertyToID("_FadeDistance");

		public float Intensity
		{
			get => _intensity;
			set => _intensity = Mathf.Clamp01(value);
		}
		public Transform Follow
		{
			get => _followTarget;
			set => AssignFollowTarget(value, true);
		}
		public bool Active
		{
			get => _active;
			set => _active = value;
		}

		public void SetWind(Vector3 direction, float strength)
		{
			_cachedWindDirectionNormalized = direction.normalized;
			_windDirection = _cachedWindDirectionNormalized;
			_windStrength = Mathf.Clamp01(strength);
		}

		[Inject]
		public void Construct(PlayerRoot playerRoot)
		{
			_playerRoot = playerRoot;
		}

		private void Start()
		{
			TryResolveFollowTarget(true);
		}

		private void Awake()
		{
			_radius = _config ? _config.Radius : DefaultRadius;
			_height = _config ? _config.Height : DefaultHeight;
			_fallDepth = _config ? _config.FallDepth : DefaultFallDepth;
			_gravity = _config ? _config.Gravity : DefaultGravity;
			_windInfluence = _config ? _config.WindInfluence : DefaultWindInfluence;
			_turbulence = _config ? _config.Turbulence : DefaultTurbulence;
			_minSize = _config ? _config.MinSize : DefaultMinSize;
			_maxSize = _config ? _config.MaxSize : DefaultMaxSize;
			_softness = _config ? _config.Softness : DefaultSoftness;
			_sparkleIntensity = _config ? _config.SparkleIntensity : DefaultSparkleIntensity;
			_fadeDistance = _config ? _config.FadeDistance : DefaultFadeDistance;
			_depthFade = DefaultDepthFade;
			_color = _config ? _config.SnowColor : DefaultSnowColor;
			_maxParticles = _config ? _config.MaxParticles : DefaultMaxParticles;
			_seedOffset = Random.Range(0f, MaxRandomSeed);
			_cachedWindDirectionNormalized = _windDirection.normalized;
			_cachedIndexCount = 0;

			_computeShader = Resources.Load<ComputeShader>("Shaders/SnowCompute");
			if (!_computeShader || !_snowShader)
			{
				TLNLogger.LogError("[Snow] Shaders not found");
				return;
			}

			_kernelIndex = _computeShader.FindKernel("CSMain");
			_material = new Material(_snowShader) { hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
			_quadMesh = CreateQuadMesh();
			_particles = new Particle[_maxParticles];
			_particleBuffer = new ComputeBuffer(_maxParticles, sizeof(float) * 8);
			_argsBuffer = CreateArgsBuffer(_quadMesh, (uint)_maxParticles);
			_argsData[0] = _quadMesh.GetIndexCount(0);
			_argsData[1] = (uint)_maxParticles;
			_lastArgsActiveCount = _maxParticles;

			Vector3 targetPosition = _followTarget ? _followTarget.position : transform.position;
			Vector3 volumeCenter = targetPosition + Vector3.up * ((_height - _fallDepth) * 0.5f);
			FillParticleBuffer(volumeCenter);
			_lastVolumeCenter = volumeCenter;
			_mpb = new MaterialPropertyBlock();
			_initialized = true;

			SetConstantShaderParameters();
		}

		private void SetConstantShaderParameters()
		{
			_computeShader.SetFloat(PropGravity, _gravity);
			_computeShader.SetFloat(PropTurbulence, _turbulence);
			_computeShader.SetFloat(PropSeedOffset, _seedOffset);
			_computeShader.SetFloat(PropMinSize, _minSize);
			_computeShader.SetFloat(PropMaxSize, _maxSize);
			_computeShader.SetBuffer(_kernelIndex, PropSnowParticles, _particleBuffer);

			_mpb.SetBuffer(PropSnowParticles, _particleBuffer);
			_mpb.SetColor(PropColor, _color);
			_mpb.SetFloat(PropSoftness, _softness);
			_mpb.SetFloat(PropSparkleIntensity, _sparkleIntensity);
			_mpb.SetFloat(PropSparkleFrequency, SparkleFrequency);
			_mpb.SetFloat(PropDepthFade, _depthFade);
			_mpb.SetFloat(PropFadeDistance, _fadeDistance);

			_cachedIndexCount = _quadMesh.GetIndexCount(0);
			Vector3 halfExtents = new Vector3(_radius, (_height + _fallDepth) * 0.5f, _radius);
			Vector3 drawSize = halfExtents * DrawBoundsPadding + Vector3.one * (_maxSize * MaxSizePadding);
			_cachedDrawBounds = new Bounds(_lastVolumeCenter, drawSize);
		}

		private void OnDestroy()
		{
			_particleBuffer?.Release();
			_argsBuffer?.Release();
			if (_material)
			{
				Destroy(_material);
			}

			if (_quadMesh)
			{
				Destroy(_quadMesh);
			}
		}

		private void Update()
		{
			if (!_initialized || !_active)
			{
				return;
			}

			float deltaTime = Mathf.Min(UnityEngine.Time.deltaTime, MaxDeltaTime);
			if (!_followTarget)
			{
				TryResolveFollowTarget(false);
			}

			Vector3 targetPosition = _followTarget ? _followTarget.position : transform.position;
			Vector3 halfExtents = new Vector3(_radius, (_height + _fallDepth) * 0.5f, _radius);
			Vector3 volumeCenter = targetPosition + Vector3.up * ((_height - _fallDepth) * 0.5f);
			Vector3 volumeDelta = volumeCenter - _lastVolumeCenter;
			int activeCount = Mathf.RoundToInt(_maxParticles * _intensity);
			if (activeCount <= 0)
			{
				UpdateArgsBuffer(0);
				return;
			}

			_cachedDrawBounds.center = volumeCenter;

			if (volumeDelta.sqrMagnitude > _radius * _radius)
			{
				FillParticleBuffer(volumeCenter);
			}
			_lastVolumeCenter = volumeCenter;

			float currentTime = UnityEngine.Time.time;
			float gust = Mathf.Sin(currentTime * GustFrequency1) * GustAmplitude1 +
				Mathf.Sin(currentTime * GustFrequency2 + GustPhase2) * GustAmplitude2;
			_cachedWindForce = _cachedWindDirectionNormalized * (Mathf.Max(0f, _windStrength + gust) * _windInfluence * WindForceMultiplier);

			_computeShader.SetVector(PropVolumeCenter, volumeCenter);
			_computeShader.SetVector(PropVolumeHalfExtents, halfExtents);
			_computeShader.SetVector(PropWind, _cachedWindForce);
			_computeShader.SetFloat(PropDeltaTime, deltaTime);
			_computeShader.SetInt(PropParticleCount, activeCount);
			_computeShader.SetFloat(PropTime, currentTime);
			_computeShader.Dispatch(_kernelIndex, Mathf.CeilToInt(activeCount / (float)ComputeThreadGroupSize), 1, 1);

			_mpb.SetFloat(PropTimeGlobal, currentTime);

			UpdateArgsBuffer(activeCount);
			Graphics.DrawMeshInstancedIndirect(
				_quadMesh,
				0,
				_material,
				_cachedDrawBounds,
				_argsBuffer,
				0,
				_mpb,
				ShadowCastingMode.Off,
				false
			);
		}

		private bool TryResolveFollowTarget(bool force)
		{
			if (!_autoFollowPlayerCamera || _followTarget)
			{
				return _followTarget != null;
			}

			if (!force && UnityEngine.Time.unscaledTime < _nextFollowSearchTime)
			{
				return false;
			}

			_nextFollowSearchTime = UnityEngine.Time.unscaledTime + FollowSearchCooldown;

			if (_playerRoot != null && _playerRoot.Camera != null)
			{
				AssignFollowTarget(_playerRoot.Camera.transform, true);
				return true;
			}

			return false;
		}

		private void AssignFollowTarget(Transform target, bool recenter)
		{
			_followTarget = target;
			if (!_initialized || !recenter || _followTarget == null)
			{
				return;
			}

			Vector3 volumeCenter = _followTarget.position + Vector3.up * ((_height - _fallDepth) * 0.5f);
			FillParticleBuffer(volumeCenter);
			_lastVolumeCenter = volumeCenter;
		}

		private void FillParticleBuffer(Vector3 center)
		{
			Vector3 halfExtents = new Vector3(_radius, _height * 0.5f, _radius);

			Random.State state = Random.state;
			Random.InitState(FillSeed ^ Mathf.RoundToInt(_seedOffset * SeedOffsetMultiplier));

			for (int i = 0; i < _maxParticles; i++)
			{
				_particles[i] = new Particle
				{
					position = center +
						new Vector3(
							Random.Range(-halfExtents.x, halfExtents.x),
							Random.Range(-halfExtents.y, halfExtents.y),
							Random.Range(-halfExtents.z, halfExtents.z)
						),
					seed = Random.Range(0f, MaxRandomSeed),
					velocity = new Vector3(
						Random.Range(-InitVelocityRangeXZ, InitVelocityRangeXZ),
						Random.Range(InitVelocityMinY, InitVelocityMaxY),
						Random.Range(-InitVelocityRangeXZ, InitVelocityRangeXZ)
					),
					size = RandomSnowflakeSize()
				};
			}

			Random.state = state;
			_particleBuffer.SetData(_particles);
		}

		private float RandomSnowflakeSize()
		{
			float sizeT = Mathf.Pow(Random.value, SnowflakeSizePow);
			if (Random.value > SnowflakeLargeChanceThreshold)
			{
				sizeT = Mathf.Lerp(sizeT, 1f, SnowflakeLargeBlend);
			}

			if (Random.value < SnowflakeSmallChanceThreshold)
			{
				sizeT *= SnowflakeSmallMultiplier;
			}

			return Mathf.Lerp(_minSize, _maxSize, Mathf.Clamp01(sizeT));
		}

		private void UpdateArgsBuffer(int activeCount)
		{
			uint clamped = (uint)Mathf.Clamp(activeCount, 0, _maxParticles);
			if (clamped == _argsData[1] && _lastArgsActiveCount >= 0)
			{
				return;
			}

			_lastArgsActiveCount = (int)clamped;
			_argsData[0] = _cachedIndexCount;
			_argsData[1] = clamped;
			_argsBuffer.SetData(_argsData);
		}

		private static ComputeBuffer CreateArgsBuffer(Mesh mesh, uint maxInstances)
		{
			ComputeBuffer buffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			buffer.SetData(new uint[] { mesh.GetIndexCount(0), maxInstances, 0, 0, 0 });
			return buffer;
		}

		private static Mesh CreateQuadMesh()
		{
			Mesh mesh = new Mesh { name = "SnowQuad", hideFlags = HideFlags.HideAndDontSave };
			mesh.SetVertices(
				new[]
				{
					new Vector3(-1, -1, 0), new Vector3(1, -1, 0),
					new Vector3(1, 1, 0), new Vector3(-1, 1, 0)
				}
			);

			mesh.SetUVs(
				0,
				new[]
				{
					new Vector2(0, 0), new Vector2(1, 0),
					new Vector2(1, 1), new Vector2(0, 1)
				}
			);

			mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}
