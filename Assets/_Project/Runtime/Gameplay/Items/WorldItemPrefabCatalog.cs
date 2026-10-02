using System;
using System.Collections.Generic;
using TLN.Core.Logging;
using UnityEngine;

namespace TLN.Gameplay.Items
{
	/// <summary>
	/// Maps item definition ids to their world pickup prefabs (used when dropping items).
	/// </summary>
	[CreateAssetMenu(
		fileName = "WorldItemPrefabCatalog",
		menuName = "TLN/Items/World Item Prefab Catalog"
	)]
	public sealed class WorldItemPrefabCatalog : ScriptableObject
	{
		[Serializable]
		private sealed class Entry
		{
			[SerializeField] private string _itemId;
			[SerializeField] private GameObject _prefab;

			public string ItemId => _itemId;
			public GameObject Prefab => _prefab;

			public Entry(string itemId, GameObject prefab)
			{
				_itemId = itemId;
				_prefab = prefab;
			}
		}

		[SerializeField] private Entry[] _entries;

		private Dictionary<string, GameObject> _prefabsByItemId;

		public bool TryGetPrefab(string itemId, out GameObject prefab)
		{
			EnsureCache();

			if (string.IsNullOrWhiteSpace(itemId))
			{
				prefab = null;
				return false;
			}

			return _prefabsByItemId.TryGetValue(itemId, out prefab);
		}

		private void EnsureCache()
		{
			if (_prefabsByItemId != null)
			{
				return;
			}

			_prefabsByItemId = new Dictionary<string, GameObject>();

			if (_entries == null)
			{
				return;
			}

			for (int i = 0; i < _entries.Length; i++)
			{
				Entry entry = _entries[i];

				if (entry == null ||
				    string.IsNullOrWhiteSpace(entry.ItemId) ||
				    entry.Prefab == null)
				{
					continue;
				}

				if (!_prefabsByItemId.TryAdd(entry.ItemId, entry.Prefab))
				{
					TLNLogger.LogWarning($"Duplicate world item prefab entry: {entry.ItemId}", this);
				}
			}
		}

#if UNITY_EDITOR
		public readonly struct EntryDraft
		{
			public string ItemId { get; }
			public GameObject Prefab { get; }

			public EntryDraft(string itemId, GameObject prefab)
			{
				ItemId = itemId;
				Prefab = prefab;
			}
		}

		public void EditorSetEntries(IReadOnlyList<EntryDraft> entries)
		{
			if (entries == null)
			{
				_entries = Array.Empty<Entry>();
				_prefabsByItemId = null;
				UnityEditor.EditorUtility.SetDirty(this);
				return;
			}

			List<Entry> result = new();

			for (int i = 0; i < entries.Count; i++)
			{
				EntryDraft draft = entries[i];

				if (string.IsNullOrWhiteSpace(draft.ItemId) || draft.Prefab == null)
				{
					continue;
				}

				result.Add(new Entry(draft.ItemId, draft.Prefab));
			}

			_entries = result.ToArray();
			_prefabsByItemId = null;

			UnityEditor.EditorUtility.SetDirty(this);
		}
#endif
	}
}
