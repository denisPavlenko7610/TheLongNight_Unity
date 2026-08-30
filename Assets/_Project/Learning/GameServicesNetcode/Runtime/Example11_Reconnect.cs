using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 11. После временного disconnect игрок может восстановить членство Session и NGO.
    ///
    /// Reconnect работает, только пока backend ещё считает PlayerId участником.
    /// Если host удалил игрока или Session уже удалена, нужен обычный Join.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example11_Reconnect : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task<IReadOnlyList<string>> GetJoinedSessionIdsAsync()
        {
            await Context.EnsureReadyAsync();
            List<string> ids = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
            return ids;
        }

        public async Task<ISession> ReconnectAsync(string sessionId)
        {
            await Context.EnsureReadyAsync();

            ISession session = await MultiplayerService.Instance.ReconnectToSessionAsync(sessionId);
            Context.SetCurrentSession(session);
            Debug.Log($"Reconnect завершён. Session={session.Id}, Network={session.Network.State}", this);
            return session;
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
