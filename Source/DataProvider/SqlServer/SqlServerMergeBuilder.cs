using LinqToDB.Data;
using System;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServerMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		private bool _hasIdentityInsert;

		public SqlServerMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool BySourceOperationsSupported
		{
			get
			{
				// SQL Server-only commands
				return true;
			}
		}

		protected override bool IsIdentityInsertSupported
		{
			get
			{
				// SQL Server supports explicit identity insert
				return true;
			}
		}

		protected override int MaxOperationsCount
		{
			get
			{
				// Only 3 operations per command supported
				return 3;
			}
		}

		protected override bool SameTypeOperationsAllowed
		{
			get
			{
				// all operations should have different types
				return false;
			}
		}

		protected override void BuildTerminator()
		{
			// merge command must be terminated with semicolon
			Command.AppendLine(";");

			// disable explicit identity insert
			if (_hasIdentityInsert)
				Command.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}

		protected override void OnInsertWithIdentity()
		{
			// enable explicit identity insert
			if (!_hasIdentityInsert)
			{
				_hasIdentityInsert = true;

				// this code should be added before MERGE and command already partially generated at this stage
				Command.Insert(0, string.Format("SET IDENTITY_INSERT {0} ON{1}", TargetTableName, Environment.NewLine));
			}
		}
	}
}
