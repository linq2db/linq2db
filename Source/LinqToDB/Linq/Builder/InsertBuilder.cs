using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(
		nameof(LinqExtensions.Insert), 
		nameof(LinqExtensions.InsertWithIdentity), 
		nameof(LinqExtensions.InsertWithOutput), 
		nameof(LinqExtensions.InsertWithOutputInto))]
	sealed class InsertBuilder : MethodCallBuilder
	{
		#region InsertBuilder

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

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
					new SqlInsertStatement(sequence.SelectQuery), null, false);
			}
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			ExtractSequence(ref sequence, out var insertContext);

			var insertStatement = insertContext.InsertStatement;

			var insertType = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.Insert)               => InsertContext.InsertTypeEnum.Insert,
				nameof(LinqExtensions.InsertWithIdentity)   => InsertContext.InsertTypeEnum.InsertWithIdentity,
				nameof(LinqExtensions.InsertWithOutput)     => InsertContext.InsertTypeEnum.InsertOutput,
				nameof(LinqExtensions.InsertWithOutputInto) => InsertContext.InsertTypeEnum.InsertOutputInto,
				_ => InsertContext.InsertTypeEnum.Insert,
			};

			insertContext.InsertType = insertType;

			static LambdaExpression BuildDefaultOutputExpression(Type outputType)
			{
				var param = Expression.Parameter(outputType);
				return Expression.Lambda(param, param);
			}

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

					insertContext.CreateColumns = typeof(ISelectInsertable<,>).IsSameOrParentOf(argument.Type);
					insertContext.Into ??= sequence;

					if (insertContext.SetExpressions.Count == 0 && !insertContext.RequiresSetters)
					{
						var sourceRef = new ContextRefExpression(genericArguments[0], sequence);
						var targetRef = new ContextRefExpression(genericArguments.Skip(1).FirstOrDefault() ?? sourceRef.Type,
								insertContext.Into);

						var sqlExpr = builder.BuildSqlExpression(sequence, sourceRef);

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
					var tableContext = SequenceHelper.GetTableOrCteContext(builder, new ContextRefExpression(into.ElementType, into));

					if (tableContext == null)
						throw new LinqToDBException("Invalid Into table");

					into = tableContext;

					insertContext.CreateColumns = true;
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
						case LambdaExpression lambda
							when lambda.Parameters.Count != 0:
						{
							throw new NotImplementedException();
						}

						case LambdaExpression lambda:
						{
							setterExpr = lambda.Body;
							break;
						}

						default:
						{
							setterExpr = builder.BuildFullEntityExpression(sequence.MappingSchema, arg, targetType, ProjectFlags.SQL, EntityConstructorBase.FullEntityPurpose.Insert);
							break;
						}
					}

					var sourceSequence = new SelectContext(
						builder.GetTranslationModifier(), 
						buildInfo.Parent,
						builder,
						null,
						setterExpr,
						new SelectQuery(), buildInfo.IsSubQuery);

					var sourceRef = new ContextRefExpression(sourceSequence.ElementType, sourceSequence);

					var redirectedExpression = builder.BuildSqlExpression(
						sourceSequence, sourceRef
					);

					insertContext.QuerySequence = sourceSequence;
					insertContext.InsertStatement.SelectQuery = sourceSequence.SelectQuery;

					UpdateBuilder.ParseSetter(builder,
						intoContextRef,
						redirectedExpression,
						insertContext.SetExpressions);
				}

				if (insertType is InsertContext.InsertTypeEnum.InsertOutput or InsertContext.InsertTypeEnum.InsertOutputInto)
				{
					outputExpression =
						methodCall.GetArgumentByName("outputExpression")?.UnwrapLambda()
						?? BuildDefaultOutputExpression(genericArguments.Last());

					insertStatement.Output = new SqlOutputClause();
					insertContext.OutputExpression = outputExpression;

					SqlTable? insertedTable = null;

					if (insertedTable == null && insertContext.Into != null)
					{
						insertedTable = SequenceHelper.GetTableContext(insertContext.Into)?.SqlTable;
					}

					if (insertedTable == null)
						throw new InvalidOperationException("Cannot find target table for INSERT statement");

					var outputTableContext = new TableBuilder.TableContext(builder.GetTranslationModifier(), builder, sequence.MappingSchema, new SelectQuery(), insertedTable, false);
					var outputAnchor       = new AnchorContext(buildInfo.Parent, outputTableContext, SqlAnchor.AnchorKindEnum.Inserted);
					insertContext.OutputContext = outputAnchor;

					if (insertType is InsertContext.InsertTypeEnum.InsertOutputInto)
					{
						var outputTable = methodCall.GetArgumentByName("outputTable")!;
						var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

						var destinationRef = new ContextRefExpression(outputExpression.Body.Type, destination);

						var outputExpr   = SequenceHelper.PrepareBody(outputExpression, outputAnchor);

						insertStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;

						var outputSetters = new List<UpdateBuilder.SetExpressionEnvelope>();
						UpdateBuilder.ParseSetter(builder, destinationRef, outputExpr, outputSetters);

						UpdateBuilder.InitializeSetExpressions(builder, outputAnchor, outputAnchor,
							outputSetters, insertStatement.Output.OutputItems, false);
					}
				}
			}

			if (insertContext.RequiresSetters && insertContext.SetExpressions.Count == 0)
				throw new LinqToDBException("Insert query has no setters defined.");

			insertContext.LastBuildInfo = buildInfo;
			insertContext.FinalizeSetters();

			insertStatement.Insert.WithIdentity = insertType is InsertContext.InsertTypeEnum.InsertWithIdentity;

			return BuildSequenceResult.FromContext(insertContext);
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

			public InsertContext(IBuildContext querySequence, InsertTypeEnum insertType, SqlInsertStatement insertStatement, LambdaExpression? outputExpression, bool createColumns)
				: base(querySequence, querySequence.SelectQuery)
			{
				CreateColumns    = createColumns;
				QuerySequence    = querySequence;
				InsertType       = insertType;
				InsertStatement  = insertStatement;
				OutputExpression = outputExpression;
			}

			public InsertTypeEnum InsertType { get; set; }

			public List<UpdateBuilder.SetExpressionEnvelope> SetExpressions { get; } = new ();

			public IBuildContext     QuerySequence    { get; set; }
			public IBuildContext?    Into             { get; set; }
			public BuildInfo?        LastBuildInfo    { get; set; }
			public LambdaExpression? OutputExpression { get; set; }
			public IBuildContext?    OutputContext    { get; set; }
			public bool              RequiresSetters  { get; set; }
			public bool              CreateColumns    { get; set; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					FinalizeSetters();

					if (InsertType == InsertTypeEnum.InsertOutput)
					{
						if (OutputExpression == null || OutputContext == null || LastBuildInfo == null)
							throw new InvalidOperationException();

						var outputSelectQuery = OutputContext.SelectQuery;

						IBuildContext selectContext = OutputContext is AnchorContext 
							? new SelectContext(Parent, OutputExpression, false, OutputContext)
							: new AnchorContext(Parent, new SelectContext(Parent, OutputExpression, false, OutputContext), SqlAnchor.AnchorKindEnum.Inserted);

						var outputRef     = new ContextRefExpression(path.Type, selectContext);

						var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

						var sqlExpr = Builder.BuildSqlExpression(selectContext, outputRef);
						sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, outputSelectQuery);

						if (sqlExpr is SqlPlaceholderExpression)
							outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr, false));
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

				UpdateBuilder.InitializeSetExpressions(Builder, tableContext, QuerySequence, SetExpressions, insert.Items, CreateColumns);

				var q = insert.Into.IdentityFields
					.Except(insert.Items.Select(e => e.Column).OfType<SqlField>());

				foreach (var field in q)
				{
					var expr = Builder.DataContext.CreateSqlProvider().GetIdentityExpression(insert.Into);

					if (expr != null)
					{
						var identitySet = new SqlSetExpression(field, expr);
						insert.Items.Insert(0, identitySet);

						if (CreateColumns)
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
				return new InsertContext(context.CloneContext(QuerySequence), InsertType, context.CloneElement(InsertStatement), context.CloneExpression(OutputExpression), CreateColumns);
			}
		}

		#endregion

		#region Into

		[BuildsMethodCall("Into")]
		internal sealed class Into : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsQueryable();

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder,
				MethodCallExpression                                                 methodCall, BuildInfo buildInfo)
			{
				var source = methodCall.Arguments[0].Unwrap();
				var into   = methodCall.Arguments[1].Unwrap();

				IBuildContext      sequence;
				IBuildContext      destinationSequence;
				SqlInsertStatement insertStatement;
				InsertContext      insertContext;
				bool               createColumns;

				// static IValueInsertable<T> Into<T>(this IDataContext dataContext, Table<T> target)
				//
				if (source.IsNullValue() || typeof(IDataContext).IsSameOrParentOf(source.Type))
				{
					createColumns = false;
					sequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQuery()));
					destinationSequence = sequence;
				}
				// static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target)
				//
				else
				{
					createColumns = true;
					sequence = builder.BuildSequence(new BuildInfo(buildInfo, source));
					destinationSequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, into, new SelectQuery()));

				}

				insertStatement = new SqlInsertStatement(sequence.SelectQuery);
				insertContext = new InsertContext(sequence, InsertContext.InsertTypeEnum.Insert, insertStatement, null, createColumns)
				{
					Into = destinationSequence,
					LastBuildInfo = buildInfo
				};

				return BuildSequenceResult.FromContext(insertContext);
			}
		}

		#endregion

		#region Value

		[BuildsMethodCall("Value")]
		internal sealed class Value : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsQueryable();

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder,
				MethodCallExpression                                                 methodCall, BuildInfo buildInfo)
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

				var forceParameters = true;
				if (updateExpr is LambdaExpression updateLambda)
				{
					updateExpr      = SequenceHelper.PrepareBody(updateLambda, sequence);
					forceParameters = false;
				}

				UpdateBuilder.ParseSet(contextRef, extractExp, updateExpr, insertContext.SetExpressions, forceParameters);
				insertContext.LastBuildInfo = buildInfo;

				return BuildSequenceResult.FromContext(insertContext);
			}
		}

		#endregion
	}
}
