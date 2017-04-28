using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Extensions;
	using Reflection;

	[DebuggerDisplay("SQL = {SqlText}")]
	public class SelectQuery : ISqlTableSource
	{
		#region Init

		public SelectQuery()
		{
			SourceID = Interlocked.Increment(ref SourceIDCounter);

			_select  = new SelectClause (this);
			_from    = new FromClause   (this);
			_where   = new WhereClause  (this);
			_groupBy = new GroupByClause(this);
			_having  = new WhereClause  (this);
			_orderBy = new OrderByClause(this);
		}

		internal SelectQuery(int id)
		{
			SourceID = id;
		}

		internal void Init(
			InsertClause         insert,
			UpdateClause         update,
			DeleteClause         delete,
			SelectClause         select,
			FromClause           from,
			WhereClause          where,
			GroupByClause        groupBy,
			WhereClause          having,
			OrderByClause        orderBy,
			List<Union>          unions,
			SelectQuery          parentSelect,
			CreateTableStatement createTable,
			bool                 parameterDependent,
			List<SqlParameter>   parameters)
		{
			_insert              = insert;
			_update              = update;
			_delete              = delete;
			_select              = select;
			_from                = from;
			_where               = where;
			_groupBy             = groupBy;
			_having              = having;
			_orderBy             = orderBy;
			_unions              = unions;
			ParentSelect         = parentSelect;
			_createTable         = createTable;
			IsParameterDependent = parameterDependent;

			_parameters.AddRange(parameters);

			foreach (var col in select.Columns)
				col.Parent = this;

			_select. SetSqlQuery(this);
			_from.   SetSqlQuery(this);
			_where.  SetSqlQuery(this);
			_groupBy.SetSqlQuery(this);
			_having. SetSqlQuery(this);
			_orderBy.SetSqlQuery(this);
		}

		readonly List<SqlParameter> _parameters = new List<SqlParameter>();
		public   List<SqlParameter>  Parameters
		{
			get { return _parameters; }
		}

		private List<object> _properties;
		public  List<object>  Properties
		{
			get { return _properties ?? (_properties = new List<object>()); }
		}

		public bool        IsParameterDependent { get; set; }
		public SelectQuery ParentSelect         { get; set; }

		public bool IsSimple
		{
			get { return !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty; }
		}

		private QueryType _queryType = QueryType.Select;
		public  QueryType  QueryType
		{
			get { return _queryType;  }
			set { _queryType = value; }
		}

		public bool IsCreateTable    { get { return _queryType == QueryType.CreateTable;    } }
		public bool IsSelect         { get { return _queryType == QueryType.Select;         } }
		public bool IsDelete         { get { return _queryType == QueryType.Delete;         } }
		public bool IsInsertOrUpdate { get { return _queryType == QueryType.InsertOrUpdate; } }
		public bool IsInsert         { get { return _queryType == QueryType.Insert || _queryType == QueryType.InsertOrUpdate; } }
		public bool IsUpdate         { get { return _queryType == QueryType.Update || _queryType == QueryType.InsertOrUpdate; } }

		#endregion

		#region Column

		public class Column : IEquatable<Column>, ISqlExpression
		{
			public Column(SelectQuery parent, ISqlExpression expression, string alias)
			{
				if (expression == null) throw new ArgumentNullException("expression");

				Parent     = parent;
				Expression = expression;
				_alias     = alias;

#if DEBUG
				_columnNumber = ++_columnCounter;
#endif
			}

			public Column(SelectQuery builder, ISqlExpression expression)
				: this(builder, expression, null)
			{
			}

#if DEBUG
			readonly int _columnNumber;
			static   int _columnCounter;
#endif

			public ISqlExpression Expression { get; set; }
			public SelectQuery    Parent     { get; set; }

			internal string _alias;
			public   string  Alias
			{
				get
				{
					if (_alias == null)
					{
						if (Expression is SqlField)
						{
							var field = (SqlField)Expression;
							return field.Alias ?? field.PhysicalName;
						}

						if (Expression is Column)
						{
							var col = (Column)Expression;
							return col.Alias;
						}
					}

					return _alias;
				}
				set { _alias = value; }
			}

			private bool   _underlyingColumnSet = false;
			private Column _underlyingColumn;
			private Column  UnderlyingColumn
			{
				get
				{
					if (_underlyingColumnSet)
						return _underlyingColumn;

					var columns = new List<Column>(10);

					var column = Expression as Column;

					while (column != null)
					{
						if (column._underlyingColumn != null)
						{
							columns.Add(column._underlyingColumn);
							break;
						}

						columns.Add(column);
						column = column.Expression as Column;
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

			public bool Equals(Column other)
			{
				if (other == null)
					return false;

				if (!object.Equals(Parent, other.Parent))
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

			public bool CanBeNull
			{
				get { return Expression.CanBeNull; }
			}

			public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
			{
				if (this == other)
					return true;

				return
					other is Column &&
					Expression.Equals(((Column)other).Expression, comparer) &&
					comparer(this, other);
			}

			public int Precedence
			{
				get { return SqlQuery.Precedence.Primary; }
			}

			public Type SystemType
			{
				get { return Expression.SystemType; }
			}

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				var parent = (SelectQuery)Parent.Clone(objectTree, doClone);

				if (!objectTree.TryGetValue(this, out clone))
					objectTree.Add(this, clone = new Column(
						parent,
						(ISqlExpression)Expression.Clone(objectTree, doClone),
						_alias));

				return clone;
			}

#endregion

#region IEquatable<ISqlExpression> Members

			bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
			{
				if (this == other)
					return true;

				return other is Column && Equals((Column)other);
			}

#endregion

#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				if (!(skipColumns && Expression is Column))
					Expression = Expression.Walk(skipColumns, func);

				return func(this);
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.Column; } }

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
					sb
						.Append("(\n\t\t");
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

#endregion

#region TableSource

		public class TableSource : ISqlTableSource
		{
			public TableSource(ISqlTableSource source, string alias)
				: this(source, alias, null)
			{
			}

			public TableSource(ISqlTableSource source, string alias, params JoinedTable[] joins)
			{
				if (source == null) throw new ArgumentNullException("source");

				Source = source;
				_alias = alias;

				if (joins != null)
					_joins.AddRange(joins);
			}

			public TableSource(ISqlTableSource source, string alias, IEnumerable<JoinedTable> joins)
			{
				if (source == null) throw new ArgumentNullException("source");

				Source = source;
				_alias = alias;

				if (joins != null)
					_joins.AddRange(joins);
			}

			public ISqlTableSource Source       { get; set; }
			public SqlTableType    SqlTableType { get { return Source.SqlTableType; } }

			// TODO: remove internal.
			internal string _alias;
			public   string  Alias
			{
				get
				{
					if (string.IsNullOrEmpty(_alias))
					{
						if (Source is TableSource)
							return (Source as TableSource).Alias;

						if (Source is SqlTable)
							return ((SqlTable)Source).Alias;
					}

					return _alias;
				}
				set { _alias = value; }
			}

			public TableSource this[ISqlTableSource table]
			{
				get { return this[table, null]; }
			}

			public TableSource this[ISqlTableSource table, string alias]
			{
				get
				{
					foreach (var tj in Joins)
					{
						var t = CheckTableSource(tj.Table, table, alias);

						if (t != null)
							return t;
					}

					return null;
				}
			}

			readonly List<JoinedTable> _joins = new List<JoinedTable>();
			public   List<JoinedTable>  Joins
			{
				get { return _joins;  }
			}

			public void ForEach(Action<TableSource> action, HashSet<SelectQuery> visitedQueries)
			{
				action(this);
				foreach (var join in Joins)
					join.Table.ForEach(action, visitedQueries);

				if (Source is SelectQuery && visitedQueries.Contains((SelectQuery)Source))
					((SelectQuery)Source).ForEachTable(action, visitedQueries);
			}

			public IEnumerable<ISqlTableSource> GetTables()
			{
				yield return Source;

				foreach (var join in Joins)
					foreach (var table in join.Table.GetTables())
						yield return table;
			}

			public int GetJoinNumber()
			{
				var n = Joins.Count;

				foreach (var join in Joins)
					n += join.Table.GetJoinNumber();

				return n;
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#region IEquatable<ISqlExpression> Members

			bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
			{
				return this == other;
			}

#endregion

#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				Source = (ISqlTableSource)Source.Walk(skipColumns, func);

				foreach (var t in Joins)
					((ISqlExpressionWalkable)t).Walk(skipColumns, func);

				return this;
			}

#endregion

#region ISqlTableSource Members

			public int       SourceID { get { return Source.SourceID; } }
			public SqlField  All      { get { return Source.All;      } }

			IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty)
			{
				return Source.GetKeys(allIfEmpty);
			}

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				if (!objectTree.TryGetValue(this, out clone))
				{
					var ts = new TableSource((ISqlTableSource)Source.Clone(objectTree, doClone), _alias);

					objectTree.Add(this, clone = ts);

					ts._joins.AddRange(_joins.Select(jt => (JoinedTable)jt.Clone(objectTree, doClone)));
				}

				return clone;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.TableSource; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (dic.ContainsKey(this))
					return sb.Append("...");

				dic.Add(this, this);

				if (Source is SelectQuery)
				{
					sb.Append("(\n\t");
					var len = sb.Length;
					Source.ToString(sb, dic).Replace("\n", "\n\t", len, sb.Length - len);
					sb.Append("\n)");
				}
				else
					Source.ToString(sb, dic);

				sb
					.Append(" as t")
					.Append(SourceID);

				foreach (IQueryElement join in Joins)
				{
					sb.AppendLine().Append('\t');
					var len = sb.Length;
					join.ToString(sb, dic).Replace("\n", "\n\t", len, sb.Length - len);
				}

				dic.Remove(this);

				return sb;
			}

#endregion

#region ISqlExpression Members

			public bool CanBeNull
			{
				get { return Source.CanBeNull; }
			}

			public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
			{
				return this == other;
			}

			public int  Precedence { get { return Source.Precedence; } }
			public Type SystemType { get { return Source.SystemType; } }

#endregion
		}

#endregion

#region TableJoin

		public enum JoinType
		{
			Auto,
			Inner,
			Left,
			CrossApply,
			OuterApply
		}

		public class JoinedTable : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public JoinedTable(JoinType joinType, TableSource table, bool isWeak, SearchCondition searchCondition)
			{
				JoinType        = joinType;
				Table           = table;
				IsWeak          = isWeak;
				Condition       = searchCondition;
				CanConvertApply = true;
			}

			public JoinedTable(JoinType joinType, TableSource table, bool isWeak)
				: this(joinType, table, isWeak, new SearchCondition())
			{
			}

			public JoinedTable(JoinType joinType, ISqlTableSource table, string alias, bool isWeak)
				: this(joinType, new TableSource(table, alias), isWeak)
			{
			}

			public JoinType        JoinType        { get; set; }
			public TableSource     Table           { get; set; }
			public SearchCondition Condition       { get; private set; }
			public bool            IsWeak          { get; set; }
			public bool            CanConvertApply { get; set; }

			public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				if (!objectTree.TryGetValue(this, out clone))
					objectTree.Add(this, clone = new JoinedTable(
						JoinType,
						(TableSource)Table.Clone(objectTree, doClone), 
						IsWeak,
						(SearchCondition)Condition.Clone(objectTree, doClone)));

				return clone;
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#region ISqlExpressionWalkable Members

			public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> action)
			{
				Condition = (SearchCondition)((ISqlExpressionWalkable)Condition).Walk(skipColumns, action);

				Table.Walk(skipColumns, action);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.JoinedTable; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (dic.ContainsKey(this))
					return sb.Append("...");

				dic.Add(this, this);

				switch (JoinType)
				{
					case JoinType.Inner      : sb.Append("INNER JOIN ");  break;
					case JoinType.Left       : sb.Append("LEFT JOIN ");   break;
					case JoinType.CrossApply : sb.Append("CROSS APPLY "); break;
					case JoinType.OuterApply : sb.Append("OUTER APPLY "); break;
					default                  : sb.Append("SOME JOIN "); break;
				}

				((IQueryElement)Table).ToString(sb, dic);
				sb.Append(" ON ");
				((IQueryElement)Condition).ToString(sb, dic);

				dic.Remove(this);

				return sb;
			}

