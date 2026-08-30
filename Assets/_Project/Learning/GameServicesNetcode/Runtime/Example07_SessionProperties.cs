using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 07. Session properties — Lobby-данные матча, а не realtime-состояние NGO.
    ///
    /// Только host меняет Session property. Используйте её для map/mode/build/phase,
    /// поиска и pre-game UI. Частые gameplay-изменения передавайте через NGO.
    /// SetProperty меняет локальную модель, SavePropertiesAsync отправляет её в backend.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example07_SessionProperties : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task SetMapAsync(string map)
        {
            ISession session = RequireSession();
            IHostSession hostSession = session.AsHost();

            hostSession.SetProperty(
                "map",
                new SessionProperty(
                    map,
                    VisibilityPropertyOptions.Public,
                    PropertyIndex.String1));

            await hostSession.SavePropertiesAsync();
        }

        public string ReadMap()
        {
            ISession session = RequireSession();
            return session.Properties.TryGetValue("map", out SessionProperty property)
                ? property.Value
                : string.Empty;
        }

        private ISession RequireSession()
        {
            return Context.CurrentSession ??
                   throw new System.InvalidOperationException("Сначала создайте или присоедините Session.");
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
