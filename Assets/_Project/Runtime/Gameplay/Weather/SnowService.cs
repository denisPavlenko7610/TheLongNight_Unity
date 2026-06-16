using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace TLN.Gameplay.Weather
{
	public sealed class SnowService : MonoBehaviour
	{
		[SerializeField] SnowConfig _config;
		[SerializeField] Transform _followTarget;
		[SerializeField, Range(0, 1)] float _intensity = 0.5f;
		[SerializeField] Vector3 _windDirection = new Vector3(0.5f, 0, 0.3f);
		[SerializeField, Range(0, 1)] float _windStrength = 0.5f;
		[SerializeField] bool _active = true;

		[StructLayout(LayoutKind.Sequential)]
		struct Particle
		{
			public Vector3 position;
			public float seed;
			public Vector3 velocity;
			public float size;
		}

		ComputeShader _computeShader;
		Material _material;
		Mesh _quadMesh;
		ComputeBuffer _particleBuffer;
		ComputeBuffer _argsBuffer;
		int _kernelIndex;
		int _maxParticles;
		bool _initialized;

		float _radius, _height, _fallDepth, _gravity, _windInfluence, _turbulence;
		float _softness, _sparkleIntensity, _depthFade;
		Color _color;

		static readonly int PropSnowParticles = Shader.PropertyToID("_SnowParticles");
		static readonly int PropVolumeCenter = Shader.PropertyToID("_VolumeCenter");
		static readonly int PropVolumeHalfExtents = Shader.PropertyToID("_VolumeHalfExtents");
		static readonly int PropWind = Shader.PropertyToID("_Wind");
		static readonly int PropGravity = Shader.PropertyToID("_Gravity");
		static readonly int PropTurbulence = Shader.PropertyToID("_Turbulence");
		static readonly int PropDeltaTime = Shader.PropertyToID("_DeltaTime");
		static readonly int PropParticleCount = Shader.PropertyToID("_ParticleCount");
		static readonly int PropTime = Shader.PropertyToID("_Time");
		static readonly int PropColor = Shader.PropertyToID("_Color");
		static readonly int PropSoftness = Shader.PropertyToID("_Softness");
		static readonly int PropSparkleIntensity = Shader.PropertyToID("_SparkleIntensity");
		static readonly int PropSparkleFrequency = Shader.PropertyToID("_SparkleFrequency");
		static readonly int PropTimeGlobal = Shader.PropertyToID("_TimeGlobal");
		static readonly int PropDepthFade = Shader.PropertyToID("_DepthFade");

		public float Intensity { get => _intensity; set => _intensity = Mathf.Clamp01(value); }
		public Transform Follow { get => _followTarget; set => _followTarget = value; }
		public bool Active { get => _active; set => _active = value; }
		public void SetWind(Vector3 direction, float strength) { _windDirection = direction.normalized; _windStrength = Mathf.Clamp01(strength); }

		void Start()
		{
			if (!_followTarget && Camera.main)
			{
				_followTarget = Camera.main.transform;
			}
		}

		void Awake()
		{
			_radius = _config ? _config.Radius : 40f;
			_height = _config ? _config.Height : 30f;
			_fallDepth = _config ? _config.FallDepth : 8f;
			_gravity = _config ? _config.Gravity : 0.6f;
			_windInfluence = _config ? _config.WindInfluence : 0.7f;
			_turbulence = _config ? _config.Turbulence : 1.2f;
			_softness = _config ? _config.Softness : 0.7f;
			_sparkleIntensity = _config ? _config.SparkleIntensity : 0.15f;
			_depthFade = 0.5f;
			_color = _config ? _config.SnowColor : new Color(0.88f, 0.91f, 0.98f);
			_maxParticles = _config ? _config.MaxParticles : 15000;

			_computeShader = Resources.Load<ComputeShader>("Shaders/SnowCompute");
			Shader shader = Shader.Find("TLN/VFX/SnowParticle");
			if (!_computeShader || !shader)
			{
				Debug.LogError("[Snow] Shaders not found");
				return;
			}

			_kernelIndex = _computeShader.FindKernel("CSMain");
			_material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
			_quadMesh = CreateQuadMesh();
			_particleBuffer = new ComputeBuffer(_maxParticles, sizeof(float) * 8);
			_argsBuffer = CreateArgsBuffer(_quadMesh, (uint)_maxParticles);

			Vector3 center = _followTarget ? _followTarget.position : transform.position;
			FillParticleBuffer(center);
			_initialized = true;
		}

		void OnDestroy()
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

		void Update()
		{
			if (!_initialized || !_active)
			{
				return;
			}

			float deltaTime = Mathf.Min(UnityEngine.Time.deltaTime, 0.1f);
			Vector3 targetPosition = _followTarget ? _followTarget.position : transform.position;
			Vector3 halfExtents = new Vector3(_radius, (_height + _fallDepth) * 0.5f, _radius);
			Vector3 volumeCenter = targetPosition + Vector3.up * ((_height - _fallDepth) * 0.5f);

			float gust = Mathf.Sin(UnityEngine.Time.time * 0.7f) * 0.3f + Mathf.Sin(UnityEngine.Time.time * 1.3f + 1.5f) * 0.2f;
			Vector3 windForce = _windDirection.normalized * ((_windStrength + gust) * _windInfluence * 2f);

			_computeShader.SetVector(PropVolumeCenter, volumeCenter);
			_computeShader.SetVector(PropVolumeHalfExtents, halfExtents);
			_computeShader.SetVector(PropWind, windForce);
			_computeShader.SetFloat(PropGravity, _gravity);
			_computeShader.SetFloat(PropTurbulence, _turbulence);
			_computeShader.SetFloat(PropDeltaTime, deltaTime);
			_computeShader.SetInt(PropParticleCount, _maxParticles);
			_computeShader.SetFloat(PropTime, UnityEngine.Time.time);
			_computeShader.SetBuffer(_kernelIndex, PropSnowParticles, _particleBuffer);
			_computeShader.Dispatch(_kernelIndex, Mathf.CeilToInt(_maxParticles / 64f), 1, 1);

			_material.SetBuffer(PropSnowParticles, _particleBuffer);
			_material.SetColor(PropColor, _color);
			_material.SetFloat(PropSoftness, _softness);
			_material.SetFloat(PropSparkleIntensity, _sparkleIntensity);
			_material.SetFloat(PropSparkleFrequency, 2f);
			_material.SetFloat(PropTimeGlobal, UnityEngine.Time.time);
			_material.SetFloat(PropDepthFade, _depthFade);

			int activeCount = Mathf.RoundToInt(_maxParticles * _intensity);
			_argsBuffer.SetData(new uint[] { _quadMesh.GetIndexCount(0), (uint)Mathf.Max(0, activeCount), 0, 0, 0 });
			Graphics.DrawMeshInstancedIndirect(_quadMesh, 0, _material, new Bounds(volumeCenter, halfExtents * 2f), _argsBuffer, 0, null, ShadowCastingMode.Off, false);
		}

		void FillParticleBuffer(Vector3 center)
		{
			Vector3 halfExtents = new Vector3(_radius, _height * 0.5f, _radius);
			Particle[] particles = new Particle[_maxParticles];

			Random.State state = Random.state;
			Random.InitState(42);

			for (int i = 0; i < _maxParticles; i++)
			{
				particles[i] = new Particle
				{
					position = center + new Vector3(
						Random.Range(-halfExtents.x, halfExtents.x),
						Random.Range(-halfExtents.y, halfExtents.y),
						Random.Range(-halfExtents.z, halfExtents.z)),
					seed = Random.Range(0f, 10000f),
					velocity = new Vector3(
						Random.Range(-0.5f, 0.5f),
						Random.Range(-2f, -0.2f),
						Random.Range(-0.5f, 0.5f)),
					size = Random.Range(0.01f, 0.07f)
				};
			}

			Random.state = state;
			_particleBuffer.SetData(particles);
		}

		static ComputeBuffer CreateArgsBuffer(Mesh mesh, uint maxInstances)
		{
			ComputeBuffer buffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			buffer.SetData(new uint[] { mesh.GetIndexCount(0), maxInstances, 0, 0, 0 });
			return buffer;
		}

		static Mesh CreateQuadMesh()
		{
			Mesh mesh = new Mesh { name = "SnowQuad", hideFlags = HideFlags.HideAndDontSave };
			mesh.SetVertices(new[]
			{
				new Vector3(-1, -1, 0), new Vector3(1, -1, 0),
				new Vector3(1, 1, 0), new Vector3(-1, 1, 0)
			});

			mesh.SetUVs(0, new[] {
				new Vector2(0, 0), new Vector2(1, 0),
				new Vector2(1, 1), new Vector2(0, 1)
			});

			mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}
