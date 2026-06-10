using System;
using System.Collections.Generic;
using TLN.Core.Lifetime;
using TLN.Core.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TLN.Application.Assets
{
    public sealed class AddressableAssetService : IAddressableAssetService, IDisposableService, IDisposable
    {
        private readonly Dictionary<string, CachedOperation> _cachedOperations =
            new Dictionary<string, CachedOperation>();

        public void LoadSprite(
            AssetReferenceSprite spriteReference,
            Action<Sprite> completed)
        {
            LoadAsset(spriteReference, completed);
        }

        public void LoadPrefab(AssetReferenceGameObject prefabReference, Action<GameObject> completed)
        {
            LoadAsset(prefabReference, completed);
        }

        public void LoadAsset<TAsset>(
            AssetReference assetReference,
            Action<TAsset> completed)
            where TAsset : UnityEngine.Object
        {
            if (completed == null)
            {
                return;
            }

            if (assetReference == null)
            {
                TLNLogger.Warning($"Addressables: cannot load {typeof(TAsset).Name}. AssetReference is null.");
                completed.Invoke(null);
                return;
            }

            if (!assetReference.RuntimeKeyIsValid())
            {
                TLNLogger.Warning($"Addressables: cannot load {typeof(TAsset).Name}. Runtime key is invalid.");
                completed.Invoke(null);
                return;
            }

            string key = CreateKey<TAsset>(assetReference);

            if (_cachedOperations.TryGetValue(key, out CachedOperation cachedOperation))
            {
                if (cachedOperation.Handle.IsValid() && cachedOperation.Handle.IsDone)
                {
                    TAsset loadedAsset = cachedOperation.Handle.Result as TAsset;
                    completed.Invoke(loadedAsset);
                    return;
                }

                cachedOperation.AddCompletedCallback(asset => completed.Invoke(asset as TAsset));
                return;
            }

            AsyncOperationHandle<TAsset> handle =
                Addressables.LoadAssetAsync<TAsset>(assetReference);

            CachedOperation operation = new CachedOperation(handle);
            operation.AddCompletedCallback(asset => completed.Invoke(asset as TAsset));

            _cachedOperations.Add(key, operation);

            handle.Completed += completedHandle =>
            {
                OnAssetLoaded(key, completedHandle);
            };
        }

        public void ReleaseAll()
        {
            foreach (CachedOperation operation in _cachedOperations.Values)
            {
                operation.Release();
            }

            _cachedOperations.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }

        private void OnAssetLoaded<TAsset>(
            string key,
            AsyncOperationHandle<TAsset> handle)
            where TAsset : UnityEngine.Object
        {
            if (!_cachedOperations.TryGetValue(key, out CachedOperation operation))
            {
                return;
            }

            UnityEngine.Object result = null;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                result = handle.Result;
            }
            else
            {
                string exceptionMessage = handle.OperationException == null
                    ? "No exception message."
                    : handle.OperationException.Message;

                TLNLogger.Warning(
                    $"Addressables: failed to load asset. Key: {key}. Status: {handle.Status}. Error: {exceptionMessage}");
            }

            operation.Complete(result);
        }

        private static string CreateKey<TAsset>(AssetReference assetReference)
            where TAsset : UnityEngine.Object
        {
            return $"{typeof(TAsset).FullName}:{assetReference.RuntimeKey}";
        }

        private sealed class CachedOperation
        {
            private readonly List<Action<UnityEngine.Object>> _completedCallbacks =
                new List<Action<UnityEngine.Object>>();

            public AsyncOperationHandle Handle { get; }

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
