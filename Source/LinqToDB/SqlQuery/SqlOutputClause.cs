﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlOutputClause : IQueryElement, ISqlExpressionWalkable
	{
		private List<SqlSetExpression>? _outputItems;

		public SqlTable?    SourceTable    { get; set; }
		public SqlTable?    InsertedTable  { get; set; }
		public SqlTable?    DeletedTable   { get; set; }
		public SqlTable?    OutputTable    { get; set; }
		public SelectQuery? OutputQuery    { get; set; }

		public bool                   HasOutputItems => _outputItems != null && _outputItems.Count > 0 || OutputQuery != null;
		public List<SqlSetExpression> OutputItems    => _outputItems ??= new List<SqlSetExpression>();

		#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression? ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable?)SourceTable  )?.Walk(options, context, func);
			((ISqlExpressionWalkable?)DeletedTable )?.Walk(options, context, func);
			((ISqlExpressionWalkable?)InsertedTable)?.Walk(options, context, func);
			((ISqlExpressionWalkable?)OutputTable  )?.Walk(options, context, func);

			if (HasOutputItems)
				foreach (var t in OutputItems)
					((ISqlExpressionWalkable)t).Walk(options, context, func);

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.OutputClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("OUTPUT ");

			return sb;
		}

		#endregion
	}
}