#endregion
		}

#endregion

#region Predicate

		public abstract class Predicate : ISqlPredicate
		{
			public enum Operator
			{
				Equal,          // =     Is the operator used to test the equality between two expressions.
				NotEqual,       // <> != Is the operator used to test the condition of two expressions not being equal to each other.
				Greater,        // >     Is the operator used to test the condition of one expression being greater than the other.
				GreaterOrEqual, // >=    Is the operator used to test the condition of one expression being greater than or equal to the other expression.
				NotGreater,     // !>    Is the operator used to test the condition of one expression not being greater than the other expression.
				Less,           // <     Is the operator used to test the condition of one expression being less than the other.
				LessOrEqual,    // <=    Is the operator used to test the condition of one expression being less than or equal to the other expression.
				NotLess         // !<    Is the operator used to test the condition of one expression not being less than the other expression.
			}

			public class Expr : Predicate
			{
				public Expr([JetBrains.Annotations.NotNull] ISqlExpression exp1, int precedence)
					: base(precedence)
				{
					if (exp1 == null) throw new ArgumentNullException("exp1");

					Expr1 = exp1;
				}

				public Expr([JetBrains.Annotations.NotNull] ISqlExpression exp1)
					: base(exp1.Precedence)
				{
					if (exp1 == null) throw new ArgumentNullException("exp1");

					Expr1 = exp1;
				}

				public ISqlExpression Expr1 { get; set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					Expr1 = Expr1.Walk(skipColumns, func);

					if (Expr1 == null)
						throw new InvalidOperationException();
				}

				public override bool CanBeNull
				{
					get { return Expr1.CanBeNull; }
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new Expr((ISqlExpression)Expr1.Clone(objectTree, doClone), Precedence));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.ExprPredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);
				}
			}

			public class NotExpr : Expr
			{
				public NotExpr(ISqlExpression exp1, bool isNot, int precedence)
					: base(exp1, precedence)
				{
					IsNot = isNot;
				}

				public bool IsNot { get; set; }

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new NotExpr((ISqlExpression)Expr1.Clone(objectTree, doClone), IsNot, Precedence));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.NotExprPredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					if (IsNot) sb.Append("NOT (");
					base.ToString(sb, dic);
					if (IsNot) sb.Append(")");
				}
			}

			// { expression { = | <> | != | > | >= | ! > | < | <= | !< } expression
			//
			public class ExprExpr : Expr
			{
				public ExprExpr(ISqlExpression exp1, Operator op, ISqlExpression exp2)
					: base(exp1, SqlQuery.Precedence.Comparison)
				{
					Operator = op;
					Expr2    = exp2;
				}

				public new Operator   Operator { get; private  set; }
				public ISqlExpression Expr2    { get; internal set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					base.Walk(skipColumns, func);
					Expr2 = Expr2.Walk(skipColumns, func);
				}

				public override bool CanBeNull
				{
					get { return base.CanBeNull || Expr2.CanBeNull; }
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new ExprExpr(
							(ISqlExpression)Expr1.Clone(objectTree, doClone), Operator, (ISqlExpression)Expr2.Clone(objectTree, doClone)));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.ExprExprPredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);

					string op;

					switch (Operator)
					{
						case Operator.Equal         : op = "=";  break;
						case Operator.NotEqual      : op = "<>"; break;
						case Operator.Greater       : op = ">";  break;
						case Operator.GreaterOrEqual: op = ">="; break;
						case Operator.NotGreater    : op = "!>"; break;
						case Operator.Less          : op = "<";  break;
						case Operator.LessOrEqual   : op = "<="; break;
						case Operator.NotLess       : op = "!<"; break;
						default: throw new InvalidOperationException();
					}

					sb.Append(" ").Append(op).Append(" ");

					Expr2.ToString(sb, dic);
				}
			}

			// string_expression [ NOT ] LIKE string_expression [ ESCAPE 'escape_character' ]
			//
			public class Like : NotExpr
			{
				public Like(ISqlExpression exp1, bool isNot, ISqlExpression exp2, ISqlExpression escape)
					: base(exp1, isNot, SqlQuery.Precedence.Comparison)
				{
					Expr2  = exp2;
					Escape = escape;
				}

				public ISqlExpression Expr2  { get; internal set; }
				public ISqlExpression Escape { get; internal set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					base.Walk(skipColumns, func);
					Expr2 = Expr2.Walk(skipColumns, func);

					if (Escape != null)
						Escape = Escape.Walk(skipColumns, func);
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new Like(
							(ISqlExpression)Expr1.Clone(objectTree, doClone), IsNot, (ISqlExpression)Expr2.Clone(objectTree, doClone), Escape));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.LikePredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);

					if (IsNot) sb.Append(" NOT");
					sb.Append(" LIKE ");

					Expr2.ToString(sb, dic);

					if (Escape != null)
					{
						sb.Append(" ESCAPE ");
						Escape.ToString(sb, dic);
					}
				}
			}

			// expression [ NOT ] BETWEEN expression AND expression
			//
			public class Between : NotExpr
			{
				public Between(ISqlExpression exp1, bool isNot, ISqlExpression exp2, ISqlExpression exp3)
					: base(exp1, isNot, SqlQuery.Precedence.Comparison)
				{
					Expr2 = exp2;
					Expr3 = exp3;
				}

				public ISqlExpression Expr2 { get; internal set; }
				public ISqlExpression Expr3 { get; internal set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					base.Walk(skipColumns, func);
					Expr2 = Expr2.Walk(skipColumns, func);
					Expr3 = Expr3.Walk(skipColumns, func);
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new Between(
							(ISqlExpression)Expr1.Clone(objectTree, doClone),
							IsNot,
							(ISqlExpression)Expr2.Clone(objectTree, doClone),
							(ISqlExpression)Expr3.Clone(objectTree, doClone)));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.BetweenPredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);

					if (IsNot) sb.Append(" NOT");
					sb.Append(" BETWEEN ");

					Expr2.ToString(sb, dic);
					sb.Append(" AND ");
					Expr3.ToString(sb, dic);
				}
			}

			// expression IS [ NOT ] NULL
			//
			public class IsNull : NotExpr
			{
				public IsNull(ISqlExpression exp1, bool isNot)
					: base(exp1, isNot, SqlQuery.Precedence.Comparison)
				{
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new IsNull((ISqlExpression)Expr1.Clone(objectTree, doClone), IsNot));

					return clone;
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);
					sb
						.Append(" IS ")
						.Append(IsNot ? "NOT " : "")
						.Append("NULL");
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.IsNullPredicate; }
				}
			}

			// expression [ NOT ] IN ( subquery | expression [ ,...n ] )
			//
			public class InSubQuery : NotExpr
			{
				public InSubQuery(ISqlExpression exp1, bool isNot, SelectQuery subQuery)
					: base(exp1, isNot, SqlQuery.Precedence.Comparison)
				{
					SubQuery = subQuery;
				}

				public SelectQuery SubQuery { get; private set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					base.Walk(skipColumns, func);
					SubQuery = (SelectQuery)((ISqlExpression)SubQuery).Walk(skipColumns, func);
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new InSubQuery(
							(ISqlExpression)Expr1.Clone(objectTree, doClone),
							IsNot,
							(SelectQuery)SubQuery.Clone(objectTree, doClone)));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.InSubQueryPredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);

					if (IsNot) sb.Append(" NOT");
					sb.Append(" IN (");

					((IQueryElement)SubQuery).ToString(sb, dic);
					sb.Append(")");
				}
			}

			public class InList : NotExpr
			{
				public InList(ISqlExpression exp1, bool isNot, params ISqlExpression[] values)
					: base(exp1, isNot, SqlQuery.Precedence.Comparison)
				{
					if (values != null && values.Length > 0)
						_values.AddRange(values);
				}

				public InList(ISqlExpression exp1, bool isNot, IEnumerable<ISqlExpression> values)
					: base(exp1, isNot, SqlQuery.Precedence.Comparison)
				{
					if (values != null)
						_values.AddRange(values);
				}

				readonly List<ISqlExpression> _values = new List<ISqlExpression>();
				public   List<ISqlExpression>  Values { get { return _values; } }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> action)
				{
					base.Walk(skipColumns, action);
					for (var i = 0; i < _values.Count; i++)
						_values[i] = _values[i].Walk(skipColumns, action);
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
					{
						objectTree.Add(this, clone = new InList(
							(ISqlExpression)Expr1.Clone(objectTree, doClone),
							IsNot,
							_values.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray()));
					}

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.InListPredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					Expr1.ToString(sb, dic);

					if (IsNot) sb.Append(" NOT");
					sb.Append(" IN (");

					foreach (var value in Values)
					{
						value.ToString(sb, dic);
						sb.Append(',');
					}

					if (Values.Count > 0)
						sb.Length--;

					sb.Append(")");
				}
			}

			// CONTAINS ( { column | * } , '< contains_search_condition >' )
			// FREETEXT ( { column | * } , 'freetext_string' )
			// expression { = | <> | != | > | >= | !> | < | <= | !< } { ALL | SOME | ANY } ( subquery )
			// EXISTS ( subquery )

			public class FuncLike : Predicate
			{
				public FuncLike(SqlFunction func)
					: base(func.Precedence)
				{
					Function = func;
				}

				public SqlFunction Function { get; private set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					Function = (SqlFunction)((ISqlExpression)Function).Walk(skipColumns, func);
				}

				public override bool CanBeNull
				{
					get { return Function.CanBeNull; }
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new FuncLike((SqlFunction)Function.Clone(objectTree, doClone)));

					return clone;
				}

				public override QueryElementType ElementType
				{
					get { return QueryElementType.FuncLikePredicate; }
				}

				protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
				{
					((IQueryElement)Function).ToString(sb, dic);
				}
			}

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

			protected Predicate(int precedence)
			{
				Precedence = precedence;
			}

