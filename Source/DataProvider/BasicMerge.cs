using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToDB.Mapping;

namespace LinqToDB.DataProvider
{
	using Data;
	using SqlQuery;
	using SqlProvider;

	class BasicMerge
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

		public int Merge<T>(DataConnection dataConnection, bool delete, IEnumerable<T> source)
		{
			if (!BuildCommand(dataConnection, delete, source))
				return 0;

			return Execute(dataConnection);
		}

		protected virtual bool BuildCommand<T>(DataConnection dataConnection, bool delete, IEnumerable<T> source)
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
			sqlBuilder.BuildTableName(StringBuilder,
				(string)sqlBuilder.Convert(table.DatabaseName, ConvertType.NameToDatabase),
				(string)sqlBuilder.Convert(table.SchemaName,   ConvertType.NameToOwner),
				(string)sqlBuilder.Convert(table.TableName,    ConvertType.NameToQueryTable));

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

			var updateColumns = Columns.Where(c => !c.Column.IsPrimaryKey).ToList();

			if (updateColumns.Count > 0)
			{
				StringBuilder
					.AppendLine()
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

			StringBuilder
				.AppendLine()
				.AppendLine()
				.AppendLine("-- insert new rows")
				.Append("WHEN NOT MATCHED ").Append(ByTargetText).Append("THEN")
				.AppendLine("\tINSERT")
				.AppendLine("\t(")
				;

			foreach (var column in Columns)
				StringBuilder.AppendFormat("\t\t{0},", column.Name).AppendLine();

			StringBuilder.Length -= 1 + Environment.NewLine.Length;

			StringBuilder
				.AppendLine()
				.AppendLine("\t)")
				.AppendLine("\tVALUES")
				.AppendLine("\t(")
				;

			foreach (var column in Columns)
				StringBuilder.AppendFormat("\t\tSource.{0},", column.Name).AppendLine();

			StringBuilder.Length -= 1 + Environment.NewLine.Length;

			StringBuilder
				.AppendLine()
				.AppendLine("\t)")
				;

			if (delete)
			{
				StringBuilder
					.AppendLine()
					.AppendLine("-- delete rows that are in the target but not in the sourse")
					.AppendLine("WHEN NOT MATCHED BY Source THEN")
					.AppendLine("\tDELETE")
					;
			}

			return true;
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
					var value  = column.GetValue(item);

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
					var value  = column.Column.GetValue(item);

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

			StringBuilder.AppendLine();

			StringBuilder
				.AppendLine(")")
				.AppendLine("Source")
				;

			return hasData;
		}

		protected virtual int Execute(DataConnection dataConnection)
		{
			var cmd = StringBuilder.AppendLine().ToString();

			return dataConnection.Execute(cmd, Parameters.ToArray());
		}
	}
}
