using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	using Common.Internal;

	//TODO: Investigate how to implement only ISqlTableSource interface
	public class SqlRawSqlTable : SqlTable, IQueryElement
	{
		public string SQL { get; }

		public ISqlExpression[] Parameters { get; }

		public SqlRawSqlTable(
			EntityDescriptor endtityDescriptor,
			string           sql,
			ISqlExpression[] parameters)
			: base(endtityDescriptor)
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
			: base(id, string.Empty, alias, new (string.Empty), objectType, null, fields, SqlTableType.RawSql, null, TableOptions.NotSet, null)
		{
			SQL        = sql;
			Parameters = parameters;
		}

		public SqlRawSqlTable(SqlRawSqlTable table, ISqlExpression[] parameters)
			: base(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;

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
			using var sb = Pools.StringBuilder.Allocate();
			return ((IQueryElement)this).ToString(sb.Value, new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

		#region IQueryElement Members

		public string SqlText
		{
			get
			{
				using var sb = Pools.StringBuilder.Allocate();
				return ((IQueryElement)this).ToString(sb.Value, new Dictionary<IQueryElement, IQueryElement>()).ToString();
			}
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public override ISqlExpression Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			if (Parameters != null)
				for (var i = 0; i < Parameters.Length; i++)
					Parameters[i] = Parameters[i].Walk(options, context, func)!;

			return func(context, this);
		}

		#endregion

	}
}