#region IPredicate Members

			public int Precedence { get; private set; }

			public    abstract bool              CanBeNull { get; }
			protected abstract ICloneableElement Clone    (Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone);
			protected abstract void              Walk     (bool skipColumns, Func<ISqlExpression,ISqlExpression> action);

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				Walk(skipColumns, func);
				return null;
			}

			ICloneableElement ICloneableElement.Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				return Clone(objectTree, doClone);
			}

#endregion

#region IQueryElement Members

			public abstract QueryElementType ElementType { get; }

			protected abstract void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic);

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (dic.ContainsKey(this))
					return sb.Append("...");

				dic.Add(this, this);
				ToString(sb, dic);
				dic.Remove(this);

				return sb;
			}

#endregion
		}

#endregion

#region Condition

		public class Condition : IQueryElement, ICloneableElement
		{
			public Condition(bool isNot, ISqlPredicate predicate)
			{
				IsNot     = isNot;
				Predicate = predicate;
			}

			public Condition(bool isNot, ISqlPredicate predicate, bool isOr)
			{
				IsNot     = isNot;
				Predicate = predicate;
				IsOr      = isOr;
			}

			public bool          IsNot     { get; set; }
			public ISqlPredicate Predicate { get; set; }
			public bool          IsOr      { get; set; }

			public int Precedence
			{
				get
				{
					return
						IsNot ? SqlQuery.Precedence.LogicalNegation :
						IsOr  ? SqlQuery.Precedence.LogicalDisjunction :
						        SqlQuery.Precedence.LogicalConjunction;
				}
			}

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				if (!objectTree.TryGetValue(this, out clone))
					objectTree.Add(this, clone = new Condition(IsNot, (ISqlPredicate)Predicate.Clone(objectTree, doClone), IsOr));

				return clone;
			}

			public bool CanBeNull
			{
				get { return Predicate.CanBeNull; }
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.Condition; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (dic.ContainsKey(this))
					return sb.Append("...");

				dic.Add(this, this);

				sb.Append('(');

				if (IsNot) sb.Append("NOT ");

				Predicate.ToString(sb, dic);
				sb.Append(')').Append(IsOr ? " OR " : " AND ");

				dic.Remove(this);

				return sb;
			}

#endregion
		}

#endregion

#region SearchCondition

		public class SearchCondition : ConditionBase<SearchCondition, SearchCondition.Next>, ISqlPredicate, ISqlExpression
		{
			public SearchCondition()
			{
			}

			public SearchCondition(IEnumerable<Condition> list)
			{
				_conditions.AddRange(list);
			}

			public SearchCondition(params Condition[] list)
			{
				_conditions.AddRange(list);
			}

			public class Next
			{
				internal Next(SearchCondition parent)
				{
					_parent = parent;
				}

				readonly SearchCondition _parent;

				public SearchCondition Or  { get { return _parent.SetOr(true);  } }
				public SearchCondition And { get { return _parent.SetOr(false); } }

				public ISqlExpression  ToExpr() { return _parent; }
			}

			readonly List<Condition> _conditions = new List<Condition>();
			public   List<Condition>  Conditions
			{
				get { return _conditions; }
			}

			protected override SearchCondition Search
			{
				get { return this; }
			}

			protected override Next GetNext()
			{
				return new Next(this);
			}

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region IPredicate Members

			public int Precedence
			{
				get
				{
					if (_conditions.Count == 0) return SqlQuery.Precedence.Unknown;
					if (_conditions.Count == 1) return _conditions[0].Precedence;

					return _conditions.Select(_ =>
						_.IsNot ? SqlQuery.Precedence.LogicalNegation :
						_.IsOr  ? SqlQuery.Precedence.LogicalDisjunction :
						          SqlQuery.Precedence.LogicalConjunction).Min();
				}
			}

			public Type SystemType
			{
				get { return typeof(bool); }
			}

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				foreach (var condition in Conditions)
					condition.Predicate.Walk(skipColumns, func);

				return func(this);
			}

#endregion

#region IEquatable<ISqlExpression> Members

			bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
			{
				return this == other;
			}

#endregion

#region ISqlExpression Members

			public bool CanBeNull
			{
				get
				{
					foreach (var c in Conditions)
						if (c.CanBeNull)
							return true;

					return false;
				}
			}

			public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
			{
				return this == other;
			}

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				if (!objectTree.TryGetValue(this, out clone))
				{
					var sc = new SearchCondition();

					objectTree.Add(this, clone = sc);

					sc._conditions.AddRange(_conditions.Select(c => (Condition)c.Clone(objectTree, doClone)));
				}

				return clone;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.SearchCondition; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (dic.ContainsKey(this))
					return sb.Append("...");

				dic.Add(this, this);

				foreach (IQueryElement c in Conditions)
					c.ToString(sb, dic);

				if (Conditions.Count > 0)
					sb.Length -= 4;

				dic.Remove(this);

				return sb;
			}

#endregion
		}

#endregion

