namespace TLN.Core.Results
{
	public readonly struct OperationResult
	{
		public bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess;
		public string Message { get; }

		private OperationResult(bool isSuccess, string message)
		{
			IsSuccess = isSuccess;
			Message = message ?? string.Empty;
		}

		public static OperationResult Success(string message = "")
		{
			return new OperationResult(true, message);
		}

		public static OperationResult Failure(string message)
		{
			return new OperationResult(false, message);
		}
	}

	public readonly struct OperationResult<T>
	{
		public bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess;
		public string Message { get; }
		public T Value { get; }

		private OperationResult(bool isSuccess, T value, string message)
		{
			IsSuccess = isSuccess;
			Value = value;
			Message = message ?? string.Empty;
		}

		public static OperationResult<T> Success(T value, string message = "")
		{
			return new OperationResult<T>(true, value, message);
		}

		public static OperationResult<T> Failure(string message)
		{
			return new OperationResult<T>(false, default, message);
		}
	}
}
