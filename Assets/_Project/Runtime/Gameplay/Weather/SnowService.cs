using System.Runtime.InteropServices;
using TLN.Core.Logging;
using TLN.Core.Utilities;
using TLN.Gameplay.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace TLN.Gameplay.Weather
{
	public sealed class SnowService : MonoBehaviour
	{
		[SerializeField] private SnowConfig _config;
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
		private readonly uint[] _argsData = new uint[5];

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
			_windDirection = direction.normalized;
			_windStrength = Mathf.Clamp01(strength);
		}

		private void Start()
		{
			TryResolveFollowTarget(true);
		}

		private void Awake()
		{
			_radius = _config ? _config.Radius : 40f;
			_height = _config ? _config.Height : 30f;
			_fallDepth = _config ? _config.FallDepth : 8f;
			_gravity = _config ? _config.Gravity : 0.6f;
			_windInfluence = _config ? _config.WindInfluence : 0.7f;
			_turbulence = _config ? _config.Turbulence : 1.2f;
			_minSize = _config ? _config.MinSize : 0.01f;
			_maxSize = _config ? _config.MaxSize : 0.083f;
			_softness = _config ? _config.Softness : 0.7f;
			_sparkleIntensity = _config ? _config.SparkleIntensity : 0.15f;
			_fadeDistance = _config ? _config.FadeDistance : 45f;
			_depthFade = 0.5f;
			_color = _config ? _config.SnowColor : new Color(0.88f, 0.91f, 0.98f);
			_maxParticles = _config ? _config.MaxParticles : 15000;
			_seedOffset = Random.Range(0f, 10000f);

			_computeShader = Resources.Load<ComputeShader>("Shaders/SnowCompute");
			Shader shader = Shader.Find("TLN/VFX/SnowParticle");
			if (!_computeShader || !shader)
			{
				TLNLogger.LogError("[Snow] Shaders not found");
				return;
			}

			_kernelIndex = _computeShader.FindKernel("CSMain");
			_material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
			_quadMesh = CreateQuadMesh();
			_particleBuffer = new ComputeBuffer(_maxParticles, sizeof(float) * 8);
			_argsBuffer = CreateArgsBuffer(_quadMesh, (uint)_maxParticles);

			Vector3 targetPosition = _followTarget ? _followTarget.position : transform.position;
			Vector3 volumeCenter = targetPosition + Vector3.up * ((_height - _fallDepth) * 0.5f);
			FillParticleBuffer(volumeCenter);
			_lastVolumeCenter = volumeCenter;
			_initialized = true;
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

			float deltaTime = Mathf.Min(UnityEngine.Time.deltaTime, 0.1f);
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

			if (volumeDelta.sqrMagnitude > _radius * _radius)
			{
				FillParticleBuffer(volumeCenter);
			}
			_lastVolumeCenter = volumeCenter;

			float gust = Mathf.Sin(UnityEngine.Time.time * 0.7f) * 0.3f +
				Mathf.Sin(UnityEngine.Time.time * 1.3f + 1.5f) * 0.2f;
			Vector3 windForce = _windDirection.normalized * (Mathf.Max(0f, _windStrength + gust) * _windInfluence * 2f);

			_computeShader.SetVector(PropVolumeCenter, volumeCenter);
			_computeShader.SetVector(PropVolumeHalfExtents, halfExtents);
			_computeShader.SetVector(PropWind, windForce);
			_computeShader.SetFloat(PropGravity, _gravity);
			_computeShader.SetFloat(PropTurbulence, _turbulence);
			_computeShader.SetFloat(PropDeltaTime, deltaTime);
			_computeShader.SetInt(PropParticleCount, activeCount);
			_computeShader.SetFloat(PropTime, UnityEngine.Time.time);
			_computeShader.SetFloat(PropSeedOffset, _seedOffset);
			_computeShader.SetFloat(PropMinSize, _minSize);
			_computeShader.SetFloat(PropMaxSize, _maxSize);
			_computeShader.SetBuffer(_kernelIndex, PropSnowParticles, _particleBuffer);
			_computeShader.Dispatch(_kernelIndex, Mathf.CeilToInt(activeCount / 64f), 1, 1);

			_material.SetBuffer(PropSnowParticles, _particleBuffer);
			_material.SetColor(PropColor, _color);
			_material.SetFloat(PropSoftness, _softness);
			_material.SetFloat(PropSparkleIntensity, _sparkleIntensity);
			_material.SetFloat(PropSparkleFrequency, 2f);
			_material.SetFloat(PropTimeGlobal, UnityEngine.Time.time);
			_material.SetFloat(PropDepthFade, _depthFade);
			_material.SetFloat(PropFadeDistance, _fadeDistance);

			UpdateArgsBuffer(activeCount);
			Vector3 drawBoundsSize = halfExtents * 2.2f + Vector3.one * (_maxSize * 4f);
			Graphics.DrawMeshInstancedIndirect(
				_quadMesh,
				0,
				_material,
				new Bounds(volumeCenter, drawBoundsSize),
				_argsBuffer,
				0,
				null,
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

			_nextFollowSearchTime = UnityEngine.Time.unscaledTime + 0.5f;

			PlayerRoot player = FindFirstObjectByType<PlayerRoot>();
			if (player != null && player.Camera != null)
			{
				AssignFollowTarget(player.Camera.transform, true);
				return true;
			}

			Camera mainCamera = CameraUtility.GetMainCamera();
			if (mainCamera != null)
			{
				AssignFollowTarget(mainCamera.transform, true);
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
			Particle[] particles = new Particle[_maxParticles];

			Random.State state = Random.state;
			Random.InitState(42 ^ Mathf.RoundToInt(_seedOffset * 997.3f));

			for (int i = 0; i < _maxParticles; i++)
			{
				particles[i] = new Particle
				{
					position = center +
						new Vector3(
							Random.Range(-halfExtents.x, halfExtents.x),
							Random.Range(-halfExtents.y, halfExtents.y),
							Random.Range(-halfExtents.z, halfExtents.z)
						),
					seed = Random.Range(0f, 10000f),
					velocity = new Vector3(
						Random.Range(-0.5f, 0.5f),
						Random.Range(-2f, -0.2f),
						Random.Range(-0.5f, 0.5f)
					),
					size = RandomSnowflakeSize()
				};
			}

			Random.state = state;
			_particleBuffer.SetData(particles);
		}

		private float RandomSnowflakeSize()
		{
			float sizeT = Mathf.Pow(Random.value, 1.35f);
			if (Random.value > 0.92f)
			{
				sizeT = Mathf.Lerp(sizeT, 1f, 0.65f);
			}

			if (Random.value < 0.18f)
			{
				sizeT *= 0.35f;
			}

			return Mathf.Lerp(_minSize, _maxSize, Mathf.Clamp01(sizeT));
		}

		private void UpdateArgsBuffer(int activeCount)
		{
			_argsData[0] = _quadMesh.GetIndexCount(0);
			_argsData[1] = (uint)Mathf.Clamp(activeCount, 0, _maxParticles);
			_argsData[2] = 0;
			_argsData[3] = 0;
			_argsData[4] = 0;
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
