using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 06. Matchmaker создаёт ticket в настроенной Dashboard queue.
    ///
    /// Queue и pool должны существовать в Unity Dashboard. Для client-hosted pool
    /// используйте Relay; для dedicated server pool обычно нужен Direct Network.
    /// CancellationToken удаляет ожидающий ticket и прекращает поиск.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example06_Matchmaker : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;
        [SerializeField] private string _queueName = "Friendly";

        private CancellationTokenSource _searchCancellation;

        public async Task<ISession> FindMatchAsync(int skill = 1000, int maxPlayers = 4)
        {
            await Context.EnsureReadyAsync();
            Context.EnsureNoActiveSession();

            _searchCancellation?.Dispose();
            _searchCancellation = new CancellationTokenSource();

            var matchmaker = new MatchmakerOptions
            {
                QueueName = _queueName,
                TicketAttributes = new Dictionary<string, object>
                {
                    ["skill"] = skill
                },
                PlayerProperties = new Dictionary<string, PlayerProperty>
                {
                    ["skill"] = new PlayerProperty(
                        skill.ToString(),
                        VisibilityPropertyOptions.Member)
                }
            };

            var sessionOptions = new SessionOptions
            {
                Type = UgsNetcodeLearningContext.SessionType,
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            ISession session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                matchmaker,
                sessionOptions,
                _searchCancellation.Token);

            Context.SetCurrentSession(session);
            Debug.Log($"Matchmaker нашёл Session={session.Id}", this);
            return session;
        }

        public void CancelSearch()
        {
            _searchCancellation?.Cancel();
        }

        private void OnDestroy()
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
