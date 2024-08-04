using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.EntityFrameworkCore.Internal
{
	/// <summary>
	/// Maps Linq To DB expression.
	/// </summary>
	public sealed class EFCoreExpressionAttribute : Sql.ExpressionAttribute
	{
		/// <summary>
		/// Creates instance of expression mapper.
		/// </summary>
		/// <param name="expression">Mapped expression.</param>
		public EFCoreExpressionAttribute(string expression) : base(expression)
		{
		}

		/// <inheritdoc/>
		public override ISqlExpression? GetExpression<TContext>(
			TContext context,
			IDataContext dataContext,
			SelectQuery query,
			Expression expression,
			Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
		{
			var knownExpressions = new List<Expression>();
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression) expression;
				if (!mc.Method.IsStatic)
					knownExpressions.Add(mc.Object!);
				knownExpressions.AddRange(mc.Arguments);
			}
			else
			{
				var me = (MemberExpression) expression;
				knownExpressions.Add(me.Expression!);
			}

			var @params = new List<ISqlExpression?>(knownExpressions.Select(_ => (ISqlExpression?) null));

			_ = ResolveExpressionValues((context, @params, knownExpressions, converter), Expression!,
				static (ctx, v, d) =>
				{
					var idx = int.Parse(v, CultureInfo.InvariantCulture);

					if (ctx.@params[idx] == null)
						ctx.@params[idx] = ctx.converter(ctx.context, ctx.knownExpressions[idx], null);

					return v;
				});

			var parameters = @params.Select(p => p ?? new SqlExpression("!!!")).ToArray();
			return new SqlExpression(expression.Type, Expression!, Precedence, parameters);
		}
	}
}
