using System;

namespace LinqToDB.Data
{
	public delegate void OperationTypeEventHandler(object sender, OperationTypeEventArgs ea);

	public class OperationTypeEventArgs : EventArgs
	{
		private readonly OperationType _operation;
		public           OperationType  Operation
		{
			get { return _operation; }
		}

		public OperationTypeEventArgs(OperationType operation)
		{
			_operation = operation;
		}
	}
}
