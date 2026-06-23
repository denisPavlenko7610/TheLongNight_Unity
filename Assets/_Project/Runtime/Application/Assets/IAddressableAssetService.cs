using System;
using TLN.Core.Lifetime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TLN.Application.Assets
{
	public interface IAddressableAssetService : IGameService
	{
		void LoadAsset<TAsset>(AssetReference assetReference, Action<TAsset> completed) where TAsset : UnityEngine.Object;

		void LoadSprite(AssetReferenceSprite spriteReference, Action<Sprite> completed);
		void LoadPrefab(AssetReferenceGameObject prefabReference, Action<GameObject> completed);
		void ReleaseAll();
	}
}
