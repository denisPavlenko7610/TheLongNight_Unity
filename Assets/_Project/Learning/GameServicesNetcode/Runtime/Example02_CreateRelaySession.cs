using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 02. Host создаёт Lobby-backed Session и Relay-соединение для NGO.
    ///
    /// WithRelayNetwork связывает три слоя:
    /// Session хранит группу игроков, Relay маршрутизирует трафик через интернет,
    /// а встроенный network handler запускает NetworkManager.Singleton как Host.
    /// Отдельно вызывать StartHost здесь не нужно.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example02_CreateRelaySession : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task<IHostSession> CreateAsync(
            string sessionName,
            string playerName,
            string map = "forest",
            int maxPlayers = 4)
        {
            await Context.EnsureReadyAsync();
            Context.EnsureNoActiveSession();

            var options = new SessionOptions
            {
                Type = UgsNetcodeLearningContext.SessionType,
                Name = sessionName,
                MaxPlayers = maxPlayers,
                IsPrivate = false,
                PlayerProperties = new Dictionary<string, PlayerProperty>
                {
                    ["displayName"] = new PlayerProperty(playerName, VisibilityPropertyOptions.Member)
                },
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    // Indexed public property можно использовать в Query и Quick Join.
                    ["map"] = new SessionProperty(
                        map,
                        VisibilityPropertyOptions.Public,
                        PropertyIndex.String1)
                }
            }.WithRelayNetwork();

            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);
            Context.SetCurrentSession(session);

            Debug.Log(
                $"Session создана: Id={session.Id}, Code={session.Code}, " +
                $"UGS Network={session.Network.State}", this);
            return session;
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
