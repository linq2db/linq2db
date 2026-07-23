using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.NHibernateExtension.Internal
{
	/// <summary>
	/// Maps linq2db exression.
	/// </summary>
	public class EFCoreExpressionAttribute : Sql.ExpressionAttribute
	{
		/// <summary>
		/// Creates instance of expression mapper.
		/// </summary>
		/// <param name="expression">Mapped expression.</param>
		public EFCoreExpressionAttribute(string expression) : base(expression)
		{
		}

		/// <inheritdoc cref="Sql.ExpressionAttribute.GetExpression(IDataContext, SelectQuery, System.Linq.Expressions.Expression, Func{Expression, ColumnDescriptor?, ISqlExpression})" />
		public override ISqlExpression GetExpression(
			IDataContext dataContext,
			SelectQuery query,
			Expression expression,
			Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
		{
			var knownExpressions = new List<Expression>();
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression) expression;
				if (!mc.Method.IsStatic)
					knownExpressions.Add(mc.Object);
				knownExpressions.AddRange(mc.Arguments);
			}
			else
			{
				var me = (MemberExpression) expression;
				knownExpressions.Add(me.Expression);
			}

			var pams = new List<ISqlExpression?>(knownExpressions.Select(_ => (ISqlExpression?) null));

			_ = Sql.ExtensionAttribute.ResolveExpressionValues(Expression!,
				(v, d) =>
				{
					var idx = int.Parse(v);

					if (pams[idx] == null)
						pams[idx] = converter(knownExpressions[idx], null);

					return v;
				});

			var parameters = pams.Select(p => p ?? new SqlExpression("!!!")).ToArray();
			return new SqlExpression(expression.Type, Expression!, Precedence, parameters);
		}
	}
}
