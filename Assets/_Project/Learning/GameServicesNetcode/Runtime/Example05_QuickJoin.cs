using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 05. Quick Join ищет подходящую Lobby-backed Session и при необходимости создаёт новую.
    ///
    /// Это простое случайное сведение по фильтрам. Оно не заменяет Matchmaker с queue,
    /// rules, QoS и backfill.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example05_QuickJoin : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task<ISession> FindOrCreateAsync(
            string map = "forest",
            int maxPlayers = 4,
            int timeoutSeconds = 5)
        {
            await Context.EnsureReadyAsync();
            Context.EnsureNoActiveSession();

            var quickJoin = new QuickJoinOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                CreateSession = true,
                Filters = new List<FilterOption>
                {
                    new FilterOption(
                        FilterField.AvailableSlots,
                        "1",
                        FilterOperation.GreaterOrEqual),
                    new FilterOption(
                        FilterField.StringIndex1,
                        map,
                        FilterOperation.Equal)
                }
            };

            var createFallback = new SessionOptions
            {
                Type = UgsNetcodeLearningContext.SessionType,
                Name = $"Quick {map}",
                MaxPlayers = maxPlayers,
                IsPrivate = false,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    ["map"] = new SessionProperty(
                        map,
                        VisibilityPropertyOptions.Public,
                        PropertyIndex.String1)
                }
            }.WithRelayNetwork();

            ISession session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                quickJoin,
                createFallback);

            Context.SetCurrentSession(session);
            Debug.Log($"Quick Join завершён. Session={session.Id}, Host={session.IsHost}", this);
            return session;
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