#region ConditionBase

		interface IConditionExpr<T>
		{
			T Expr    (ISqlExpression expr);
			T Field   (SqlField       field);
			T SubQuery(SelectQuery    selectQuery);
			T Value   (object         value);
		}

		public abstract class ConditionBase<T1,T2> : IConditionExpr<ConditionBase<T1,T2>.Expr_>
			where T1 : ConditionBase<T1,T2>
		{
			public class Expr_
			{
				internal Expr_(ConditionBase<T1,T2> condition, bool isNot, ISqlExpression expr)
				{
					_condition = condition;
					_isNot     = isNot;
					_expr      = expr;
				}

				readonly ConditionBase<T1,T2> _condition;
				readonly bool                 _isNot;
				readonly ISqlExpression       _expr;

				T2 Add(ISqlPredicate predicate)
				{
					_condition.Search.Conditions.Add(new Condition(_isNot, predicate));
					return _condition.GetNext();
				}

#region Predicate.ExprExpr

				public class Op_ : IConditionExpr<T2>
				{
					internal Op_(Expr_ expr, Predicate.Operator op) 
					{
						_expr = expr;
						_op   = op;
					}

					readonly Expr_              _expr;
					readonly Predicate.Operator _op;

					public T2 Expr    (ISqlExpression expr)       { return _expr.Add(new Predicate.ExprExpr(_expr._expr, _op, expr)); }
					public T2 Field   (SqlField      field)       { return Expr(field);               }
					public T2 SubQuery(SelectQuery   selectQuery) { return Expr(selectQuery);         }
					public T2 Value   (object        value)       { return Expr(new SqlValue(value)); }

					public T2 All     (SelectQuery   subQuery)    { return Expr(SqlFunction.CreateAll (subQuery)); }
					public T2 Some    (SelectQuery   subQuery)    { return Expr(SqlFunction.CreateSome(subQuery)); }
					public T2 Any     (SelectQuery   subQuery)    { return Expr(SqlFunction.CreateAny (subQuery)); }
				}

				public Op_ Equal          { get { return new Op_(this, Predicate.Operator.Equal);          } }
				public Op_ NotEqual       { get { return new Op_(this, Predicate.Operator.NotEqual);       } }
				public Op_ Greater        { get { return new Op_(this, Predicate.Operator.Greater);        } }
				public Op_ GreaterOrEqual { get { return new Op_(this, Predicate.Operator.GreaterOrEqual); } }
				public Op_ NotGreater     { get { return new Op_(this, Predicate.Operator.NotGreater);     } }
				public Op_ Less           { get { return new Op_(this, Predicate.Operator.Less);           } }
				public Op_ LessOrEqual    { get { return new Op_(this, Predicate.Operator.LessOrEqual);    } }
				public Op_ NotLess        { get { return new Op_(this, Predicate.Operator.NotLess);        } }

#endregion

#region Predicate.Like

				public T2 Like(ISqlExpression expression, SqlValue escape) { return Add(new Predicate.Like(_expr, false, expression, escape)); }
				public T2 Like(ISqlExpression expression)                  { return Like(expression, null); }
				public T2 Like(string expression,         SqlValue escape) { return Like(new SqlValue(expression), escape); }
				public T2 Like(string expression)                          { return Like(new SqlValue(expression), null);   }

#endregion

#region Predicate.Between

				public T2 Between   (ISqlExpression expr1, ISqlExpression expr2) { return Add(new Predicate.Between(_expr, false, expr1, expr2)); }
				public T2 NotBetween(ISqlExpression expr1, ISqlExpression expr2) { return Add(new Predicate.Between(_expr, true,  expr1, expr2)); }

#endregion

#region Predicate.IsNull

				public T2 IsNull    { get { return Add(new Predicate.IsNull(_expr, false)); } }
				public T2 IsNotNull { get { return Add(new Predicate.IsNull(_expr, true));  } }

#endregion

#region Predicate.In

				public T2 In   (SelectQuery subQuery) { return Add(new Predicate.InSubQuery(_expr, false, subQuery)); }
				public T2 NotIn(SelectQuery subQuery) { return Add(new Predicate.InSubQuery(_expr, true,  subQuery)); }

				Predicate.InList CreateInList(bool isNot, object[] exprs)
				{
					var list = new Predicate.InList(_expr, isNot, null);

					if (exprs != null && exprs.Length > 0)
					{
						foreach (var item in exprs)
						{
							if (item == null || item is SqlValue && ((SqlValue)item).Value == null)
								continue;

							if (item is ISqlExpression)
								list.Values.Add((ISqlExpression)item);
							else
								list.Values.Add(new SqlValue(item));
						}
					}

					return list;
				}

				public T2 In   (params object[] exprs) { return Add(CreateInList(false, exprs)); }
				public T2 NotIn(params object[] exprs) { return Add(CreateInList(true,  exprs)); }

#endregion
			}

			public class Not_ : IConditionExpr<Expr_>
			{
				internal Not_(ConditionBase<T1,T2> condition)
				{
					_condition = condition;
				}

				readonly ConditionBase<T1,T2> _condition;

				public Expr_ Expr    (ISqlExpression expr)        { return new Expr_(_condition, true, expr); }
				public Expr_ Field   (SqlField       field)       { return Expr(field);               }
				public Expr_ SubQuery(SelectQuery    selectQuery) { return Expr(selectQuery);            }
				public Expr_ Value   (object         value)       { return Expr(new SqlValue(value)); }

				public T2 Exists(SelectQuery subQuery)
				{
					_condition.Search.Conditions.Add(new Condition(true, new Predicate.FuncLike(SqlFunction.CreateExists(subQuery))));
					return _condition.GetNext();
				}
			}

			protected abstract SearchCondition Search { get; }
			protected abstract T2              GetNext();

			protected T1 SetOr(bool value)
			{
				Search.Conditions[Search.Conditions.Count - 1].IsOr = value;
				return (T1)this;
			}

			public Not_  Not { get { return new Not_(this); } }

			public Expr_ Expr    (ISqlExpression expr)        { return new Expr_(this, false, expr); }
			public Expr_ Field   (SqlField       field)       { return Expr(field);                  }
			public Expr_ SubQuery(SelectQuery    selectQuery) { return Expr(selectQuery);            }
			public Expr_ Value   (object         value)       { return Expr(new SqlValue(value));    }

			public T2 Exists(SelectQuery subQuery)
			{
				Search.Conditions.Add(new Condition(false, new Predicate.FuncLike(SqlFunction.CreateExists(subQuery))));
				return GetNext();
			}
		}

#endregion

#region OrderByItem

		public class OrderByItem : IQueryElement, ICloneableElement
		{
			public OrderByItem(ISqlExpression expression, bool isDescending)
			{
				Expression   = expression;
				IsDescending = isDescending;
			}

			public ISqlExpression Expression   { get; internal set; }
			public bool           IsDescending { get; private set; }

			internal void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				Expression = Expression.Walk(skipColumns, func);
			}

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				if (!objectTree.TryGetValue(this, out clone))
					objectTree.Add(this, clone = new OrderByItem((ISqlExpression)Expression.Clone(objectTree, doClone), IsDescending));

				return clone;
			}

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region IQueryElement Members

			public QueryElementType ElementType
			{
				get { return QueryElementType.OrderByItem; }
			}

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				Expression.ToString(sb, dic);

				if (IsDescending)
					sb.Append(" DESC");

				return sb;
			}

#endregion
		}

#endregion

#region ClauseBase

		public abstract class ClauseBase
		{
			protected ClauseBase(SelectQuery selectQuery)
			{
				SelectQuery = selectQuery;
			}

			public SelectClause  Select  { get { return SelectQuery.Select;  } }
			public FromClause    From    { get { return SelectQuery.From;    } }
			public WhereClause   Where   { get { return SelectQuery.Where;   } }
			public GroupByClause GroupBy { get { return SelectQuery.GroupBy; } }
			public WhereClause   Having  { get { return SelectQuery.Having;  } }
			public OrderByClause OrderBy { get { return SelectQuery.OrderBy; } }
			public SelectQuery   End()   { return SelectQuery; }

			protected internal SelectQuery SelectQuery { get; private set; }

			internal void SetSqlQuery(SelectQuery selectQuery)
			{
				SelectQuery = selectQuery;
			}
		}

		public abstract class ClauseBase<T1, T2> : ConditionBase<T1, T2>
			where T1 : ClauseBase<T1, T2>
		{
			protected ClauseBase(SelectQuery selectQuery)
			{
				SelectQuery = selectQuery;
			}

			public SelectClause  Select  { get { return SelectQuery.Select;  } }
			public FromClause    From    { get { return SelectQuery.From;    } }
			public GroupByClause GroupBy { get { return SelectQuery.GroupBy; } }
			public WhereClause   Having  { get { return SelectQuery.Having;  } }
			public OrderByClause OrderBy { get { return SelectQuery.OrderBy; } }
			public SelectQuery   End()   { return SelectQuery; }

			protected internal SelectQuery SelectQuery { get; private set; }

			internal void SetSqlQuery(SelectQuery selectQuery)
			{
				SelectQuery = selectQuery;
			}
		}

#endregion

#region SelectClause

		public class SelectClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
		{
#region Init

			internal SelectClause(SelectQuery selectQuery) : base(selectQuery)
			{
			}

			internal SelectClause(
				SelectQuery  selectQuery,
				SelectClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(selectQuery)
			{
				_columns.AddRange(clone._columns.Select(c => (Column)c.Clone(objectTree, doClone)));
				IsDistinct = clone.IsDistinct;
				TakeValue  = clone.TakeValue == null ? null : (ISqlExpression)clone.TakeValue.Clone(objectTree, doClone);
				SkipValue  = clone.SkipValue == null ? null : (ISqlExpression)clone.SkipValue.Clone(objectTree, doClone);
			}

			internal SelectClause(bool isDistinct, ISqlExpression takeValue, ISqlExpression skipValue, IEnumerable<Column> columns)
				: base(null)
			{
				IsDistinct = isDistinct;
				TakeValue  = takeValue;
				SkipValue  = skipValue;
				_columns.AddRange(columns);
			}

#endregion

#region Columns

			public SelectClause Field(SqlField field)
			{
				AddOrFindColumn(new Column(SelectQuery, field));
				return this;
			}

			public SelectClause Field(SqlField field, string alias)
			{
				AddOrFindColumn(new Column(SelectQuery, field, alias));
				return this;
			}

			public SelectClause SubQuery(SelectQuery subQuery)
			{
				if (subQuery.ParentSelect != null && subQuery.ParentSelect != SelectQuery)
					throw new ArgumentException("SqlQuery already used as subquery");

				subQuery.ParentSelect = SelectQuery;

				AddOrFindColumn(new Column(SelectQuery, subQuery));
				return this;
			}

			public SelectClause SubQuery(SelectQuery selectQuery, string alias)
			{
				if (selectQuery.ParentSelect != null && selectQuery.ParentSelect != SelectQuery)
					throw new ArgumentException("SqlQuery already used as subquery");

				selectQuery.ParentSelect = SelectQuery;

				AddOrFindColumn(new Column(SelectQuery, selectQuery, alias));
				return this;
			}

			public SelectClause Expr(ISqlExpression expr)
			{
				AddOrFindColumn(new Column(SelectQuery, expr));
				return this;
			}

			public SelectClause Expr(ISqlExpression expr, string alias)
			{
				AddOrFindColumn(new Column(SelectQuery, expr, alias));
				return this;
			}

			public SelectClause Expr(string expr, params ISqlExpression[] values)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlExpression(null, expr, values)));
				return this;
			}

			public SelectClause Expr(Type systemType, string expr, params ISqlExpression[] values)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlExpression(systemType, expr, values)));
				return this;
			}

			public SelectClause Expr(string expr, int priority, params ISqlExpression[] values)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlExpression(null, expr, priority, values)));
				return this;
			}

			public SelectClause Expr(Type systemType, string expr, int priority, params ISqlExpression[] values)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlExpression(systemType, expr, priority, values)));
				return this;
			}

			public SelectClause Expr(string alias, string expr, int priority, params ISqlExpression[] values)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlExpression(null, expr, priority, values)));
				return this;
			}

			public SelectClause Expr(Type systemType, string alias, string expr, int priority, params ISqlExpression[] values)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlExpression(systemType, expr, priority, values)));
				return this;
			}

			public SelectClause Expr<T>(ISqlExpression expr1, string operation, ISqlExpression expr2)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2)));
				return this;
			}

			public SelectClause Expr<T>(ISqlExpression expr1, string operation, ISqlExpression expr2, int priority)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2, priority)));
				return this;
			}

			public SelectClause Expr<T>(string alias, ISqlExpression expr1, string operation, ISqlExpression expr2, int priority)
			{
				AddOrFindColumn(new Column(SelectQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2, priority), alias));
				return this;
			}

			public int Add(ISqlExpression expr)
			{
				if (expr is Column && ((Column)expr).Parent == SelectQuery)
					throw new InvalidOperationException();

				return AddOrFindColumn(new Column(SelectQuery, expr));
			}

			public int AddNew(ISqlExpression expr)
			{
				if (expr is Column && ((Column)expr).Parent == SelectQuery)
					throw new InvalidOperationException();

				Columns.Add(new Column(SelectQuery, expr));
				return Columns.Count - 1;
			}

			public int Add(ISqlExpression expr, string alias)
			{
				return AddOrFindColumn(new Column(SelectQuery, expr, alias));
			}

			/// <summary>
			/// Adds column if it is not added yet.
			/// <returns>Returns index of column in Columns list.</returns>
			int AddOrFindColumn(Column col)
			{
				for (var i = 0; i < Columns.Count; i++)
				{
					if (Columns[i].Equals(col))
					{
						return i;
					}
				}

#if DEBUG

				switch (col.Expression.ElementType)
				{
					case QueryElementType.SqlField :
						{
							var table = ((SqlField)col.Expression).Table;

							//if (SqlQuery.From.GetFromTables().Any(_ => _ == table))
							//	throw new InvalidOperationException("Wrong field usage.");

							break;
						}

					case QueryElementType.Column :
						{
							var query = ((Column)col.Expression).Parent;

							//if (!SqlQuery.From.GetFromQueries().Any(_ => _ == query))
							//	throw new InvalidOperationException("Wrong column usage.");

							if (SelectQuery.HasUnion)
							{
								if (SelectQuery.Unions.Any(u => u.SelectQuery == query))
								{
								
								}
							}

							break;
						}

					case QueryElementType.SqlQuery :
						{
							if (col.Expression == SelectQuery)
								throw new InvalidOperationException("Wrong query usage.");
							break;
						}
				}

#endif
				Columns.Add(col);

				return Columns.Count - 1;
			}

			readonly List<Column> _columns = new List<Column>();
			public   List<Column>  Columns
			{
				get { return _columns; }
			}

