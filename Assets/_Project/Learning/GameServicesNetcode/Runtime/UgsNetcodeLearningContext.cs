using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Общая инфраструктура учебных примеров.
    ///
    /// Хранит текущую UGS Session и один раз выполняет обязательную цепочку
    /// UnityServices.InitializeAsync -> Authentication.SignIn.
    /// Это не глобальный production-сервис и не пример DI-архитектуры.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public sealed class UgsNetcodeLearningContext : MonoBehaviour
    {
        public const string SessionType = "tln-ugs-ngo-learning";

        [SerializeField] private string _environmentName = "production";
        [SerializeField] private string _authenticationProfile = "learning-player";

        private Task _readyTask;

        public ISession CurrentSession { get; private set; }
        public bool HasSession => CurrentSession != null;

        public Task EnsureReadyAsync()
        {
            return _readyTask ??= InitializeAndAuthenticateAsync();
        }

        public void EnsureNoActiveSession()
        {
            if (CurrentSession != null)
            {
                throw new InvalidOperationException(
                    $"Сначала покиньте текущую Session {CurrentSession.Id}. " +
                    "Один учебный процесс должен работать только с одной Session этого типа.");
            }
        }

        internal void SetCurrentSession(ISession session)
        {
            CurrentSession = session ?? throw new ArgumentNullException(nameof(session));
        }

        internal void ClearCurrentSession(ISession expectedSession)
        {
            if (ReferenceEquals(CurrentSession, expectedSession))
            {
                CurrentSession = null;
            }
        }

        private async Task InitializeAndAuthenticateAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions()
                    .SetEnvironmentName(_environmentName)
                    .SetProfile(_authenticationProfile);

                await UnityServices.InitializeAsync(options);
            }
            else if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                throw new InvalidOperationException(
                    "Unity Services уже инициализируются другим кодом. Дождитесь его завершения.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
