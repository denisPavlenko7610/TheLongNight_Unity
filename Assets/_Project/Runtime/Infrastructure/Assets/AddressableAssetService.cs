using System;
using System.Collections.Generic;
using TLN.Application.Assets;
using TLN.Core.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TLN.Infrastructure.Assets
{
	public sealed class AddressableAssetService : IAddressableAssetService, IDisposable
	{
		private readonly Dictionary<AssetCacheKey, CachedOperation> _cachedOperations = new();
		private bool _isDisposed;

		public void LoadSprite(AssetReferenceSprite spriteReference, Action<Sprite> completed)
		{
			LoadAsset(spriteReference, completed);
		}

		public void LoadPrefab(AssetReferenceGameObject prefabReference, Action<GameObject> completed)
		{
			LoadAsset(prefabReference, completed);
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			ReleaseAll();
		}

		private void LoadAsset<TAsset>(AssetReference assetReference, Action<TAsset> completed)
			where TAsset : UnityEngine.Object
		{
			if (completed == null)
			{
				return;
			}

			if (_isDisposed)
			{
				TLNLogger.LogWarning($"Addressables: cannot load {typeof(TAsset).Name}. Service is disposed.");
				completed.Invoke(null);
				return;
			}

			if (assetReference == null)
			{
				TLNLogger.LogWarning($"Addressables: cannot load {typeof(TAsset).Name}. AssetReference is null.");
				completed.Invoke(null);
				return;
			}

			if (!assetReference.RuntimeKeyIsValid())
			{
				TLNLogger.LogWarning($"Addressables: cannot load {typeof(TAsset).Name}. Runtime key is invalid.");
				completed.Invoke(null);
				return;
			}

			AssetCacheKey key = CreateKey<TAsset>(assetReference);

			if (_cachedOperations.TryGetValue(key, out CachedOperation cachedOperation))
			{
				if (!cachedOperation.IsValid)
				{
					_cachedOperations.Remove(key);
					cachedOperation.Release();
				}
				else if (cachedOperation.IsDone)
				{
					TAsset loadedAsset = cachedOperation.Handle.Result as TAsset;
					completed.Invoke(loadedAsset);
					return;
				}
				else
				{
					cachedOperation.AddCompletedCallback(asset => completed.Invoke(asset as TAsset));
					return;
				}
			}

			AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(assetReference);

			CachedOperation operation = new CachedOperation(handle);
			operation.AddCompletedCallback(asset => completed.Invoke(asset as TAsset));

			_cachedOperations.Add(key, operation);

			handle.Completed += completedHandle =>
			{
				OnAssetLoaded(key, completedHandle);
			};
		}

		private void ReleaseAll()
		{
			foreach (CachedOperation operation in _cachedOperations.Values)
			{
				operation.Release();
			}

			_cachedOperations.Clear();
		}

		private void OnAssetLoaded<TAsset>(AssetCacheKey key, AsyncOperationHandle<TAsset> handle)
			where TAsset : UnityEngine.Object
		{
			if (!_cachedOperations.TryGetValue(key, out CachedOperation operation))
			{
				return;
			}

			bool succeeded = handle.Status == AsyncOperationStatus.Succeeded;
			UnityEngine.Object result = null;

			if (succeeded)
			{
				result = handle.Result;
			}
			else
			{
				string exceptionMessage = handle.OperationException == null
					? "No exception message."
					: handle.OperationException.Message;

				TLNLogger.LogWarning(
					$"Addressables: failed to load asset. Key: {key}. Status: {handle.Status}. Error: {exceptionMessage}"
				);
			}

			operation.Complete(result);

			if (!succeeded)
			{
				_cachedOperations.Remove(key);
				operation.Release();
			}
		}

		private static AssetCacheKey CreateKey<TAsset>(AssetReference assetReference) where TAsset : UnityEngine.Object
		{
			return new AssetCacheKey(typeof(TAsset), assetReference.RuntimeKey);
		}

		private readonly struct AssetCacheKey : IEquatable<AssetCacheKey>
		{
			private readonly Type _assetType;
			private readonly object _runtimeKey;

			public AssetCacheKey(Type assetType, object runtimeKey)
			{
				_assetType = assetType;
				_runtimeKey = runtimeKey;
			}

			public bool Equals(AssetCacheKey other)
			{
				return _assetType == other._assetType && Equals(_runtimeKey, other._runtimeKey);
			}

			public override bool Equals(object obj)
			{
				return obj is AssetCacheKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(_assetType, _runtimeKey);
			}

			public override string ToString()
			{
				return $"{_assetType.Name}:{_runtimeKey}";
			}
		}

		private sealed class CachedOperation
		{
			private readonly List<Action<UnityEngine.Object>> _completedCallbacks = new();

			public AsyncOperationHandle Handle { get; }
			public bool IsValid => Handle.IsValid();
			public bool IsDone => Handle.IsDone;

			public CachedOperation(AsyncOperationHandle handle)
			{
				Handle = handle;
			}

			public void AddCompletedCallback(Action<UnityEngine.Object> completed)
			{
				if (completed == null)
				{
					return;
				}

				_completedCallbacks.Add(completed);
			}

			public void Complete(UnityEngine.Object asset)
			{
				for (int i = 0; i < _completedCallbacks.Count; i++)
				{
					_completedCallbacks[i]?.Invoke(asset);
				}

				_completedCallbacks.Clear();
			}

			public void Release()
			{
				if (!Handle.IsValid())
				{
					return;
				}

				Addressables.Release(Handle);
			}
		}
	}
}
