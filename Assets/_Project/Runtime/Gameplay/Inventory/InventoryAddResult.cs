namespace TLN.Gameplay.Inventory
{
	public readonly struct InventoryAddResult
	{
		public bool IsSuccess { get; }
		public string FailureReason { get; }

		private InventoryAddResult(bool isSuccess, string failureReason)
		{
			IsSuccess = isSuccess;
			FailureReason = failureReason;
		}

		public static InventoryAddResult Success()
		{
			return new InventoryAddResult(true, string.Empty);
		}

		public static InventoryAddResult Failure(string reason)
		{
			return new InventoryAddResult(false, reason);
		}
	}
}
