using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Data;
	using SqlQuery;
	using SqlProvider;

	class BasicMerge
	{
		public virtual int Merge<T>(DataConnection dataConnection, bool delete, IEnumerable<T> source)
		{
			var sb             = new StringBuilder();
			var table          = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var sqlBuilder     = dataConnection.DataProvider.CreateSqlBuilder();
			var pname          = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();
			var valueConverter = dataConnection.MappingSchema.ValueToSqlConverter;

			sb.Append("MERGE INTO ");
			sqlBuilder.BuildTableName(sb,
				(string)sqlBuilder.Convert(table.DatabaseName, ConvertType.NameToDatabase),
				(string)sqlBuilder.Convert(table.SchemaName,   ConvertType.NameToOwner),
				(string)sqlBuilder.Convert(table.TableName,    ConvertType.NameToQueryTable));

			sb
				.AppendLine(" AS Target")
				.AppendLine("USING")
				.AppendLine("(")
				.AppendLine("\tVALUES")
				;

			var parms = new List<DataParameter>();
			var pidx  = 0;

			var columns = table.Columns
				.Select(c => new
				{
					Column = c,
					Name   = sqlBuilder.Convert(c.ColumnName, ConvertType.NameToQueryField)
				})
				.ToList();

			var hasData     = false;
			var columnTypes = columns
				.Select(c => new SqlDataType(c.Column.DataType, c.Column.MemberType, c.Column.Length, c.Column.Precision, c.Column.Scale))
				.ToArray();

			foreach (var item in source)
			{
				hasData = true;

				sb.Append("\t(");

				for (var i = 0; i < columns.Count; i++)
				{
					var column = columns[i];
					var value  = column.Column.GetValue(item);

					if (!valueConverter.TryConvert(sb, columnTypes[i], value))
					{
						var name = pname == "?" ? pname : pname + ++pidx;

						sb.Append(name);
						parms.Add(new DataParameter(pname == "?" ? pname : "p" + pidx, value,
							column.Column.DataType));
					}

					sb.Append(",");
				}

				sb.Length--;
				sb.AppendLine("),");
			}

			if (!hasData)
				return 0;

			var idx = sb.Length;
			while (sb[--idx] != ',') {}
			sb.Remove(idx, 1);

			sb
				.AppendLine(")")
				.Append("AS Source (")
				;

			foreach (var column in columns)
				sb.AppendFormat("{0}, ", column.Name);

			sb.Length -= 2;

			sb
				.AppendLine(")")
				.AppendLine("ON")
				;

			foreach (var column in columns.Where(c => c.Column.IsPrimaryKey))
			{
				sb
					.AppendFormat("\tTarget.{0} = Source.{0} AND", column.Name)
					.AppendLine()
					;
			}

			sb.Length -= 4 + Environment.NewLine.Length;

			var updateColumns = columns.Where(c => !c.Column.IsPrimaryKey).ToList();

			if (updateColumns.Count > 0)
			{
				sb
					.AppendLine()
					.AppendLine()
					.AppendLine("-- update matched rows")
					.AppendLine("WHEN MATCHED THEN")
					.AppendLine("\tUPDATE")
					.AppendLine("\tSET")
					;

				foreach (var column in updateColumns)
				{
					sb
						.AppendFormat("\t\t{0} = Source.{0},", column.Name)
						.AppendLine()
						;
				}

				sb.Length -= 1 + Environment.NewLine.Length;
			}

			sb
				.AppendLine()
				.AppendLine()
				.AppendLine("-- insert new rows")
				.AppendLine("WHEN NOT MATCHED BY Target THEN")
				.AppendLine("\tINSERT (")
				;

			foreach (var column in columns)
				sb.AppendFormat("{0}, ", column.Name);

			sb.Length -= 2;

			sb
				.AppendLine(")")
				.Append("\tVALUES (")
				;

			foreach (var column in columns)
				sb.AppendFormat("{0}, ", column.Name);

			sb.Length -= 2;

			sb.AppendLine(")");

			if (delete)
			{
				sb
					.AppendLine()
					.AppendLine("-- delete rows that are in the target but not te sourse")
					.AppendLine("WHEN NOT MATCHED BY Source THEN")
					.AppendLine("\tDELETE")
					;
			}

			sb.AppendLine(";");

			return dataConnection.Execute(sb.AppendLine().ToString(), parms.ToArray());
		}
	}
}
