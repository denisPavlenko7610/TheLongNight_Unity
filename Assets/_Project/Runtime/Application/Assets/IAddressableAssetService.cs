using System;
using TLN.Core.Lifetime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TLN.Application.Assets
{
	public interface IAddressableAssetService : IGameService
	{
		void LoadSprite(AssetReferenceSprite spriteReference, Action<Sprite> completed);
		void LoadPrefab(AssetReferenceGameObject prefabReference, Action<GameObject> completed);
	}
}
