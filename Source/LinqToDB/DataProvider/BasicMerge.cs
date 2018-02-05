using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider
{
	using Common;
	using Data;
	using Linq;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	/// <summary>
	/// Basic MERGE operation implementation for all providers.
	/// For provider-specific logic create child class.
	/// </summary>
	public class BasicMerge
	{
		protected class ColumnInfo
		{
			public string           Name;
			public ColumnDescriptor Column;
		}

		protected string ByTargetText;

		protected StringBuilder       StringBuilder = new StringBuilder();
		protected List<DataParameter> Parameters    = new List<DataParameter>();
		protected List<ColumnInfo>    Columns;

		protected virtual bool IsIdentitySupported { get { return false; } }

		public virtual int Merge<T>(DataConnection dataConnection, Expression<Func<T,bool>> predicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
			where T : class
		{
			if (!BuildCommand(dataConnection, predicate, delete, source, tableName, databaseName, schemaName))
				return 0;

			return Execute(dataConnection);
		}

		public virtual async Task<int> MergeAsync<T>(DataConnection dataConnection, Expression<Func<T,bool>> predicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName,
			CancellationToken token)
			where T : class
		{
			if (!BuildCommand(dataConnection, predicate, delete, source, tableName, databaseName, schemaName))
				return 0;

			return await ExecuteAsync(dataConnection, token);
		}

		/// <summary>
		/// Builds MERGE INTO command text.
		/// For ON condition primary key fields used.
		/// UPDATE operation generated if there are any updateable columns (and only for them): NOT PK AND (identity OR !SkipOnUpdate).
		/// INSERT operation generated for following columns: identity OR !SkipOnInsert.
		/// DELETE operation generated if corresponding flag is set and could include optional condition. It is generated
		/// as WHEN NOT MATCHED BY SOURCE match clause, which is supported only by SQL Server.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="deletePredicate">Optional DELETE operation condition.</param>
		/// <param name="delete">Should MERGE command include DELETE operation or not.</param>
		/// <param name="source">Source data.</param>
		/// <param name="tableName">Optional target table name.</param>
		/// <param name="databaseName">Optional target table's database name.</param>
		/// <param name="schemaName">Optional target table's schema name.</param>
		/// <returns>True if command built and false if source is empty and command execution not required.</returns>
		protected virtual bool BuildCommand<T>(
			DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
			where T : class
		{
			var table      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder();

			Columns = table.Columns
				.Select(c => new ColumnInfo
				{
					Column = c,
					Name   = (string)sqlBuilder.Convert(c.ColumnName, ConvertType.NameToQueryField)
				})
				.ToList();

			StringBuilder.Append("MERGE INTO ");
			sqlBuilder.ConvertTableName(StringBuilder,
				databaseName ?? table.DatabaseName,
				schemaName   ?? table.SchemaName,
				tableName    ?? table.TableName);

			StringBuilder
				.AppendLine(" Target")
				;

			if (!BuildUsing(dataConnection, source))
				return false;

			StringBuilder
				.AppendLine("ON")
				.AppendLine("(")
				;

			foreach (var column in Columns.Where(c => c.Column.IsPrimaryKey))
			{
				StringBuilder
					.AppendFormat("\tTarget.{0} = Source.{0} AND", column.Name)
					.AppendLine()
					;
			}

			StringBuilder.Length -= 4 + Environment.NewLine.Length;

			StringBuilder
				.AppendLine()
				.AppendLine(")")
				;

			var updateColumns = Columns.Where(c => !c.Column.IsPrimaryKey && (IsIdentitySupported && c.Column.IsIdentity || !c.Column.SkipOnUpdate)).ToList();

			if (updateColumns.Count > 0)
			{
				StringBuilder
					.AppendLine("-- update matched rows")
					.AppendLine("WHEN MATCHED THEN")
					.AppendLine("\tUPDATE")
					.AppendLine("\tSET")
					;

				var maxLen = updateColumns.Max(c => c.Name.Length);

				foreach (var column in updateColumns)
				{
					StringBuilder
						.AppendFormat("\t\t{0} ", column.Name)
						;

					StringBuilder.Append(' ', maxLen - column.Name.Length);

					StringBuilder
						.AppendFormat("= Source.{0},", column.Name)
						.AppendLine()
						;
				}

				StringBuilder.Length -= 1 + Environment.NewLine.Length;
			}

			var insertColumns = Columns.Where(c => IsIdentitySupported && c.Column.IsIdentity || !c.Column.SkipOnInsert).ToList();

			StringBuilder
				.AppendLine()
				.AppendLine("-- insert new rows")
				.Append("WHEN NOT MATCHED ").Append(ByTargetText).AppendLine("THEN")
				.AppendLine("\tINSERT")
				.AppendLine("\t(")
				;

			foreach (var column in insertColumns)
				StringBuilder.AppendFormat("\t\t{0},", column.Name).AppendLine();

			StringBuilder.Length -= 1 + Environment.NewLine.Length;

			StringBuilder
				.AppendLine()
				.AppendLine("\t)")
				.AppendLine("\tVALUES")
				.AppendLine("\t(")
				;

			foreach (var column in insertColumns)
				StringBuilder.AppendFormat("\t\tSource.{0},", column.Name).AppendLine();

			StringBuilder.Length -= 1 + Environment.NewLine.Length;

			StringBuilder
				.AppendLine()
				.AppendLine("\t)")
				;

			if (delete)
			{
				var predicate = "";

				if (deletePredicate != null)
				{
					// generate SQL for delete condition
					var inlineParameters = dataConnection.InlineParameters;

					try
					{
						// toggle parameters embedding as literals
						dataConnection.InlineParameters = true;

						var q         = dataConnection.GetTable<T>().Where(deletePredicate);
						var ctx       = q.GetContext();
						var statement = ctx.GetResultStatement();

						var tableSet  = new HashSet<SqlTable>();
						var tables    = new List<SqlTable>();

						var fromTable = (SqlTable)statement.SelectQuery.From.Tables[0].Source;

						new QueryVisitor().Visit(statement.SelectQuery.From, e =>
						{
							if (e.ElementType == QueryElementType.TableSource)
							{
								var et = (SqlTableSource)e;

								tableSet.Add((SqlTable)et.Source);
								tables.  Add((SqlTable)et.Source);
							}
						});

						var whereClause = new QueryVisitor().Convert(statement.SelectQuery.Where, e =>
						{
							if (e.ElementType == QueryElementType.SqlQuery)
							{

							}

							if (e.ElementType == QueryElementType.SqlField)
							{
								var fld = (SqlField)e;
								var tbl = (SqlTable)fld.Table;

								if (tbl != fromTable && tableSet.Contains(tbl))
								{
									var tempCopy   = statement.Clone();
									var tempTables = new List<SqlTableSource>();

									new QueryVisitor().Visit(tempCopy.SelectQuery.From, ee =>
									{
										if (ee.ElementType == QueryElementType.TableSource)
											tempTables.Add((SqlTableSource)ee);
									});

									var tt = tempTables[tables.IndexOf(tbl)];

									tempCopy.SelectQuery.Select.Columns.Clear();
									tempCopy.SelectQuery.Select.Add(((SqlTable)tt.Source).Fields[fld.Name]);

									tempCopy.SelectQuery.Where.SearchCondition.Conditions.Clear();

									var keys = tempCopy.SelectQuery.From.Tables[0].Source.GetKeys(true);

									foreach (SqlField key in keys)
										tempCopy.SelectQuery.Where.Field(key).Equal.Field(fromTable.Fields[key.Name]);

									tempCopy.SelectQuery.ParentSelect = statement.SelectQuery;

									return tempCopy.SelectQuery;
								}
							}

							return e;
						}).SearchCondition.Conditions.ToList();

						statement.SelectQuery.Where.SearchCondition.Conditions.Clear();
						statement.SelectQuery.Where.SearchCondition.Conditions.AddRange(whereClause);

						statement.SelectQuery.From.Tables[0].Alias = "Target";

						ctx.SetParameters();

						var pq = DataConnection.QueryRunner.SetQuery(dataConnection, new QueryContext
						{
							Statement     = statement,
							SqlParameters = statement.Parameters.ToArray(),
						});

						var cmd = pq.Commands[0];

						predicate = "AND " + cmd.Substring(cmd.IndexOf("WHERE") + "WHERE".Length);
					}
					finally
					{
						dataConnection.InlineParameters = inlineParameters;
					}
				}

				StringBuilder
					.AppendLine("-- delete rows that are in the target but not in the source")
					.AppendLine($"WHEN NOT MATCHED BY Source {predicate}THEN")
					.AppendLine("\tDELETE")
					;
			}

			return true;
		}

		class QueryContext : IQueryContext
		{
			public SqlStatement   Statement   { get; set; }
			public object         Context     { get; set; }
			public SqlParameter[] SqlParameters;
			public List<string>   QueryHints  { get; set; }

			public SqlParameter[] GetParameters()
			{
				return SqlParameters;
			}
		}

		/// <summary>
		/// Generates USING source statement with direct VALUES.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="source">Source data collection.</param>
		/// <returns>Returns true on success an false if source is empty.</returns>
		protected virtual bool BuildUsing<T>(DataConnection dataConnection, IEnumerable<T> source)
		{
			var table          = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var sqlBuilder     = dataConnection.DataProvider.CreateSqlBuilder();
			var pname          = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();
			var valueConverter = dataConnection.MappingSchema.ValueToSqlConverter;

			StringBuilder
				.AppendLine("USING")
				.AppendLine("(")
				.AppendLine("\tVALUES")
				;

			var pidx  = 0;

			var hasData     = false;
			var columnTypes = table.Columns
				.Select(c => new SqlDataType(c.DataType, c.MemberType, c.Length, c.Precision, c.Scale))
				.ToArray();

			foreach (var item in source)
			{
				hasData = true;

				StringBuilder.Append("\t(");

				for (var i = 0; i < table.Columns.Count; i++)
				{
					var column = table.Columns[i];
					var value  = column.GetValue(dataConnection.MappingSchema, item);

					if (!valueConverter.TryConvert(StringBuilder, columnTypes[i], value))
					{
						var name = pname == "?" ? pname : pname + ++pidx;

						StringBuilder.Append(name);
						Parameters.Add(new DataParameter(pname == "?" ? pname : "p" + pidx, value,
							column.DataType));
					}

					StringBuilder.Append(",");
				}

				StringBuilder.Length--;
				StringBuilder.AppendLine("),");
			}

			if (hasData)
			{
				var idx = StringBuilder.Length;
				while (StringBuilder[--idx] != ',') {}
				StringBuilder.Remove(idx, 1);

				StringBuilder
					.AppendLine(")")
					.AppendLine("AS Source")
					.AppendLine("(")
					;

				foreach (var column in Columns)
					StringBuilder.AppendFormat("\t{0},", column.Name).AppendLine();

				StringBuilder.Length -= 1 + Environment.NewLine.Length;

				StringBuilder
					.AppendLine()
					.AppendLine(")")
					;
			}

			return hasData;
		}

		/// <summary>
		/// Generates USING source statement using union subquery with dummy select for each source record for databases
		/// that doesn't support VALUES in source.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataConnection">Database connection.</param>
		/// <param name="source">Source data collection.</param>
		/// <param name="top">TOP 1 clause equivalent for current database engine.</param>
		/// <param name="fromDummyTable">Database engine-specific dummy table for FROM statement with at least one record.</param>
		/// <returns>Returns true on success an false if source is empty.</returns>
		protected bool BuildUsing2<T>(DataConnection dataConnection, IEnumerable<T> source, string top, string fromDummyTable)
		{
			var table          = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var sqlBuilder     = dataConnection.DataProvider.CreateSqlBuilder();
			var pname          = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();
			var valueConverter = dataConnection.MappingSchema.ValueToSqlConverter;

			StringBuilder
				.AppendLine("USING")
				.AppendLine("(")
				;

			var pidx  = 0;

			var hasData     = false;
			var columnTypes = table.Columns
				.Select(c => new SqlDataType(c.DataType, c.MemberType, c.Length, c.Precision, c.Scale))
				.ToArray();

			foreach (var item in source)
			{
				if (hasData)
					StringBuilder.Append(" UNION ALL").AppendLine();

				StringBuilder.Append("\tSELECT ");

				if (top != null)
					StringBuilder.Append(top);

				for (var i = 0; i < Columns.Count; i++)
				{
					var column = Columns[i];
					var value  = column.Column.GetValue(dataConnection.MappingSchema, item);

					if (!valueConverter.TryConvert(StringBuilder, columnTypes[i], value))
					{
						var name = pname == "?" ? pname : pname + ++pidx;

						StringBuilder.Append(name);
						Parameters.Add(new DataParameter(pname == "?" ? pname : "p" + pidx, value,
							column.Column.DataType));
					}

					if (!hasData)
						StringBuilder.Append(" as ").Append(column.Name);

					StringBuilder.Append(",");
				}

				StringBuilder.Length--;
				StringBuilder.Append(' ').Append(fromDummyTable);

				hasData = true;
			}

			if (hasData)
			{
				StringBuilder.AppendLine();

				StringBuilder
					.AppendLine(")")
					.AppendLine("Source")
					;
			}

			return hasData;
		}

		/// <summary>
		/// Executes generated MERGE query against database connection.
		/// </summary>
		/// <returns>Returns total number of affected records - inserted, updated or deleted.</returns>
		protected virtual int Execute(DataConnection dataConnection)
		{
			var cmd = StringBuilder.AppendLine().ToString();

			return dataConnection.Execute(cmd, Parameters.ToArray());
		}

		protected virtual Task<int> ExecuteAsync(DataConnection dataConnection, CancellationToken token)
		{
			var cmd = StringBuilder.AppendLine().ToString();

			return new CommandInfo(dataConnection, cmd, Parameters.ToArray()).ExecuteAsync(token);
		}
	}
}
