using System;

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	public class DataConnectionTransaction : IDisposable
	{
		public DataConnectionTransaction([NotNull] DataConnection dataConnection, bool autoCommitOnDispose)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			DataConnection      = dataConnection;
			AutoCommitOnDispose = autoCommitOnDispose;
		}

		public DataConnection DataConnection      { get; private set; }
		public bool           AutoCommitOnDispose { get; set; }

		bool _disposeTransaction = true;

		public void Commit()
		{
			DataConnection.CommitTransaction();
			_disposeTransaction = false;
		}

		public void Rollback()
		{
			DataConnection.RollbackTransaction();
			_disposeTransaction = false;
		}

		public void Dispose()
		{
			if (_disposeTransaction)
			{
				if (AutoCommitOnDispose)
					DataConnection.CommitTransaction();
				else
					DataConnection.RollbackTransaction();
			}
		}
	}
}
