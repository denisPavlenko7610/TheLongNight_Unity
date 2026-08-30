using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 03. Client входит по короткому коду Session.
    ///
    /// Session API получает сохранённые host connection data, настраивает Relay
    /// и запускает NetworkManager.Singleton как Client. Join code Session не равен
    /// NGO clientId и не должен использоваться как постоянный ID матча.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example03_JoinByCode : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task<ISession> JoinAsync(string joinCode, string playerName)
        {
            await Context.EnsureReadyAsync();
            Context.EnsureNoActiveSession();

            var options = new JoinSessionOptions
            {
                Type = UgsNetcodeLearningContext.SessionType,
                PlayerProperties = new Dictionary<string, PlayerProperty>
                {
                    ["displayName"] = new PlayerProperty(playerName, VisibilityPropertyOptions.Member)
                }
            };

            ISession session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                joinCode.Trim().ToUpperInvariant(),
                options);

            Context.SetCurrentSession(session);
            Debug.Log(
                $"Вход выполнен: Session={session.Id}, UGS Network={session.Network.State}", this);
            return session;
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
