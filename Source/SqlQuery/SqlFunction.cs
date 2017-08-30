using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlFunction : ISqlExpression//ISqlTableSource
	{
		public SqlFunction(Type systemType, string name, params ISqlExpression[] parameters)
			: this(systemType, name, false, SqlQuery.Precedence.Primary, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, SqlQuery.Precedence.Primary, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, int precedence, params ISqlExpression[] parameters)
		{
			//_sourceID = Interlocked.Increment(ref SqlQuery.SourceIDCounter);

			if (parameters == null) throw new ArgumentNullException("parameters");

			foreach (var p in parameters)
				if (p == null) throw new ArgumentNullException("parameters");

			SystemType  = systemType;
			Name        = name;
			Precedence  = precedence;
			IsAggregate = isAggregate;
			Parameters  = parameters;
		}

		public Type             SystemType  { get; private set; }
		public string           Name        { get; private set; }
		public int              Precedence  { get; private set; }
		public bool             IsAggregate { get; private set; }
		public ISqlExpression[] Parameters  { get; private set; }

		public static SqlFunction CreateCount (Type type, ISqlTableSource table) { return new SqlFunction(type, "Count", true, new SqlExpression("*")); }

		public static SqlFunction CreateAll   (SelectQuery subQuery) { return new SqlFunction(typeof(bool), "ALL",    false, SqlQuery.Precedence.Comparison, subQuery); }
		public static SqlFunction CreateSome  (SelectQuery subQuery) { return new SqlFunction(typeof(bool), "SOME",   false, SqlQuery.Precedence.Comparison, subQuery); }
		public static SqlFunction CreateAny   (SelectQuery subQuery) { return new SqlFunction(typeof(bool), "ANY",    false, SqlQuery.Precedence.Comparison, subQuery); }
		public static SqlFunction CreateExists(SelectQuery subQuery) { return new SqlFunction(typeof(bool), "EXISTS", false, SqlQuery.Precedence.Comparison, subQuery); }

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> action)
		{
			for (var i = 0; i < Parameters.Length; i++)
				Parameters[i] = Parameters[i].Walk(skipColumns, action);

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
		public  bool   CanBeNull
		{
			get { return _canBeNull ?? true; }
			set { _canBeNull = value;        }
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
			{
				objectTree.Add(this, clone = new SqlFunction(
					SystemType,
					Name,
					IsAggregate,
					Precedence, 
					Parameters.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray()));
			}

			return clone;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			var func = other as SqlFunction;

			if (func == null || Name != func.Name || Parameters.Length != func.Parameters.Length && SystemType != func.SystemType)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(func.Parameters[i], comparer))
					return false;

			return comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlFunction; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb
				.Append(Name)
				.Append("(");

			foreach (var p in Parameters)
			{
				p.ToString(sb, dic);
				sb.Append(", ");
			}

			if (Parameters.Length > 0)
				sb.Length -= 2;

			return sb.Append(")");
		}

		#endregion
	}
}
