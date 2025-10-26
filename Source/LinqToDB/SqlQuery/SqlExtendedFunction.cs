using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.SqlQuery
{

	public class SqlExtendedFunction : SqlExpressionBase
	{
		public SqlExtendedFunction(DbDataType dbDataType,
			string                            functionName,
			IEnumerable<SqlFunctionArgument>  arguments,
			bool[]                            argumentsNullability,
			bool?                             canBeNull              = null,
			IEnumerable<SqlWindowOrderItem>?  withinGroup            = null,
			IEnumerable<ISqlExpression>?      partitionBy            = null,
			IEnumerable<SqlWindowOrderItem>?  orderBy                = null,
			SqlSearchCondition?               filter                 = null,
			SqlFrameClause?                   frameClause            = null,
			bool                              isAggregate            = false,
			bool                              canBeAffectedByOrderBy = false)
		{
			Type                   = dbDataType;
			FunctionName           = functionName;
			ArgumentsNullability   = argumentsNullability;
			CanBeNull              = canBeNull;
			Arguments              = arguments.ToList();
			WithinGroup            = withinGroup?.ToList();
			PartitionBy            = partitionBy?.ToList();
			OrderBy                = orderBy?.ToList();
			FrameClause            = frameClause;
			Filter                 = filter;
			IsAggregate            = isAggregate;
			CanBeAffectedByOrderBy = canBeAffectedByOrderBy;
		}

		public DbDataType                Type                   { get; }
		public string                    FunctionName           { get; }
		public bool[]                    ArgumentsNullability   { get; }
		public bool?                     CanBeNull              { get; }
		public List<SqlFunctionArgument> Arguments              { get; private set; }
		public List<SqlWindowOrderItem>? WithinGroup            { get; private set; }
		public List<ISqlExpression>?     PartitionBy            { get; private set; }
		public List<SqlWindowOrderItem>? OrderBy                { get; private set; }
		public SqlFrameClause?           FrameClause            { get; private set; }
		public SqlSearchCondition?       Filter                 { get; private set; }
		public bool                      IsAggregate            { get; }
		public bool                      CanBeAffectedByOrderBy { get; }

		public void Modify(List<SqlFunctionArgument> arguments,
			List<SqlWindowOrderItem>?                withinGroup,
			List<ISqlExpression>?                    partitionBy,
			List<SqlWindowOrderItem>?                orderBy,
			SqlSearchCondition?                      filter,
			SqlFrameClause?                          frameClause)
		{
			Arguments   = arguments;
			WithinGroup = withinGroup;
			PartitionBy = partitionBy;
			OrderBy     = orderBy;
			Filter      = filter;
			FrameClause = frameClause;
		}

		public SqlExtendedFunction WithType(DbDataType dbDataType)
		{
			return new SqlExtendedFunction(
				dbDataType,
				FunctionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				WithinGroup,
				PartitionBy,
				OrderBy,
				Filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithFunctionName(string functionName)
		{
			return new SqlExtendedFunction(
				Type,
				functionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				WithinGroup,
				PartitionBy,
				OrderBy,
				Filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithArguments(IEnumerable<SqlFunctionArgument> arguments, bool[] argumentsNullability)
		{
			return new SqlExtendedFunction(
				Type,
				FunctionName,
				arguments,
				argumentsNullability,
				CanBeNull,
				WithinGroup,
				PartitionBy,
				OrderBy,
				Filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithPartitionBy(IEnumerable<ISqlExpression>? partitionBy)
		{
			return new SqlExtendedFunction(
				Type,
				FunctionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				WithinGroup,
				partitionBy,
				OrderBy,
				Filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithOrderBy(IEnumerable<SqlWindowOrderItem>? orderBy)
		{
			return new SqlExtendedFunction(
				Type,
				FunctionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				WithinGroup,
				PartitionBy,
				orderBy,
				Filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithFrameClause(SqlFrameClause? frameClause)
		{
			return new SqlExtendedFunction(
				Type,
				FunctionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				WithinGroup,
				PartitionBy,
				OrderBy,
				Filter,
				frameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithFilter(SqlSearchCondition? filter)
		{
			return new SqlExtendedFunction(
				Type,
				FunctionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				WithinGroup,
				PartitionBy,
				OrderBy,
				filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		public SqlExtendedFunction WithWithinGroup(IEnumerable<SqlWindowOrderItem>? withinGroup)
		{
			return new SqlExtendedFunction(
				Type,
				FunctionName,
				Arguments,
				ArgumentsNullability,
				CanBeNull,
				withinGroup,
				PartitionBy,
				OrderBy,
				Filter,
				FrameClause, 
				IsAggregate,
				CanBeAffectedByOrderBy);
		}

		static bool CheckNulls(object? expr1, object? expr2)
		{
			return expr1 == null && expr2 == null || expr1 != null && expr2 != null;
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlExtendedFunction otherFunction)
				return false;

			if (FunctionName != otherFunction.FunctionName)
				return false;

			if (Arguments.Count != otherFunction.Arguments.Count)
				return false;

			if (!CheckNulls(FrameClause, otherFunction.FrameClause))
				return false;

			if (FrameClause != null && !FrameClause.Equals(otherFunction.FrameClause))
				return false;

			if (!CheckNulls(Filter, otherFunction.Filter))
				return false;

			if (Filter != null && !Filter.Equals(otherFunction.Filter!, comparer))
				return false;

			foreach (var argument in Arguments)
			{
				if (!otherFunction.Arguments.Any(a => argument.Modifier == a.Modifier && argument.Expression.Equals(a.Expression, comparer) && argument.Suffix.AreEqual(a.Suffix, comparer)))
					return false;
			}

			if (!CheckNulls(PartitionBy, otherFunction.PartitionBy))
				return false;

			if (PartitionBy != null && PartitionBy.Count != otherFunction.PartitionBy!.Count)
				return false;

			if (PartitionBy != null)
			{
				for (var i = 0; i < PartitionBy.Count; i++)
				{
					if (!PartitionBy[i].Equals(otherFunction.PartitionBy![i], comparer))
						return false;
				}
			}

			if (!CheckNulls(OrderBy, otherFunction.OrderBy))
				return false;

			if (OrderBy != null && OrderBy.Count != otherFunction.OrderBy!.Count)
				return false;

			if (OrderBy != null)
			{
				for (var i = 0; i < OrderBy.Count; i++)
				{
					if (OrderBy[i].IsDescending != otherFunction.OrderBy![i].IsDescending || !OrderBy[i].Expression.Equals(otherFunction.OrderBy![i].Expression, comparer))
						return false;
				}
			}

			return true;
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			if (CanBeNull.HasValue)
				return CanBeNull.Value;

			return Arguments.Any(a => a.Expression.CanBeNullable(nullability)) ||
				   (PartitionBy?.Any(p => p.CanBeNullable(nullability)) ?? false) ||
				   (OrderBy?.Any(o => o.Expression.CanBeNullable(nullability)) ?? false) ||
				   (Filter?.CanBeNullable(nullability) ?? false);
		}

		public override int Precedence => SqlQuery.Precedence.Primary;

		public override Type SystemType => Type.SystemType;

		public override QueryElementType ElementType => QueryElementType.SqlExtendedFunction;

		public bool IsWindowFunction => OrderBy?.Count > 0 || PartitionBy?.Count > 0 || FrameClause != null;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				//.DebugAppendUniqueId(this)
				.Append(FunctionName)
				.Append('(');

			for (var i = 0; i < Arguments.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");
				writer.AppendElement(Arguments[i]);
			}

			writer.Append(')');

			if (WithinGroup != null && WithinGroup.Count > 0)
			{
				writer.Append(" WITHIN GROUP (ORDER BY ");
				for (var i = 0; i < WithinGroup.Count; i++)
				{
					if (i > 0)
						writer.Append(", ");
					writer.AppendElement(WithinGroup[i]);
				}

				writer.Append(')');
			}

			if (Filter != null)
			{
				writer.Append(" FILTER (WHERE ");
				writer.AppendElement(Filter);
				writer.Append(')');
			}

			if (PartitionBy is { Count: > 0 } || OrderBy is { Count: > 0 } || FrameClause != null)
			{
				writer.Append(" OVER (");

				if (PartitionBy != null && PartitionBy.Count > 0)
				{
					writer.Append("PARTITION BY ");
					for (var i = 0; i < PartitionBy.Count; i++)
					{
						if (i > 0)
							writer.Append(", ");
						writer.AppendElement(PartitionBy[i]);
					}
				}

				if (OrderBy != null && OrderBy.Count > 0)
				{
					if (PartitionBy != null && PartitionBy.Count > 0)
						writer.Append(' ');

					writer.Append("ORDER BY ");
					for (var i = 0; i < OrderBy.Count; i++)
					{
						if (i > 0)
							writer.Append(", ");
						writer.AppendElement(OrderBy[i]);
					}
				}

				if (FrameClause != null)
				{
					writer.Append(' ');
					writer.AppendElement(FrameClause);
				}

				writer.Append(')');
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(FunctionName);
			hash.Add(Type);

			if (WithinGroup != null)
			{
				foreach (var item in WithinGroup)
				{
					hash.Add(item.GetElementHashCode());
				}
			}

			if (PartitionBy != null)
			{
				foreach (var item in PartitionBy)
				{
					hash.Add(item.GetElementHashCode());
				}
			}

			if (OrderBy != null)
			{
				foreach (var item in OrderBy)
				{
					hash.Add(item.GetElementHashCode());
				}
			}

			hash.Add(FrameClause?.GetElementHashCode());
			hash.Add(Filter?.GetElementHashCode());
			hash.Add(IsAggregate);

			foreach (var t in Arguments)
			{
				hash.Add(t.GetElementHashCode());
			}

			foreach (var t in ArgumentsNullability)
			{
				hash.Add(t);
			}

			return hash.ToHashCode();
		}
	}

	public class SqlFunctionArgument : QueryElement
	{
		public SqlFunctionArgument(ISqlExpression expression, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, ISqlExpression? suffix = default)
		{
			Expression = expression;
			Modifier   = modifier;
			Suffix     = suffix;
		}

		public ISqlExpression        Expression { get; private set; }
		public Sql.AggregateModifier Modifier   { get; }
		public ISqlExpression?       Suffix     { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlFunctionArgument;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (Modifier != Sql.AggregateModifier.None)
			{
				writer
					.Append(Modifier.ToString().ToUpper(CultureInfo.InvariantCulture))
					.Append(' ');

			}

			writer.AppendElement(Expression);
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(Expression.GetElementHashCode());
			hash.Add(Modifier);
			hash.Add(Suffix?.GetElementHashCode());

			return hash.ToHashCode();
		}

		public void Modify(ISqlExpression sqlExpression, ISqlExpression? suffix)
		{
			Expression = sqlExpression;
			Suffix     = suffix;
		}

		public SqlFunctionArgument WithExpression(SqlConditionExpression sqlExpression)
		{
			if (ReferenceEquals(Expression, sqlExpression))
				return this;

			return new SqlFunctionArgument(sqlExpression, Modifier, Suffix);
		}
	}

	public class SqlFrameBoundary : QueryElement
	{
		public enum FrameBoundaryType
		{
			Unbounded,
			CurrentRow,
			Offset
		}

		public SqlFrameBoundary(bool isPreceding, FrameBoundaryType boundaryType, ISqlExpression? offset)
		{
			IsPreceding  = isPreceding;
			BoundaryType = boundaryType;
			Offset       = offset;
		}

		public bool              IsPreceding  { get; }
		public FrameBoundaryType BoundaryType { get; }
		public ISqlExpression?   Offset       { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlFrameBoundary;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			switch (BoundaryType)
			{
				case FrameBoundaryType.Unbounded:
					writer.Append(IsPreceding ? "UNBOUNDED PRECEDING" : "UNBOUNDED FOLLOWING");
					break;
				case FrameBoundaryType.CurrentRow:
					writer.Append("CURRENT ROW");
					break;
				case FrameBoundaryType.Offset:
					writer.AppendElement(Offset);
					writer.Append(IsPreceding ? " PRECEDING" : " FOLLOWING");
					break;
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(IsPreceding);
			hash.Add(BoundaryType);
			hash.Add(Offset?.GetElementHashCode());

			return hash.ToHashCode();
		}

		public void Modify(ISqlExpression offset)
		{
			if (BoundaryType != FrameBoundaryType.Offset)
				throw new InvalidOperationException("Cannot modify non-offset boundary");
			Offset = offset;
		}
	}

	public class SqlWindowOrderItem : QueryElement
	{
		public SqlWindowOrderItem(ISqlExpression expression, bool isDescending, Sql.NullsPosition nullsPosition)
		{
			Expression    = expression;
			IsDescending  = isDescending;
			NullsPosition = nullsPosition;
		}

		public ISqlExpression    Expression    { get; private set; }
		public bool              IsDescending  { get; }
		public Sql.NullsPosition NullsPosition { get; set; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlWindowOrderItem;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			// writer.DebugAppendUniqueId(this);
			writer.AppendElement(Expression);

			if (IsDescending)
				writer.Append(" DESC");

			if (NullsPosition != Sql.NullsPosition.None)
				writer.Append(" NULLS ").Append(NullsPosition.ToString().ToUpper(CultureInfo.InvariantCulture));

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(Expression.GetElementHashCode());
			hash.Add(IsDescending);
			hash.Add(NullsPosition);

			return hash.ToHashCode();
		}

		public void Modify(ISqlExpression expression)
		{
			Expression = expression;
		}

		public override string ToString()
		{
			return this.ToDebugString();
		}

		#endregion
	}

	public class SqlFrameClause : QueryElement
	{
		public enum FrameTypeKind
		{
			Rows,
			Range,
			Groups
		}

		public SqlFrameClause(FrameTypeKind frameType, SqlFrameBoundary start, SqlFrameBoundary end)
		{
			FrameType = frameType;
			Start     = start;
			End       = end;
		}

		public FrameTypeKind    FrameType { get; }
		public SqlFrameBoundary Start     { get; private set; }
		public SqlFrameBoundary End       { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlFrameClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append(FrameType).Append(" BETWEEN ");
			Start.ToString(writer);
			writer.Append(" AND ");
			End.ToString(writer);
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(FrameType);
			hash.Add(Start.GetElementHashCode());
			hash.Add(End.GetElementHashCode());

			return hash.ToHashCode();
		}

		protected bool Equals(SqlFrameClause other)
		{
			return FrameType == other.FrameType && Start.Equals(other.Start) && End.Equals(other.End);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((SqlFrameClause)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = FrameType.GetHashCode();
				hashCode = (hashCode * 397) ^ Start.GetHashCode();
				hashCode = (hashCode * 397) ^ End.GetHashCode();
				return hashCode;
			}
		}

		public void Modify(SqlFrameBoundary start, SqlFrameBoundary end)
		{
			Start = start;
			End   = end;
		}
	}
}
