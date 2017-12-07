using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlPredicate : ISqlPredicate
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

		public class Expr : SqlPredicate
		{
			public Expr([JetBrains.Annotations.NotNull] ISqlExpression exp1, int precedence)
				: base(precedence)
			{
				Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
			}

			public Expr([JetBrains.Annotations.NotNull] ISqlExpression exp1)
				: base(exp1.Precedence)
			{
				Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
			}

			public ISqlExpression Expr1 { get; set; }

			protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				Expr1 = Expr1.Walk(skipColumns, func);

				if (Expr1 == null)
					throw new InvalidOperationException();
			}

			public override bool CanBeNull => Expr1.CanBeNull;

			protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new Expr((ISqlExpression)Expr1.Clone(objectTree, doClone), Precedence));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.ExprPredicate;

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

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new NotExpr((ISqlExpression)Expr1.Clone(objectTree, doClone), IsNot, Precedence));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.NotExprPredicate;

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

			public new Operator   Operator { get; }
			public ISqlExpression Expr2    { get; internal set; }

			protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(skipColumns, func);
				Expr2 = Expr2.Walk(skipColumns, func);
			}

			public override bool CanBeNull => base.CanBeNull || Expr2.CanBeNull;

			protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new ExprExpr(
						(ISqlExpression)Expr1.Clone(objectTree, doClone), Operator, (ISqlExpression)Expr2.Clone(objectTree, doClone)));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.ExprExprPredicate;

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

				Escape = Escape?.Walk(skipColumns, func);
			}

			protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new Like(
						(ISqlExpression)Expr1.Clone(objectTree, doClone), IsNot, (ISqlExpression)Expr2.Clone(objectTree, doClone), Escape));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.LikePredicate;

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

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new Between(
						(ISqlExpression)Expr1.Clone(objectTree, doClone),
						IsNot,
						(ISqlExpression)Expr2.Clone(objectTree, doClone),
						(ISqlExpression)Expr3.Clone(objectTree, doClone)));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.BetweenPredicate;

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

				if (!objectTree.TryGetValue(this, out var clone))
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

			public override QueryElementType ElementType => QueryElementType.IsNullPredicate;
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

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new InSubQuery(
						(ISqlExpression)Expr1.Clone(objectTree, doClone),
						IsNot,
						(SelectQuery)SubQuery.Clone(objectTree, doClone)));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.InSubQueryPredicate;

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
					Values.AddRange(values);
			}

			public InList(ISqlExpression exp1, bool isNot, IEnumerable<ISqlExpression> values)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				if (values != null)
					Values.AddRange(values);
			}

			public   List<ISqlExpression>  Values { get; } = new List<ISqlExpression>();

			protected override void Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> action)
			{
				base.Walk(skipColumns, action);
				for (var i = 0; i < Values.Count; i++)
					Values[i] = Values[i].Walk(skipColumns, action);
			}

			protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				if (!objectTree.TryGetValue(this, out var clone))
				{
					objectTree.Add(this, clone = new InList(
						(ISqlExpression)Expr1.Clone(objectTree, doClone),
						IsNot,
						Values.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray()));
				}

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.InListPredicate;

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

		public class FuncLike : SqlPredicate
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

			public override bool CanBeNull => Function.CanBeNull;

			protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			{
				if (!doClone(this))
					return this;

				if (!objectTree.TryGetValue(this, out var clone))
					objectTree.Add(this, clone = new FuncLike((SqlFunction)Function.Clone(objectTree, doClone)));

				return clone;
			}

			public override QueryElementType ElementType => QueryElementType.FuncLikePredicate;

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

		protected SqlPredicate(int precedence)
		{
			Precedence = precedence;
		}

		#region IPredicate Members

		public             int               Precedence { get; }

		public    abstract bool              CanBeNull  { get; }
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
}
