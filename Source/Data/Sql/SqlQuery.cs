using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using JetBrains.Annotations;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Sql
{
	using Reflection;

	using FJoin = SqlQuery.FromClause.Join;

	[DebuggerDisplay("SQL = {SqlText}")]
	public class SqlQuery : ISqlTableSource
	{
		#region Init

		static readonly Dictionary<string,object> _reservedWords = new Dictionary<string,object>();

		static SqlQuery()
		{
			using (var stream = typeof(SqlQuery).Assembly.GetManifestResourceStream(typeof(SqlQuery), "ReservedWords.txt"))
			using (var reader = new StreamReader(stream))
			{
				/*
				var words = reader.ReadToEnd().Replace(' ', '\n').Replace('\t', '\n').Split('\n');
				var q = from w in words where w.Length > 0 orderby w select w;

				var text = string.Join("\n", q.Distinct().ToArray());
				*/

				string s;
				while ((s = reader.ReadLine()) != null)
					_reservedWords.Add(s, s);
			}
		}

		public SqlQuery()
		{
			SourceID = Interlocked.Increment(ref SourceIDCounter);

			_select  = new SelectClause (this);
			_from    = new FromClause   (this);
			_where   = new WhereClause  (this);
			_groupBy = new GroupByClause(this);
			_having  = new WhereClause  (this);
			_orderBy = new OrderByClause(this);
		}

		internal SqlQuery(int id)
		{
			SourceID = id;
		}

		internal void Init(
			InsertClause       insert,
			UpdateClause       update,
			SelectClause       select,
			FromClause         from,
			WhereClause        where,
			GroupByClause      groupBy,
			WhereClause        having,
			OrderByClause      orderBy,
			List<Union>        unions,
			SqlQuery           parentSql,
			bool               parameterDependent,
			List<SqlParameter> parameters)
		{
			_insert             = insert;
			_update             = update;
			_select             = select;
			_from               = from;
			_where              = where;
			_groupBy            = groupBy;
			_having             = having;
			_orderBy            = orderBy;
			_unions             = unions;
			ParentSql          = parentSql;
			ParameterDependent = parameterDependent;
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

		public bool     ParameterDependent { get; set; }
		public SqlQuery ParentSql          { get; set; }

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

		public bool IsSelect         { get { return _queryType == QueryType.Select;        } }
		public bool IsDelete         { get { return _queryType == QueryType.Delete;        } }
		public bool IsInsertOrUpdate { get { return _queryType == QueryType.InsertOrUpdate; } }
		public bool IsInsert         { get { return _queryType == QueryType.Insert || _queryType == QueryType.InsertOrUpdate; } }
		public bool IsUpdate         { get { return _queryType == QueryType.Update || _queryType == QueryType.InsertOrUpdate; } }

		#endregion

		#region Column

		public class Column : IEquatable<Column>, ISqlExpression, IChild<SqlQuery>
		{
			public Column(SqlQuery parent, ISqlExpression expression, string alias)
			{
				if (expression == null) throw new ArgumentNullException("expression");

				Parent     = parent;
				Expression = expression;
				_alias      = alias;

#if DEBUG
				_columnNumber = ++_columnCounter;
#endif
			}

			public Column(SqlQuery builder, ISqlExpression expression)
				: this(builder, expression, null)
			{
			}

#if DEBUG
			readonly int _columnNumber;
			static   int _columnCounter;
#endif

			public ISqlExpression Expression { get; set; }

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

			public bool Equals(Column other)
			{
				return Expression.Equals(other.Expression);
			}

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

			#region ISqlExpression Members

			public int Precedence
			{
				get { return Sql.Precedence.Primary; }
			}

			public Type SystemType
			{
				get { return Expression.SystemType; }
			}

			public bool CanBeNull()
			{
				return Expression.CanBeNull();
			}

			public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				ICloneableElement clone;

				var parent = (SqlQuery)Parent.Clone(objectTree, doClone);

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

			#region IChild<ISqlTableSource> Members

			string IChild<SqlQuery>.Name
			{
				get { return Alias; }
			}

			public SqlQuery Parent { get; set; }

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

				if (Expression is SqlQuery)
				{
					sb
						.Append("(\n\t\t");
					var len = sb.Length;
					Expression.ToString(sb, dic).Replace("\n", "\n\t\t", len, sb.Length - len);
					sb.Append("\n\t)");
				}
				/*else if (Expression is Column)
				{
					var col = (Column)Expression;
					sb
						.Append("t")
						.Append(col.Parent.SourceID)
						.Append(".")
						.Append(col.Alias ?? "c" + (col.Parent.Select.Columns.IndexOf(col) + 1));
				}*/
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

			public void ForEach(Action<TableSource> action)
			{
				action(this);
				foreach (var join in Joins)
					join.ForEach(action);

				if (Source is SqlQuery)
					((SqlQuery)Source).ForEachTable(action);
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

					ts._joins.AddRange(_joins.ConvertAll(jt => (JoinedTable)jt.Clone(objectTree, doClone)));
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

				if (Source is SqlQuery)
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

			public bool CanBeNull()
			{
				return Source.CanBeNull();
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
				JoinType  = joinType;
				Table     = table;
				IsWeak    = isWeak;
				Condition = searchCondition;
			}

			public JoinedTable(JoinType joinType, TableSource table, bool isWeak)
				: this(joinType, table, isWeak, new SearchCondition())
			{
			}

			public JoinedTable(JoinType joinType, ISqlTableSource table, string alias, bool isWeak)
				: this(joinType, new TableSource(table, alias), isWeak)
			{
			}

			public JoinType        JoinType  { get; set; }
			public TableSource     Table     { get; set; }
			public SearchCondition Condition { get; private set; }
			public bool            IsWeak    { get; set; }

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

			public void ForEach(Action<TableSource> action)
			{
				Table.ForEach(action);
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
				public Expr([NotNull] ISqlExpression exp1, int precedence)
					: base(precedence)
				{
					if (exp1 == null) throw new ArgumentNullException("exp1");

					Expr1 = exp1;
				}

				public Expr([NotNull] ISqlExpression exp1)
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

				public override bool CanBeNull()
				{
					return Expr1.CanBeNull();
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
					: base(exp1, Sql.Precedence.Comparison)
				{
					this.Operator = op;
					Expr2 = exp2;
				}

				public new Operator   Operator { get; private set; }
				public ISqlExpression Expr2    { get; internal set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					base.Walk(skipColumns, func);
					Expr2 = Expr2.Walk(skipColumns, func);
				}

				public override bool CanBeNull()
				{
					return base.CanBeNull() || Expr2.CanBeNull();
				}

				protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
				{
					if (!doClone(this))
						return this;

					ICloneableElement clone;

					if (!objectTree.TryGetValue(this, out clone))
						objectTree.Add(this, clone = new ExprExpr(
							(ISqlExpression)Expr1.Clone(objectTree, doClone), this.Operator, (ISqlExpression)Expr2.Clone(objectTree, doClone)));

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

					switch (this.Operator)
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
					: base(exp1, isNot, Sql.Precedence.Comparison)
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
					: base(exp1, isNot, Sql.Precedence.Comparison)
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
					: base(exp1, isNot, Sql.Precedence.Comparison)
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
				public InSubQuery(ISqlExpression exp1, bool isNot, SqlQuery subQuery)
					: base(exp1, isNot, Sql.Precedence.Comparison)
				{
					SubQuery = subQuery;
				}

				public SqlQuery SubQuery { get; private set; }

				protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
				{
					base.Walk(skipColumns, func);
					SubQuery = (SqlQuery)((ISqlExpression)SubQuery).Walk(skipColumns, func);
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
							(SqlQuery)SubQuery.Clone(objectTree, doClone)));

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
					: base(exp1, isNot, Sql.Precedence.Comparison)
				{
					if (values != null && values.Length > 0)
						_values.AddRange(values);
				}

				public InList(ISqlExpression exp1, bool isNot, IEnumerable<ISqlExpression> values)
					: base(exp1, isNot, Sql.Precedence.Comparison)
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
							_values.ConvertAll(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray()));
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

				public override bool CanBeNull()
				{
					return Function.CanBeNull();
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

			public    abstract bool              CanBeNull();
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
						IsNot ? Sql.Precedence.LogicalNegation :
						IsOr  ? Sql.Precedence.LogicalDisjunction :
						        Sql.Precedence.LogicalConjunction;
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

			public bool CanBeNull()
			{
				return Predicate.CanBeNull();
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
					if (_conditions.Count == 0) return Sql.Precedence.Unknown;
					if (_conditions.Count == 1) return _conditions[0].Precedence;

					return _conditions.Select(_ =>
						_.IsNot ? Sql.Precedence.LogicalNegation :
						_.IsOr  ? Sql.Precedence.LogicalDisjunction :
						          Sql.Precedence.LogicalConjunction).Min();
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

			public bool CanBeNull()
			{
				foreach (var c in Conditions)
					if (c.CanBeNull())
						return true;

				return false;
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

					sc._conditions.AddRange(_conditions.ConvertAll(c => (Condition)c.Clone(objectTree, doClone)));
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
			T SubQuery(SqlQuery       sqlQuery);
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

					public T2 Expr    (ISqlExpression expr) { return _expr.Add(new Predicate.ExprExpr(_expr._expr, _op, expr)); }
					public T2 Field   (SqlField      field) { return Expr(field);               }
					public T2 SubQuery(SqlQuery   subQuery) { return Expr(subQuery);            }
					public T2 Value   (object        value) { return Expr(new SqlValue(value)); }

					public T2 All     (SqlQuery   subQuery) { return Expr(SqlFunction.CreateAll (subQuery)); }
					public T2 Some    (SqlQuery   subQuery) { return Expr(SqlFunction.CreateSome(subQuery)); }
					public T2 Any     (SqlQuery   subQuery) { return Expr(SqlFunction.CreateAny (subQuery)); }
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

				public T2 In   (SqlQuery subQuery) { return Add(new Predicate.InSubQuery(_expr, false, subQuery)); }
				public T2 NotIn(SqlQuery subQuery) { return Add(new Predicate.InSubQuery(_expr, true,  subQuery)); }

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

				public Expr_ Expr    (ISqlExpression expr)     { return new Expr_(_condition, true, expr); }
				public Expr_ Field   (SqlField       field)    { return Expr(field);               }
				public Expr_ SubQuery(SqlQuery       subQuery) { return Expr(subQuery);            }
				public Expr_ Value   (object         value)    { return Expr(new SqlValue(value)); }

				public T2 Exists(SqlQuery subQuery)
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

			public Expr_ Expr    (ISqlExpression expr)     { return new Expr_(this, false, expr); }
			public Expr_ Field   (SqlField       field)    { return Expr(field);                  }
			public Expr_ SubQuery(SqlQuery       subQuery) { return Expr(subQuery);               }
			public Expr_ Value   (object         value)    { return Expr(new SqlValue(value));    }

			public T2 Exists(SqlQuery subQuery)
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
			protected ClauseBase(SqlQuery sqlQuery)
			{
				SqlQuery = sqlQuery;
			}

			public SelectClause  Select  { get { return SqlQuery.Select;  } }
			public FromClause    From    { get { return SqlQuery.From;    } }
			public WhereClause   Where   { get { return SqlQuery.Where;   } }
			public GroupByClause GroupBy { get { return SqlQuery.GroupBy; } }
			public WhereClause   Having  { get { return SqlQuery.Having;  } }
			public OrderByClause OrderBy { get { return SqlQuery.OrderBy; } }
			public SqlQuery      End()   { return SqlQuery; }

			protected internal SqlQuery SqlQuery { get; private set; }

			internal void SetSqlQuery(SqlQuery sqlQuery)
			{
				SqlQuery = sqlQuery;
			}
		}

		public abstract class ClauseBase<T1, T2> : ConditionBase<T1, T2>
			where T1 : ClauseBase<T1, T2>
		{
			protected ClauseBase(SqlQuery sqlQuery)
			{
				SqlQuery = sqlQuery;
			}

			public SelectClause  Select  { get { return SqlQuery.Select;  } }
			public FromClause    From    { get { return SqlQuery.From;    } }
			public GroupByClause GroupBy { get { return SqlQuery.GroupBy; } }
			public WhereClause   Having  { get { return SqlQuery.Having;  } }
			public OrderByClause OrderBy { get { return SqlQuery.OrderBy; } }
			public SqlQuery      End()   { return SqlQuery; }

			protected internal SqlQuery SqlQuery { get; private set; }

			internal void SetSqlQuery(SqlQuery sqlQuery)
			{
				SqlQuery = sqlQuery;
			}
		}

		#endregion

		#region SelectClause

		public class SelectClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
		{
			#region Init

			internal SelectClause(SqlQuery sqlQuery) : base(sqlQuery)
			{
			}

			internal SelectClause(
				SqlQuery   sqlQuery,
				SelectClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(sqlQuery)
			{
				_columns.AddRange(clone._columns.ConvertAll(c => (Column)c.Clone(objectTree, doClone)));
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
				AddOrGetColumn(new Column(SqlQuery, field));
				return this;
			}

			public SelectClause Field(SqlField field, string alias)
			{
				AddOrGetColumn(new Column(SqlQuery, field, alias));
				return this;
			}

			public SelectClause SubQuery(SqlQuery subQuery)
			{
				if (subQuery.ParentSql != null && subQuery.ParentSql != SqlQuery)
					throw new ArgumentException("SqlQuery already used as subquery");

				subQuery.ParentSql = SqlQuery;

				AddOrGetColumn(new Column(SqlQuery, subQuery));
				return this;
			}

			public SelectClause SubQuery(SqlQuery sqlQuery, string alias)
			{
				if (sqlQuery.ParentSql != null && sqlQuery.ParentSql != SqlQuery)
					throw new ArgumentException("SqlQuery already used as subquery");

				sqlQuery.ParentSql = SqlQuery;

				AddOrGetColumn(new Column(SqlQuery, sqlQuery, alias));
				return this;
			}

			public SelectClause Expr(ISqlExpression expr)
			{
				AddOrGetColumn(new Column(SqlQuery, expr));
				return this;
			}

			public SelectClause Expr(ISqlExpression expr, string alias)
			{
				AddOrGetColumn(new Column(SqlQuery, expr, alias));
				return this;
			}

			public SelectClause Expr(string expr, params ISqlExpression[] values)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlExpression(null, expr, values)));
				return this;
			}

			public SelectClause Expr(Type systemType, string expr, params ISqlExpression[] values)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlExpression(systemType, expr, values)));
				return this;
			}

			public SelectClause Expr(string expr, int priority, params ISqlExpression[] values)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlExpression(null, expr, priority, values)));
				return this;
			}

			public SelectClause Expr(Type systemType, string expr, int priority, params ISqlExpression[] values)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlExpression(systemType, expr, priority, values)));
				return this;
			}

			public SelectClause Expr(string alias, string expr, int priority, params ISqlExpression[] values)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlExpression(null, expr, priority, values)));
				return this;
			}

			public SelectClause Expr(Type systemType, string alias, string expr, int priority, params ISqlExpression[] values)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlExpression(systemType, expr, priority, values)));
				return this;
			}

			public SelectClause Expr<T>(ISqlExpression expr1, string operation, ISqlExpression expr2)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2)));
				return this;
			}

			public SelectClause Expr<T>(ISqlExpression expr1, string operation, ISqlExpression expr2, int priority)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2, priority)));
				return this;
			}

			public SelectClause Expr<T>(string alias, ISqlExpression expr1, string operation, ISqlExpression expr2, int priority)
			{
				AddOrGetColumn(new Column(SqlQuery, new SqlBinaryExpression(typeof(T), expr1, operation, expr2, priority), alias));
				return this;
			}

			public int Add(ISqlExpression expr)
			{
				if (expr is Column && ((Column)expr).Parent == SqlQuery)
					throw new InvalidOperationException();

				return Columns.IndexOf(AddOrGetColumn(new Column(SqlQuery, expr)));
			}

			public int Add(ISqlExpression expr, string alias)
			{
				return Columns.IndexOf(AddOrGetColumn(new Column(SqlQuery, expr, alias)));
			}

			Column AddOrGetColumn(Column col)
			{
				foreach (var c in Columns)
					if (c.Equals(col))
						return col;

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

							break;
						}

					case QueryElementType.SqlQuery :
						{
							if (col.Expression == SqlQuery)
								throw new InvalidOperationException("Wrong query usage.");
							break;
						}
				}

