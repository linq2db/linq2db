using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Internal;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public class WindowFunctionsMemberTranslator : MemberTranslatorBase
	{
		public WindowFunctionsMemberTranslator()
		{
			Registration.RegisterMethod(() => Sql.Window.RowNumber(f => f.OrderBy(1)), TranslateRowNumber);
		}

		public class WindowFunctionInformation
		{
			public required (Expression expr, Sql.AggregateModifier modifier)[]?             Arguments   { get; set; }
			public required Expression[]?                                                    PartitionBy { get; set; }
			public required (Expression expr, bool isDescending, Sql.NullsPosition nulls)[]? OrderBy     { get; set; }
			public required Expression?                                                      Filter      { get; set; }
		}

		protected static WindowFunctionInformation CollectWindowFunctionInformation(ITranslationContext translationContext, Expression[]? functionArguments, Expression buildBody)
		{
			List<(Expression expr, Sql.AggregateModifier modifier)>?             argumentsList   = null;
			List<Expression>?                                                    partitionByList = null;
			List<(Expression expr, bool isDescending, Sql.NullsPosition nulls)>? orderByList     = null;
			Expression?                                                          filter          = null;

			if (functionArguments != null)
			{
				argumentsList ??= new();
				foreach (var argument in functionArguments)
				{
					argumentsList.Add((argument, Sql.AggregateModifier.None));
				}
			}

			while (buildBody is MethodCallExpression mc)
			{
				switch (mc.Method.Name)
				{
					case nameof(WindowFunctionBuilder.IOrderByPart<object>.OrderBy):
					case nameof(WindowFunctionBuilder.IOrderByPart<object>.OrderByDesc):
					case nameof(WindowFunctionBuilder.IThenOrderPart<object>.ThenBy):
					case nameof(WindowFunctionBuilder.IThenOrderPart<object>.ThenByDesc):
					{
						var isDesc = mc.Method.Name == nameof(WindowFunctionBuilder.IOrderByPart<object>.OrderByDesc) ||
						             mc.Method.Name == nameof(WindowFunctionBuilder.IThenOrderPart<object>.ThenByDesc);

						orderByList ??= new();

						var nulls = Sql.NullsPosition.None;
						Expression argument;
						if (mc.Arguments.Count == 2)
						{
							argument = mc.Arguments[0];
							nulls = (Sql.NullsPosition)mc.Arguments[1].EvaluateExpression()!;
						}
						else
						{
							argument = mc.Arguments[0];
						}

						orderByList.Insert(0, (argument, isDesc, nulls));

						buildBody = mc.Object!;
						break;
					}

					case nameof(WindowFunctionBuilder.IPartitionPart<object>.PartitionBy):
					{
						partitionByList ??= new();

						if (mc.Arguments[0].NodeType == ExpressionType.NewArrayInit)
						{
							foreach (var argument in ((NewArrayExpression)mc.Arguments[0]).Expressions)
							{
								partitionByList.Add(argument);
							}
						}
						else
						{
							partitionByList.Add(mc.Arguments[0]);
						}

						buildBody = mc.Object!;
						break;
					}

					case nameof(WindowFunctionBuilder.IArgumentPart<object>.Argument):
					{
						argumentsList ??= new();
						var modifier = Sql.AggregateModifier.None;
						Expression argument;
						if (mc.Arguments.Count == 2)
						{
							modifier = (Sql.AggregateModifier)mc.Arguments[0].EvaluateExpression()!;
							argument = mc.Arguments[1];
						}
						else
						{
							argument = mc.Arguments[0];
						}

						argumentsList.Add((argument, modifier));

						buildBody = mc.Object!;
						break;
					}

					case nameof(WindowFunctionBuilder.IFilterPart<object>.Filter):
					{
						filter = mc.Arguments[0];

						buildBody = mc.Object!;
						break;
					}

					case nameof(WindowFunctionBuilder.IUseWindow<object>.UseWindow):
					{
						buildBody = mc.Arguments[0];
						var expanded = translationContext.Translate(buildBody, TranslationFlags.Expand);
						if (expanded is MethodCallExpression { Method.Name: nameof(WindowFunctionBuilder.DefineWindow) } mce)
						{
							buildBody = mce.Arguments[1].UnwrapLambda().Body;
						}
						break;
					}

					case nameof(WindowFunctionBuilder.DefineWindow):
					{
						buildBody = mc.Arguments[1];
						break;
					}

				}

				if (buildBody == mc)
					break;
			}

			return new WindowFunctionInformation
			{
				Arguments = argumentsList?.ToArray(),
				PartitionBy = partitionByList?.ToArray(),
				OrderBy = orderByList?.ToArray(),
				Filter = filter
			};
		}

		protected Expression TranslateWindowFunction(ITranslationContext translationContext, MethodCallExpression methodCall, DbDataType dbDataType, string functionName)
		{
			var information = CollectWindowFunctionInformation(translationContext, null, methodCall.Arguments[1].UnwrapLambda().Body);

			var arguments   = new List<SqlFunctionArgument>();
			List<ISqlExpression>?     partitionBy = null;
			List<SqlWindowOrderItem>? orderItems  = null;
			SqlSearchCondition?       filter      = null;

			if (information.Arguments != null)
			{
				foreach (var argument in information.Arguments)
				{
					var translated = translationContext.Translate(argument.expr);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					arguments.Add(new SqlFunctionArgument(placeholder.Sql, argument.modifier));
				}
			}

			if (information.PartitionBy != null)
			{
				partitionBy ??= new();
				foreach (var partition in information.PartitionBy)
				{
					var translated = translationContext.Translate(partition);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					partitionBy.Add(placeholder.Sql);
				}
			}

			if (information.OrderBy != null)
			{
				orderItems ??= new();
				foreach (var orderBy in information.OrderBy)
				{
					var translated = translationContext.Translate(orderBy.expr);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					orderItems.Add(new SqlWindowOrderItem(placeholder.Sql, orderBy.isDescending, orderBy.nulls));
				}
			}

			if (information.Filter != null)
			{
				var translated = translationContext.Translate(information.Filter);
				if (translated is not SqlPlaceholderExpression placeholder || placeholder.Sql is not SqlSearchCondition sc)
					return SqlErrorExpression.EnsureError(translated, methodCall.Type);
				filter = sc;
			}

			var function = translationContext.ExpressionFactory.WindowFunction(dbDataType, functionName, 
				arguments.ToArray(), 
				arguments.Select(a => true).ToArray(), 
				partitionBy: partitionBy, 
				orderBy: orderItems,
				filter: filter
				);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, function, methodCall);
		}

		public virtual Expression? TranslateRowNumber(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, dbDataType, "ROW_NUMBER");
		}

	}
}
