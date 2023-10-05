using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Async;
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class ElementAtBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames = { "ElementAt", "ElementAtOrDefault", "ElementAtAsync", "ElementAtOrDefaultAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		public enum MethodKind
		{
			ElementAt,
			ElementAtOrDefault,
		}

		static MethodKind GetMethodKind(string methodName)
		{
			return methodName switch
			{
				"ElementAtOrDefault"      => MethodKind.ElementAtOrDefault,
				"ElementAtOrDefaultAsync" => MethodKind.ElementAtOrDefault,
				"ElementAt"               => MethodKind.ElementAt,
				"ElementAtAsync"          => MethodKind.ElementAt,
				_ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
			};
		}


		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence == null)
				return null;

			if (buildInfo.IsSubQuery)
			{
				if (sequence is not ElementAtContext)
				{
					if (!SequenceHelper.IsSupportedSubqueryForModifier(sequence))
						return null;
				}
			}

			var arg = methodCall.Arguments[1].Unwrap();

			ISqlExpression expr;

			var linqOptions = builder.DataContext.Options.LinqOptions;
			var parametrize = !buildInfo.IsSubQuery && linqOptions.ParameterizeTakeSkip;

			if (arg.NodeType == ExpressionType.Lambda)
			{
				arg  = ((LambdaExpression)arg).Body.Unwrap();
				expr = builder.ConvertToSql(sequence, arg);
			}
			else
			{
				// revert unwrap
				arg  = methodCall.Arguments[1];
				expr = builder.ConvertToSql(sequence, arg);

				if (expr.ElementType == QueryElementType.SqlValue)
				{
					var param = builder.ParametersContext.BuildParameter(methodCall.Arguments[1], null, forceConstant : true)!.SqlParameter;
					param.Name             = "element";
					param.IsQueryParameter = param.IsQueryParameter && parametrize;
					expr                   = param;
				}
			}

			BuildSkip(builder, sequence, expr);
			BuildTake(builder, sequence, new SqlValue(1), null);

			return new ElementAtContext(sequence, GetMethodKind(methodCall.Method.Name));
		}

		class ElementAtContext : PassThroughContext
		{
			readonly MethodKind _methodKind;

			public ElementAtContext(IBuildContext context, MethodKind methodKind) : base(context)
			{
				_methodKind = methodKind;
			}

			static void GetFirstElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).First();

				query.GetElementAsync = query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.FirstAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			static void GetFirstOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement = (db, expr, ps, preambles) =>
					query.GetResultEnumerable(db, expr, ps, preambles).FirstOrDefault();

				query.GetElementAsync = query.GetElementAsync = async (db, expr, ps, preambles, token) =>
					await query.GetResultEnumerable(db, expr, ps, preambles)
						.FirstOrDefaultAsync(token).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);

				switch (_methodKind)
				{
					case MethodKind.ElementAt           : GetFirstElement          (query); break;
					case MethodKind.ElementAtOrDefault  : GetFirstOrDefaultElement (query); break;
				}
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ElementAtContext(context.CloneContext(Context), _methodKind);
			}
		}

		static void BuildTake(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression expr, TakeHints? hints)
		{
			var sql = sequence.SelectQuery;

			if (hints != null && !builder.DataContext.SqlProviderFlags.GetIsTakeHintsSupported(hints.Value))
				throw new LinqException($"TakeHints are {hints} not supported by current database");

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqException("Take with hints could not be applied with Skip");

			if (sql.Select.TakeValue != null)
			{
				expr = new SqlFunction(
					typeof(int),
					"CASE",
					new SqlBinaryExpression(typeof(bool), sql.Select.TakeValue, "<", expr, Precedence.Comparison),
					sql.Select.TakeValue,
					expr);
			}

			sql.Select.Take(expr, hints);

			if ( sql.Select.SkipValue != null                         &&
			     builder.DataContext.SqlProviderFlags.IsTakeSupported &&
			     !builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql.Select.TakeValue, sql.Select.SkipValue))
			{
				sql.Select.Take(
					new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue!,
						Precedence.Additive), hints);
			}
		}

		static void BuildSkip(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression expr)
		{
			var sql = sequence.SelectQuery;

			if (sql.Select.TakeHints != null)
				throw new LinqException("Skip could not be applied with Take with hints");

			if (sql.Select.SkipValue != null)
				sql.Select.Skip(new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", expr, Precedence.Additive));
			else
				sql.Select.Skip(expr);

			if (sql.Select.TakeValue != null)
			{
				if (builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql.Select.TakeValue, sql.Select.SkipValue) ||
				    !builder.DataContext.SqlProviderFlags.IsTakeSupported)
				{
					sql.Select.Take(
						new SqlBinaryExpression(typeof(int), sql.Select.TakeValue, "-", expr, Precedence.Additive),
						sql.Select.TakeHints);
				}
			}
		}
	}
}
