namespace TLN.Gameplay.Sleep
{
	public readonly struct SleepResult
	{
		public bool IsSuccess { get; }
		public string Message { get; }

		private SleepResult(bool isSuccess, string message)
		{
			IsSuccess = isSuccess;
			Message = message;
		}

		public static SleepResult Success(string message)
		{
			return new SleepResult(true, message);
		}

		public static SleepResult Failure(string message)
		{
			return new SleepResult(false, message);
		}
	}
}