using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 04. Lobby-backed Session можно найти запросом и войти по её ID.
    ///
    /// Query возвращает только публичные доступные Session. Для фильтрации своей
    /// property используется назначенный ей индекс, а не строковый ключ "map".
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example04_BrowseSessions : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        private QuerySessionsResults _lastResults;

        public async Task<string> QueryAsync(string map)
        {
            await Context.EnsureReadyAsync();

            var filters = new List<FilterOption>
            {
                new FilterOption(
                    FilterField.AvailableSlots,
                    "1",
                    FilterOperation.GreaterOrEqual)
            };

            if (!string.IsNullOrWhiteSpace(map))
            {
                filters.Add(new FilterOption(
                    FilterField.StringIndex1,
                    map,
                    FilterOperation.Equal));
            }

            _lastResults = await MultiplayerService.Instance.QuerySessionsAsync(
                new QuerySessionsOptions
                {
                    Count = 20,
                    FilterOptions = filters
                });

            var text = new StringBuilder();
            for (int i = 0; i < _lastResults.Sessions.Count; i++)
            {
                ISessionInfo info = _lastResults.Sessions[i];
                text.AppendLine(
                    $"[{i}] {info.Name}: {info.AvailableSlots}/{info.MaxPlayers} свободно, Id={info.Id}");
            }

            string result = text.Length > 0 ? text.ToString() : "Подходящих Session нет.";
            Debug.Log(result, this);
            return result;
        }

        public async Task<ISession> JoinResultAsync(int index)
        {
            if (_lastResults == null)
            {
                throw new System.InvalidOperationException("Сначала вызовите QueryAsync.");
            }

            if (index < 0 || index >= _lastResults.Sessions.Count)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index));
            }

            Context.EnsureNoActiveSession();

            ISession session = await MultiplayerService.Instance.JoinSessionByIdAsync(
                _lastResults.Sessions[index].Id,
                new JoinSessionOptions { Type = UgsNetcodeLearningContext.SessionType });

            Context.SetCurrentSession(session);
            return session;
        }

        public void StartPolling(int intervalSeconds = 5)
        {
            _lastResults?.StartPolling(intervalSeconds);
        }

        public void StopPolling()
        {
            _lastResults?.StopPolling();
        }

        private void OnDisable()
        {
            StopPolling();
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
