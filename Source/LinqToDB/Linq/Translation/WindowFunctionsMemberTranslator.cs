using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
			Registration.RegisterMethod(() => Sql.Window.Rank(f => f.OrderBy(1)), TranslateRank);
			Registration.RegisterMethod(() => Sql.Window.DenseRank(f => f.OrderBy(1)), TranslateDenseRank);
			Registration.RegisterMethod(() => Sql.Window.PercentRank(f => f.OrderBy(1)), TranslatePercentRank);
			Registration.RegisterMethod(() => Sql.Window.CumeDist(f => f.OrderBy(1)), TranslateCumeDist);
			Registration.RegisterMethod(() => Sql.Window.NTile(1, f => f.OrderBy(1)), TranslateNTile);

			Registration.RegisterMethod((IEnumerable<int> g) => g.PercentileCont(0.5, (e, f) => f.OrderBy(e)), TranslatePercentileCont);
			Registration.RegisterMethod((IQueryable<int>  g) => g.PercentileCont(0.5, (e, f) => f.OrderBy(e)), TranslatePercentileCont);

			RegisterSum();
			RegisterAvg();
			RegisterMin();
			RegisterMax();
		}

		void RegisterSum()
		{
			Registration.RegisterMethod(() => Sql.Window.Sum((int)1,        f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((int?)1,       f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((long)1L,      f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((long?)1L,     f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((double)1.0,   f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((double?)1.0,  f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((decimal)1.0,  f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((decimal?)1.0, f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((float)1f,     f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((float?)1f,    f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((short)1,      f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((short?)1,     f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((byte)1,       f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((byte?)1,      f => f.OrderBy(1)), TranslateSum);
		}

		void RegisterAvg()
		{
			Registration.RegisterMethod(() => Sql.Window.Average((int)1,        f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((int?)1,       f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((long)1L,      f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((long?)1L,     f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((double)1.0,   f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((double?)1.0,  f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((decimal)1.0,  f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((decimal?)1.0, f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((float)1f,     f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((float?)1f,    f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((short)1,      f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((short?)1,     f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((byte)1,       f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((byte?)1,      f => f.OrderBy(1)), TranslateAverage);
		}

		void RegisterMin()
		{
			Registration.RegisterMethod(() => Sql.Window.Min((int)1,        f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((int?)1,       f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((long)1L,      f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((long?)1L,     f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((double)1.0,   f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((double?)1.0,  f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((decimal)1.0,  f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((decimal?)1.0, f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((float)1f,     f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((float?)1f,    f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((short)1,      f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((short?)1,     f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((byte)1,       f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((byte?)1,      f => f.OrderBy(1)), TranslateMin);
		}

		void RegisterMax()
		{
			Registration.RegisterMethod(() => Sql.Window.Max((int)1,        f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((int?)1,       f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((long)1L,      f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((long?)1L,     f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((double)1.0,   f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((double?)1.0,  f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((decimal)1.0,  f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((decimal?)1.0, f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((float)1f,     f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((float?)1f,    f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((short)1,      f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((short?)1,     f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((byte)1,       f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((byte?)1,      f => f.OrderBy(1)), TranslateMax);
		}

		public record ArgumentInformation(Expression Expr, Sql.AggregateModifier Modifier);
		public record OrderByInformation(Expression Expr, bool IsDescending, Sql.NullsPosition Nulls);
		public record FrameBoundary(bool IsPreceding, SqlFrameBoundary.FrameBoundaryType BoundaryType, Expression? Offset);

		public class WindowFunctionInformation
		{
			public required ArgumentInformation[]?        Arguments   { get; set; }
			public required Expression[]?                 PartitionBy { get; set; }
			public required OrderByInformation[]?         OrderBy     { get; set; }
			public required Expression?                   Filter      { get; set; }
			public required SqlFrameClause.FrameTypeKind? FrameType   { get; set; }
			public required FrameBoundary?                Start       { get; set; }
			public required FrameBoundary?                End         { get; set; }
		}

		protected static bool CollectWindowFunctionInformation(
			ITranslationContext                                 translationContext,
			Type                                                expressionType,
			Expression[]?                                       functionArguments,
			Expression                                          buildBody,
			[NotNullWhen(true)] out  WindowFunctionInformation? functionInfo,
			[NotNullWhen(false)] out SqlErrorExpression?        error)
		{
			functionInfo = null;
			error        = null;

			List<ArgumentInformation>? argumentsList   = null;
			List<Expression>?          partitionByList = null;
			List<OrderByInformation>?  orderByList     = null;
			Expression?                filter          = null;

#pragma warning disable CS0219 // Variable is assigned but its value is never used
			SqlFrameClause.FrameTypeKind? frameType     = null;
			FrameBoundary?             endBoundary   = null;
			FrameBoundary?             startBoundary = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used

			if (functionArguments != null)
			{
				argumentsList ??= new();
				foreach (var argument in functionArguments)
				{
					argumentsList.Add(new(argument, Sql.AggregateModifier.None));
				}
			}

			while (true)
			{
				var current = buildBody;

				if (buildBody is MethodCallExpression mc)
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

							var        nulls = Sql.NullsPosition.None;
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

							orderByList.Insert(0, new(argument, isDesc, nulls));

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
							var        modifier = Sql.AggregateModifier.None;
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

							argumentsList.Add(new(argument, modifier));

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
							else
							{
								error = translationContext.CreateErrorExpression(buildBody, "Expected window definition", expressionType);
								return false;
							}

							break;
						}

						case nameof(WindowFunctionBuilder.DefineWindow):
						{
							buildBody = mc.Arguments[1];
							break;
						}

						case nameof(WindowFunctionBuilder.IBoundaryPart<int>.Value):
						{
							var boundary = new FrameBoundary(endBoundary != null, SqlFrameBoundary.FrameBoundaryType.Offset, mc.Arguments[0].UnwrapConvertToObject());
							if (endBoundary == null)
								endBoundary = boundary;
							else
								startBoundary = boundary;

							buildBody = mc.Object ?? buildBody;
							break;
						}

					}
				}
				else if (buildBody is MemberExpression me)
				{
					switch (me.Member.Name)
					{
						case nameof(WindowFunctionBuilder.IFramePartFunction.GroupsBetween):
						case nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetween):
						case nameof(WindowFunctionBuilder.IFramePartFunction.RowsBetween):
						{
							switch (me.Member.Name)
							{
								case nameof(WindowFunctionBuilder.IFramePartFunction.GroupsBetween):
									frameType = SqlFrameClause.FrameTypeKind.Groups;
									break;
								case nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetween):
									frameType = SqlFrameClause.FrameTypeKind.Range;
									break;
								case nameof(WindowFunctionBuilder.IFramePartFunction.RowsBetween):
									frameType = SqlFrameClause.FrameTypeKind.Rows;
									break;
								default:
									error = translationContext.CreateErrorExpression(buildBody, $"Unexpected frame type {me.Member.Name}", expressionType);
									return false;
							}

							buildBody = me.Expression ?? buildBody;

							break;
						}

						case nameof(WindowFunctionBuilder.IBoundaryPart<int>.CurrentRow):
						{
							var boundary = new FrameBoundary(endBoundary != null, SqlFrameBoundary.FrameBoundaryType.CurrentRow, null);
							if (endBoundary == null)
								endBoundary = boundary;
							else
								startBoundary = boundary;

							buildBody = me.Expression ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IBoundaryPart<int>.Unbounded):
						{
							var boundary = new FrameBoundary(endBoundary != null, SqlFrameBoundary.FrameBoundaryType.Unbounded, null);
							if (endBoundary == null)
								endBoundary = boundary;
							else
								startBoundary = boundary;

							buildBody = me.Expression ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IRangePrecedingPartFunction.And):
						{
							buildBody = me.Expression ?? buildBody;
							break;	
						}
					}
				}

				if (buildBody == current)
				{
					if (current is not ParameterExpression)
					{
						error = translationContext.CreateErrorExpression(buildBody, "Unexpected member.", expressionType);
						return false;
					}

					break;
				}
			}

			if (frameType != null && (startBoundary == null || endBoundary == null))
			{
				error = translationContext.CreateErrorExpression(buildBody, "Expected both start and end boundaries", expressionType);
				return false;
			}
			
			functionInfo = new WindowFunctionInformation
			{
				Arguments = argumentsList?.ToArray(),
				PartitionBy = partitionByList?.ToArray(),
				OrderBy = orderByList?.ToArray(),
				Filter = filter,
				FrameType = frameType,
				Start = startBoundary,
				End = endBoundary
			};

			return true;
		}

		protected bool TranslateOrderItems(ISqlExpressionTranslator translator, Type errorType, IEnumerable<OrderByInformation> orderBy, List<SqlWindowOrderItem> orderItems, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			error = null;
			foreach (var orderItem in orderBy)
			{
				if (!translator.TranslateExpression(orderItem.Expr, out var sql, out error))
				{
					error = error!.WithType(errorType);
					return false;
				}

				orderItems.Add(new SqlWindowOrderItem(sql, orderItem.IsDescending, orderItem.Nulls));
			}

			return true;
		}

		protected bool TranslatePartitionBy(ISqlExpressionTranslator translator, Type errorType, IEnumerable<Expression> partitionBy, List<ISqlExpression> partitionByItems, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			error = null;
			foreach (var partition in partitionBy)
			{
				if (!translator.TranslateExpression(partition, out var sql, out error))
				{
					error = error!.WithType(errorType);
					return false;
				}

				partitionByItems.Add(sql);
			}

			return true;
		}

		protected Expression TranslateWindowFunction(
			ITranslationContext  translationContext,
			MethodCallExpression methodCall,
			int?                 argumentIndex,
			int                  windowArgument,
			DbDataType           dbDataType,
			string               functionName)
		{
			if (!CollectWindowFunctionInformation(
				    translationContext, 
				    methodCall.Type, 
				    argumentIndex == null ? null : [methodCall.Arguments[argumentIndex.Value]],
				    methodCall.Arguments[windowArgument].UnwrapLambda().Body, 
				    out var information, 
				    out var error))
				return error;

			var                       arguments   = new List<SqlFunctionArgument>();
			List<ISqlExpression>?     partitionBy = null;
			List<SqlWindowOrderItem>? orderItems  = null;
			SqlSearchCondition?       filter      = null;
			SqlFrameClause?           frame       = null;

			if (information.Arguments != null)
			{
				foreach (var argument in information.Arguments)
				{
					var translated = translationContext.Translate(argument.Expr);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					arguments.Add(new SqlFunctionArgument(placeholder.Sql, argument.Modifier));
				}
			}

			if (information.PartitionBy != null)
			{
				partitionBy ??= new();
				if (!TranslatePartitionBy(translationContext, methodCall.Type, information.PartitionBy, partitionBy, out var partitionError))
					return partitionError;
			}

			if (information.OrderBy != null)
			{
				orderItems ??= new();
				if (!TranslateOrderItems(translationContext, methodCall.Type, information.OrderBy, orderItems, out var orderError))
					return orderError;
			}

			if (information.Filter != null)
			{
				var translated = translationContext.Translate(information.Filter);
				if (translated is not SqlPlaceholderExpression placeholder || placeholder.Sql is not SqlSearchCondition sc)
					return SqlErrorExpression.EnsureError(translated, methodCall.Type);
				filter = sc;
			}

			if (information.FrameType != null)
			{
				var frameType = information.FrameType.Value;
				var start     = information.Start;
				var end       = information.End;

				if (start == null || end == null)
					throw new InvalidOperationException("Expected both start and end boundaries");

				ISqlExpression? startOffset = null;
				ISqlExpression? endOffset   = null;

				if (start.Offset != null)
				{
					var translated = translationContext.Translate(start.Offset);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					startOffset = placeholder.Sql;
				}

				if (end.Offset != null) 
				{
					var translated = translationContext.Translate(end.Offset);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					endOffset = placeholder.Sql;
				}

				var startBoundary = new SqlFrameBoundary(start.IsPreceding, start.BoundaryType, startOffset);
				var endBoundary   = new SqlFrameBoundary(end.IsPreceding, end.BoundaryType, endOffset);
				frame = new SqlFrameClause(frameType, startBoundary, endBoundary);
			}

			var function = translationContext.ExpressionFactory.WindowFunction(dbDataType, functionName,
				arguments.ToArray(),
				arguments.Select(a => true).ToArray(),
				partitionBy : partitionBy,
				orderBy : orderItems,
				filter : filter,
				frameClause : frame
			);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, function, methodCall);
		}

		static LambdaExpression SimplifyEntityLambda(LambdaExpression lambda, int parameterIndex, Expression contextExpression)
		{
			var paramToReplace = lambda.Parameters[parameterIndex];
			var newBody = lambda.Body.Transform(e =>
			{
				if (e == paramToReplace)
				{
					if (contextExpression is ContextRefExpression contextRefExpression)
					{
						var contextTyped = contextRefExpression.WithType(e.Type);
						return contextTyped;
					}
				}

				return e;
			});

			var newParameters = lambda.Parameters.ToList();
			newParameters.RemoveAt(parameterIndex);

			return Expression.Lambda(newBody, newParameters);
		}

		public virtual Expression? TranslateRowNumber(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "ROW_NUMBER");
		}

		public virtual Expression? TranslateRank(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "RANK");
		}

		public virtual Expression? TranslateDenseRank(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "DENSE_RANK");
		}

		public virtual Expression? TranslatePercentRank(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "PERCENT_RANK");
		}

		public virtual Expression? TranslateCumeDist(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "CUME_DIST");
		}

		public virtual Expression? TranslateNTile(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "NTILE");
		}

		public virtual Expression? TranslatePercentileCont(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var result = translationContext.BuildAggregationFunction(methodCall.Arguments[0], methodCall, ITranslationContext.AllowedAggregationOperators.None, ac =>
			{
				var argumentExpr = methodCall.Arguments[1];
				if (!ac.TranslateExpression(argumentExpr, out var argumentSql, out var error))
					return (null, error);

				var builderLambda = methodCall.Arguments[2].UnwrapLambda();

				builderLambda = ac.SimplifyEntityLambda(builderLambda, 0);

				if (!CollectWindowFunctionInformation(
					    translationContext,
					    methodCall.Type,
					    null,
					    builderLambda.Body,
					    out var information,
					    out error))
					return (null, error);

				if (information.OrderBy!.Length != 1)
					return (null, translationContext.CreateErrorExpression(methodCall.Arguments[2], "Expected single order by expression", methodCall.Type));

				List<SqlWindowOrderItem> withinGroupOrder = new();
				if (!TranslateOrderItems(ac, methodCall.Type, information.OrderBy, withinGroupOrder, out var orderError))
					return (null, orderError);

				List<ISqlExpression>? partitionBy = null;
				if (information.PartitionBy != null)
				{
					partitionBy ??= new();
					if (!TranslatePartitionBy(ac, methodCall.Type, information.PartitionBy, partitionBy, out var partitionError))
						return (null, partitionError);
				}

				var functionType = translationContext.GetDbDataType(withinGroupOrder[0].Expression);

				var windowFunction = translationContext.ExpressionFactory.WindowFunction(
					functionType,
					"PERCENTILE_CONT",
					[new SqlFunctionArgument(argumentSql, Sql.AggregateModifier.None)],
					[true],
					withinGroup : withinGroupOrder,
					partitionBy : partitionBy,
					isAggregate: true
				);

				return (windowFunction, null);
			});

			if (result == null)
				return translationContext.CreateErrorExpression(methodCall, "Failed to build aggregation function for PERCENTILE_CONT.", methodCall.Type);

			return result;
		}

		public virtual Expression? TranslatePercentileContOld(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var enumerableContext = translationContext.GetAggregationContext(methodCall.Arguments[0]);
			if (enumerableContext == null)
				return translationContext.CreateErrorExpression(methodCall.Arguments[0], "Enumerable context is not discoverable.", methodCall.Type);

			using var contextScope = translationContext.UsingCurrentAggregationContext(enumerableContext);

			var argumentExpr = methodCall.Arguments[1];
			if (!translationContext.TranslateToSqlExpression(argumentExpr, out var argumentSql))
				return translationContext.CreateErrorExpression(argumentExpr, type : methodCall.Type);

			var builderLambda = methodCall.Arguments[2].UnwrapLambda();

			builderLambda = SimplifyEntityLambda(builderLambda, 0, enumerableContext);

			if (!CollectWindowFunctionInformation(
				    translationContext,
				    methodCall.Type,
				    null,
				    builderLambda.Body,
				    out var information,
				    out var error))
				return error;

			if (information.OrderBy!.Length != 1)
				return translationContext.CreateErrorExpression(methodCall.Arguments[2], "Expected single order by expression", methodCall.Type);

			List<SqlWindowOrderItem> withinGroupOrder = new();
			if (!TranslateOrderItems(translationContext, methodCall.Type, information.OrderBy, withinGroupOrder, out var orderError))
				return orderError;

			List<ISqlExpression>? partitionBy = null;
			if (information.PartitionBy != null)
			{
				partitionBy ??= new();
				if (!TranslatePartitionBy(translationContext, methodCall.Type, information.PartitionBy, partitionBy, out var partitionError))
					return partitionError;
			}

			var functionType = translationContext.GetDbDataType(withinGroupOrder[0].Expression);

			var windowFunction = translationContext.ExpressionFactory.WindowFunction(
				functionType,
				"PERCENTILE_CONT",
				[new SqlFunctionArgument(argumentSql, Sql.AggregateModifier.None)],
				[true],
				withinGroup : withinGroupOrder,
				partitionBy : partitionBy,
				isAggregate: true
			);

			return translationContext.CreatePlaceholder(translationContext.GetAggregationSelectQuery(enumerableContext), windowFunction, methodCall);
		}

		public virtual Expression? TransformPercentileCont(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return WindowFunctionHelpers.BuildAggregateExecuteExpression(methodCall); 
		}

		public virtual Expression? TranslateSum(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "SUM");
		}

		public virtual Expression? TranslateAverage(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "AVG");
		}

		public virtual Expression? TranslateMin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "MIN");
		}

		public virtual Expression? TranslateMax(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "MAX");
		}
	}
}