#endif

				Columns.Add(col);

				return col;
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

			public SelectClause Take(int value)
			{
				TakeValue = new SqlValue(value);
				return this;
			}

			public SelectClause Take(ISqlExpression value)
			{
				TakeValue = value;
				return this;
			}

			public ISqlExpression TakeValue { get; set; }

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
					foreach (var c in Columns)
					{
						sb.Append("\t");
						((IQueryElement)c).ToString(sb, dic);
						sb
							.Append(" as ")
							.Append(c.Alias ?? "c" + (Columns.IndexOf(c) + 1))
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

		#region InsertClause

		public class SetExpression : IQueryElement, ISqlExpressionWalkable, ICloneableElement
		{
			public SetExpression(ISqlExpression column, ISqlExpression expression)
			{
				Column     = column;
				Expression = expression;
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

			internal FromClause(SqlQuery sqlQuery) : base(sqlQuery)
			{
			}

			internal FromClause(
				SqlQuery sqlQuery,
				FromClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(sqlQuery)
			{
				_tables.AddRange(clone._tables.ConvertAll(ts => (TableSource)ts.Clone(objectTree, doClone)));
			}

			internal FromClause(IEnumerable<TableSource> tables)
				: base(null)
			{
				_tables.AddRange(tables);
			}

			public FromClause Table(ISqlTableSource table, params FJoin[] joins)
			{
				return Table(table, null, joins);
			}

			public FromClause Table(ISqlTableSource table, string alias, params FJoin[] joins)
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

			IEnumerable<ISqlTableSource> GetJoinTables(TableSource source, QueryElementType elementType)
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

		public static FJoin InnerJoin    (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.Inner,      table, null,  false, joins); }
		public static FJoin InnerJoin    (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.Inner,      table, alias, false, joins); }
		public static FJoin LeftJoin     (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.Left,       table, null,  false, joins); }
		public static FJoin LeftJoin     (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.Left,       table, alias, false, joins); }
		public static FJoin Join         (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.Auto,       table, null,  false, joins); }
		public static FJoin Join         (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.Auto,       table, alias, false, joins); }
		public static FJoin CrossApply   (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.CrossApply, table, null,  false, joins); }
		public static FJoin CrossApply   (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.CrossApply, table, alias, false, joins); }
		public static FJoin OuterApply   (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.OuterApply, table, null,  false, joins); }
		public static FJoin OuterApply   (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.OuterApply, table, alias, false, joins); }

		public static FJoin WeakInnerJoin(ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.Inner,      table, null,  true,  joins); }
		public static FJoin WeakInnerJoin(ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.Inner,      table, alias, true,  joins); }
		public static FJoin WeakLeftJoin (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.Left,       table, null,  true,  joins); }
		public static FJoin WeakLeftJoin (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.Left,       table, alias, true,  joins); }
		public static FJoin WeakJoin     (ISqlTableSource table,               params FJoin[] joins) { return new FJoin(JoinType.Auto,       table, null,  true,  joins); }
		public static FJoin WeakJoin     (ISqlTableSource table, string alias, params FJoin[] joins) { return new FJoin(JoinType.Auto,       table, alias, true,  joins); }

		private FromClause _from;
		public  FromClause  From
		{
			get { return _from; }
		}

		#endregion

		#region WhereClause

		public class WhereClause : ClauseBase<WhereClause,WhereClause.Next>, IQueryElement, ISqlExpressionWalkable
		{
			public class Next : ClauseBase
			{
				internal Next(WhereClause parent) : base(parent.SqlQuery)
				{
					_parent = parent;
				}

				readonly WhereClause _parent;

				public WhereClause Or  { get { return _parent.SetOr(true);  } }
				public WhereClause And { get { return _parent.SetOr(false); } }
			}

			internal WhereClause(SqlQuery sqlQuery) : base(sqlQuery)
			{
				SearchCondition = new SearchCondition();
			}

			internal WhereClause(
				SqlQuery sqlQuery,
				WhereClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(sqlQuery)
			{
				SearchCondition = (SearchCondition)clone.SearchCondition.Clone(objectTree, doClone);
			}

			internal WhereClause(SearchCondition searchCondition) : base(null)
			{
				SearchCondition = searchCondition;
			}

			public SearchCondition SearchCondition { get; private set; }

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
			internal GroupByClause(SqlQuery sqlQuery) : base(sqlQuery)
			{
			}

			internal GroupByClause(
				SqlQuery    sqlQuery,
				GroupByClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(sqlQuery)
			{
				_items.AddRange(clone._items.ConvertAll(e => (ISqlExpression)e.Clone(objectTree, doClone)));
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
			internal OrderByClause(SqlQuery sqlQuery) : base(sqlQuery)
			{
			}

			internal OrderByClause(
				SqlQuery    sqlQuery,
				OrderByClause clone,
				Dictionary<ICloneableElement,ICloneableElement> objectTree,
				Predicate<ICloneableElement> doClone)
				: base(sqlQuery)
			{
				_items.AddRange(clone._items.ConvertAll(item => (OrderByItem)item.Clone(objectTree, doClone)));
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
					if (item.Expression.Equals(expr))
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

			public Union(SqlQuery sqlQuery, bool isAll)
			{
				SqlQuery = sqlQuery;
				IsAll    = isAll;
			}

			public SqlQuery SqlQuery { get; private set; }
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
				return ((IQueryElement)SqlQuery).ToString(sb, dic);
			}
		}

		private List<Union> _unions;
		public  List<Union>  Unions
		{
			get { return _unions ?? (_unions = new List<Union>()); }
		}

		public bool HasUnion { get { return _unions != null && _unions.Count > 0; } }

		public void AddUnion(SqlQuery union, bool isAll)
		{
			Unions.Add(new Union(union, isAll));
		}

		#endregion

		#region FinalizeAndValidate

		public void FinalizeAndValidate(bool isApplySupported, bool optimizeColumns)
		{
#if DEBUG
			var sqlText = SqlText;

			var dic = new Dictionary<SqlQuery,SqlQuery>();

			new QueryVisitor().VisitAll(this, e =>
			{
				var sql = e as SqlQuery;

				if (sql != null)
				{
					if (dic.ContainsKey(sql))
						throw new InvalidOperationException("SqlQuery circle reference detected.");

					dic.Add(sql, sql);
				}
			});
#endif

			OptimizeUnions();
			FinalizeAndValidateInternal(isApplySupported, optimizeColumns);
			ResolveFields();
			SetAliases();

#if DEBUG
			sqlText = SqlText;
#endif
		}

		class QueryData
		{
			public SqlQuery             Query;
			public List<ISqlExpression> Fields  = new List<ISqlExpression>();
			public List<QueryData>      Queries = new List<QueryData>();
		}

		void ResolveFields()
		{
			var root = GetQueryData();

			ResolveFields(root);
		}

		QueryData GetQueryData()
		{
			var data = new QueryData { Query = this };

			new QueryVisitor().Visit(this, true, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField :
						{
							var field = (SqlField)e;

							if (field.Name.Length != 1 || field.Name[0] != '*')
								data.Fields.Add(field);

							break;
						}

					case QueryElementType.SqlQuery :
						{
							if (e != this)
							{
								data.Queries.Add(((SqlQuery)e).GetQueryData());
								return false;
							}

							break;
						}

					case QueryElementType.Column :
						return ((Column)e).Parent == this;

					case QueryElementType.SqlTable :
						return false;
				}

				return true;
			});

			return data;
		}

		static TableSource FindField(SqlField field, TableSource table)
		{
			if (field.Table == table.Source)
				return table;

			foreach (var @join in table.Joins)
			{
				var t = FindField(field, @join.Table);

				if (t != null)
					return @join.Table;
			}

			return null;
		}

		static ISqlExpression GetColumn(QueryData data, SqlField field)
		{
			foreach (var query in data.Queries)
			{
				var q = query.Query;

				foreach (var table in q.From.Tables)
				{
					var t = FindField(field, table);

					if (t != null)
					{
						var n   = q.Select.Columns.Count;
						var idx = q.Select.Add(field);

						if (n != q.Select.Columns.Count)
							if (!q.GroupBy.IsEmpty || q.Select.Columns.Exists(c => IsAggregationFunction(c.Expression)))
								q.GroupBy.Items.Add(field);

						return q.Select.Columns[idx];
					}
				}
			}

			return null;
		}

		static void ResolveFields(QueryData data)
		{
			if (data.Queries.Count == 0)
				return;

			var dic = new Dictionary<ISqlExpression,ISqlExpression>();

			foreach (SqlField field in data.Fields)
			{
				if (dic.ContainsKey(field))
					continue;

				var found = false;

				foreach (var table in data.Query.From.Tables)
				{
					found = FindField(field, table) != null;

					if (found)
						break;
				}

				if (!found)
				{
					var expr = GetColumn(data, field);

					if (expr != null)
						dic.Add(field, expr);
				}
			}

			if (dic.Count > 0)
				new QueryVisitor().Visit(data.Query, true, e =>
				{
					ISqlExpression ex;

					switch (e.ElementType)
					{
						case QueryElementType.SqlQuery :
							return e == data.Query;

						case QueryElementType.SqlFunction :
							{
								var parms = ((SqlFunction)e).Parameters;

								for (var i = 0; i < parms.Length; i++)
									if (dic.TryGetValue(parms[i], out ex))
										parms[i] = ex;

								break;
							}

						case QueryElementType.SqlExpression :
							{
								var parms = ((SqlExpression)e).Parameters;

								for (var i = 0; i < parms.Length; i++)
									if (dic.TryGetValue(parms[i], out ex))
										parms[i] = ex;

								break;
							}

						case QueryElementType.SqlBinaryExpression :
							{
								var expr = (SqlBinaryExpression)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								break;
							}

						case QueryElementType.ExprPredicate       :
						case QueryElementType.NotExprPredicate    :
						case QueryElementType.IsNullPredicate     :
						case QueryElementType.InSubQueryPredicate :
							{
								var expr = (Predicate.Expr)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								break;
							}

						case QueryElementType.ExprExprPredicate :
							{
								var expr = (Predicate.ExprExpr)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								break;
							}

						case QueryElementType.LikePredicate :
							{
								var expr = (Predicate.Like)e;
								if (dic.TryGetValue(expr.Expr1,  out ex)) expr.Expr1  = ex;
								if (dic.TryGetValue(expr.Expr2,  out ex)) expr.Expr2  = ex;
								if (dic.TryGetValue(expr.Escape, out ex)) expr.Escape = ex;
								break;
							}

						case QueryElementType.BetweenPredicate :
							{
								var expr = (Predicate.Between)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								if (dic.TryGetValue(expr.Expr3, out ex)) expr.Expr3 = ex;
								break;
							}

						case QueryElementType.InListPredicate :
							{
								var expr = (Predicate.InList)e;

								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;

								for (var i = 0; i < expr.Values.Count; i++)
									if (dic.TryGetValue(expr.Values[i], out ex))
										expr.Values[i] = ex;

								break;
							}

						case QueryElementType.Column :
							{
								var expr = (Column)e;

								if (expr.Parent != data.Query)
									return false;

								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;

								break;
							}

						case QueryElementType.SetExpression :
							{
								var expr = (SetExpression)e;
								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;
								break;
							}

						case QueryElementType.GroupByClause :
							{
								var expr = (GroupByClause)e;

								for (var i = 0; i < expr.Items.Count; i++)
									if (dic.TryGetValue(expr.Items[i], out ex))
										expr.Items[i] = ex;

								break;
							}

						case QueryElementType.OrderByItem :
							{
								var expr = (OrderByItem)e;
								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;
								break;
							}
					}

					return true;
				});

			foreach (var query in data.Queries)
				if (query.Queries.Count > 0)
					ResolveFields(query);
		}

		void OptimizeUnions()
		{
			var exprs = new Dictionary<ISqlExpression,ISqlExpression>();

			new QueryVisitor().Visit(this, e =>
			{
				var sql = e as SqlQuery;

				if (sql == null || sql.From.Tables.Count != 1 || !sql.IsSimple || sql._insert != null || sql._update != null)
					return;

				var table = sql.From.Tables[0];

				if (table.Joins.Count != 0 || !(table.Source is SqlQuery))
					return;

				var union = (SqlQuery)table.Source;

				if (!union.HasUnion)
					return;

				for (var i = 0; i < sql.Select.Columns.Count; i++)
				{
					var scol = sql.  Select.Columns[i];
					var ucol = union.Select.Columns[i];

					if (scol.Expression != ucol)
						return;
				}

				exprs.Add(union, sql);

				for (var i = 0; i < sql.Select.Columns.Count; i++)
				{
					var scol = sql.  Select.Columns[i];
					var ucol = union.Select.Columns[i];

					scol.Expression = ucol.Expression;
					scol._alias     = ucol._alias;

					exprs.Add(ucol, scol);
				}

				for (var i = sql.Select.Columns.Count; i < union.Select.Columns.Count; i++)
					sql.Select.Expr(union.Select.Columns[i].Expression);

				sql.From.Tables.Clear();
				sql.From.Tables.AddRange(union.From.Tables);

				sql.Where.  SearchCondition.Conditions.AddRange(union.Where. SearchCondition.Conditions);
				sql.Having. SearchCondition.Conditions.AddRange(union.Having.SearchCondition.Conditions);
				sql.GroupBy.Items.                     AddRange(union.GroupBy.Items);
				sql.OrderBy.Items.                     AddRange(union.OrderBy.Items);
				sql.Unions.InsertRange(0, union.Unions);
			});

			((ISqlExpressionWalkable)this).Walk(false, expr =>
			{
				ISqlExpression e;

				if (exprs.TryGetValue(expr, out e))
					return e;

				return expr;
			});
		}

		void FinalizeAndValidateInternal(bool isApplySupported, bool optimizeColumns)
		{
			OptimizeSearchCondition(Where. SearchCondition);
			OptimizeSearchCondition(Having.SearchCondition);

			ForEachTable(table =>
			{
				foreach (var join in table.Joins)
					OptimizeSearchCondition(join.Condition);
			});

			new QueryVisitor().Visit(this, e =>
			{
				var sql = e as SqlQuery;

				if (sql != null && sql != this)
				{
					sql.ParentSql = this;
					sql.FinalizeAndValidateInternal(isApplySupported, optimizeColumns);

					if (sql.ParameterDependent)
						ParameterDependent = true;
				}
			});

			ResolveWeakJoins();
			OptimizeColumns();
			OptimizeApplies   (isApplySupported, optimizeColumns);
			OptimizeSubQueries(isApplySupported, optimizeColumns);
			OptimizeApplies   (isApplySupported, optimizeColumns);

			new QueryVisitor().Visit(this, e =>
			{
				var sql = e as SqlQuery;

				if (sql != null && sql != this)
					sql.RemoveOrderBy();
			});
		}

		internal static void OptimizeSearchCondition(SearchCondition searchCondition)
		{
			// This 'if' could be replaced by one simple match:
			//
			// match (searchCondition.Conditions)
			// {
			// | [SearchCondition(true, _) sc] =>
			//     searchCondition.Conditions = sc.Conditions;
			//     OptimizeSearchCondition(searchCodition)
			//
			// | [SearchCondition(false, [SearchCondition(true, [ExprExpr]) sc])] => ...
			//
			// | [Expr(true,  SqlValue(true))]
			// | [Expr(false, SqlValue(false))]
			//     searchCondition.Conditions = []
			// }
			//
			// One day I am going to rewrite all this crap in Nemerle.
			//
			if (searchCondition.Conditions.Count == 1)
			{
				var cond = searchCondition.Conditions[0];

				if (cond.Predicate is SearchCondition)
				{
					var sc = (SearchCondition)cond.Predicate;

					if (!cond.IsNot)
					{
						searchCondition.Conditions.Clear();
						searchCondition.Conditions.AddRange(sc.Conditions);

						OptimizeSearchCondition(searchCondition);
						return;
					}

					if (sc.Conditions.Count == 1)
					{
						var c1 = sc.Conditions[0];

						if (!c1.IsNot && c1.Predicate is Predicate.ExprExpr)
						{
							var ee = (Predicate.ExprExpr)c1.Predicate;
							Predicate.Operator op;

							switch (ee.Operator)
							{
								case Predicate.Operator.Equal          : op = Predicate.Operator.NotEqual;       break;
								case Predicate.Operator.NotEqual       : op = Predicate.Operator.Equal;          break;
								case Predicate.Operator.Greater        : op = Predicate.Operator.LessOrEqual;    break;
								case Predicate.Operator.NotLess        :
								case Predicate.Operator.GreaterOrEqual : op = Predicate.Operator.Less;           break;
								case Predicate.Operator.Less           : op = Predicate.Operator.GreaterOrEqual; break;
								case Predicate.Operator.NotGreater     :
								case Predicate.Operator.LessOrEqual    : op = Predicate.Operator.Greater;        break;
								default: throw new InvalidOperationException();
							}

							c1.Predicate = new Predicate.ExprExpr(ee.Expr1, op, ee.Expr2);

							searchCondition.Conditions.Clear();
							searchCondition.Conditions.AddRange(sc.Conditions);

							OptimizeSearchCondition(searchCondition);
							return;
						}
					}
				}

				if (cond.Predicate is Predicate.Expr)
				{
					var expr = (Predicate.Expr)cond.Predicate;

					if (expr.Expr1 is SqlValue)
					{
						var value = (SqlValue)expr.Expr1;

						if (value.Value is bool)
							if (cond.IsNot ? !(bool)value.Value : (bool)value.Value)
								searchCondition.Conditions.Clear();
					}
				}
			}

			for (var i = 0; i < searchCondition.Conditions.Count; i++)
			{
				var cond = searchCondition.Conditions[i];

				if (cond.Predicate is Predicate.Expr)
				{
					var expr = (Predicate.Expr)cond.Predicate;

					if (expr.Expr1 is SqlValue)
					{
						var value = (SqlValue)expr.Expr1;

						if (value.Value is bool)
						{
							if (cond.IsNot ? !(bool)value.Value : (bool)value.Value)
							{
								if (i > 0)
								{
									if (searchCondition.Conditions[i-1].IsOr)
									{
										searchCondition.Conditions.RemoveRange(0, i);
										OptimizeSearchCondition(searchCondition);

										break;
									}
								}
							}
						}
					}
				}
				else if (cond.Predicate is SearchCondition)
				{
					var sc = (SearchCondition)cond.Predicate;
					OptimizeSearchCondition(sc);
				}
			}
		}

		void ForEachTable(Action<TableSource> action)
		{
			From.Tables.ForEach(tbl => tbl.ForEach(action));

			new QueryVisitor().Visit(this, e =>
			{
				if (e is SqlQuery && e != this)
					((SqlQuery)e).ForEachTable(action);
			});
		}

		void RemoveOrderBy()
		{
			if (OrderBy.Items.Count > 0 && Select.SkipValue == null && Select.TakeValue == null)
				OrderBy.Items.Clear();
		}

		void ResolveWeakJoins()
		{
			List<ISqlTableSource> tables = null;

			Func<TableSource,bool> findTable = null; findTable = table =>
			{
				if (tables.Contains(table.Source))
					return true;

				foreach (var join in table.Joins)
				{
					if (findTable(join.Table))
					{
						join.IsWeak = false;
						return true;
					}
				}

				if (table.Source is SqlQuery)
					foreach (var t in ((SqlQuery)table.Source).From.Tables)
						if (findTable(t))
							return true;

				return false;
			};

			ForEachTable(table =>
			{
				for (var i = 0; i < table.Joins.Count; i++)
				{
					var join = table.Joins[i];

					if (join.IsWeak)
					{
						if (tables == null)
						{
							tables = new List<ISqlTableSource>();

							Action<IQueryElement> tableCollector = expr =>
							{
								var field = expr as SqlField;

								if (field != null && !tables.Contains(field.Table))
									tables.Add(field.Table);
							};

							var visitor = new QueryVisitor();

							visitor.VisitAll(Select,  tableCollector);
							visitor.VisitAll(Where,   tableCollector);
							visitor.VisitAll(GroupBy, tableCollector);
							visitor.VisitAll(Having,  tableCollector);
							visitor.VisitAll(OrderBy, tableCollector);

							if (_insert != null)
								visitor.VisitAll(Insert, tableCollector);

							if (_update != null)
								visitor.VisitAll(Update, tableCollector);
						}

						if (findTable(join.Table))
						{
							join.IsWeak = false;
						}
						else
						{
							table.Joins.RemoveAt(i);
							i--;
							continue;
						}
					}
				}
			});
		}

		TableSource OptimizeSubQuery(
			TableSource source,
			bool        optimizeWhere,
			bool        allColumns,
			bool        isApplySupported,
			bool        optimizeValues,
			bool        optimizeColumns)
		{
			foreach (var jt in source.Joins)
			{
				var table = OptimizeSubQuery(
					jt.Table,
					jt.JoinType == JoinType.Inner || jt.JoinType == JoinType.CrossApply,
					false,
					isApplySupported,
					jt.JoinType == JoinType.Inner || jt.JoinType == JoinType.CrossApply,
					optimizeColumns);

				if (table != jt.Table)
				{
					var sql = jt.Table.Source as SqlQuery;

					if (sql != null && sql.OrderBy.Items.Count > 0)
						foreach (var item in sql.OrderBy.Items)
							OrderBy.Expr(item.Expression, item.IsDescending);

					jt.Table = table;
				}
			}

			return source.Source is SqlQuery ?
				RemoveSubQuery(source, optimizeWhere, allColumns && !isApplySupported, optimizeValues, optimizeColumns) :
				source;
		}

		static bool CheckColumn(Column column, ISqlExpression expr, SqlQuery query, bool optimizeValues, bool optimizeColumns)
		{
			if (expr is SqlField || expr is Column)
				return false;

			if (expr is SqlValue)
				return !optimizeValues && 1.Equals(((SqlValue)expr).Value);

			if (expr is SqlBinaryExpression)
			{
				var e = (SqlBinaryExpression)expr;

				if (e.Operation == "*" && e.Expr1 is SqlValue)
				{
					var value = (SqlValue)e.Expr1;

					if (value.Value is int && (int)value.Value == -1)
						return CheckColumn(column, e.Expr2, query, optimizeValues, optimizeColumns);
				}
			}

			var visitor = new QueryVisitor();

			if (optimizeColumns &&
				visitor.Find(expr, e => e is SqlQuery || IsAggregationFunction(e)) == null)
			{
				var n = 0;
				var q = query.ParentSql ?? query;

				visitor.VisitAll(q, e => { if (e == column) n++; });

				return n > 2;
			}

			return true;
		}

		TableSource RemoveSubQuery(
			TableSource childSource,
			bool        concatWhere,
			bool        allColumns,
			bool        optimizeValues,
			bool        optimizeColumns)
		{
			var query = (SqlQuery)childSource. Source;

			var isQueryOK = query.From.Tables.Count == 1;

			isQueryOK = isQueryOK && (concatWhere || query.Where.IsEmpty && query.Having.IsEmpty);
			isQueryOK = isQueryOK && !query.HasUnion && query.GroupBy.IsEmpty && !query.Select.HasModifier;

			if (!isQueryOK)
				return childSource;

			var isColumnsOK =
				(allColumns && !query.Select.Columns.Exists(c => IsAggregationFunction(c.Expression))) ||
				!query.Select.Columns.Exists(c => CheckColumn(c, c.Expression, query, optimizeValues, optimizeColumns));

			if (!isColumnsOK)
				return childSource;

			var map = new Dictionary<ISqlExpression,ISqlExpression>(query.Select.Columns.Count);

			foreach (var c in query.Select.Columns)
				map.Add(c, c.Expression);

			var top = this;

			while (top.ParentSql != null)
				top = top.ParentSql;

			((ISqlExpressionWalkable)top).Walk(false, expr =>
			{
				ISqlExpression fld;
				return map.TryGetValue(expr, out fld) ? fld : expr;
			});

			new QueryVisitor().Visit(top, expr =>
			{
				if (expr.ElementType == QueryElementType.InListPredicate)
				{
					var p = (Predicate.InList)expr;

					if (p.Expr1 == query)
						p.Expr1 = query.From.Tables[0];
				}
			});

			query.From.Tables[0].Joins.AddRange(childSource.Joins);

			if (query.From.Tables[0].Alias == null)
				query.From.Tables[0].Alias = childSource.Alias;

			if (!query.Where. IsEmpty) ConcatSearchCondition(Where,  query.Where);
			if (!query.Having.IsEmpty) ConcatSearchCondition(Having, query.Having);

			((ISqlExpressionWalkable)top).Walk(false, expr =>
			{
				if (expr is SqlQuery)
				{
					var sql = (SqlQuery)expr;

					if (sql.ParentSql == query)
						sql.ParentSql = query.ParentSql ?? this;
				}

				return expr;
			});

			return query.From.Tables[0];
		}

		static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction)
				switch (((SqlFunction)expr).Name)
				{
					case "Count"   :
					case "Average" :
					case "Min"     :
					case "Max"     :
					case "Sum"     : return true;
				}

			return false;
		}

		void OptimizeApply(TableSource tableSource, JoinedTable joinTable, bool isApplySupported, bool optimizeColumns)
		{
			var joinSource = joinTable.Table;

			foreach (var join in joinSource.Joins)
				if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
					OptimizeApply(joinSource, join, isApplySupported, optimizeColumns);

			if (joinSource.Source.ElementType == QueryElementType.SqlQuery)
			{
				var sql   = (SqlQuery)joinSource.Source;
				var isAgg = sql.Select.Columns.Exists(c => IsAggregationFunction(c.Expression));

				if (isApplySupported && (isAgg || sql.Select.TakeValue != null || sql.Select.SkipValue != null))
					return;

				var searchCondition = new List<Condition>(sql.Where.SearchCondition.Conditions);

				sql.Where.SearchCondition.Conditions.Clear();

				if (!ContainsTable(tableSource.Source, sql))
				{
					joinTable.JoinType = joinTable.JoinType == JoinType.CrossApply ? JoinType.Inner : JoinType.Left;
					joinTable.Condition.Conditions.AddRange(searchCondition);
				}
				else
				{
					sql.Where.SearchCondition.Conditions.AddRange(searchCondition);

					var table = OptimizeSubQuery(
						joinTable.Table,
						joinTable.JoinType == JoinType.Inner || joinTable.JoinType == JoinType.CrossApply,
						joinTable.JoinType == JoinType.CrossApply,
						isApplySupported,
						joinTable.JoinType == JoinType.Inner || joinTable.JoinType == JoinType.CrossApply,
						optimizeColumns);

					if (table != joinTable.Table)
					{
						var q = joinTable.Table.Source as SqlQuery;

						if (q != null && q.OrderBy.Items.Count > 0)
							foreach (var item in q.OrderBy.Items)
								OrderBy.Expr(item.Expression, item.IsDescending);

						joinTable.Table = table;

						OptimizeApply(tableSource, joinTable, isApplySupported, optimizeColumns);
					}
				}
			}
			else
			{
				if (!ContainsTable(tableSource.Source, joinSource.Source))
					joinTable.JoinType = joinTable.JoinType == JoinType.CrossApply ? JoinType.Inner : JoinType.Left;
			}
		}

		static bool ContainsTable(ISqlTableSource table, IQueryElement sql)
		{
			return null != new QueryVisitor().Find(sql, e =>
				e == table ||
				e.ElementType == QueryElementType.SqlField && table == ((SqlField)e).Table ||
				e.ElementType == QueryElementType.Column   && table == ((Column)  e).Parent);
		}

		static void ConcatSearchCondition(WhereClause where1, WhereClause where2)
		{
			if (where1.IsEmpty)
			{
				where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
			else
			{
				if (where1.SearchCondition.Precedence < Sql.Precedence.LogicalConjunction)
				{
					var sc1 = new SearchCondition();

					sc1.Conditions.AddRange(where1.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Clear();
					where1.SearchCondition.Conditions.Add(new Condition(false, sc1));
				}

				if (where2.SearchCondition.Precedence < Sql.Precedence.LogicalConjunction)
				{
					var sc2 = new SearchCondition();

					sc2.Conditions.AddRange(where2.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Add(new Condition(false, sc2));
				}
				else
					where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
		}

		void OptimizeSubQueries(bool isApplySupported, bool optimizeColumns)
		{
			for (var i = 0; i < From.Tables.Count; i++)
			{
				var table = OptimizeSubQuery(From.Tables[i], true, false, isApplySupported, true, optimizeColumns);

				if (table != From.Tables[i])
				{
					var sql = From.Tables[i].Source as SqlQuery;

					if (sql != null && sql.OrderBy.Items.Count > 0)
						foreach (var item in sql.OrderBy.Items)
							OrderBy.Expr(item.Expression, item.IsDescending);

					From.Tables[i] = table;
				}
			}
		}

		void OptimizeApplies(bool isApplySupported, bool optimizeColumns)
		{
			foreach (var table in From.Tables)
				foreach (var join in table.Joins)
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
						OptimizeApply(table, join, isApplySupported, optimizeColumns);
		}

		void OptimizeColumns()
		{
			((ISqlExpressionWalkable)Select).Walk(false, expr =>
			{
				var query = expr as SqlQuery;
					
				if (query != null && query.From.Tables.Count == 0 && query.Select.Columns.Count == 1)
				{
					new QueryVisitor().Visit(query.Select.Columns[0].Expression, e =>
					{
						if (e.ElementType == QueryElementType.SqlQuery)
						{
							var q = (SqlQuery)e;

							if (q.ParentSql == query)
								q.ParentSql = query.ParentSql;
						}
					});

					return query.Select.Columns[0].Expression;
				}

				return expr;
			});
		}

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

			if (string.IsNullOrEmpty(desiredAlias))
			{
				desiredAlias = defaultAlias;
				alias        = defaultAlias + "1";
			}

			for (var i = 1; ; i++)
			{
				var s = alias.ToUpper();

				if (!_aliases.ContainsKey(s) && !_reservedWords.ContainsKey(s))
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

		void SetAliases()
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
								}

								Parameters.Add(p);
							}
							else
								ParameterDependent = true;
						}

						break;

					case QueryElementType.Column:
						{
							if (!objs.ContainsKey(expr))
							{
								objs.Add(expr, expr);

								var c = (Column)expr;

								if (c.Alias != "*")
									c.Alias = GetAlias(c.Alias, "c");
							}
						}

						break;

					case QueryElementType.TableSource:
						{
							var table = (TableSource)expr;

							if (!objs.ContainsKey(table))
							{
								objs.Add(table, table);
								table.Alias = GetAlias(table.Alias, "t");
							}
						}

						break;

					case QueryElementType.SqlQuery:
						{
							var sql = (SqlQuery)expr;

							if (sql.HasUnion)
							{
								for (var i = 0; i < sql.Select.Columns.Count; i++)
								{
									var col = sql.Select.Columns[i];

									foreach (var t in sql.Unions)
									{
										var union = t.SqlQuery.Select;

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

		#region ProcessParameters

		public SqlQuery ProcessParameters()
		{
			if (ParameterDependent)
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

									return new Predicate.Expr(new SqlValue(value), Sql.Precedence.Comparison);
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
						if (expr.ElementType == QueryElementType.SqlParameter)
						{
							var p = (SqlParameter)expr;
							if (p.IsQueryParameter)
								query.Parameters.Add(p);
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

				if (pr.Value is IEnumerable && p.Expr1 is ISqlTableSource)
				{
					var items = (IEnumerable)pr.Value;
					var table = (ISqlTableSource)p.Expr1;
					var keys  = table.GetKeys(true);

					if (keys == null || keys.Count == 0)
						throw new SqlException("Cant create IN expression.");

					if (keys.Count == 1)
					{
						var values = new List<ISqlExpression>();
						var field  = GetUnderlayingField(keys[0]);

						foreach (var item in items)
						{
							var value = field.MemberMapper.GetValue(item);
							values.Add(new SqlValue(value));
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
								var value = field.MemberMapper.GetValue(item);
								var cond  = value == null ?
									new Condition(false, new Predicate.IsNull  (field, false)) :
									new Condition(false, new Predicate.ExprExpr(field, Predicate.Operator.Equal, new SqlValue(value)));

								itemCond.Conditions.Add(cond);
							}

							sc.Conditions.Add(new Condition(false, new Predicate.Expr(itemCond), true));
						}

						if (sc.Conditions.Count == 0)
							return new Predicate.Expr(new SqlValue(p.IsNot));

						if (p.IsNot)
							return new Predicate.NotExpr(sc, true, Sql.Precedence.LogicalNegation);

						return new Predicate.Expr(sc, Sql.Precedence.LogicalDisjunction);
					}
				}

				if (pr.Value is IEnumerable && p.Expr1 is SqlExpression)
				{
					var expr  = (SqlExpression)p.Expr1;

					if (expr.Expr.Length > 1 && expr.Expr[0] == '\x1')
					{
						var items = (IEnumerable)pr.Value;
						var type  = items.GetListItemType();
						var ta    = TypeAccessor.GetAccessor(type);
						var names = expr.Expr.Substring(1).Split(',');

						if (expr.Parameters.Length == 1)
						{
							var values = new List<ISqlExpression>();

							foreach (var item in items)
							{
								var value = ta[names[0]].GetValue(item);
								values.Add(new SqlValue(value));
							}

							if (values.Count == 0)
								return new Predicate.Expr(new SqlValue(p.IsNot));

							return new Predicate.InList(expr.Parameters[0], p.IsNot, values);
						}

						{
							var sc = new SearchCondition();

							foreach (var item in items)
							{
								var itemCond = new SearchCondition();

								for (var i = 0; i < expr.Parameters.Length; i++)
								{
									var sql   = expr.Parameters[i];
									var value = ta[names[i]].GetValue(item);
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
								return new Predicate.NotExpr(sc, true, Sql.Precedence.LogicalNegation);

							return new Predicate.Expr(sc, Sql.Precedence.LogicalDisjunction);
						}
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

		SqlQuery(SqlQuery clone, Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			objectTree.Add(clone,     this);
			objectTree.Add(clone.All, All);

			SourceID = Interlocked.Increment(ref SourceIDCounter);

			_queryType = clone._queryType;

			if (IsInsert) _insert = (InsertClause)clone._insert.Clone(objectTree, doClone);
			if (IsUpdate) _update = (UpdateClause)clone._update.Clone(objectTree, doClone);

			_select  = new SelectClause (this, clone._select,  objectTree, doClone);
			_from    = new FromClause   (this, clone._from,    objectTree, doClone);
			_where   = new WhereClause  (this, clone._where,   objectTree, doClone);
			_groupBy = new GroupByClause(this, clone._groupBy, objectTree, doClone);
			_having  = new WhereClause  (this, clone._having,  objectTree, doClone);
			_orderBy = new OrderByClause(this, clone._orderBy, objectTree, doClone);

			_parameters.AddRange(clone._parameters.ConvertAll(p => (SqlParameter)p.Clone(objectTree, doClone)));
			ParameterDependent = clone.ParameterDependent;

			new QueryVisitor().Visit(this, expr =>
			{
				var sb = expr as SqlQuery;

				if (sb != null && sb.ParentSql == clone)
					sb.ParentSql = this;
			});
		}

		public SqlQuery Clone()
		{
			return (SqlQuery)Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);
		}

		public SqlQuery Clone(Predicate<ICloneableElement> doClone)
		{
			return (SqlQuery)Clone(new Dictionary<ICloneableElement,ICloneableElement>(), doClone);
		}

		#endregion

		#region Helpers

		public TableSource GetTableSource(ISqlTableSource table)
		{
			var ts = From[table];
			return ts == null && ParentSql != null? ParentSql.GetTableSource(table) : ts;
		}

		static TableSource CheckTableSource(TableSource ts, ISqlTableSource table, string alias)
		{
			if (ts.Source == table && (alias == null || ts.Alias == alias))
				return ts;

			var jt = ts[table, alias];

			if (jt != null)
				return jt;

			if (ts.Source is SqlQuery)
			{
				var s = ((SqlQuery)ts.Source).From[table, alias];

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

		public bool CanBeNull()
		{
			return true;
		}

		public int Precedence
		{
			get { return Sql.Precedence.Unknown; }
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
				clone = new SqlQuery(this, objectTree, doClone);

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (_insert != null) ((ISqlExpressionWalkable)_insert).Walk(skipColumns, func);
			if (_update != null) ((ISqlExpressionWalkable)_update).Walk(skipColumns, func);

			((ISqlExpressionWalkable)Select) .Walk(skipColumns, func);
			((ISqlExpressionWalkable)From)   .Walk(skipColumns, func);
			((ISqlExpressionWalkable)Where)  .Walk(skipColumns, func);
			((ISqlExpressionWalkable)GroupBy).Walk(skipColumns, func);
			((ISqlExpressionWalkable)Having) .Walk(skipColumns, func);
			((ISqlExpressionWalkable)OrderBy).Walk(skipColumns, func);

			if (HasUnion)
				foreach (var union in Unions)
					((ISqlExpressionWalkable)union.SqlQuery).Walk(skipColumns, func);

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
			get
			{
				if (_all == null)
				{
					_all = new SqlField(null, "*", "*", true, -1, null, null);
					((IChild<ISqlTableSource>)_all).Parent = this;
				}

				return _all;
			}

			internal set
			{
				_all = value;

				if (_all != null)
					((IChild<ISqlTableSource>)_all).Parent = this;
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
