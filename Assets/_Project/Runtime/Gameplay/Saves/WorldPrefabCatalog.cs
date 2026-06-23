using System;
using System.Collections.Generic;
using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Gameplay.Saves
{
	[CreateAssetMenu(fileName = "WorldPrefabCatalog", menuName = "TLN/Saves/World Prefab Catalog")]
	public sealed class WorldPrefabCatalog : ScriptableObject
	{
		[SerializeField] private Entry[] _entries;

		private Dictionary<string, GameObject> _prefabsById;

		public bool TryGetPrefab(string prefabId, out GameObject prefab)
		{
			EnsureCache();

			if (string.IsNullOrWhiteSpace(prefabId))
			{
				prefab = null;
				return false;
			}

			return _prefabsById.TryGetValue(prefabId, out prefab);
		}

		private void EnsureCache()
		{
			if (_prefabsById != null)
			{
				return;
			}

			_prefabsById = new Dictionary<string, GameObject>();

			if (_entries == null)
			{
				return;
			}

			for (int i = 0; i < _entries.Length; i++)
			{
				Entry entry = _entries[i];

				if (entry == null
					|| string.IsNullOrWhiteSpace(entry.PrefabId)
					|| entry.Prefab == null)
				{
					continue;
				}

				if (!_prefabsById.TryAdd(entry.PrefabId, entry.Prefab))
				{
					TLNLogger.LogWarning($"Duplicate world prefab id: {entry.PrefabId}", this);
				}
			}
		}

		[Serializable]
		private sealed class Entry
		{
			[SerializeField] private string _prefabId;
			[SerializeField] private GameObject _prefab;

			public string PrefabId => _prefabId;
			public GameObject Prefab => _prefab;

			public Entry(string prefabId, GameObject prefab)
			{
				_prefabId = prefabId;
				_prefab = prefab;
			}
		}

		#if UNITY_EDITOR
		public readonly struct EntryDraft
		{
			public string PrefabId { get; }
			public GameObject Prefab { get; }

			public EntryDraft(string prefabId, GameObject prefab)
			{
				PrefabId = prefabId;
				Prefab = prefab;
			}
		}

		public void EditorSetEntries(IReadOnlyList<EntryDraft> entries)
		{
			if (entries == null)
			{
				_entries = Array.Empty<Entry>();
				_prefabsById = null;
				UnityEditor.EditorUtility.SetDirty(this);
				return;
			}

			List<Entry> result = new List<Entry>();

			for (int i = 0; i < entries.Count; i++)
			{
				EntryDraft draft = entries[i];

				if (string.IsNullOrWhiteSpace(draft.PrefabId) || draft.Prefab == null)
				{
					continue;
				}

				result.Add(new Entry(draft.PrefabId, draft.Prefab));
			}

			_entries = result.ToArray();
			_prefabsById = null;

			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
