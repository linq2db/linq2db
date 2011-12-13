namespace LinqToDB.Data
{
	public delegate void OperationExceptionEventHandler(object sender, OperationExceptionEventArgs ea);

	public class OperationExceptionEventArgs : OperationTypeEventArgs
	{
		private readonly DataException _exception;
		public           DataException  Exception
		{
			get { return _exception; }
		}

		public OperationExceptionEventArgs(OperationType operation, DataException exception)
			: base (operation)
		{
			_exception = exception;
		}
	}
}
