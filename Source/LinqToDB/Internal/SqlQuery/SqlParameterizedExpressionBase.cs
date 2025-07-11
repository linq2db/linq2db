using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common;

namespace LinqToDB.Internal.SqlQuery
{
	public abstract class SqlParameterizedExpressionBase : SqlExpressionBase
	{
		protected SqlParameterizedExpressionBase(
			DbDataType type,
			string exprOrName,
			int precedence,
			SqlFlags flags,
			ParametersNullabilityType nullabilityType,
			bool? canBeNull,
			params ISqlExpression[] parameters)
		{
			Type = type;
			ExprOrName = exprOrName;
			Precedence = precedence;
			Flags = flags;
			NullabilityType = nullabilityType;
			CanBeNullNullable = canBeNull;
			Parameters = parameters;
		}

		protected string ExprOrName { get; }

		public DbDataType Type { get; }
		public ISqlExpression[] Parameters { get; }
		public SqlFlags Flags { get; }
		public bool? CanBeNullNullable { get; private set; }
		public ParametersNullabilityType NullabilityType { get; }

		public bool IsAggregate => (Flags & SqlFlags.IsAggregate) != 0;
		public bool IsPure => (Flags & SqlFlags.IsPure) != 0;
		public bool IsPredicate => (Flags & SqlFlags.IsPredicate) != 0;
		public bool IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;

		public bool CanBeNull
		{
			get => CanBeNullNullable ?? NullabilityType != ParametersNullabilityType.NotNullable;
			set => CanBeNullNullable = value;
		}

		#region ISqlExpression Members

		public override Type? SystemType => Type.SystemType;
		public override int Precedence { get; }

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return QueryHelper.CalcCanBeNull(
				SystemType, CanBeNullNullable, NullabilityType,
				Parameters.Select(p => p.CanBeNullable(nullability)));
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Type);
			hash.Add(ExprOrName);
			hash.Add(Precedence);
			hash.Add(Flags);
			hash.Add(NullabilityType);
			hash.Add(CanBeNullNullable);

			foreach (var parameter in Parameters)
				hash.Add(parameter.GetElementHashCode());

			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not SqlParameterizedExpressionBase expr
				|| GetType() != other.GetType()
				|| Type != expr.Type
				|| ExprOrName != expr.ExprOrName
				|| Precedence != expr.Precedence
				|| Flags != expr.Flags
				|| NullabilityType != expr.NullabilityType
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
