using System;
using TLN.Gameplay.Survival;

namespace TLN.Gameplay.Player
{
	public sealed class LocalPlayerService
	{
		public PlayerRoot PlayerRoot { get; private set; }
		public ISurvivalService SurvivalService { get; private set; }

		public bool HasLocalPlayer => PlayerRoot != null;

		public event Action Changed;

		public void SetLocalPlayer(PlayerRoot playerRoot, ISurvivalService survivalService)
		{
			PlayerRoot = playerRoot ?? throw new ArgumentNullException(nameof(playerRoot));
			SurvivalService = survivalService ?? throw new ArgumentNullException(nameof(survivalService));

			Changed?.Invoke();
		}

		public void Clear()
		{
			if (PlayerRoot == null && SurvivalService == null)
			{
				return;
			}

			PlayerRoot = null;
			SurvivalService = null;

			Changed?.Invoke();
		}
	}
}
