namespace LinqToDB.SqlQuery
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using JetBrains.Annotations;

	public class SqlAnalyticFunction : ISqlExpression
	{
		#region Enums

		public enum BasedOn
		{
			Rows,
			Range
		}

		public enum LimitExpressionKind
		{
			UnboundedPreceding,
			ValueExprPreceding,
			CurrentRow,
			UnboundedFollowing,
			ValueExprFollowing
		}
		
		#endregion

		public class OrderByClause : IQueryElement, ISqlExpressionWalkable
		{
			public OrderByClause(bool siblings, IEnumerable<OrderByItem> items)
			{
				Siblings = siblings;
				Items    = new List<OrderByItem>(items);
			}

			public OrderByClause()
			{
				Items = new List<OrderByItem>();
			}

			public List<OrderByItem> Items    { get; private set; }
			public bool              Siblings { get; set; }

			#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.AnalyticOrderByClause; } }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: implement	
				return sb.Append("OrderByClause");
			}

			#endregion

			#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				foreach (var item in Items)
				{
					item.Walk(skipColumns, func);
				}

				return null;
			}

			#endregion
		}

		public class OrderByItem: IQueryElement, ISqlExpressionWalkable
		{
			public OrderByItem(ISqlExpression expression, bool isDescending, Sql.NullsPosition nulls)
			{
				Expression   = expression;
				IsDescending = isDescending;
				Nulls = nulls;
			}

			public bool IsDescending { get; set; }

			public ISqlExpression Expression { get; set; }
			public Sql.NullsPosition Nulls   { get; set; }

			#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.AnalyticOrderByItem; } }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: implement	
				return sb.Append("OrderByItem");
			}

			#endregion

			#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (Expression != null)
					Expression = Expression.Walk(skipColumns, func);
				return null;
			}

			#endregion
		}

		public class AnalyticClause: IQueryElement, ISqlExpressionWalkable
		{
			public AnalyticClause([CanBeNull] QueryPartitionClause queryPartition, [CanBeNull] OrderByClause orderBy, [CanBeNull] WindowingClause windowing)
			{
				QueryPartition = queryPartition ?? new QueryPartitionClause(new ISqlExpression[0]);
				OrderBy        = orderBy	    ?? new OrderByClause();
				Windowing      = windowing      ?? new WindowingClause();
			}

			public AnalyticClause()
			{
				QueryPartition = new QueryPartitionClause(new ISqlExpression[0]);
				OrderBy        = new OrderByClause();
				Windowing      = new WindowingClause();
			}

			
			public QueryPartitionClause QueryPartition { get; set; }
			public OrderByClause        OrderBy        { get; set; }
			public WindowingClause      Windowing      { get; set; }

			#region ICloneableElement Members
			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				throw new NotImplementedException();
			}
			#endregion

			#region IQueryElement Members

			public QueryElementType ElementType { get {return QueryElementType.AnalyticClause;} }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: implement	
				return sb.Append("AnalyticClause");
			}

			#endregion

			#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (QueryPartition != null)
					QueryPartition.Walk(skipColumns, func);

				if (OrderBy != null)
					OrderBy.Walk(skipColumns, func);

				if (Windowing != null)
					Windowing.Walk(skipColumns, func);

				return null;
			}

			#endregion
		}

		public class QueryPartitionClause : IQueryElement, ISqlExpressionWalkable
		{
			[NotNull] public ISqlExpression[] Arguments { get; private set; }

			public QueryPartitionClause([NotNull] ISqlExpression[] arguments)
			{
				if (arguments == null) throw new ArgumentNullException("arguments");
				Arguments = arguments;
			}

			#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.QueryPartitionClause; } }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: implement	
				return sb.Append("QueryPartitionClause");
			}

			#endregion

			#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				for (var i = 0; i < Arguments.Length; i++)
				{
					Arguments[i] = Arguments[i].Walk(skipColumns, func);
				}

				return null;
			}

			#endregion
		}

		public class WindowFrameBound : IQueryElement, ISqlExpressionWalkable
		{
			public WindowFrameBound(LimitExpressionKind kind, [CanBeNull] ISqlExpression valueExpression)
			{
				Kind = kind;
				ValueExpression = valueExpression;
			}

			public WindowFrameBound()
			{
			}

			public LimitExpressionKind Kind { get; set; }

			[CanBeNull]
			public ISqlExpression ValueExpression { get; set; }

			#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.WindowFrameBound; } }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: implement	
				return sb.Append("WindowFrameBound");
			}

			#endregion

			#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (ValueExpression != null)
					ValueExpression = func(ValueExpression);
				return null;
			}

			#endregion
		}

		public class WindowingClause : IQueryElement, ISqlExpressionWalkable
		{

			public WindowingClause()
			{
			}

			public WindowingClause(BasedOn basedOn, WindowFrameBound start, [CanBeNull] WindowFrameBound end)
			{
				BasedOn = basedOn;
				Start   = start;
				End     = end;
			}

			public BasedOn          BasedOn { get; set; }
			public WindowFrameBound Start   { get; set; }
			public WindowFrameBound End     { get; set; }

			#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.WindowingClause; } }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: implement	
				return sb.Append("WindowingClause");
			}

			#endregion

			#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (Start != null)
					Start.Walk(skipColumns, func);
				if (End != null)
					End.Walk(skipColumns, func);
				return null;
			}

			#endregion
		}

		public SqlAnalyticFunction()
		{
			Analytic  = new AnalyticClause();
			Arguments = new List<ISqlExpression>();
		}

		public SqlAnalyticFunction(Type systemType, string expression, AnalyticClause analytic)
			: this(systemType, expression, SqlQuery.Precedence.Primary, analytic)
		{
		}

		public SqlAnalyticFunction(Type systemType, string expression, int precedence, AnalyticClause analytic, List<ISqlExpression> arguments)
			: this()
		{
			SystemType = systemType;
			Expression       = expression;
			Precedence = precedence;
			if (analytic != null)
				Analytic = analytic;
			if (arguments != null)
				Arguments = arguments;
		}

		public SqlAnalyticFunction(Type systemType, string expression, int precedence, AnalyticClause analytic)
			: this()
		{
			SystemType = systemType;
			Expression       = expression;
			Precedence = precedence;
			if (analytic != null)
				Analytic = analytic;
		}

		public Type                       SystemType { get; private set; }
		public string                     Expression { get;         set; }
		public int                        Precedence { get; private set; }
		public AnalyticClause             Analytic   { get; private set; }
		public List<ISqlExpression>       Arguments  { get; private set; }

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> action)
		{
			for (var i = 0; i < Arguments.Count; i++)
			{
				Arguments[i] = Arguments[i].Walk(skipColumns, action);
			}

			if (Analytic != null)
			{
				Analytic.Walk(skipColumns, action);
			}

			return action(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return Equals(other, SqlExpression.DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		private bool? _canBeNull;

		public bool CanBeNull
		{
			get { return _canBeNull ?? true; }
			set { _canBeNull = value; }
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
			{
				var function = new SqlAnalyticFunction(
					SystemType,
					Expression,
					Precedence,
					(AnalyticClause) Analytic.Clone(objectTree, doClone),
					Arguments.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToList());

				clone = function;
				objectTree.Add(this, clone);
			}

			return clone;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (this == other)
				return true;

			var func = other as SqlAnalyticFunction;

			if (func == null || Expression != func.Expression || Arguments.Count != func.Arguments.Count &&
				SystemType != func.SystemType)
				return false;

			for (var i = 0; i < Arguments.Count; i++)
			{
				if (!Arguments[i].Equals(func.Arguments[i]))
					return false;
			}

			return comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType
		{
			get { return QueryElementType.SqlAnalyticFunction; }
		}

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			//TODO: Implement
			sb.Append(Expression);
			return sb;
		}

		#endregion

	}
}