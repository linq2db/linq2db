﻿using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	//TODO: Investigate how to implement only ISqlTableSource interface
	public class SqlRawSqlTable : SqlTable, IQueryElement
	{
		public string SQL { get; }

		public ISqlExpression[] Parameters { get; }

		public SqlRawSqlTable(
			MappingSchema    mappingSchema,
			Type             objectType,
			string           sql,
			ISqlExpression[] parameters)
			: base(mappingSchema, objectType)
		{
			SQL        = sql        ?? throw new ArgumentNullException(nameof(sql));
			Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));
		}

		internal SqlRawSqlTable(
			int id, string alias, Type objectType,
			SqlField[]       fields,
			string           sql,
			ISqlExpression[] parameters)
			: base(id, string.Empty, alias, null, null, null, string.Empty, objectType, null, fields, SqlTableType.RawSql, null, TableOptions.NotSet)
		{
			SQL        = sql;
			Parameters = parameters;
		}

		public SqlRawSqlTable(SqlRawSqlTable table, ISqlExpression[] parameters)
		{
			Alias              = table.Alias;
			Server             = table.Server;
			Database           = table.Database;
			Schema             = table.Schema;

			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
			SequenceAttributes = table.SequenceAttributes;

			SQL                = table.SQL;
			Parameters         = parameters;
		}

		public override QueryElementType ElementType  => QueryElementType.SqlRawSqlTable;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb
				.AppendLine("(")
				.Append(SQL)
				.Append(')')
				.AppendLine();
		}

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();

		#endregion

		#region ISqlExpressionWalkable Members

		public override ISqlExpression Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			if (Parameters != null)
				for (var i = 0; i < Parameters.Length; i++)
					Parameters[i] = Parameters[i].Walk(options, func)!;

			return func(this);
		}

		#endregion

	}
}
