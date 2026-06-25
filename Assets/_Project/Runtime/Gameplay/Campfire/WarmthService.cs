using System.Collections.Generic;
using UnityEngine;

namespace TLN.Gameplay.Campfire
{
	public sealed class WarmthService : IWarmthService
	{
		private readonly List<IWarmthProvider> _providers = new();

		public void Register(IWarmthProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			if (_providers.Contains(provider))
			{
				return;
			}

			_providers.Add(provider);
		}

		public void Unregister(IWarmthProvider provider)
		{
			if (provider == null)
			{
				return;
			}

			_providers.Remove(provider);
		}

		public float GetWarmthAt(Vector3 position)
		{
			float bestWarmth = 0f;

			for (int i = _providers.Count - 1; i >= 0; i--)
			{
				IWarmthProvider provider = _providers[i];

				if (provider == null)
				{
					_providers.RemoveAt(i);
					continue;
				}

				if (!provider.IsWarmthActive)
				{
					continue;
				}

				float radius = provider.WarmthRadius;
				if (radius <= 0f)
				{
					continue;
				}

				Vector3 offset = position - provider.Position;
				float sqrDistance = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;
				float sqrRadius = radius * radius;

				if (sqrDistance > sqrRadius)
				{
					continue;
				}

				float normalizedDistance = Mathf.Sqrt(sqrDistance) / radius;
				float strength = 1f - normalizedDistance;
				float warmth = provider.WarmthBonus * strength;

				if (warmth > bestWarmth)
				{
					bestWarmth = warmth;
				}
			}

			return bestWarmth;
		}
	}
}
