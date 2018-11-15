using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlColumn : IEquatable<SqlColumn>, ISqlExpression
	{
		public SqlColumn(SelectQuery parent, ISqlExpression expression, string alias)
		{
			Parent     = parent;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			RawAlias     = alias;

#if DEBUG
			_columnNumber = ++_columnCounter;
#endif
		}

		public SqlColumn(SelectQuery builder, ISqlExpression expression)
			: this(builder, expression, null)
		{
		}

#if DEBUG
		readonly int _columnNumber;
		static   int _columnCounter;
#endif

		public   ISqlExpression Expression { get; set; }
		public   SelectQuery    Parent     { get; set; }
		internal string         RawAlias   { get; set; }

		public string Alias
		{
			get
			{
				if (RawAlias == null)
				{
					switch (Expression)
					{
						case SqlField    field  : return field.Alias ?? field.PhysicalName;
						case SqlColumn   column : return column.Alias;
						case SelectQuery query:
							{
								if (query.Select.Columns.Count == 1 && query.Select.Columns[0].Alias != "*")
									return query.Select.Columns[0].Alias;
								break;
							}
					}
				}

				return RawAlias;
			}
			set => RawAlias = value;
		}

		private bool   _underlyingColumnSet;

		private SqlColumn _underlyingColumn;
		public  SqlColumn  UnderlyingColumn
		{
			get
			{
				if (_underlyingColumnSet)
					return _underlyingColumn;

				var columns = new List<SqlColumn>(10);
				var column  = Expression as SqlColumn;

				while (column != null)
				{
					if (column._underlyingColumn != null)
					{
						columns.Add(column._underlyingColumn);
						break;
					}

					columns.Add(column);
					column = column.Expression as SqlColumn;
				}

				_underlyingColumnSet = true;
				if (columns.Count == 0)
					return null;

				_underlyingColumn = columns[columns.Count - 1];

				for (var i = 0; i < columns.Count - 1; i++)
				{
					var c = columns[i];
					c._underlyingColumn    = _underlyingColumn;
					c._underlyingColumnSet = true;
				}

				return _underlyingColumn;
			}
		}

		public bool Equals(SqlColumn other)
		{
			if (other == null)
				return false;

			if (!Equals(Parent, other.Parent))
				return false;

			if (Expression.Equals(other.Expression))
				return true;

			//return false;
			return UnderlyingColumn != null && UnderlyingColumn.Equals(other.UnderlyingColumn);

			//var found =
			//
			//	|| new QueryVisitor().Find(other, e =>
			//		{
			//			switch(e.ElementType)
			//			{
			//				case QueryElementType.Column: return ((Column)e).Expression.Equals(Expression);
			//			}
			//			return false;
			//		}) != null
			//	|| new QueryVisitor().Find(Expression, e =>
			//		{
			//			switch (e.ElementType)
			//			{
			//				case QueryElementType.Column: return ((Column)e).Expression.Equals(other.Expression);
			//			}
			//			return false;
			//		}) != null;

			//return found;
		}

		public override string ToString()
		{
#if OVERRIDETOSTRING
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
#else
			if (Expression is SqlField)
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();

			return base.ToString();
#endif
		}

		#region ISqlExpression Members

		public bool CanBeNull => Expression.CanBeNull;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			var otherColumn = other as SqlColumn;

			if (otherColumn == null)
				return false;

			if (Parent != otherColumn.Parent)
				return false;

			if (Parent.HasUnion)
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

		public int  Precedence => SqlQuery.Precedence.Primary;
		public Type SystemType => Expression.SystemType;

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var parent = (SelectQuery)Parent.Clone(objectTree, doClone);

			if (!objectTree.TryGetValue(this, out var clone))
				objectTree.Add(this, clone = new SqlColumn(
					parent,
					(ISqlExpression)Expression.Clone(objectTree, doClone),
					RawAlias));

			return clone;
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			if (this == other)
				return true;

			return other is SqlColumn && Equals((SqlColumn)other);
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (!(skipColumns && Expression is SqlColumn))
				Expression = Expression.Walk(skipColumns, func);

			return func(this);
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.Column;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);

			sb
				.Append('t')
				.Append(Parent.SourceID)
				.Append(".");

#if DEBUG
			sb.Append('[').Append(_columnNumber).Append(']');
#endif

			if (Expression is SelectQuery)
			{
				sb.Append("(\n\t\t");
				var len = sb.Length;
				Expression.ToString(sb, dic).Replace("\n", "\n\t\t", len, sb.Length - len);
				sb.Append("\n\t)");
			}
			else
			{
				Expression.ToString(sb, dic);
			}

			dic.Remove(this);

			return sb;
		}

		#endregion
	}
}
