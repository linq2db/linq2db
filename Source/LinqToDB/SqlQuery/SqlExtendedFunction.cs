using System;
using System.Collections.Generic;
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

			if (CanBeAffectedByOrderBy != otherFunction.CanBeAffectedByOrderBy)
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
			hash.Add(CanBeAffectedByOrderBy);

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
}
