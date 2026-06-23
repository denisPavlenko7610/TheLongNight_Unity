using TLN.Gameplay.Inventory;

namespace TLN.Gameplay.Items
{
	public interface IItemUseService
	{
		ItemUseResult UseItemAt(int index);
	}
}