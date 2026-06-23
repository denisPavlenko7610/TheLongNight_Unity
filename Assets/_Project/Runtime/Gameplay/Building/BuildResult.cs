namespace TLN.Gameplay.Building
{
	public readonly struct BuildResult
	{
		public bool IsSuccess { get; }
		public string Message { get; }

		private BuildResult(bool isSuccess, string message)
		{
			IsSuccess = isSuccess;
			Message = message;
		}

		public static BuildResult Success(string message)
		{
			return new BuildResult(true, message);
		}

		public static BuildResult Failure(string message)
		{
			return new BuildResult(false, message);
		}
	}
}