﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	public class SqlColumn : IEquatable<SqlColumn>, ISqlExpression
	{
		public SqlColumn(SelectQuery? parent, ISqlExpression expression, string? alias)
		{
			if (expression is SqlSearchCondition)
			{

			}

			Parent      = parent;
			_expression = expression ?? throw new ArgumentNullException(nameof(expression));
			RawAlias    = alias;

#if DEBUG
			Number = Interlocked.Increment(ref _columnCounter);

			// useful for putting breakpoint when finding when SqlColumn was created
			if (Number == 0)
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

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ISqlExpression _expression;

		public ISqlExpression Expression
		{
			get => _expression;
			set
			{
				if (_expression == value)
					return;
				if (value == this)
					throw new InvalidOperationException();
				_expression = value;
				_hashCode   = null;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		SelectQuery? _parent;

		public SelectQuery? Parent
		{
			get => _parent;
			set
			{
				if (_parent == value)
					return;
				_parent   = value;
				_hashCode = null;
			}
		}

		internal string? RawAlias   { get; set; }

		public ISqlExpression UnderlyingExpression()
		{
			var current = QueryHelper.UnwrapExpression(Expression, true);
			while (current.ElementType == QueryElementType.Column)
			{
				var column      = (SqlColumn)current;
				var columnQuery = column.Parent;
				if (columnQuery == null || columnQuery.HasSetOperators || QueryHelper.EnumerateLevelSources(columnQuery).Take(2).Count() > 1)
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
						if (query.Select.Columns.Count == 1 && query.Select.Columns[0].Alias != "*")
							return query.Select.Columns[0].Alias;
						break;
					}
				case SqlExpression e
					when e.Expr is "{0}": return GetAlias(e.Parameters[0]);
			}

			return null;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = Parent?.GetHashCode() ?? 0;

			hashCode = unchecked(hashCode + (hashCode * 397) ^ Expression.GetHashCode());

			_hashCode = hashCode;

			return hashCode;
		}

		public bool Equals(SqlColumn? other)
		{
			if (other == null)
				return false;

			if (ReferenceEquals(this, other))
				return true;

			if (!Equals(Parent, other.Parent))
				return false;

			if (Expression.Equals(other.Expression))
				return false;

			return false;
		}

		public override string ToString()
		{
#if OVERRIDETOSTRING
			var writer = new QueryElementTextWriter(NullabilityContext.GetContext(Parent));

			writer
				.Append('t')
				.Append(Parent?.SourceID ?? -1)
#if DEBUG
				.Append("[Id:").Append(Number).Append(']')
#endif
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
			if (Expression is SqlField)
				return this.ToDebugString();

			return base.ToString()!;
#endif
		}

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability)
		{
			if (nullability.CanBeNull(this))
				return true;

			if (Parent != null)
			{
				if (Parent.HasSetOperators)
				{
					var index = Parent.Select.Columns.IndexOf(this);
					if (index < 0) return true;

					foreach (var set in Parent.SetOperators)
					{
						if (index >= set.SelectQuery.Select.Columns.Count)
							return true;

						if (set.SelectQuery.Select.Columns[index].CanBeNullable(nullability))
							return true;
					}
				}
			}

			return false;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			if (!(other is SqlColumn otherColumn))
				return false;

			if (Parent != otherColumn.Parent)
				return false;

			if (Parent!.HasSetOperators)
				return false;

			return
				Expression.Equals(
					otherColumn.Expression,
					(ex1, ex2) =>
					{
//							var c = ex1 as Column;
//							if (c != null && c.Parent != Parent)
//								return false;
//							c = ex2 as Column;
//							if (c != null && c.Parent != Parent)
//								return false;
						return comparer(ex1, ex2);
					})
				&&
				comparer(this, other);
		}

		public int   Precedence => SqlQuery.Precedence.Primary;
		public Type? SystemType => Expression.SystemType;

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			if (this == other)
				return true;

			return other is SqlColumn column && Equals(column);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.Column;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
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
				.Append(Alias ?? "c" + (parentIndex >= 0 ? parentIndex + 1 : parentIndex));

				if (!Expression.CanBeNullable(writer.Nullability) && CanBeNullable(writer.Nullability))
					writer.Append('?');

			return writer;
		}

		#endregion
	}
}
