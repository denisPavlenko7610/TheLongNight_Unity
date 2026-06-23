using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using TLN.Application.Saves;
using TLN.Core.Logging;

namespace TLN.Infrastructure.Saves
{
	public sealed class JsonSaveRepository : ISaveRepository
	{
		private const int DefaultSlotCount = 3;
		private const string SaveFolderName = "Saves";
		private const string SaveFileFormat = "slot_{0}.json";

		private readonly object _ioLock = new object();
		private string _saveDirectory;

		private static readonly JsonSerializerSettings SerializerSettings =
			new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				NullValueHandling = NullValueHandling.Include,
				DateParseHandling = DateParseHandling.None
			};

		public int SlotCount => DefaultSlotCount;

		public bool SaveExists(int slotId)
		{
			lock (_ioLock)
			{
				return IsValidSlot(slotId) && File.Exists(GetSlotPath(slotId));
			}
		}

		public void PrepareForBackgroundAccess()
		{
			lock (_ioLock)
			{
				EnsureSaveDirectoryPath();
			}
		}

		public GameSaveData Load(int slotId)
		{
			lock (_ioLock)
			{
				if (!IsValidSlot(slotId))
				{
					return null;
				}

				string path = GetSlotPath(slotId);

				if (!File.Exists(path))
				{
					return null;
				}

				try
				{
					string json = File.ReadAllText(path);
					return string.IsNullOrWhiteSpace(json)
						? null
						: JsonConvert.DeserializeObject<GameSaveData>(json, SerializerSettings);
				}
				catch (Exception exception)
				{
					TLNLogger.LogError($"Failed to load save slot {slotId}. {exception}");

					return null;
				}
			}
		}

		public void Save(GameSaveData data)
		{
			lock (_ioLock)
			{
				if (data == null)
				{
					return;
				}

				if (!IsValidSlot(data.slotId))
				{
					throw new ArgumentOutOfRangeException(nameof(data.slotId), data.slotId, "Invalid save slot.");
				}

				Directory.CreateDirectory(GetSaveDirectory());

				string json = JsonConvert.SerializeObject(data, SerializerSettings);

				string path = GetSlotPath(data.slotId);
				string temporaryPath = path + ".tmp";

				File.WriteAllText(temporaryPath, json);

				if (File.Exists(path))
				{
					File.Delete(path);
				}

				File.Move(temporaryPath, path);
			}
		}

		public IReadOnlyList<SaveSlotSummary> GetSlotSummaries()
		{
			List<SaveSlotSummary> summaries = new List<SaveSlotSummary>();

			for (int slotId = 1; slotId <= SlotCount; slotId++)
			{
				GameSaveData data = Load(slotId);

				if (data == null)
				{
					summaries.Add(new SaveSlotSummary(slotId, false, string.Empty, string.Empty));

					continue;
				}

				summaries.Add(new SaveSlotSummary(slotId, true, data.savedAtUtc, data.saveReason));
			}

			return summaries;
		}

		public bool TryGetMostRecentSlot(out int slotId)
		{
			slotId = 0;

			DateTime bestTime = DateTime.MinValue;

			for (int currentSlot = 1; currentSlot <= SlotCount; currentSlot++)
			{
				GameSaveData data = Load(currentSlot);

				if (data == null)
				{
					continue;
				}

				if (!DateTime.TryParse(
						data.savedAtUtc,
						CultureInfo.InvariantCulture,
						DateTimeStyles.RoundtripKind,
						out DateTime savedAtUtc
					))
				{
					continue;
				}

				if (savedAtUtc <= bestTime)
				{
					continue;
				}

				bestTime = savedAtUtc;
				slotId = currentSlot;
			}

			return slotId > 0;
		}

		public bool Delete(int slotId)
		{
			lock (_ioLock)
			{
				if (!IsValidSlot(slotId))
				{
					return false;
				}

				bool deleted = false;

				DeleteFileIfExists(GetSlotPath(slotId), ref deleted);

				DeleteFileIfExists(GetSlotPath(slotId) + ".tmp", ref deleted);

				return deleted;
			}
		}

		private static void DeleteFileIfExists(string path, ref bool deleted)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			if (!File.Exists(path))
			{
				return;
			}

			try
			{
				File.Delete(path);
				deleted = true;
			}
			catch (Exception exception)
			{
				TLNLogger.LogError($"Failed to delete save file: {path}. {exception}");
			}
		}

		private bool IsValidSlot(int slotId)
		{
			return slotId >= 1 && slotId <= SlotCount;
		}

		private string GetSlotPath(int slotId)
		{
			string fileName = string.Format(SaveFileFormat, slotId);

			return Path.Combine(GetSaveDirectory(), fileName);
		}

		private string GetSaveDirectory()
		{
			return EnsureSaveDirectoryPath();
		}

		private string EnsureSaveDirectoryPath()
		{
			if (!string.IsNullOrWhiteSpace(_saveDirectory))
			{
				return _saveDirectory;
			}

			_saveDirectory = Path.Combine(UnityEngine.Application.persistentDataPath, SaveFolderName);

			return _saveDirectory;
		}
	}
}