#endregion

#region HasModifier

			public bool HasModifier
			{
				get { return IsDistinct || SkipValue != null || TakeValue != null; }
			}

#endregion

#region Distinct

			public SelectClause Distinct
			{
				get { IsDistinct = true; return this; }
			}

			public bool IsDistinct { get; set; }

#endregion

#region Take

			public SelectClause Take(int value, TakeHints? hints)
			{
				TakeValue = new SqlValue(value);
				TakeHints = hints;
				return this;
			}

			public SelectClause Take(ISqlExpression value, TakeHints? hints)
			{
				TakeHints = hints;
				TakeValue = value;
				return this;
			}

			public ISqlExpression TakeValue { get; private set; }
			public TakeHints?     TakeHints { get; private set; }

#endregion

#region Skip

			public SelectClause Skip(int value)
			{
				SkipValue = new SqlValue(value);
				return this;
			}

			public SelectClause Skip(ISqlExpression value)
			{
				SkipValue = value;
				return this;
			}

			public ISqlExpression SkipValue { get; set; }

#endregion

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				for (var i = 0; i < Columns.Count; i++)
				{
					var col  = Columns[i];
					var expr = col.Walk(skipColumns, func);

					if (expr is Column)
						Columns[i] = (Column)expr;
					else
						Columns[i] = new Column(col.Parent, expr, col.Alias);
				}

				if (TakeValue != null) TakeValue = TakeValue.Walk(skipColumns, func);
				if (SkipValue != null) SkipValue = SkipValue.Walk(skipColumns, func);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.SelectClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (dic.ContainsKey(this))
					return sb.Append("...");

				dic.Add(this, this);

				sb.Append("SELECT ");

				if (IsDistinct) sb.Append("DISTINCT ");

				if (SkipValue != null)
				{
					sb.Append("SKIP ");
					SkipValue.ToString(sb, dic);
					sb.Append(" ");
				}

				if (TakeValue != null)
				{
					sb.Append("TAKE ");
					TakeValue.ToString(sb, dic);
					sb.Append(" ");
				}

				sb.AppendLine();

				if (Columns.Count == 0)
					sb.Append("\t*, \n");
				else
					for (var i = 0; i < Columns.Count; i++)
					{
						var c = Columns[i];
						sb.Append("\t");
						((IQueryElement)c).ToString(sb, dic);
						sb
							.Append(" as ")
							.Append(c.Alias ?? "c" + (i + 1))
							.Append(", \n");
					}

				sb.Length -= 3;

				dic.Remove(this);

				return sb;
			}

#endregion
		}

		private SelectClause _select;
		public  SelectClause  Select
		{
			get { return _select; }
		}

#endregion

#region CreateTableStatement

		public class CreateTableStatement : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public SqlTable       Table           { get; set; }
			public bool           IsDrop          { get; set; }
			public string         StatementHeader { get; set; }
			public string         StatementFooter { get; set; }
			public DefaulNullable DefaulNullable  { get; set; }

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.CreateTableStatement; } }

			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				sb.Append(IsDrop ? "DROP TABLE " : "CREATE TABLE ");

				if (Table != null)
					((IQueryElement)Table).ToString(sb, dic);

				sb.AppendLine();

				return sb;
			}

#endregion

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				if (Table != null)
					((ISqlExpressionWalkable)Table).Walk(skipColumns, func);

				return null;
			}

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				var clone = new CreateTableStatement { };

				if (Table != null)
					clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

				objectTree.Add(this, clone);

				return clone;
			}

#endregion
		}

		private CreateTableStatement _createTable;
		public  CreateTableStatement  CreateTable
		{
			get { return _createTable ?? (_createTable = new CreateTableStatement()); }
		}

#endregion

#region InsertClause

		public class SetExpression : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public SetExpression(ISqlExpression column, ISqlExpression expression)
			{
				Column     = column;
				Expression = expression;

				if (expression is SqlParameter)
				{
					var p = (SqlParameter)expression;

					if (column is SqlField)
					{
						var field = (SqlField)column;

						if (field.ColumnDescriptor != null)
						{
							if (field.ColumnDescriptor.DataType != DataType.Undefined && p.DataType == DataType.Undefined)
								p.DataType = field.ColumnDescriptor.DataType;
//							if (field.ColumnDescriptorptor.MapMemberInfo.IsDbTypeSet)
//								p.DbType = field.ColumnDescriptorptor.MapMemberInfo.DbType;
//
//							if (field.ColumnDescriptorptor.MapMemberInfo.IsDbSizeSet)
//								p.DbSize = field.ColumnDescriptor.MapMemberInfo.DbSize;
						}
					}
				}
			}

			public ISqlExpression Column     { get; set; }
			public ISqlExpression Expression { get; set; }

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				if (!objectTree.TryGetValue(this, out clone))
				{
					objectTree.Add(this, clone = new SetExpression(
						(ISqlExpression)Column.    Clone(objectTree, doClone),
						(ISqlExpression)Expression.Clone(objectTree, doClone)));
				}

				return clone;
			}

#endregion

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				Column     = Column.    Walk(skipColumns, func);
				Expression = Expression.Walk(skipColumns, func);
				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.SetExpression; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				Column.ToString(sb, dic);
				sb.Append(" = ");
				Expression.ToString(sb, dic);

				return sb;
			}

#endregion
		}

		public class InsertClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public InsertClause()
			{
				Items = new List<SetExpression>();
			}

			public List<SetExpression> Items        { get; private set; }
			public SqlTable            Into         { get; set; }
			public bool                WithIdentity { get; set; }

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				var clone = new InsertClause { WithIdentity = WithIdentity };

				if (Into != null)
					clone.Into = (SqlTable)Into.Clone(objectTree, doClone);

				foreach (var item in Items)
					clone.Items.Add((SetExpression)item.Clone(objectTree, doClone));

				objectTree.Add(this, clone);

				return clone;
			}

#endregion

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				if (Into != null)
					((ISqlExpressionWalkable)Into).Walk(skipColumns, func);

				foreach (var t in Items)
					((ISqlExpressionWalkable)t).Walk(skipColumns, func);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.InsertClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				sb.Append("VALUES ");

				if (Into != null)
					((IQueryElement)Into).ToString(sb, dic);

				sb.AppendLine();

				foreach (var e in Items)
				{
					sb.Append("\t");
					((IQueryElement)e).ToString(sb, dic);
					sb.AppendLine();
				}

				return sb;
			}

#endregion
		}

		private InsertClause _insert;
		public  InsertClause  Insert
		{
			get { return _insert ?? (_insert = new InsertClause()); }
		}

		public void ClearInsert()
		{
			_insert = null;
		}

#endregion

#region UpdateClause

		public class UpdateClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public UpdateClause()
			{
				Items = new List<SetExpression>();
				Keys  = new List<SetExpression>();
			}

			public List<SetExpression> Items { get; private set; }
			public List<SetExpression> Keys  { get; private set; }
			public SqlTable            Table { get; set; }

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				var clone = new UpdateClause();

				if (Table != null)
					clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

				foreach (var item in Items)
					clone.Items.Add((SetExpression)item.Clone(objectTree, doClone));

				foreach (var item in Keys)
					clone.Keys.Add((SetExpression)item.Clone(objectTree, doClone));

				objectTree.Add(this, clone);

				return clone;
			}

