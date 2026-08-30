using System.Text;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 08. Каждый игрок может менять только свои Player properties.
    ///
    /// Они подходят для ready/character/displayName в Lobby UI. Server-authoritative
    /// gameplay-данные нельзя доверять этой записи: клиент сам отправляет её в backend.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example08_PlayerProperties : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task SetReadyAsync(bool isReady)
        {
            ISession session = RequireSession();

            session.CurrentPlayer.SetProperty(
                "ready",
                new PlayerProperty(
                    isReady ? "true" : "false",
                    VisibilityPropertyOptions.Member));

            await session.SaveCurrentPlayerDataAsync();
        }

        public string DescribePlayers()
        {
            ISession session = RequireSession();
            var text = new StringBuilder();

            foreach (IReadOnlyPlayer player in session.Players)
            {
                string ready = player.Properties.TryGetValue(
                    "ready",
                    out PlayerProperty readyProperty)
                    ? readyProperty.Value
                    : "не задано";

                text.AppendLine($"{player.Id}: ready={ready}");
            }

            return text.ToString();
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
