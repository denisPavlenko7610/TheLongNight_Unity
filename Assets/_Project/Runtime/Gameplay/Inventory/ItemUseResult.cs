namespace TLN.Gameplay.Inventory
{
	public readonly struct ItemUseResult
	{
		public bool IsSuccess { get; }
		public string Message { get; }

		private ItemUseResult(bool isSuccess, string message)
		{
			IsSuccess = isSuccess;
			Message = message;
		}

		public static ItemUseResult Success(string message)
		{
			return new ItemUseResult(true, message);
		}

		public static ItemUseResult Failure(string message)
		{
			return new ItemUseResult(false, message);
		}
	}
}