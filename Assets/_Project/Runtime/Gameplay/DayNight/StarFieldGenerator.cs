using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace TLN.Gameplay.DayNight
{
	public sealed class StarFieldGenerator : MonoBehaviour
	{
		[SerializeField] private int _cubeFaceResolution = 512;
		[SerializeField] private int _starCount = 4200;
		[SerializeField] private float _minStarBrightness = 0.35f;
		[SerializeField] private float _maxStarBrightness = 4f;
		[SerializeField, Range(0f, 1f)] private float _starSizeSoftness = 0.65f;
		[SerializeField] private int _seed = 42;

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

			sky.spaceEmissionTexture.overrideState = true;
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
			System.Random rng = new System.Random(_seed);

			for (int i = 0; i < _starCount; i++)
			{
				Vector3 dir = RandomOnSphere(rng);
				float brightnessRoll = Mathf.Pow((float)rng.NextDouble(), 2.4f);
				float sizeRoll = Mathf.Pow((float)rng.NextDouble(), 3f);

				stars[i] = new StarData
				{
					direction = dir,
					brightness = Mathf.Lerp(_minStarBrightness, _maxStarBrightness, brightnessRoll),
					starSize = Mathf.Lerp(0.35f, 1.6f, sizeRoll),
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
			Color[] pixels = new Color[res * res];

			for (int face = 0; face < 6; face++)
			{
				System.Array.Clear(pixels, 0, pixels.Length);

				for (int i = 0; i < stars.Length; i++)
				{
					Vector2 uv;
					float weight;

					if (!ProjectToCubemapFace(stars[i].direction, face, out uv, out weight))
						continue;

					if (weight <= 0.001f)
						continue;

					float pixelStarRadius = stars[i].starSize * weight * (res * 0.0035f);
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

							int pixelIndex = py * res + px;
							Color existing = pixels[pixelIndex];
							Color blended = existing + stars[i].color * contribution;

							pixels[pixelIndex] = new Color(
								Mathf.Min(blended.r, _maxStarBrightness),
								Mathf.Min(blended.g, _maxStarBrightness),
								Mathf.Min(blended.b, _maxStarBrightness),
								1f);
						}
					}
				}

				_starCubemap.SetPixels(pixels, (CubemapFace)face);
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
					if (dir.x <= 0f || absX < absY || absX < absZ) return false;
					ma = absX;
					uv = new Vector2(-dir.z / ma, -dir.y / ma);
					break;
				case 1:
					if (dir.x >= 0f || absX < absY || absX < absZ) return false;
					ma = absX;
					uv = new Vector2(dir.z / ma, -dir.y / ma);
					break;
				case 2:
					if (dir.y <= 0f || absY < absX || absY < absZ) return false;
					ma = absY;
					uv = new Vector2(dir.x / ma, dir.z / ma);
					break;
				case 3:
					if (dir.y >= 0f || absY < absX || absY < absZ) return false;
					ma = absY;
					uv = new Vector2(dir.x / ma, -dir.z / ma);
					break;
				case 4:
					if (dir.z <= 0f || absZ < absX || absZ < absY) return false;
					ma = absZ;
					uv = new Vector2(dir.x / ma, -dir.y / ma);
					break;
				case 5:
					if (dir.z >= 0f || absZ < absX || absZ < absY) return false;
					ma = absZ;
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
