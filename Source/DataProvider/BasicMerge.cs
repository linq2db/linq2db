﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Common;
	using Data;
	using Linq;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

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
					var inlineParameters = dataConnection.InlineParameters;

					try
					{
						dataConnection.InlineParameters = true;

						var q   = dataConnection.GetTable<T>().Where(deletePredicate);
						var ctx = q.GetContext();
						var sql = ctx.SelectQuery;

						var tableSet  = new HashSet<SqlTable>();
						var tables    = new List<SqlTable>();

						var fromTable = (SqlTable)sql.From.Tables[0].Source;

						new QueryVisitor().Visit(sql.From, e =>
						{
							if (e.ElementType == QueryElementType.TableSource)
							{
								var et = (SelectQuery.TableSource)e;

								tableSet.Add((SqlTable)et.Source);
								tables.  Add((SqlTable)et.Source);
							}
						});

						var whereClause = new QueryVisitor().Convert(sql.Where, e =>
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
									var tempCopy   = sql.Clone();
									var tempTables = new List<SelectQuery.TableSource>();

									new QueryVisitor().Visit(tempCopy.From, ee =>
									{
										if (ee.ElementType == QueryElementType.TableSource)
											tempTables.Add((SelectQuery.TableSource)ee);
									});

									var tt = tempTables[tables.IndexOf(tbl)];

									tempCopy.Select.Columns.Clear();
									tempCopy.Select.Add(((SqlTable)tt.Source).Fields[fld.Name]);

									tempCopy.Where.SearchCondition.Conditions.Clear();

									var keys = tempCopy.From.Tables[0].Source.GetKeys(true);

									foreach (SqlField key in keys)
										tempCopy.Where.Field(key).Equal.Field(fromTable.Fields[key.Name]);

									tempCopy.ParentSelect = sql;

									return tempCopy;
								}
							}

							return e;
						}).SearchCondition.Conditions.ToList();

						sql.Where.SearchCondition.Conditions.Clear();
						sql.Where.SearchCondition.Conditions.AddRange(whereClause);

						sql.From.Tables[0].Alias = "Target";

						ctx.SetParameters();

						var pq = (DataConnection.PreparedQuery)((IDataContext)dataConnection).SetQuery(new QueryContext
						{
							SelectQuery   = sql,
							SqlParameters = sql.Parameters.ToArray(),
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
					.AppendLine("-- delete rows that are in the target but not in the sourse")
					.AppendLine("WHEN NOT MATCHED BY Source {0}THEN".Args(predicate))
					.AppendLine("\tDELETE")
					;
			}

			return true;
		}

		class QueryContext : IQueryContext
		{
			public SelectQuery    SelectQuery { get; set; }
			public object         Context     { get; set; }
			public SqlParameter[] SqlParameters;
			public List<string>   QueryHints  { get; set; }

			public SqlParameter[] GetParameters()
			{
				return SqlParameters;
			}
		}

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

		protected virtual int Execute(DataConnection dataConnection)
		{
			var cmd = StringBuilder.AppendLine().ToString();

			return dataConnection.Execute(cmd, Parameters.ToArray());
		}
	}
}
