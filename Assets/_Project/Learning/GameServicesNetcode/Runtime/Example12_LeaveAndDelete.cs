using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 12. Выход из Session завершает и связанное Relay/NGO-соединение.
    ///
    /// LeaveAsync — игрок покидает группу. DeleteAsync — host удаляет всю Session.
    /// Не вызывайте только NetworkManager.Shutdown: это остановит NGO, но само по себе
    /// не выразит backend-намерение выйти из Lobby-backed Session.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example12_LeaveAndDelete : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task LeaveAsync()
        {
            ISession session = RequireSession();
            await session.LeaveAsync();
            Context.ClearCurrentSession(session);
            Debug.Log("Игрок вышел из Session; связанная сеть остановлена.", this);
        }

        public async Task DeleteAsHostAsync()
        {
            ISession session = RequireSession();
            await session.AsHost().DeleteAsync();
            Context.ClearCurrentSession(session);
            Debug.Log("Host удалил Session для всех игроков.", this);
        }

        private ISession RequireSession()
        {
            return Context.CurrentSession ??
                   throw new System.InvalidOperationException("Активной Session нет.");
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
