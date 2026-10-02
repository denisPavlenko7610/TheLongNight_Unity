using TLN.Core.Logging;

namespace TLN.Gameplay.Items
{
	public interface IItemDropService
	{
		bool DropItemAt(int inventoryIndex, int amount, out string failureReason);
	}
}
