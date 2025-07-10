using System;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlFunction : SqlParameterizedExpressionBase
	{
		private const SqlFlags                  DefaultFlags       = SqlFlags.IsPure;
		private const ParametersNullabilityType DefaultNullability = ParametersNullabilityType.IfAnyParameterNullable;

		public SqlFunction(DbDataType type, string name, params ISqlExpression[] parameters)
			: this(type, name, DefaultFlags, DefaultNullability, null, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, bool? canBeNull, params ISqlExpression[] parameters)
			: this(type, name, DefaultFlags, DefaultNullability, canBeNull, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, SqlFlags flags, params ISqlExpression[] parameters)
			: this(type, name, flags, DefaultNullability, null, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, bool isAggregate, bool? canBeNull, params ISqlExpression[] parameters)
			: this(type, name, isAggregate ? SqlFlags.IsAggregate | SqlFlags.IsPure : SqlFlags.IsPure, DefaultNullability, canBeNull, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, bool isAggregate, ParametersNullabilityType parametersNullability, params ISqlExpression[] parameters)
			: this(type, name, isAggregate ? SqlFlags.IsAggregate | SqlFlags.IsPure : SqlFlags.IsPure, parametersNullability, null, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, ParametersNullabilityType parametersNullability, params ISqlExpression[] parameters)
			: this(type, name, DefaultFlags, parametersNullability, null, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, bool isAggregate, ParametersNullabilityType parametersNullability, bool? canBeNull, params ISqlExpression[] parameters)
			: this(type, name, isAggregate ? SqlFlags.IsAggregate | SqlFlags.IsPure : SqlFlags.IsPure, parametersNullability, canBeNull, parameters)
		{
		}

		public SqlFunction(DbDataType type, string name, ParametersNullabilityType parametersNullability, bool? canBeNull, params ISqlExpression[] parameters)
			: this(type, name, DefaultFlags, parametersNullability, canBeNull, parameters)
		{
		}

		public SqlFunction(
			DbDataType                type,
			string                    name,
			SqlFlags                  flags,
			ParametersNullabilityType nullabilityType,
			bool?                     canBeNull,
			params ISqlExpression[]   parameters)
			: base(type, name, SqlQuery.Precedence.Primary, flags, nullabilityType, canBeNull, parameters)
		{
		}

		public string Name => ExprOrName;

		public bool DoNotOptimize { get; set; }

		#region Equals Members

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(base.GetElementHashCode());
			hash.Add(DoNotOptimize);
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (!base.Equals(other, comparer)
				|| other is SqlFunction func && DoNotOptimize != func.DoNotOptimize)
				return false;

			return comparer(this, other!);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlFunction;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);

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
	}
}
