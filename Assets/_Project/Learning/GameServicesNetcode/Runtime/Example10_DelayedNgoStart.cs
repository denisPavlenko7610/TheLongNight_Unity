using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 10. Session можно создать раньше, чем Relay и NGO.
    ///
    /// В CreateWaitingRoomAsync намеренно нет WithRelayNetwork: игроки входят в Lobby,
    /// выбирают персонажей и ставят ready, но NetworkManager ещё не listening.
    /// Затем host вызывает StartRelayAndNgoAsync; клиенты получают connection data
    /// через Session и их NGO запускается автоматически.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example10_DelayedNgoStart : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task<IHostSession> CreateWaitingRoomAsync(
            string name = "Waiting Room",
            int maxPlayers = 4)
        {
            await Context.EnsureReadyAsync();
            Context.EnsureNoActiveSession();

            // Без WithRelayNetwork NGO пока не запускается.
            var options = new SessionOptions
            {
                Type = UgsNetcodeLearningContext.SessionType,
                Name = name,
                MaxPlayers = maxPlayers,
                IsPrivate = false
            };

            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);
            Context.SetCurrentSession(session);
            Debug.Log($"Waiting room создана. Network={session.Network.State}", this);
            return session;
        }

        public async Task StartRelayAndNgoAsync(string relayRegion = null)
        {
            ISession session = Context.CurrentSession ??
                               throw new System.InvalidOperationException("Session ещё не создана.");

            IHostSession hostSession = session.AsHost();
            var relayOptions = new RelayNetworkOptions(
                string.IsNullOrWhiteSpace(relayRegion) ? null : relayRegion,
                preserveRegion: true);

            await hostSession.Network.StartRelayNetworkAsync(relayOptions);
            Debug.Log($"Relay и NGO запущены. Network={hostSession.Network.State}", this);
        }

        public async Task StopNgoButKeepSessionAsync()
        {
            ISession session = Context.CurrentSession ??
                               throw new System.InvalidOperationException("Session ещё не создана.");

            await session.AsHost().Network.StopNetworkAsync();
            Debug.Log("NGO остановлен, но игроки остаются в UGS Session.", this);
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
