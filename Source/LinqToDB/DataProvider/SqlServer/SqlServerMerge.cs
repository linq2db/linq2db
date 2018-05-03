using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Data;

	class SqlServerMerge : BasicMerge
	{
		public SqlServerMerge()
		{
			ByTargetText = "BY Target ";
		}

		protected override bool IsIdentitySupported { get { return true; } }

		public override int Merge<T>(DataConnection dataConnection, Expression<Func<T, bool>> predicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
		{
			var table       = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var hasIdentity = table.Columns.Any(c => c.IsIdentity);

			string tblName = null;

			if (hasIdentity)
			{
				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder();

				tblName = sqlBuilder.ConvertTableName(new StringBuilder(),
					databaseName ?? table.DatabaseName,
					schemaName   ?? table.SchemaName,
					tableName    ?? table.TableName).ToString();

				dataConnection.Execute($"SET IDENTITY_INSERT {tblName} ON");
			}

			try
			{
				return base.Merge(dataConnection, predicate, delete, source, tableName, databaseName, schemaName);
			}
			finally
			{
				if (hasIdentity)
					dataConnection.Execute($"SET IDENTITY_INSERT {tblName} OFF");
			}
		}

		public override async Task<int> MergeAsync<T>(DataConnection dataConnection, Expression<Func<T, bool>> predicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName, CancellationToken token)
		{
			var table       = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var hasIdentity = table.Columns.Any(c => c.IsIdentity);

			string tblName = null;

			if (hasIdentity)
			{
				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder();

				tblName = sqlBuilder.ConvertTableName(new StringBuilder(),
					databaseName ?? table.DatabaseName,
					schemaName   ?? table.SchemaName,
					tableName    ?? table.TableName).ToString();

				await dataConnection.ExecuteAsync($"SET IDENTITY_INSERT {tblName} ON", token);
			}

			Exception ex = null;
			var ret = -1;

			try
			{
				ret = await base.MergeAsync(dataConnection, predicate, delete, source, tableName, databaseName, schemaName, token);
			}
			catch (Exception e)
			{
				ex = e;
			}

			if (hasIdentity)
				await dataConnection.ExecuteAsync($"SET IDENTITY_INSERT {tblName} OFF", token);

			if (ex != null)
				throw ex;

			return ret;
		}

		protected override bool BuildCommand<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
		{
			if (!base.BuildCommand(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName))
				return false;

			StringBuilder.AppendLine(";");

			return true;
		}
	}
}
