using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	//TODO: Investigate how to implement only ISqlTableSource interface 
	public class SqlRawSqlTable : SqlTable
	{
		[JetBrains.Annotations.NotNull]
		public string SQL { get; }

		public ISqlExpression[] Parameters { get; }

		public SqlRawSqlTable(
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema,
			[JetBrains.Annotations.NotNull] Type objectType,
			[JetBrains.Annotations.NotNull] string sql,
			[JetBrains.Annotations.NotNull] params ISqlExpression[] parameters) : base(mappingSchema, objectType)
		{
			SQL = sql ?? throw new ArgumentNullException(nameof(sql));
			Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));
		}

		internal SqlRawSqlTable(
			int id, string alias, Type objectType,
			SqlField[]       fields,
			string           sql,
			ISqlExpression[] parameters)  : base(id, string.Empty, alias, string.Empty, string.Empty, string.Empty, objectType, null, fields, SqlTableType.RawSql, null)
		{
			SQL        = sql;
			Parameters = parameters;
		}

		public SqlRawSqlTable(SqlRawSqlTable table, IEnumerable<SqlField> fields, ISqlExpression[] parameters)
		{
			Alias              = table.Alias;
			Database           = table.Database;
			Schema             = table.Schema;

			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
			SequenceAttributes = table.SequenceAttributes;

			SQL                = table.SQL;
			Parameters         = parameters;

			AddRange(fields);
		}

		public override QueryElementType ElementType  => QueryElementType.SqlRawSqlTable;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb
				.AppendLine("(")
				.Append(SQL)
				.Append(")")
				.AppendLine();
		}

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();


		#endregion

		#region ISqlExpressionWalkable Members

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (Parameters != null)
				for (var i = 0; i < Parameters.Length; i++)
					Parameters[i] = Parameters[i].Walk(skipColumns, func);

			return func(this);
		}

		#endregion

	}
}
