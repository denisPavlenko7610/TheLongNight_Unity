using TLN.Application.Services;
using TLN.Core.Logging;
using TLN.Gameplay.Inventory;
using UnityEngine;

namespace TLN.SceneComposition.Installers
{
	public sealed class GameplayServiceInstaller : MonoBehaviour, IServiceInstaller
	{
		[SerializeField] private InventoryConfig _inventoryConfig;

		public void Install(ServiceRegistry services)
		{
			if (_inventoryConfig == null)
			{
				TLNLogger.Error("InventoryConfig is not assigned in GameplayServiceInstaller.", this);
				return;
			}

			InventoryService inventoryService = new InventoryService(_inventoryConfig);

			services.Register<IInventoryService>(inventoryService);
		}
	}
}
