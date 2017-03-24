namespace LinqToDB.SqlQuery
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using JetBrains.Annotations;

	public class SqlAnalyticFunction : ISqlExpression
	{
		public class OrderByClause : ISqlExpressionWalkable
		{
			public OrderByClause()
			{
				Items = new List<OrderByItem>();
			}

			public List<OrderByItem> Items    { get; private set; }
			public bool              Siblings { get; set; }

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				foreach (var item in Items)
				{
					((ISqlExpressionWalkable) item).Walk(skipColumns, func);
				}

				return null;
			}
		}

		public enum Nulls
		{
			None,
			First,
			Last
		}

		public class OrderByItem: ISqlExpressionWalkable
		{
			public OrderByItem(ISqlExpression expression, bool isDescending)
			{
				Expression   = expression;
				IsDescending = isDescending;
			}

			public bool IsDescending { get; set; }

			public ISqlExpression Expression { get; set; }
			public Nulls Nulls { get; set; }

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (Expression != null)
					Expression = Expression.Walk(skipColumns, func);
				return null;
			}
		}

		public class AnalyticClause: ICloneableElement, ISqlExpressionWalkable
		{
			public AnalyticClause([CanBeNull] QueryPartitionClause queryPartition, [CanBeNull] OrderByClause orderBy, [CanBeNull] WindowingClause windowing)
			{
				QueryPartition = queryPartition;
				OrderBy        = orderBy;
				Windowing      = windowing;
			}

			public AnalyticClause()
			{
			}

			
			[CanBeNull] public QueryPartitionClause QueryPartition { get; set; }
			[CanBeNull] public OrderByClause        OrderBy        { get; set; }
			[CanBeNull] public WindowingClause      Windowing      { get; set; }

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (QueryPartition != null)
					((ISqlExpressionWalkable) QueryPartition).Walk(skipColumns, func);

				if (OrderBy != null)
					((ISqlExpressionWalkable) OrderBy).Walk(skipColumns, func);

				if (Windowing != null)
					((ISqlExpressionWalkable) Windowing).Walk(skipColumns, func);

				return null;
			}

			#region Implementation of ICloneableElement
			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				throw new NotImplementedException();
			}
			#endregion
		}

		public class QueryPartitionClause : ISqlExpressionWalkable
		{
			[NotNull] public ISqlExpression[] Arguments { get; private set; }

			public QueryPartitionClause([NotNull] ISqlExpression[] arguments)
			{
				if (arguments == null) throw new ArgumentNullException("arguments");
				Arguments = arguments;
			}

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				for (var i = 0; i < Arguments.Length; i++)
				{
					var argument = Arguments[i];
					var walkable = argument as ISqlExpressionWalkable;
					if (walkable != null)
						Arguments[i] = walkable.Walk(skipColumns, func);
				}

				return null;
			}
		}

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

		public class PointExpression : ISqlExpressionWalkable
		{
			public PointExpression(LimitExpressionKind kind, [CanBeNull] ISqlExpression valueExpression)
			{
				Kind = kind;
				ValueExpression = valueExpression;
			}

			public PointExpression()
			{
			}

			public LimitExpressionKind Kind { get; set; }

			[CanBeNull]
			public ISqlExpression ValueExpression { get; set; }

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (ValueExpression != null)
					ValueExpression = func(ValueExpression);
				return null;
			}
		}

		public class WindowingClause : ISqlExpressionWalkable
		{

			public WindowingClause()
			{
			}

			public WindowingClause(BasedOn basedOn, PointExpression start, [CanBeNull] PointExpression end)
			{
				BasedOn = basedOn;
				Start   = start;
				End     = end;
			}

			public BasedOn BasedOn { get; set; }
			[CanBeNull] public PointExpression Start { get; set; }
			[CanBeNull] public PointExpression End   { get; set; }

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
			{
				if (Start != null)
					((ISqlExpressionWalkable) Start).Walk(skipColumns, func);
				return null;
			}
		}

		public SqlAnalyticFunction()
		{
			Analytic  = new AnalyticClause();
			Arguments = new Dictionary<string, object>();
		}

		public SqlAnalyticFunction(Type systemType, string name, AnalyticClause analytic)
			: this(systemType, name, SqlQuery.Precedence.Primary, analytic)
		{
		}

		public SqlAnalyticFunction(Type systemType, string name, int precedence, AnalyticClause analytic)
			: this()
		{
			SystemType = systemType;
			Name       = name;
			Precedence = precedence;
			if (analytic != null)
				Analytic = analytic;
		}

		public Type                       SystemType { get; private set; }
		public string                     Name       { get; private set; }
		public int                        Precedence { get; private set; }
		public AnalyticClause             Analytic   { get; private set; }
		public Dictionary<string, object> Arguments  { get; private set; }

		public bool TryGetArgument<T>(string name, out T argument)
		{
			object value;
			if (Arguments.TryGetValue(name, out value))
			{
				if (value is T)
				{
					argument = (T) value;
					return true;
				}
			}

			argument = default(T);
			return false;
		}

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> action)
		{
			Dictionary<string, object> newValues = null;
			foreach (var pair in Arguments)
			{
				var walkable = pair.Value as ISqlExpressionWalkable;
				if (walkable != null)
				{
					var newValue = walkable.Walk(skipColumns, action);
					if (!ReferenceEquals(newValue, pair.Value))
					{
						if (newValues == null)
							newValues = new Dictionary<string, object>();
						newValues.Add(pair.Key, newValue);
					}
				}
			}

			if (newValues != null)
				foreach (var pair in newValues)
				{
					Arguments[pair.Key] = pair.Value;
				}

			if (Analytic != null)
			{
				((ISqlExpressionWalkable) Analytic).Walk(skipColumns, action);
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
					Name,
					Precedence,
					(AnalyticClause) Analytic.Clone(objectTree, doClone));

				foreach (var pair in Arguments)
				{
					var cloneableParam = pair.Value as ICloneableElement;
					if (cloneableParam != null)
						function.Arguments.Add(pair.Key, cloneableParam.Clone(objectTree, doClone));
					else
						function.Arguments.Add(pair.Key, pair.Value);
				}

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

			if (func == null || Name != func.Name || Arguments.Count != func.Arguments.Count &&
				SystemType != func.SystemType)
				return false;

			foreach (var pair in Arguments)
			{
				object value;
				if (!func.Arguments.TryGetValue(pair.Key, out value))
					return false;
				if (!Equals(pair.Key, value))
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
			//TODO:
//			sb
//				.Append(Name)
//				.Append("(");
//
//			foreach (var p in Arguments)
//			{
//				p.ToString(sb, dic);
//				sb.Append(", ");
//			}
//
//			if (Arguments.Length > 0)
//				sb.Length -= 2;
//
			return sb.Append("()");
		}

		#endregion
	}
}