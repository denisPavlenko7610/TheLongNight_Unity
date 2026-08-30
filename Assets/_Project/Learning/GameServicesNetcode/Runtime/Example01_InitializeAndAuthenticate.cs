using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 01. Любой клиентский UGS API требует инициализации и игрока.
    ///
    /// КАК ИСПОЛЬЗОВАТЬ:
    /// 1. Свяжите проект с Unity Cloud Project.
    /// 2. Вызовите InitializeAndSignInAsync до Lobby, Relay или Matchmaker.
    /// 3. Для двух локальных игроков используйте разные Authentication Profile.
    ///
    /// Anonymous Sign-In удобен для обучения, но production-аккаунт нужно связать
    /// с внешним identity provider, иначе после очистки данных игрок потеряет доступ.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example01_InitializeAndAuthenticate : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        public async Task<string> InitializeAndSignInAsync()
        {
            await Context.EnsureReadyAsync();

            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"UGS готов. PlayerId={playerId}", this);
            return playerId;
        }

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