#endregion

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				if (Table != null)
					((ISqlExpressionWalkable)Table).Walk(skipColumns, func);

				foreach (var t in Items)
					((ISqlExpressionWalkable)t).Walk(skipColumns, func);

				foreach (var t in Keys)
					((ISqlExpressionWalkable)t).Walk(skipColumns, func);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.UpdateClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				sb.Append("SET ");

				if (Table != null)
					((IQueryElement)Table).ToString(sb, dic);

				sb.AppendLine();

				foreach (var e in Items)
				{
					sb.Append("\t");
					((IQueryElement)e).ToString(sb, dic);
					sb.AppendLine();
				}

				return sb;
			}

#endregion
		}

		private UpdateClause _update;
		public  UpdateClause  Update
		{
			get { return _update ?? (_update = new UpdateClause()); }
		}

		public void ClearUpdate()
		{
			_update = null;
		}

#endregion

#region DeleteClause

		public class DeleteClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public SqlTable Table { get; set; }

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region ICloneableElement Members

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				var clone = new DeleteClause();

				if (Table != null)
					clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

				objectTree.Add(this, clone);

				return clone;
			}

#endregion

#region ISqlExpressionWalkable Members

			[Obsolete]
			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				if (Table != null)
					((ISqlExpressionWalkable)Table).Walk(skipColumns, func);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.DeleteClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				sb.Append("DELETE FROM ");

				if (Table != null)
					((IQueryElement)Table).ToString(sb, dic);

				sb.AppendLine();

				return sb;
			}

#endregion
		}

		private DeleteClause _delete;
		public  DeleteClause  Delete
		{
			get { return _delete ?? (_delete = new DeleteClause()); }
		}

		public void ClearDelete()
		{
			_delete = null;
		}

#endregion

#region FromClause

		public class FromClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
		{
#region Join

			public class Join : ConditionBase<Join,Join.Next>
			{
				public class Next
				{
					internal Next(Join parent)
					{
						_parent = parent;
					}

					readonly Join _parent;

					public Join Or  { get { return _parent.SetOr(true);  } }
					public Join And { get { return _parent.SetOr(false); } }

					public static implicit operator Join(Next next)
					{
						return next._parent;
					}
				}

				protected override SearchCondition Search
				{
					get { return JoinedTable.Condition; }
				}

				protected override Next GetNext()
				{
					return new Next(this);
				}

				internal Join(JoinType joinType, ISqlTableSource table, string alias, bool isWeak, ICollection<Join> joins)
				{
					JoinedTable = new JoinedTable(joinType, table, alias, isWeak);

					if (joins != null && joins.Count > 0)
						foreach (var join in joins)
							JoinedTable.Table.Joins.Add(join.JoinedTable);
				}

				public JoinedTable JoinedTable { get; private set; }
			}

#endregion

			internal FromClause(SelectQuery selectQuery) : base(selectQuery)
			{
			}

			internal FromClause(
				SelectQuery selectQuery,
				FromClause  clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(selectQuery)
			{
				_tables.AddRange(clone._tables.Select(ts => (TableSource)ts.Clone(objectTree, doClone)));
			}

			internal FromClause(IEnumerable<TableSource> tables)
				: base(null)
			{
				_tables.AddRange(tables);
			}

			public FromClause Table(ISqlTableSource table, params Join[] joins)
			{
				return Table(table, null, joins);
			}

			public FromClause Table(ISqlTableSource table, string alias, params Join[] joins)
			{
				var ts = AddOrGetTable(table, alias);

				if (joins != null && joins.Length > 0)
					foreach (var join in joins)
						ts.Joins.Add(join.JoinedTable);

				return this;
			}

			TableSource GetTable(ISqlTableSource table, string alias)
			{
				foreach (var ts in Tables)
					if (ts.Source == table)
						if (alias == null || ts.Alias == alias)
							return ts;
						else
							throw new ArgumentException("alias");

				return null;
			}

			TableSource AddOrGetTable(ISqlTableSource table, string alias)
			{
				var ts = GetTable(table, alias);

				if (ts != null)
					return ts;

				var t = new TableSource(table, alias);

				Tables.Add(t);

				return t;
			}

			public TableSource this[ISqlTableSource table]
			{
				get { return this[table, null]; }
			}

			public TableSource this[ISqlTableSource table, string alias]
			{
				get
				{
					foreach (var ts in Tables)
					{
						var t = CheckTableSource(ts, table, alias);

						if (t != null)
							return t;
					}

					return null;
				}
			}

			public bool IsChild(ISqlTableSource table)
			{
				foreach (var ts in Tables)
					if (ts.Source == table || CheckChild(ts.Joins, table))
						return true;
				return false;
			}

			static bool CheckChild(IEnumerable<JoinedTable> joins, ISqlTableSource table)
			{
				foreach (var j in joins)
					if (j.Table.Source == table || CheckChild(j.Table.Joins, table))
						return true;
				return false;
			}

			readonly List<TableSource> _tables = new List<TableSource>();
			public   List<TableSource>  Tables
			{
				get { return _tables; }
			}

			static IEnumerable<ISqlTableSource> GetJoinTables(TableSource source, QueryElementType elementType)
			{
				if (source.Source.ElementType == elementType)
					yield return source.Source;

				foreach (var join in source.Joins)
					foreach (var table in GetJoinTables(join.Table, elementType))
						yield return table;
			}

			internal IEnumerable<ISqlTableSource> GetFromTables()
			{
				return Tables.SelectMany(_ => GetJoinTables(_, QueryElementType.SqlTable));
			}

			internal IEnumerable<ISqlTableSource> GetFromQueries()
			{
				return Tables.SelectMany(_ => GetJoinTables(_, QueryElementType.SqlQuery));
			}

			static TableSource FindTableSource(TableSource source, SqlTable table)
			{
				if (source.Source == table)
					return source;

				foreach (var join in source.Joins)
				{
					var ts = FindTableSource(join.Table, table);
					if (ts != null)
						return ts;
				}

				return null;
			}

			public ISqlTableSource FindTableSource(SqlTable table)
			{
				foreach (var source in Tables)
				{
					var ts = FindTableSource(source, table);
					if (ts != null)
						return ts;
				}

				return null;
			}

#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#endregion

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				for (var i = 0; i <	Tables.Count; i++)
					((ISqlExpressionWalkable)Tables[i]).Walk(skipColumns, func);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.FromClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				sb.Append(" \nFROM \n");

				if (Tables.Count > 0)
				{
					foreach (IQueryElement ts in Tables)
					{
						sb.Append('\t');
						var len = sb.Length;
						ts.ToString(sb, dic).Replace("\n", "\n\t", len, sb.Length - len);
						sb.Append(", ");
					}

					sb.Length -= 2;
				}

				return sb;
			}

#endregion
		}

		public static FromClause.Join InnerJoin    (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Inner,      table, null,  false, joins); }
		public static FromClause.Join InnerJoin    (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Inner,      table, alias, false, joins); }
		public static FromClause.Join LeftJoin     (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Left,       table, null,  false, joins); }
		public static FromClause.Join LeftJoin     (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Left,       table, alias, false, joins); }
		public static FromClause.Join Join         (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Auto,       table, null,  false, joins); }
		public static FromClause.Join Join         (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Auto,       table, alias, false, joins); }
		public static FromClause.Join CrossApply   (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.CrossApply, table, null,  false, joins); }
		public static FromClause.Join CrossApply   (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.CrossApply, table, alias, false, joins); }
		public static FromClause.Join OuterApply   (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.OuterApply, table, null,  false, joins); }
		public static FromClause.Join OuterApply   (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.OuterApply, table, alias, false, joins); }

		public static FromClause.Join WeakInnerJoin(ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Inner,      table, null,  true,  joins); }
		public static FromClause.Join WeakInnerJoin(ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Inner,      table, alias, true,  joins); }
		public static FromClause.Join WeakLeftJoin (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Left,       table, null,  true,  joins); }
		public static FromClause.Join WeakLeftJoin (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Left,       table, alias, true,  joins); }
		public static FromClause.Join WeakJoin     (ISqlTableSource table,               params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Auto,       table, null,  true,  joins); }
		public static FromClause.Join WeakJoin     (ISqlTableSource table, string alias, params FromClause.Join[] joins) { return new FromClause.Join(JoinType.Auto,       table, alias, true,  joins); }

		private FromClause _from;
		public  FromClause  From
		{
			get { return _from; }
		}

#endregion

