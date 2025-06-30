using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlParameterizedExpressionBase : SqlExpressionBase
	{
		protected SqlParameterizedExpressionBase(
			DbDataType                type,
			string                    exprOrName,
			int                       precedence,
			SqlFlags                  flags,
			ParametersNullabilityType nullabilityType,
			bool?                     canBeNull,
			params ISqlExpression[]   parameters)
		{
			Type              = type;
			ExprOrName        = exprOrName;
			Precedence        = precedence;
			Flags             = flags;
			NullabilityType   = nullabilityType;
			CanBeNullNullable = canBeNull;
			Parameters        = parameters;
		}

		protected string ExprOrName { get; }

		public DbDataType                Type              { get; }
		public ISqlExpression[]          Parameters        { get; }
		public SqlFlags                  Flags             { get; }
		public bool?                     CanBeNullNullable { get; private set; }
		public ParametersNullabilityType NullabilityType   { get; }

		public bool IsAggregate      => (Flags & SqlFlags.IsAggregate     ) != 0;
		public bool IsPure           => (Flags & SqlFlags.IsPure          ) != 0;
		public bool IsPredicate      => (Flags & SqlFlags.IsPredicate     ) != 0;
		public bool IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;

		public bool CanBeNull
		{
			get => CanBeNullNullable ?? NullabilityType != ParametersNullabilityType.NotNullable;
			set => CanBeNullNullable = value;
		}

		#region ISqlExpression Members

		public override Type? SystemType => Type.SystemType;
		public override int   Precedence { get; }

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return QueryHelper.CalcCanBeNull(
				SystemType, CanBeNullNullable, NullabilityType,
				Parameters.Select(p => p.CanBeNullable(nullability)));
		}

		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode != null)
				return _hashCode.Value;

			var hashCode = Type.GetHashCode();

			hashCode = unchecked(hashCode + (hashCode * 397) ^ ExprOrName     .GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ Precedence     .GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ Flags          .GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ NullabilityType.GetHashCode());
			
			if (CanBeNullNullable != null)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ CanBeNullNullable.Value.GetHashCode());

			for (var i = 0; i < Parameters.Length; i++)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Parameters[i].GetHashCode());

			_hashCode = hashCode;

			return hashCode;
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not SqlParameterizedExpressionBase expr
				|| GetType()         != other.GetType()
				|| Type              != expr.Type
				|| ExprOrName        != expr.ExprOrName
				|| Precedence        != expr.Precedence
				|| Flags             != expr.Flags
				|| NullabilityType   != expr.NullabilityType
				|| CanBeNullNullable != expr.CanBeNullNullable
				|| Parameters.Length != expr.Parameters.Length)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(expr.Parameters[i], comparer))
					return false;

			return comparer(this, expr);
		}
		#endregion
	}
}
