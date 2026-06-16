using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace TLN.Gameplay.DayNight
{
	public sealed class StarFieldGenerator : MonoBehaviour
	{
		[SerializeField] private int _cubeFaceResolution = 512;
		[SerializeField] private int _starCount = 2800;
		[SerializeField] private float _minStarBrightness = 0.3f;
		[SerializeField] private float _maxStarBrightness = 1f;
		[SerializeField] private float _starSizeSoftness = 0.6f;

		private Cubemap _starCubemap;
		private bool _isGenerated;

		public Cubemap StarCubemap
		{
			get
			{
				if (!_isGenerated)
					GenerateStarField();
				return _starCubemap;
			}
		}

		private void Awake()
		{
			GenerateStarField();
		}

		private void OnDestroy()
		{
			if (_starCubemap != null)
			{
				Destroy(_starCubemap);
				_starCubemap = null;
			}
		}

		public void ApplyToSky(PhysicallyBasedSky sky)
		{
			if (sky == null)
				return;

			sky.spaceEmissionTexture.value = StarCubemap;
		}

		private void GenerateStarField()
		{
			if (_isGenerated)
				return;

			_starCubemap = new Cubemap(_cubeFaceResolution, TextureFormat.RGBAHalf, false)
			{
				filterMode = FilterMode.Trilinear,
				wrapMode = TextureWrapMode.Clamp
			};

			StarData[] stars = GenerateStarData();
			RenderStarsToCubemap(stars);
			_starCubemap.Apply(false);

			_isGenerated = true;
		}

		private StarData[] GenerateStarData()
		{
			StarData[] stars = new StarData[_starCount];
			System.Random rng = new System.Random(42);

			for (int i = 0; i < _starCount; i++)
			{
				Vector3 dir = RandomOnSphere(rng);

				stars[i] = new StarData
				{
					direction = dir,
					brightness = Mathf.Lerp(_minStarBrightness, _maxStarBrightness, (float)rng.NextDouble()),
					starSize = Mathf.Lerp(0.4f, 1f, (float)rng.NextDouble()),
					color = GenerateStarColor(rng)
				};
			}

			return stars;
		}

		private Color GenerateStarColor(System.Random rng)
		{
			float type = (float)rng.NextDouble();

			if (type < 0.08f)
				return new Color(0.7f, 0.8f, 1f, 1f);
			if (type < 0.16f)
				return new Color(0.9f, 0.85f, 1f, 1f);
			if (type < 0.22f)
				return new Color(1f, 0.85f, 0.7f, 1f);
			if (type < 0.26f)
				return new Color(1f, 0.7f, 0.5f, 1f);
			if (type < 0.30f)
				return new Color(1f, 0.95f, 0.8f, 1f);

			return Color.white;
		}

		private static Vector3 RandomOnSphere(System.Random rng)
		{
			float u = (float)rng.NextDouble() * 2f - 1f;
			float theta = (float)rng.NextDouble() * Mathf.PI * 2f;
			float r = Mathf.Sqrt(1f - u * u);

			return new Vector3(r * Mathf.Cos(theta), u, r * Mathf.Sin(theta));
		}

		private void RenderStarsToCubemap(StarData[] stars)
		{
			int res = _cubeFaceResolution;
			Color[] clearColors = new Color[res * res];

			for (int face = 0; face < 6; face++)
			{
				System.Array.Fill(clearColors, Color.clear);
				_starCubemap.SetPixels(clearColors, (CubemapFace)face);

				for (int i = 0; i < stars.Length; i++)
				{
					Vector2 uv;
					float weight;

					if (!ProjectToCubemapFace(stars[i].direction, face, out uv, out weight))
						continue;

					if (weight <= 0.001f)
						continue;

					float pixelStarRadius = stars[i].starSize * weight * (res * 0.004f);
					int radius = Mathf.Max(1, Mathf.RoundToInt(pixelStarRadius));
					float falloffPow = Mathf.Lerp(2.5f, 1.5f, _starSizeSoftness);

					int cx = Mathf.RoundToInt(uv.x * (res - 1));
					int cy = Mathf.RoundToInt(uv.y * (res - 1));

					for (int dy = -radius; dy <= radius; dy++)
					{
						for (int dx = -radius; dx <= radius; dx++)
						{
							int px = cx + dx;
							int py = cy + dy;

							if (px < 0 || px >= res || py < 0 || py >= res)
								continue;

							float dist = Mathf.Sqrt(dx * dx + dy * dy);
							float normalizedDist = dist / radius;

							if (normalizedDist > 1f)
								continue;

							float falloff = 1f - Mathf.Pow(normalizedDist, falloffPow);
							float contribution = stars[i].brightness * falloff * weight;

							Color existing = _starCubemap.GetPixel((CubemapFace)face, px, py);
							Color blended = Color.Lerp(existing, stars[i].color, contribution);

							float maxComponent = Mathf.Max(existing.r, existing.g, existing.b);
							float newMax = Mathf.Max(blended.r, blended.g, blended.b);

							if (newMax > maxComponent)
								_starCubemap.SetPixel((CubemapFace)face, px, py, blended);
						}
					}
				}
			}
		}

		private static bool ProjectToCubemapFace(Vector3 dir, int faceIndex, out Vector2 uv, out float weight)
		{
			uv = Vector2.zero;
			weight = 1f;

			float absX = Mathf.Abs(dir.x);
			float absY = Mathf.Abs(dir.y);
			float absZ = Mathf.Abs(dir.z);
			float ma;

			switch (faceIndex)
			{
				case 0:
					ma = absX;
					if (Mathf.Approximately(ma, 0f)) return false;
					uv = new Vector2(-dir.z / ma, -dir.y / ma);
					break;
				case 1:
					ma = absX;
					if (Mathf.Approximately(ma, 0f)) return false;
					uv = new Vector2(dir.z / ma, -dir.y / ma);
					break;
				case 2:
					ma = absY;
					if (Mathf.Approximately(ma, 0f)) return false;
					uv = new Vector2(dir.x / ma, dir.z / ma);
					break;
				case 3:
					ma = absY;
					if (Mathf.Approximately(ma, 0f)) return false;
					uv = new Vector2(dir.x / ma, -dir.z / ma);
					break;
				case 4:
					ma = absZ;
					if (Mathf.Approximately(ma, 0f)) return false;
					uv = new Vector2(dir.x / ma, -dir.y / ma);
					break;
				case 5:
					ma = absZ;
					if (Mathf.Approximately(ma, 0f)) return false;
					uv = new Vector2(-dir.x / ma, -dir.y / ma);
					break;
				default:
					return false;
			}

			uv = uv * 0.5f + Vector2.one * 0.5f;
			weight = ma;

			return true;
		}

		private struct StarData
		{
			public Vector3 direction;
			public float brightness;
			public float starSize;
			public Color color;
		}
	}
}
