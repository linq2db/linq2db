using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlQuery;
	using Common;
	using LinqToDB.Expressions;

	sealed class InsertBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames = 
		{
			nameof(LinqExtensions.Insert),
			nameof(LinqExtensions.InsertWithIdentity),
			nameof(LinqExtensions.InsertWithOutput),
			nameof(LinqExtensions.InsertWithOutputInto)
		};

		#region InsertBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		static void ExtractSequence(ref IBuildContext sequence, out InsertContext insertContext)
		{
			if (sequence is InsertContext ic)
			{
				insertContext = ic;
				sequence      = insertContext.QuerySequence;
			}
			else
			{
				insertContext = new InsertContext(sequence, InsertContext.InsertTypeEnum.Insert,
					new SqlInsertStatement(sequence.SelectQuery), null);
			}
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			ExtractSequence(ref sequence, out var insertContext);

			var insertStatement = insertContext.InsertStatement;

			var insertType = InsertContext.InsertTypeEnum.Insert;

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.Insert)                : insertType = InsertContext.InsertTypeEnum.Insert;             break;
				case nameof(LinqExtensions.InsertWithIdentity)    : insertType = InsertContext.InsertTypeEnum.InsertWithIdentity; break;
				case nameof(LinqExtensions.InsertWithOutput)      : insertType = InsertContext.InsertTypeEnum.InsertOutput;       break;
				case nameof(LinqExtensions.InsertWithOutputInto)  : insertType = InsertContext.InsertTypeEnum.InsertOutputInto;   break;
			}

			insertContext.InsertType = insertType;

			static LambdaExpression BuildDefaultOutputExpression(Type outputType)
			{
				var param = Expression.Parameter(outputType);
				return Expression.Lambda(param, param);
			}

			IBuildContext?    outputContext    = null;
			LambdaExpression? outputExpression = null;

			if (methodCall.Arguments.Count > 0)
			{
				var argument         = methodCall.Arguments[0];
				var genericArguments = methodCall.Method.GetGenericArguments();

				if (typeof(IValueInsertable<>).IsSameOrParentOf(argument.Type) ||
				    typeof(ISelectInsertable<,>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)
					//

					insertContext.Into ??= sequence;

					if (insertContext.SetExpressions.Count == 0 && !insertContext.RequiresSetters)
					{
						var sourceRef = new ContextRefExpression(genericArguments[0], sequence);
						var targetRef = new ContextRefExpression(genericArguments.Skip(1).FirstOrDefault() ?? sourceRef.Type,
								insertContext.Into);

						var sqlExpr = builder.ConvertToSqlExpr(sequence, sourceRef);

						UpdateBuilder.ParseSetter(builder, targetRef, sqlExpr, insertContext.SetExpressions);
					}
				}
				else if (methodCall.Arguments.Count > 1                  &&
					typeof(IQueryable<>).IsSameOrParentOf(argument.Type) &&
					typeof(ITable<>).IsSameOrParentOf(methodCall.Arguments[1].Type))
				{
					// static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)
					//

					var into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
					insertContext.Into = into;

					var setter     = methodCall.GetArgumentByName("setter")!.UnwrapLambda();
					var setterExpr = SequenceHelper.PrepareBody(setter, sequence);

					var targetType = genericArguments[1];
					var contextRef = new ContextRefExpression(targetType, into);

					UpdateBuilder.ParseSetter(builder, contextRef, setterExpr, insertContext.SetExpressions);
				}
				else if (typeof(ITable<>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>(this Table<T> target, Expression<Func<T>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter, Expression<Func<TTarget,TOutput>> outputExpression)
					//

					var argIndex   = 1;
					var arg        = methodCall.Arguments[argIndex].Unwrap();
					var targetType = genericArguments[0];

					insertContext.Into = sequence;

					var tableContext = SequenceHelper.GetTableContext(sequence);
					if (tableContext == null)
						throw new InvalidOperationException("Table context not found.");

					var intoContextRef = new ContextRefExpression(targetType, insertContext.Into);

					Expression setterExpr;
					switch (arg)
					{
						case LambdaExpression lambda:
						{
							if (lambda.Parameters.Count == 0)
							{
								setterExpr = lambda.Body;
							}	
							else
								throw new NotImplementedException();

							break;
						}
						default:
						{
							setterExpr = builder.BuildFullEntityExpression(arg, targetType,
								ProjectFlags.SQL, ExpressionBuilder.FullEntityPurpose.Insert);

							break;
						}
					}

					UpdateBuilder.ParseSetter(builder,
						intoContextRef, 
						setterExpr, 
						insertContext.SetExpressions);
				}

				if (insertType == InsertContext.InsertTypeEnum.InsertOutput || insertType == InsertContext.InsertTypeEnum.InsertOutputInto)
				{
					outputExpression =
						methodCall.GetArgumentByName("outputExpression")?.UnwrapLambda()
						?? BuildDefaultOutputExpression(genericArguments.Last());

					insertStatement.Output = new SqlOutputClause();
					insertContext.OutputExpression = outputExpression;

					var insertedTable = builder.DataContext.SqlProviderFlags.OutputInsertUseSpecialTable 
                        ? SqlTable.Inserted(builder.MappingSchema.GetEntityDescriptor(outputExpression.Parameters[0].Type, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated))
                        : null;

					if (insertedTable == null && insertContext.Into != null)
					{
						insertedTable = SequenceHelper.GetTableContext(insertContext.Into)?.SqlTable;
					}

					if (insertedTable == null)
						throw new InvalidOperationException("Cannot find target table for INSERT statement");

					insertContext.OutputContext = new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable);

					if (builder.DataContext.SqlProviderFlags.OutputInsertUseSpecialTable)
						insertStatement.Output.InsertedTable = insertedTable;

					if (insertType == InsertContext.InsertTypeEnum.InsertOutputInto)
					{
						var outputTable = methodCall.GetArgumentByName("outputTable")!;
						var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

						var destinationRef = new ContextRefExpression(outputExpression.Parameters[0].Type, destination);
						var outputExpr     = SequenceHelper.PrepareBody(outputExpression, insertContext.OutputContext);

						insertStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;

						var outputSetters = new List<UpdateBuilder.SetExpressionEnvelope>();
						UpdateBuilder.ParseSetter(builder, destinationRef, outputExpr, outputSetters);

						UpdateBuilder.InitializeSetExpressions(builder, insertContext.OutputContext, insertContext.OutputContext,
							outputSetters, insertStatement.Output.OutputItems, false);
					}
				}
			}

			if (insertContext.RequiresSetters && insertContext.SetExpressions.Count == 0)
				throw new LinqToDBException("Insert query has no setters defined.");

			insertContext.LastBuildInfo = buildInfo;
			insertContext.FinalizeSetters();

			insertStatement.Insert.WithIdentity = insertType == InsertContext.InsertTypeEnum.InsertWithIdentity;

			return insertContext;
		}

		#endregion

		#region InsertContext

		public sealed class InsertContext : PassThroughContext
		{
			public SqlInsertStatement InsertStatement { get; }

			public enum InsertTypeEnum
			{
				Insert,
				InsertWithIdentity,
				InsertOutput,
				InsertOutputInto
			}

			public InsertContext(IBuildContext querySequence, InsertTypeEnum insertType, SqlInsertStatement insertStatement, LambdaExpression? outputExpression)
				: base(querySequence, querySequence.SelectQuery)
			{
				InsertType       = insertType;
				InsertStatement  = insertStatement;
				OutputExpression = outputExpression;
			}

			public InsertTypeEnum InsertType { get; set; }

			public List<UpdateBuilder.SetExpressionEnvelope> SetExpressions { get; } = new ();

			public IBuildContext              QuerySequence    => Context;
			public IBuildContext?             Into             { get; set; }
			public BuildInfo?                 LastBuildInfo    { get; set; }
			public LambdaExpression?          OutputExpression { get; set; }
			public TableBuilder.TableContext? OutputContext    { get; set; }
			public bool                       RequiresSetters  { get; set; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					FinalizeSetters();

					if (InsertType == InsertTypeEnum.InsertOutput)
					{
						if (OutputExpression == null || OutputContext == null || LastBuildInfo == null)
							throw new InvalidOperationException();

						var selectContext = new SelectContext(Parent, OutputExpression, false, OutputContext);
						var outputRef = new ContextRefExpression(path.Type, selectContext);

						var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

						var sqlExpr = Builder.ConvertToSqlExpr(selectContext, outputRef);
						if (sqlExpr is SqlPlaceholderExpression)
							outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr));
						else
							UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

						var setItems = new List<SqlSetExpression>();
						UpdateBuilder.InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

						InsertStatement.Output!.OutputColumns = setItems.Select(c => c.Expression!).ToList();

						return sqlExpr;

					}

					return Expression.Default(path.Type);
				}

				return base.MakeExpression(path, flags);
			}

			public void FinalizeSetters()
			{
				var insert = InsertStatement.Insert;

				if (insert.Items.Count > 0 || LastBuildInfo == null)
					return;

				if (Into == null)
				{
					throw new LinqToDBException("Insert query has no defined target table.");
				}

				var tableContext = SequenceHelper.GetTableContext(Into);
			
				insert.Into = tableContext?.SqlTable;

				if (tableContext == null || insert.Into == null)
					throw new LinqToDBException("Insert query has no setters defined.");

				SetExpressions.RemoveDuplicatesFromTail((s1, s2) =>
					ExpressionEqualityComparer.Instance.Equals(s1.FieldExpression, s2.FieldExpression));

				UpdateBuilder.InitializeSetExpressions(Builder, tableContext, QuerySequence, SetExpressions, insert.Items, true);

				var q = insert.Into.IdentityFields
					.Except(insert.Items.Select(e => e.Column).OfType<SqlField>());

				foreach (var field in q)
				{
					var expr = Builder.DataContext.CreateSqlProvider().GetIdentityExpression(insert.Into);

					if (expr != null)
					{
						var identitySet = new SqlSetExpression(field, expr);
						insert.Items.Insert(0, identitySet);

						QuerySequence.SelectQuery.Select.Columns.Insert(0, new SqlColumn(QuerySequence.SelectQuery, identitySet.Expression!));
					}
				}

			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				switch (InsertType)
				{
					case InsertTypeEnum.Insert:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}
					case InsertTypeEnum.InsertWithIdentity:
					{
						QueryRunner.SetScalarQuery(query);
						break;
					}	
					case InsertTypeEnum.InsertOutput:
					{
						var mapper = Builder.BuildMapper<T>(SelectQuery, expr);
						QueryRunner.SetRunQuery(query, mapper);
						break;
					}					
					case InsertTypeEnum.InsertOutputInto:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}	
					default:
						throw new InvalidOperationException($"Unexpected insert type: {InsertType}");
				}
			}

			public override SqlStatement GetResultStatement()
			{
				return InsertStatement;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new InsertContext(context.CloneContext(QuerySequence), InsertType, context.CloneElement(InsertStatement), context.CloneExpression(OutputExpression));
			}
		}

		#endregion

		#region Into

		internal sealed class Into : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Into");
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var source = methodCall.Arguments[0].Unwrap();
				var into   = methodCall.Arguments[1].Unwrap();

				IBuildContext      sequence;
				IBuildContext      destinationSequence;
				SqlInsertStatement insertStatement;
				InsertContext      insertContext;

				// static IValueInsertable<T> Into<T>(this IDataContext dataContext, Table<T> target)
				//
				if (source.IsNullValue())
				{
					sequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQuery()));
					destinationSequence = sequence;
				}
				// static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target)
				//
				else
				{
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));
					destinationSequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQuery()));

				}

				insertStatement = new SqlInsertStatement(sequence.SelectQuery);
				insertContext = new InsertContext(sequence, InsertContext.InsertTypeEnum.Insert, insertStatement, null);
				insertContext.Into = destinationSequence;
				insertContext.LastBuildInfo = buildInfo;

				return insertContext;
			}
		}

		#endregion

		#region Value

		internal sealed class Value : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Value");
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
				var extract  = methodCall.Arguments[1].UnwrapLambda();
				var update   = methodCall.Arguments[2].Unwrap();

				ExtractSequence(ref sequence, out var insertContext);

				insertContext.Into ??= sequence;

				var tableType  = methodCall.Method.GetGenericArguments()[1];
				var contextRef = new ContextRefExpression(tableType, insertContext.Into);

				var extractExp = SequenceHelper.PrepareBody(extract, insertContext.Into);
				var updateExpr = update;

				if (updateExpr is ConstantExpression constantExpr)
				{
					builder.ParametersContext.MarkAsParameter(constantExpr);
				}
				else if (updateExpr is LambdaExpression updateLambda)
				{
					updateExpr = SequenceHelper.PrepareBody(updateLambda, sequence);
				}

				UpdateBuilder.ParseSet(contextRef, extractExp, updateExpr, insertContext.SetExpressions);
				insertContext.LastBuildInfo = buildInfo;

				return insertContext;
			}
		}

		#endregion
	}
}
