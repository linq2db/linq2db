using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlColumn : SqlExpressionBase
	{
		public SqlColumn(SelectQuery? parent, ISqlExpression expression, string? alias)
		{
#if DEBUG
			if (expression is SqlSearchCondition)
			{

			}
#endif

			Parent      = parent;
			Expression  = expression ?? throw new ArgumentNullException(nameof(expression));
			RawAlias    = alias;

#if DEBUG
			Number = System.Threading.Interlocked.Increment(ref _columnCounter);

			// useful for putting breakpoint when finding when SqlColumn was created
			if (Number is 0)
			{

			}
#endif
		}

		public SqlColumn(SelectQuery builder, ISqlExpression expression)
			: this(builder, expression, null)
		{
		}

#if DEBUG
		public int Number { get; }

		static   int _columnCounter;
#endif

		public ISqlExpression Expression
		{
			get;
			set
			{
				if (field == value)
					return;
				if (ReferenceEquals(value, this))
					throw new InvalidOperationException();
				field = value;
			}
		}

		public SelectQuery? Parent { get; set; }

		internal string? RawAlias   { get; set; }

		public ISqlExpression UnderlyingExpression()
		{
			var current = QueryHelper.UnwrapExpression(Expression, true);
			while (current.ElementType == QueryElementType.Column)
			{
				var column      = (SqlColumn)current;
				var columnQuery = column.Parent;
				if (columnQuery == null || columnQuery.HasSetOperators || QueryHelper.EnumerateLevelSources(columnQuery).Skip(1).Any())
					break;
				current = QueryHelper.UnwrapExpression(column.Expression, true);
			}

			return current;
		}

		public string? Alias
		{
			get
			{
				if (RawAlias == null)
					return GetAlias(Expression);

				return RawAlias;
			}
			set => RawAlias = value;
		}

		static string? GetAlias(ISqlExpression? expr)
		{
			switch (expr)
			{
				case SqlField    field  : return field.Alias ?? field.PhysicalName;
				case SqlColumn   column : return column.Alias;
				case SelectQuery query  :
					{
						if (query.Select.Columns.Count == 1 && !string.Equals(query.Select.Columns[0].Alias, "*", StringComparison.Ordinal))
							return query.Select.Columns[0].Alias;
						break;
					}
				case SqlExpression e
					when e.Expr is "{0}": return GetAlias(e.Parameters[0]);
			}

			return null;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				ElementType,
				Expression.GetElementHashCode(),
				Parent?.SourceID ?? -1,
				RawAlias
			);
		}

		public override string ToString()
		{
#if DEBUG
			var writer = new QueryElementTextWriter(NullabilityContext.GetContext(Parent));

			writer
				.Append('t')
				.Append(Parent?.SourceID ?? -1)
				.Append("[Id:").Append(Number).Append(']')
				.Append('.')
				.Append(Alias ?? "c")
				.Append(" => ")
				.AppendElement(Expression);

			var underlying = UnderlyingExpression();
			if (!ReferenceEquals(underlying, Expression))
			{
				writer
					.Append(" := ")
					.AppendElement(underlying);
			}

			if (CanBeNullable(writer.Nullability))
				writer.Append('?');

			return writer.ToString();

#else
			if (Expression is SqlField or SqlColumn)
				return this.ToDebugString();

			return base.ToString()!;
#endif
		}

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			if (nullability.CanBeNull(this))
				return true;

			return false;
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not SqlColumn otherColumn)
				return false;

			if (Parent != otherColumn.Parent)
				return false;

			if (Parent!.HasSetOperators)
				return false;

			return
				Expression.Equals(otherColumn.Expression, comparer) &&
				comparer(this, other);
		}

		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type? SystemType => Expression.SystemType;

		#endregion

		public override QueryElementType ElementType => QueryElementType.Column;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			var parentIndex = -1;
			if (Parent != null)
			{
				parentIndex = Parent.Select.Columns.IndexOf(this);
			}

			writer
				.Append('t')
				.Append(Parent?.SourceID ?? - 1)
#if DEBUG
				.Append('[').Append(Number).Append(']')
#endif
				.Append('.')
				.Append(Alias ?? string.Create(CultureInfo.InvariantCulture, $"c{(parentIndex >= 0 ? parentIndex + 1 : parentIndex)}"));

				if (!Expression.CanBeNullable(writer.Nullability) && CanBeNullable(writer.Nullability))
					writer.Append('?');

			return writer;
		}
	}
}