#region WhereClause

		public class WhereClause : ClauseBase<WhereClause,WhereClause.Next>, IQueryElement, ISqlExpressionWalkable
		{
			private SearchCondition _searchCondition;

			public class Next : ClauseBase
			{
				internal Next(WhereClause parent) : base(parent.SelectQuery)
				{
					_parent = parent;
				}

				readonly WhereClause _parent;

				public WhereClause Or  { get { return _parent.SetOr(true);  } }
				public WhereClause And { get { return _parent.SetOr(false); } }
			}

			internal WhereClause(SelectQuery selectQuery) : base(selectQuery)
			{
				SearchCondition = new SearchCondition();
			}

			internal WhereClause(
				SelectQuery selectQuery,
				WhereClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(selectQuery)
			{
				SearchCondition = (SearchCondition)clone.SearchCondition.Clone(objectTree, doClone);
			}

			internal WhereClause(SearchCondition searchCondition) : base(null)
			{
				SearchCondition = searchCondition;
			}

			public SearchCondition SearchCondition
			{
				get { return _searchCondition; }
				private set { _searchCondition = value; }
			}

			public bool IsEmpty
			{
				get { return SearchCondition.Conditions.Count == 0; }
			}

			protected override SearchCondition Search
			{
				get { return SearchCondition; }
			}

			protected override Next GetNext()
			{
				return new Next(this);
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> action)
			{
				SearchCondition = (SearchCondition)((ISqlExpressionWalkable)SearchCondition).Walk(skipColumns, action);
				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType
			{
				get { return QueryElementType.WhereClause; }
			}

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (Search.Conditions.Count == 0)
					return sb;

				sb.Append("\nWHERE\n\t");
				return ((IQueryElement)Search).ToString(sb, dic);
			}

#endregion
		}

		private WhereClause _where;
		public  WhereClause  Where
		{
			get { return _where; }
		}

#endregion

#region GroupByClause

		public class GroupByClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
		{
			internal GroupByClause(SelectQuery selectQuery) : base(selectQuery)
			{
			}

			internal GroupByClause(
				SelectQuery   selectQuery,
				GroupByClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(selectQuery)
			{
				_items.AddRange(clone._items.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)));
			}

			internal GroupByClause(IEnumerable<ISqlExpression> items) : base(null)
			{
				_items.AddRange(items);
			}

			public GroupByClause Expr(ISqlExpression expr)
			{
				Add(expr);
				return this;
			}

			public GroupByClause Field(SqlField field)
			{
				return Expr(field);
			}

			void Add(ISqlExpression expr)
			{
				foreach (var e in Items)
					if (e.Equals(expr))
						return;

				Items.Add(expr);
			}

			readonly List<ISqlExpression> _items = new List<ISqlExpression>();
			public   List<ISqlExpression>  Items
			{
				get { return _items; }
			}

			public bool IsEmpty
			{
				get { return Items.Count == 0; }
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				for (var i = 0; i < Items.Count; i++)
					Items[i] = Items[i].Walk(skipColumns, func);

				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.GroupByClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (Items.Count == 0)
					return sb;

				sb.Append(" \nGROUP BY \n");

				foreach (var item in Items)
				{
					sb.Append('\t');
					item.ToString(sb, dic);
					sb.Append(",");
				}

				sb.Length--;

				return sb;
			}

#endregion
		}

		private GroupByClause _groupBy;
		public  GroupByClause  GroupBy
		{
			get { return _groupBy; }
		}

#endregion

#region HavingClause

		private WhereClause _having;
		public  WhereClause  Having
		{
			get { return _having; }
		}

#endregion

#region OrderByClause

		public class OrderByClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
		{
			internal OrderByClause(SelectQuery selectQuery) : base(selectQuery)
			{
			}

			internal OrderByClause(
				SelectQuery   selectQuery,
				OrderByClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(selectQuery)
			{
				_items.AddRange(clone._items.Select(item => (OrderByItem)item.Clone(objectTree, doClone)));
			}

			internal OrderByClause(IEnumerable<OrderByItem> items) : base(null)
			{
				_items.AddRange(items);
			}

			public OrderByClause Expr(ISqlExpression expr, bool isDescending)
			{
				Add(expr, isDescending);
				return this;
			}

			public OrderByClause Expr     (ISqlExpression expr)               { return Expr(expr,  false);        }
			public OrderByClause ExprAsc  (ISqlExpression expr)               { return Expr(expr,  false);        }
			public OrderByClause ExprDesc (ISqlExpression expr)               { return Expr(expr,  true);         }
			public OrderByClause Field    (SqlField field, bool isDescending) { return Expr(field, isDescending); }
			public OrderByClause Field    (SqlField field)                    { return Expr(field, false);        }
			public OrderByClause FieldAsc (SqlField field)                    { return Expr(field, false);        }
			public OrderByClause FieldDesc(SqlField field)                    { return Expr(field, true);         }

			void Add(ISqlExpression expr, bool isDescending)
			{
				foreach (var item in Items)
					if (item.Expression.Equals(expr, (x, y) =>
					{
						var col = x as Column;
						return col == null || !col.Parent.HasUnion || x == y;
					}))
						return;

				Items.Add(new OrderByItem(expr, isDescending));
			}

			readonly List<OrderByItem> _items = new List<OrderByItem>();
			public   List<OrderByItem>  Items
			{
				get { return _items; }
			}

			public bool IsEmpty
			{
				get { return Items.Count == 0; }
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

#region ISqlExpressionWalkable Members

			ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				foreach (var t in Items)
					t.Walk(skipColumns, func);
				return null;
			}

#endregion

#region IQueryElement Members

			public QueryElementType ElementType { get { return QueryElementType.OrderByClause; } }

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				if (Items.Count == 0)
					return sb;

				sb.Append(" \nORDER BY \n");

				foreach (IQueryElement item in Items)
				{
					sb.Append('\t');
					item.ToString(sb, dic);
					sb.Append(", ");
				}

				sb.Length -= 2;

				return sb;
			}

#endregion
		}

		private OrderByClause _orderBy;
		public  OrderByClause  OrderBy
		{
			get { return _orderBy; }
		}

#endregion

#region Union

		public class Union : IQueryElement
		{
			public Union()
			{
			}

			public Union(SelectQuery selectQuery, bool isAll)
			{
				SelectQuery = selectQuery;
				IsAll    = isAll;
			}

			public SelectQuery SelectQuery { get; private set; }
			public bool IsAll { get; private set; }

			public QueryElementType ElementType
			{
				get { return QueryElementType.Union; }
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

			StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
			{
				sb.Append(" \nUNION").Append(IsAll ? " ALL" : "").Append(" \n");
				return ((IQueryElement)SelectQuery).ToString(sb, dic);
			}
		}

		private List<Union> _unions;
		public  List<Union>  Unions
		{
			get { return _unions ?? (_unions = new List<Union>()); }
		}

		public bool HasUnion { get { return _unions != null && _unions.Count > 0; } }

		public void AddUnion(SelectQuery union, bool isAll)
		{
			Unions.Add(new Union(union, isAll));
		}

#endregion

#region ProcessParameters

		public SelectQuery ProcessParameters()
		{
			if (IsParameterDependent)
			{
				var query = new QueryVisitor().Convert(this, e =>
				{
					switch (e.ElementType)
					{
						case QueryElementType.SqlParameter :
							{
								var p = (SqlParameter)e;

								if (p.Value == null)
									return new SqlValue(null);
							}

							break;

						case QueryElementType.ExprExprPredicate :
							{
								var ee = (Predicate.ExprExpr)e;
								
								if (ee.Operator == Predicate.Operator.Equal || ee.Operator == Predicate.Operator.NotEqual)
								{
									object value1;
									object value2;

									if (ee.Expr1 is SqlValue)
										value1 = ((SqlValue)ee.Expr1).Value;
									else if (ee.Expr1 is SqlParameter)
										value1 = ((SqlParameter)ee.Expr1).Value;
									else
										break;

									if (ee.Expr2 is SqlValue)
										value2 = ((SqlValue)ee.Expr2).Value;
									else if (ee.Expr2 is SqlParameter)
										value2 = ((SqlParameter)ee.Expr2).Value;
									else
										break;

									var value = Equals(value1, value2);

									if (ee.Operator == Predicate.Operator.NotEqual)
										value = !value;

									return new Predicate.Expr(new SqlValue(value), SqlQuery.Precedence.Comparison);
								}
							}

							break;

						case QueryElementType.InListPredicate :
							return ConvertInListPredicate((Predicate.InList)e);
					}

					return null;
				});

				if (query != this)
				{
					query.Parameters.Clear();

					new QueryVisitor().VisitAll(query, expr =>
					{
						switch (expr.ElementType)
						{
							case QueryElementType.SqlParameter :
								{
									var p = (SqlParameter)expr;
									if (p.IsQueryParameter)
										query.Parameters.Add(p);

									break;
								}
						}
					});
				}

				return query;
			}

			return this;
		}

		static Predicate ConvertInListPredicate(Predicate.InList p)
		{
			if (p.Values == null || p.Values.Count == 0)
				return new Predicate.Expr(new SqlValue(p.IsNot));

			if (p.Values.Count == 1 && p.Values[0] is SqlParameter)
			{
				var pr = (SqlParameter)p.Values[0];

				if (pr.Value == null)
					return new Predicate.Expr(new SqlValue(p.IsNot));

				if (pr.Value is IEnumerable)
				{
					var items = (IEnumerable)pr.Value;

					if (p.Expr1 is ISqlTableSource)
					{
						var table = (ISqlTableSource)p.Expr1;
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new SqlException("Cant create IN expression.");

						if (keys.Count == 1)
						{
							var values = new List<ISqlExpression>();
							var field  = GetUnderlayingField(keys[0]);
							var cd     = field.ColumnDescriptor;

							foreach (var item in items)
							{
								var value = cd.MemberAccessor.GetValue(item);
								values.Add(cd.MappingSchema.GetSqlValue(cd.MemberType, value));
							}

							if (values.Count == 0)
								return new Predicate.Expr(new SqlValue(p.IsNot));

							return new Predicate.InList(keys[0], p.IsNot, values);
						}

						{
							var sc = new SearchCondition();

							foreach (var item in items)
							{
								var itemCond = new SearchCondition();

								foreach (var key in keys)
								{
									var field = GetUnderlayingField(key);
									var cd    = field.ColumnDescriptor;
									var value = cd.MemberAccessor.GetValue(item);
									var cond  = value == null ?
										new Condition(false, new Predicate.IsNull  (field, false)) :
										new Condition(false, new Predicate.ExprExpr(field, Predicate.Operator.Equal, cd.MappingSchema.GetSqlValue(value)));

									itemCond.Conditions.Add(cond);
								}

								sc.Conditions.Add(new Condition(false, new Predicate.Expr(itemCond), true));
							}

							if (sc.Conditions.Count == 0)
								return new Predicate.Expr(new SqlValue(p.IsNot));

							if (p.IsNot)
								return new Predicate.NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

							return new Predicate.Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
						}
					}

					if (p.Expr1 is ObjectSqlExpression)
					{
						var expr = (ObjectSqlExpression)p.Expr1;

						if (expr.Parameters.Length == 1)
						{
							var values = new List<ISqlExpression>();

							foreach (var item in items)
							{
								var value = expr.GetValue(item, 0);
								values.Add(new SqlValue(value));
							}

							if (values.Count == 0)
								return new Predicate.Expr(new SqlValue(p.IsNot));

							return new Predicate.InList(expr.Parameters[0], p.IsNot, values);
						}

						var sc = new SearchCondition();

						foreach (var item in items)
						{
							var itemCond = new SearchCondition();

							for (var i = 0; i < expr.Parameters.Length; i++)
							{
								var sql   = expr.Parameters[i];
								var value = expr.GetValue(item, i);
								var cond  = value == null ?
									new Condition(false, new Predicate.IsNull  (sql, false)) :
									new Condition(false, new Predicate.ExprExpr(sql, Predicate.Operator.Equal, new SqlValue(value)));

								itemCond.Conditions.Add(cond);
							}

							sc.Conditions.Add(new Condition(false, new Predicate.Expr(itemCond), true));
						}

						if (sc.Conditions.Count == 0)
							return new Predicate.Expr(new SqlValue(p.IsNot));

						if (p.IsNot)
							return new Predicate.NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

						return new Predicate.Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
					}
				}
			}

			return null;
		}

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column  : return GetUnderlayingField(((Column)expr).Expression);
			}

			throw new InvalidOperationException();
		}

