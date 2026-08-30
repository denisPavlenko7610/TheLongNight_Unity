using Unity.Services.Multiplayer;
using UnityEngine;

namespace TLN.Learning.GameServicesNetcode
{
    /// <summary>
    /// Шаг 09. Session events описывают backend-группу, Network events — NGO connection lifecycle.
    ///
    /// PlayerJoined не означает, что PlayerObject NGO уже spawned. Для gameplay lifecycle
    /// продолжайте использовать NetworkManager callbacks и NetworkBehaviour callbacks.
    /// </summary>
    [RequireComponent(typeof(UgsNetcodeLearningContext))]
    public sealed class Example09_SessionEvents : MonoBehaviour
    {
        [SerializeField] private UgsNetcodeLearningContext _context;

        private ISession _observedSession;

        public void BeginObserving()
        {
            StopObserving();
            _observedSession = Context.CurrentSession;

            if (_observedSession == null)
            {
                throw new System.InvalidOperationException("Сначала создайте или присоедините Session.");
            }

            _observedSession.PlayerJoined += OnPlayerJoined;
            _observedSession.PlayerLeaving += OnPlayerLeaving;
            _observedSession.PlayerHasLeft += OnPlayerHasLeft;
            _observedSession.SessionPropertiesChanged += OnSessionPropertiesChanged;
            _observedSession.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
            _observedSession.StateChanged += OnSessionStateChanged;
            _observedSession.Network.StateChanged += OnNetworkStateChanged;
            _observedSession.Network.StartFailed += OnNetworkStartFailed;
        }

        public void StopObserving()
        {
            if (_observedSession == null)
            {
                return;
            }

            _observedSession.PlayerJoined -= OnPlayerJoined;
            _observedSession.PlayerLeaving -= OnPlayerLeaving;
            _observedSession.PlayerHasLeft -= OnPlayerHasLeft;
            _observedSession.SessionPropertiesChanged -= OnSessionPropertiesChanged;
            _observedSession.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            _observedSession.StateChanged -= OnSessionStateChanged;
            _observedSession.Network.StateChanged -= OnNetworkStateChanged;
            _observedSession.Network.StartFailed -= OnNetworkStartFailed;
            _observedSession = null;
        }

        private void OnDisable()
        {
            StopObserving();
        }

        private void OnPlayerJoined(string playerId) =>
            Debug.Log($"UGS PlayerJoined: {playerId}", this);

        private void OnPlayerLeaving(string playerId) =>
            Debug.Log($"UGS PlayerLeaving: {playerId}", this);

        private void OnPlayerHasLeft(string playerId) =>
            Debug.Log($"UGS PlayerHasLeft: {playerId}", this);

        private void OnSessionPropertiesChanged() =>
            Debug.Log("UGS Session properties changed.", this);

        private void OnPlayerPropertiesChanged() =>
            Debug.Log("UGS Player properties changed.", this);

        private void OnSessionStateChanged(SessionState state) =>
            Debug.Log($"UGS Session state: {state}", this);

        private void OnNetworkStateChanged(NetworkState state) =>
            Debug.Log($"UGS/NGO Network state: {state}", this);

        private void OnNetworkStartFailed(SessionError error) =>
            Debug.LogError($"UGS не смог запустить NGO: {error}", this);

        private UgsNetcodeLearningContext Context =>
            _context != null ? _context : GetComponent<UgsNetcodeLearningContext>();
    }
}
