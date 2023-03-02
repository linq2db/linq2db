using System;

namespace LinqToDB.SqlQuery
{
	public class SqlFunction : ISqlExpression//ISqlTableSource
	{
		public SqlFunction(Type systemType, string name, params ISqlExpression[] parameters)
			: this(systemType, name, false, true, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, bool isPure, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, isPure, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, true, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, int precedence, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, bool isPure, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull, params ISqlExpression[] parameters)
		{
			//_sourceID = Interlocked.Increment(ref SqlQuery.SourceIDCounter);

			if (systemType == null) throw new ArgumentNullException(nameof(systemType));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var p in parameters)
				if (p == null) throw new ArgumentNullException(nameof(parameters));

			SystemType      = systemType;
			Name            = name;
			Precedence      = precedence;
			NullabilityType = nullabilityType;
			_canBeNull      = canBeNull;
			IsAggregate     = isAggregate;
			IsPure          = isPure;
			Parameters      = parameters;
		}

		public Type                      SystemType        { get; }
		public string                    Name              { get; }
		public int                       Precedence        { get; }
		public bool                      IsAggregate       { get; }
		public bool                      IsPure            { get; }
		public ISqlExpression[]          Parameters        { get; }
		public bool?                     CanBeNullNullable => _canBeNull;
		public ParametersNullabilityType NullabilityType   { get; }

		public bool DoNotOptimize { get; set; }

		public static SqlFunction CreateCount(Type type, ISqlTableSource table)
		{
			return new SqlFunction(type, "Count", true, true, SqlQuery.Precedence.Primary,
				ParametersNullabilityType.NotNullable, null, new SqlExpression("*", new SqlValue(table.SourceID)));
		}

		public static SqlFunction CreateAll   (SelectQuery subQuery) { return new SqlFunction(typeof(bool), "ALL",    false, SqlQuery.Precedence.Comparison, subQuery); }
		public static SqlFunction CreateSome  (SelectQuery subQuery) { return new SqlFunction(typeof(bool), "SOME",   false, SqlQuery.Precedence.Comparison, subQuery); }
		public static SqlFunction CreateAny   (SelectQuery subQuery) { return new SqlFunction(typeof(bool), "ANY",    false, SqlQuery.Precedence.Comparison, subQuery); }
		public static SqlFunction CreateExists(SelectQuery subQuery) { return new SqlFunction(typeof(bool), "EXISTS", false, SqlQuery.Precedence.Comparison, subQuery); }

		public SqlFunction WithName(string name)
		{
			if (name == Name)
				return this;
			return new SqlFunction(SystemType, name, IsAggregate, IsPure, Precedence, NullabilityType, _canBeNull, Parameters);
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			for (var i = 0; i < Parameters.Length; i++)
				Parameters[i] = Parameters[i].Walk(options, context, func)!;

			return func(context, this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return Equals(other, SqlExpression.DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability)
		{
			return QueryHelper.CalcCanBeNull(_canBeNull, NullabilityType,
				Parameters.Select(p => p.CanBeNullable(nullability)));
		}

		bool?       _canBeNull;
		public  bool   CanBeNull
		{
			get => _canBeNull ?? NullabilityType != ParametersNullabilityType.NotNullable;
			set => _canBeNull = value;
		}

		#endregion

		#region Equals Members

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


			if (!(other is SqlFunction func) || Name != func.Name || Parameters.Length != func.Parameters.Length || SystemType != func.SystemType)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(func.Parameters[i], comparer))
					return false;

			return comparer(this, func);
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlFunction;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.Append(Name)
				.Append('(');

			for (var index = 0; index < Parameters.Length; index++)
			{
				var p = Parameters[index];
				p.ToString(writer);
				if (index < Parameters.Length - 1)
					writer.Append(", ");
			}

			writer.Append(')');

			if (CanBeNullable(writer.Nullability))
				writer.Append("?");

			return writer;
		}

		#endregion

		public void Deconstruct(out Type systemType, out string name)
		{
			systemType = SystemType;
			name       = Name;
		}

		public void Deconstruct(out string name)
		{
			name = Name;
		}

		public void Deconstruct(out Type systemType, out string name, out ISqlExpression[] parameters)
		{
			systemType = SystemType;
			name       = Name;
			parameters = Parameters;
		}
	}
}
