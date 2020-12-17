using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlFunction : ISqlExpression//ISqlTableSource
	{
		public SqlFunction(Type systemType, string name, params ISqlExpression[] parameters)
			: this(systemType, name, false, true, SqlQuery.Precedence.Primary, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, bool isPure, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, isPure, SqlQuery.Precedence.Primary, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, true, SqlQuery.Precedence.Primary, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, int precedence, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, true, precedence, parameters)
		{
		}
		
		public SqlFunction(Type systemType, string name, bool isAggregate, bool isPure, int precedence, params ISqlExpression[] parameters)
		{
			//_sourceID = Interlocked.Increment(ref SqlQuery.SourceIDCounter);

			if (systemType == null) throw new ArgumentNullException(nameof(systemType));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var p in parameters)
				if (p == null) throw new ArgumentNullException(nameof(parameters));

			SystemType  = systemType;
			Name        = name;
			Precedence  = precedence;
			IsAggregate = isAggregate;
			IsPure      = isPure;
			Parameters  = parameters;
		}

		public Type             SystemType   { get; }
		public string           Name         { get; }
		public int              Precedence   { get; }
		public bool             IsAggregate  { get; }
		public bool             IsPure       { get; }
		public ISqlExpression[] Parameters   { get; }

		public bool DoNotOptimize { get; set; }

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

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> action)
		{
			for (var i = 0; i < Parameters.Length; i++)
				Parameters[i] = Parameters[i].Walk(options, action)!;

			return action(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return Equals(other, SqlExpression.DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		private bool? _canBeNull;
		public  bool   CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				objectTree.Add(this, clone = new SqlFunction(
					SystemType,
					Name,
					IsAggregate,
					Precedence,
					Parameters.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray())
				{
					CanBeNull = CanBeNull, DoNotOptimize = DoNotOptimize
				});
			}

			return clone;
		}

		int? _hashCode;

		public override int GetHashCode()
		{
			// ReSharper disable NonReadonlyMemberInGetHashCode
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = SystemType.GetHashCode();

			hashCode = unchecked(hashCode + (hashCode * 397) ^ Name.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ CanBeNull.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ DoNotOptimize.GetHashCode());
			for (var i = 0; i < Parameters.Length; i++)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Parameters[i].GetHashCode());

			_hashCode = hashCode;
			return hashCode;
			// ReSharper restore NonReadonlyMemberInGetHashCode
		}

		public bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;


			if (!(other is SqlFunction func) || Name != func.Name || Parameters.Length != func.Parameters.Length && SystemType != func.SystemType)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(func.Parameters[i], comparer))
					return false;

			return comparer(this, func);
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlFunction;

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