#endregion

#region Clone

		SelectQuery(SelectQuery clone, Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			objectTree.Add(clone,     this);
			objectTree.Add(clone.All, All);

			SourceID = Interlocked.Increment(ref SourceIDCounter);

			ICloneableElement parentClone;

			if (clone.ParentSelect != null)
				ParentSelect = objectTree.TryGetValue(clone.ParentSelect, out parentClone) ? (SelectQuery)parentClone : clone.ParentSelect;

			_queryType = clone._queryType;

			if (IsInsert) _insert = (InsertClause)clone._insert.Clone(objectTree, doClone);
			if (IsUpdate) _update = (UpdateClause)clone._update.Clone(objectTree, doClone);
			if (IsDelete) _delete = (DeleteClause)clone._delete.Clone(objectTree, doClone);

			_select  = new SelectClause (this, clone._select,  objectTree, doClone);
			_from    = new FromClause   (this, clone._from,    objectTree, doClone);
			_where   = new WhereClause  (this, clone._where,   objectTree, doClone);
			_groupBy = new GroupByClause(this, clone._groupBy, objectTree, doClone);
			_having  = new WhereClause  (this, clone._having,  objectTree, doClone);
			_orderBy = new OrderByClause(this, clone._orderBy, objectTree, doClone);

			_parameters.AddRange(clone._parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));
			IsParameterDependent = clone.IsParameterDependent;

			new QueryVisitor().Visit(this, expr =>
			{
				var sb = expr as SelectQuery;

				if (sb != null && sb.ParentSelect == clone)
					sb.ParentSelect = this;
			});
		}

		public SelectQuery Clone()
		{
			return (SelectQuery)Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);
		}

		public SelectQuery Clone(Predicate<ICloneableElement> doClone)
		{
			return (SelectQuery)Clone(new Dictionary<ICloneableElement,ICloneableElement>(), doClone);
		}

#endregion

#region Aliases

		IDictionary<string,object> _aliases;

		public void RemoveAlias(string alias)
		{
			if (_aliases != null)
			{
				alias = alias.ToUpper();
				if (_aliases.ContainsKey(alias))
					_aliases.Remove(alias);
			}
		}

		public string GetAlias(string desiredAlias, string defaultAlias)
		{
			if (_aliases == null)
				_aliases = new Dictionary<string,object>();

			var alias = desiredAlias;

			if (string.IsNullOrEmpty(desiredAlias) || desiredAlias.Length > 25)
			{
				desiredAlias = defaultAlias;
				alias        = defaultAlias + "1";
			}

			for (var i = 1; ; i++)
			{
				var s = alias.ToUpper();

				if (!_aliases.ContainsKey(s) && !ReservedWords.IsReserved(s))
				{
					_aliases.Add(s, s);
					break;
				}

				alias = desiredAlias + i;
			}

			return alias;
		}

		public string[] GetTempAliases(int n, string defaultAlias)
		{
			var aliases = new string[n];

			for (var i = 0; i < aliases.Length; i++)
				aliases[i] = GetAlias(defaultAlias, defaultAlias);

			foreach (var t in aliases)
				RemoveAlias(t);

			return aliases;
		}

		internal void SetAliases()
		{
			_aliases = null;

			var objs = new Dictionary<object,object>();

			Parameters.Clear();

			new QueryVisitor().VisitAll(this, expr =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter:
						{
							var p = (SqlParameter)expr;

							if (p.IsQueryParameter)
							{
								if (!objs.ContainsKey(expr))
								{
									objs.Add(expr, expr);
									p.Name = GetAlias(p.Name, "p");
									Parameters.Add(p);
								}
							}
							else
								IsParameterDependent = true;
						}

						break;

					case QueryElementType.Column:
						{
							if (!objs.ContainsKey(expr))
							{
								objs.Add(expr, expr);

								var c = (SelectQuery.Column)expr;

								if (c.Alias != "*")
									c.Alias = GetAlias(c.Alias, "c");
							}
						}

						break;

					case QueryElementType.TableSource:
						{
							var table = (SelectQuery.TableSource)expr;

							if (!objs.ContainsKey(table))
							{
								objs.Add(table, table);
								table.Alias = GetAlias(table.Alias, "t");
							}
						}

						break;

					case QueryElementType.SqlQuery:
						{
							var sql = (SelectQuery)expr;

							if (sql.HasUnion)
							{
								for (var i = 0; i < sql.Select.Columns.Count; i++)
								{
									var col = sql.Select.Columns[i];

									foreach (var t in sql.Unions)
									{
										var union = t.SelectQuery.Select;

										objs.Remove(union.Columns[i].Alias);

										union.Columns[i].Alias = col.Alias;
									}
								}
							}
						}

						break;
				}
			});
		}

#endregion

#region Helpers

		public void ForEachTable(Action<TableSource> action, HashSet<SelectQuery> visitedQueries)
		{
			if (!visitedQueries.Add(this))
				return;

			foreach (var table in From.Tables)
				table.ForEach(action, visitedQueries);

			new QueryVisitor().Visit(this, e =>
			{
				if (e is SelectQuery && e != this)
					((SelectQuery)e).ForEachTable(action, visitedQueries);
			});
		}

		public ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			var ts = From[table];

//			if (ts == null && IsUpdate && Update.Table == table)
//				return Update.Table;

			return ts == null && ParentSelect != null? ParentSelect.GetTableSource(table) : ts;
		}

		static TableSource CheckTableSource(TableSource ts, ISqlTableSource table, string alias)
		{
			if (ts.Source == table && (alias == null || ts.Alias == alias))
				return ts;

			var jt = ts[table, alias];

			if (jt != null)
				return jt;

			if (ts.Source is SelectQuery)
			{
				var s = ((SelectQuery)ts.Source).From[table, alias];

				if (s != null)
					return s;
			}

			return null;
		}

#endregion

#region Overrides

		public string SqlText { get { return ToString(); } }

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

#endregion

#region ISqlExpression Members

		public bool CanBeNull
		{
			get { return true; }
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		public int Precedence
		{
			get { return SqlQuery.Precedence.Unknown; }
		}

		public Type SystemType
		{
			get
			{
				if (Select.Columns.Count == 1)
					return Select.Columns[0].SystemType;

				if (From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0)
					return From.Tables[0].SystemType;

				return null;
			}
		}

#endregion

#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				clone = new SelectQuery(this, objectTree, doClone);

			return clone;
		}

#endregion

#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (_insert != null) ((ISqlExpressionWalkable)_insert).Walk(skipColumns, func);
			if (_update != null) ((ISqlExpressionWalkable)_update).Walk(skipColumns, func);
			if (_delete != null) ((ISqlExpressionWalkable)_delete).Walk(skipColumns, func);

			((ISqlExpressionWalkable)Select) .Walk(skipColumns, func);
			((ISqlExpressionWalkable)From)   .Walk(skipColumns, func);
			((ISqlExpressionWalkable)Where)  .Walk(skipColumns, func);
			((ISqlExpressionWalkable)GroupBy).Walk(skipColumns, func);
			((ISqlExpressionWalkable)Having) .Walk(skipColumns, func);
			((ISqlExpressionWalkable)OrderBy).Walk(skipColumns, func);

			if (HasUnion)
				foreach (var union in Unions)
					((ISqlExpressionWalkable)union.SelectQuery).Walk(skipColumns, func);

			return func(this);
		}

#endregion

#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return this == other;
		}

#endregion

#region ISqlTableSource Members

		public static int SourceIDCounter;

		public int           SourceID     { get; private set; }
		public SqlTableType  SqlTableType { get { return SqlTableType.Table; } }

		private SqlField _all;
		public  SqlField  All
		{
			get { return _all ?? (_all = new SqlField { Name = "*", PhysicalName = "*", Table = this }); }

			internal set
			{
				_all = value;

				if (_all != null)
					_all.Table = this;
			}
		}

		List<ISqlExpression> _keys;

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
		{
			if (_keys == null && From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0)
			{
				_keys = new List<ISqlExpression>();

				var q =
					from key in ((ISqlTableSource)From.Tables[0]).GetKeys(allIfEmpty)
					from col in Select.Columns
					where col.Expression == key
					select col as ISqlExpression;

				_keys = q.ToList();
			}

			return _keys;
		}

#endregion

#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlQuery; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);

			sb
				.Append("(")
				.Append(SourceID)
				.Append(") ");

			((IQueryElement)Select). ToString(sb, dic);
			((IQueryElement)From).   ToString(sb, dic);
			((IQueryElement)Where).  ToString(sb, dic);
			((IQueryElement)GroupBy).ToString(sb, dic);
			((IQueryElement)Having). ToString(sb, dic);
			((IQueryElement)OrderBy).ToString(sb, dic);

			if (HasUnion)
				foreach (IQueryElement u in Unions)
					u.ToString(sb, dic);

			dic.Remove(this);

			return sb;
		}

#endregion
	}
}
