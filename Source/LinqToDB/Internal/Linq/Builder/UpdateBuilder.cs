using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(
		nameof(LinqExtensions.Update),
		nameof(LinqExtensions.UpdateWithOutput),
		nameof(LinqExtensions.UpdateWithOutputInto))]
	sealed class UpdateBuilder : MethodCallBuilder
	{
		#region Update

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable();

		static void ExtractSequence(BuildInfo buildInfo, ref IBuildContext sequence, out UpdateContext updateContext)
		{
			if (sequence is UpdateContext current)
			{
				sequence      = current.QuerySequence;
				updateContext = current;
			}
			else
			{
				updateContext = new UpdateContext(sequence, UpdateTypeEnum.Update, new SqlUpdateStatement(sequence.SelectQuery), false);
			}

			updateContext.LastBuildInfo = buildInfo;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var updateType = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.UpdateWithOutput)     => UpdateTypeEnum.UpdateOutput,
				nameof(LinqExtensions.UpdateWithOutputInto) => UpdateTypeEnum.UpdateOutputInto,
				_                                           => UpdateTypeEnum.Update,
			};

			var sequence = builder.BuildSequence(new (buildInfo, methodCall.Arguments[0]));

			ExtractSequence(buildInfo, ref sequence, out var updateContext);
			updateContext.UpdateType = updateType;

			var updateStatement  = updateContext.UpdateStatement;
			var genericArguments = methodCall.Method.GetGenericArguments();
			var outputExpression = (LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap();

			updateContext.CreateColumns = updateStatement.SelectQuery.Find(e => e is SqlFromClause from && (from.Tables.Count > 1 || from.Tables[0].Joins.Count > 0)) != null;

			Type ? objectType;

			static LambdaExpression? RewriteOutputExpression(LambdaExpression? expr)
			{
				if (expr == default) return default;

				var outputType = expr.Parameters[0].Type;
				var param1     = Expression.Parameter(outputType, "source");

				return Expression.Lambda(
					// (source, deleted, inserted) => expr(deleted, inserted)
					expr.Body,
					param1, expr.Parameters[0], expr.Parameters[1]);
			}

			switch (GetOutputMethod(methodCall))
			{
				case OutputMethod.IUpdatable:
				{
					// int Update<T>(this IUpdateable<T> source)

					objectType                = genericArguments[0];
					outputExpression          = RewriteOutputExpression(outputExpression);

					if (updateContext.TargetTable == null)
					{
						var tableContext = SequenceHelper.GetTableOrCteContext(sequence);
						if (tableContext == null)
							throw new LinqToDBException("Cannot find target table for UPDATE statement");

						updateContext.TargetTable = tableContext;
					}

					break;
				}

				case OutputMethod.QueryableSetter:
				{
					// int Update<T>(this IQueryable<T> source, Expression<Func<T,T>> setter)
					// int Update<T>(this IQueryable<T> source, Expression<Func<T,bool>> predicate, Expression<Func<T,T>> setter)
					//

					if (updateContext.TargetTable == null)
					{
						var tableContext = SequenceHelper.GetTableOrCteContext(sequence);
						if (tableContext == null)
							throw new LinqToDBException("Cannot find target table for UPDATE statement");

						updateContext.TargetTable = tableContext;
					}

					var setterExpr = methodCall.Arguments[1].Unwrap();
					if (setterExpr is LambdaExpression && methodCall.Arguments.Count == 3 && updateType == UpdateTypeEnum.Update)
					{
						sequence = builder.BuildWhere(
							sequence,
							condition: methodCall.Arguments[1].UnwrapLambda(),
							enforceHaving: false,
							out var error
						);

						if (sequence == null)
							return BuildSequenceResult.Error(error ?? methodCall);

						setterExpr = methodCall.Arguments[2].Unwrap();
					}

					if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
						sequence = new SubQueryContext(sequence);

					if (setterExpr is LambdaExpression lambda)
					{
						setterExpr = SequenceHelper.PrepareBody(lambda, sequence);
					}

					updateContext.QuerySequence = sequence;
					updateStatement.SelectQuery = sequence.SelectQuery;
					objectType                  = genericArguments[0];

					var targetRef = new ContextRefExpression(objectType, updateContext.TargetTable);
					var sourceRef = SequenceHelper.CreateRef(sequence);

					ParseSetter(builder, targetRef, sourceRef, setterExpr, updateContext.SetExpressions);

					outputExpression = RewriteOutputExpression(outputExpression);

					break;
				}

				case OutputMethod.QueryableTarget:
				{
					// int Update<TSource,TTarget>(this IQueryable<TSource> source, ITable<TTarget> target, Expression<Func<TSource,TTarget>> setter)
					// int Update<TSource,TTarget>(this IQueryable<TSource> source, Expression<Func<TSource,TTarget>> target, Expression<Func<TSource,TTarget>> setter)
					//

					objectType = genericArguments[1];
					var expr = methodCall.Arguments[1].Unwrap();
					IBuildContext into;

					var setter     = methodCall.Arguments[2].UnwrapLambda();
					var setterExpr = SequenceHelper.PrepareBody(setter, sequence);

					if (expr is LambdaExpression lambda)
					{
						var body = SequenceHelper.PrepareBody(lambda, sequence);

						var tableContext = SequenceHelper.GetTableOrCteContext(builder, body);

						if (tableContext == null)
						{
							throw new LinqToDBException("Cannot retrieve Table for update.");
						}

						updateContext.TablePath = body;
						updateContext.TargetTable   = tableContext;
					}
					else
					{
						into = builder.BuildSequence(new BuildInfo(buildInfo, expr, new SelectQuery()));
						var sequenceTableContext = SequenceHelper.GetTableOrCteContext(sequence);
						var intoTableContext     = SequenceHelper.GetTableOrCteContext(into);

						if (intoTableContext == null)
						{
							throw new LinqToDBException("Cannot retrieve Table for update.");
						}

						if (intoTableContext.SqlTable.SqlQueryExtensions?.Count > 0)
							throw new LinqToDBException("Could not update table which has Query extensions.");

						if (sequenceTableContext == null)
						{
							// trying to detect join table in projection
							//

							var sequenceRefExpression = new ContextRefExpression(sequence.ElementType, sequence);
							var projection            = builder.BuildExtractExpression(sequence, sequenceRefExpression);

							var collectedTables = CollectTables(builder, projection, sequenceRefExpression, intoTableContext.ObjectType);

							if (collectedTables.Count == 0)
								throw new LinqToDBException("Could not find join table for update query.");

							if (collectedTables.Count > 1)
								throw new LinqToDBException("Could not find join table for update query. Ambiguous tables.");

							var kvp = collectedTables.First();

							updateContext.TablePath = kvp.Value;
							sequenceTableContext    = kvp.Key;
						}

						if (QueryHelper.IsEqualTables(sequenceTableContext.SqlTable, intoTableContext.SqlTable, false))
						{
							intoTableContext = sequenceTableContext;
						}
						else
						{
							// create join between tables
							//

							var sequenceRef = new ContextRefExpression(sequenceTableContext.SqlTable.ObjectType, sequenceTableContext);
							var intoRef     = new ContextRefExpression(sequenceTableContext.SqlTable.ObjectType, into);

							var compareSearchCondition = builder.GenerateComparison(sequenceTableContext, sequenceRef, intoRef, BuildPurpose.Sql);
							sequenceTableContext.SelectQuery.Where.ConcatSearchCondition(compareSearchCondition);
							updateStatement.Update.HasComparison = true;
						}

						updateContext.TargetTable = intoTableContext;
					}

					var targetRef = new ContextRefExpression(objectType, updateContext.TargetTable);
					var sourceRef = SequenceHelper.CreateRef(sequence);

					ParseSetter(builder, targetRef, sourceRef, setterExpr, updateContext.SetExpressions);

					break;
				}

				default:
					throw new InvalidOperationException("Unknown Output Method");
			}

			if (updateContext.SetExpressions.Count == 0)
				throw new LinqToDBException("Update query has no setters defined.");

			if (updateType == UpdateTypeEnum.Update)
				return BuildSequenceResult.FromContext(updateContext);

			if (updateContext.TargetTable == null)
				throw new InvalidOperationException();

			var (deletedContext, insertedContext) = CreateDeletedInsertedContexts(builder, updateContext.TargetTable, out _);

			updateStatement.Output = new SqlOutputClause();

			if (updateType == UpdateTypeEnum.UpdateOutput)
			{
				updateContext.OutputExpression = outputExpression;

				return BuildSequenceResult.FromContext(updateContext);
			}
			else // updateType == UpdateType.UpdateOutputInto
			{
				static LambdaExpression BuildDefaultOutputExpression(Type outputType)
				{
					var param1 = Expression.Parameter(outputType, "source");
					var param2 = Expression.Parameter(outputType, "deleted");
					var param3 = Expression.Parameter(outputType, "inserted");

					return Expression.Lambda(
						// (source, deleted, inserted) => inserted
						param3,
						param1, param2, param3);
				}

				var outputTable = methodCall.GetArgumentByName("outputTable")!;
				var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

				var destinationContext = SequenceHelper.GetTableContext(destination);
				if (destinationContext == null)
					throw new InvalidOperationException();

				var destinationRef = new ContextRefExpression(destinationContext.ObjectType, destinationContext);

				outputExpression ??= BuildDefaultOutputExpression(objectType);

				var outputBody = SequenceHelper.PrepareBody(outputExpression, sequence, deletedContext, insertedContext);

				var sourceRef         = SequenceHelper.CreateRef(sequence);
				var outputExpressions = new List<SetExpressionEnvelope>();
				ParseSetter(builder, destinationRef, sourceRef, outputBody, outputExpressions);

				InitializeSetExpressions(builder, destinationContext, sequence, outputExpressions, updateStatement.Output.OutputItems, false);

				updateStatement.Output.OutputTable = destinationContext.SqlTable;

				return BuildSequenceResult.FromContext(updateContext);
			}
		}

		Dictionary<ITableContext, Expression> CollectTables(ExpressionBuilder builder, Expression expr, Expression rooExpr, Type objectType)
		{
			var result = new Dictionary<ITableContext, Expression>();

			Expression Combine(Expression current, List<MemberInfo> path)
			{
				return path.Aggregate(current, Expression.MakeMemberAccess);
			}

			void Collect(Expression current, List<MemberInfo> path)
			{
				if (current is ContextRefExpression or MemberExpression)
				{
					var tableCtx = SequenceHelper.GetTableOrCteContext(builder, current);
					if (tableCtx != null && tableCtx.ObjectType == objectType && !result.ContainsKey(tableCtx))
					{
						result.Add(tableCtx, Combine(rooExpr, path));
					}
				}
				else if (current is SqlGenericConstructorExpression constructor)
				{
					foreach (var assignment in constructor.Assignments)
					{
						path.Add(assignment.MemberInfo);
						Collect(assignment.Expression, path);
						path.RemoveAt(path.Count - 1);
					}
				}
				else
				{
					var parsed = builder.ParseGenericConstructor(current, ProjectFlags.SQL, null);
					if (!ReferenceEquals(parsed, current))
					{
						Collect(parsed, path);
					}
				}
			}

			Collect(expr, new List<MemberInfo>());

			return result;
		}

		public static (IBuildContext deleted, IBuildContext inserted) CreateDeletedInsertedContexts(ExpressionBuilder builder, ITableContext targetTableContext, out IBuildContext outputContext)
		{
			// create separate query for output
			var outputSelectQuery = new SelectQuery();

			IBuildContext deletedContext;
			IBuildContext insertedContext;
			if (targetTableContext is CteTableContext cteTable)
			{
				insertedContext = new CteTableContext(builder.GetTranslationModifier(), builder, null,
					targetTableContext.SqlTable.ObjectType, outputSelectQuery, cteTable.CteContext);
				deletedContext = new CteTableContext(builder.GetTranslationModifier(), builder, null,
					targetTableContext.SqlTable.ObjectType, outputSelectQuery, cteTable.CteContext);
			}
			else
			{
				insertedContext = new TableBuilder.TableContext(builder.GetTranslationModifier(), builder, targetTableContext.MappingSchema, outputSelectQuery, targetTableContext.SqlTable, false);
				deletedContext  = new TableBuilder.TableContext(builder.GetTranslationModifier(), builder, targetTableContext.MappingSchema, outputSelectQuery, targetTableContext.SqlTable, false);
			}

			outputContext = deletedContext;

			outputSelectQuery.From.Tables.Clear();

			insertedContext = new AnchorContext(null, insertedContext, SqlAnchor.AnchorKindEnum.Inserted);
			deletedContext  = new AnchorContext(null, deletedContext, SqlAnchor.AnchorKindEnum.Deleted);

			return (deletedContext, insertedContext);
		}

		public enum UpdateTypeEnum
		{
			Update,
			UpdateOutput,
			UpdateOutputInto,
		}

		enum OutputMethod
		{
			IUpdatable,
			QueryableSetter,
			QueryableTarget,
		}

		static OutputMethod GetOutputMethod(MethodCallExpression methodCall)
		{
			if (typeof(IUpdatable<>).IsSameOrParentOf(methodCall.Arguments[0].Type))
				return OutputMethod.IUpdatable;

			var parameters = methodCall.Method.GetParameters()!;
			return parameters[1].Name switch
			{
				"predicate" => OutputMethod.QueryableSetter,
				"setter"    => OutputMethod.QueryableSetter,
				_           => OutputMethod.QueryableTarget,
			};
		}

		#endregion

		#region Helpers

		internal static void InitializeSetExpressions(
			ExpressionBuilder           builder,
			IBuildContext               fieldsContext,
			IBuildContext               valuesContext,
			List<SetExpressionEnvelope> envelopes,
			List<SqlSetExpression>      items,
			bool                        createColumns
			)
		{
			ISqlExpression GetFieldExpression(Expression fieldExpr, bool isPureExpression)
			{
				var sql = builder.ConvertToSql(fieldsContext, fieldExpr, isPureExpression : isPureExpression);
				return sql;
			}

			SqlSetExpression  setExpression;
			ColumnDescriptor? columnDescriptor = null;

			foreach (var envelope in envelopes)
			{
				var fieldExpression = envelope.FieldExpression;
				var valueExpression = envelope.ValueExpression;

				var column = GetFieldExpression(fieldExpression, valueExpression == null);
				columnDescriptor = QueryHelper.GetColumnDescriptor(column);
				setExpression    = new SqlSetExpression(column, null);

				if (valueExpression != null)
				{
					if (valueExpression.Unwrap() is LambdaExpression lambda)
					{
						valueExpression = lambda.Body;
					}
					else if (fieldExpression.Type != valueExpression.Type)
					{
						valueExpression = Expression.Convert(valueExpression, fieldExpression.Type);
					}

					using var savedDescriptor = builder.UsingColumnDescriptor(columnDescriptor);
					var sqlExpr = builder.BuildSqlExpression(valuesContext, valueExpression, BuildPurpose.Sql, envelope.ForceParameter ? BuildFlags.ForceParameter : BuildFlags.None);

					if (sqlExpr is not SqlPlaceholderExpression placeholder)
					{
						if (sqlExpr is SqlErrorExpression errorExpr)
							throw errorExpr.CreateException();

						throw SqlErrorExpression.CreateException(sqlExpr, null);
					}

					var sql = createColumns
						? valuesContext.SelectQuery.Select.AddNewColumn(placeholder.Sql)
						: placeholder.Sql;

					setExpression.Expression = sql;
				}

				items.Add(setExpression);
			}
		}

		static void ParseSet(
			ExpressionBuilder           builder,
			IBuildContext               buildContext,
			Expression                  targetPath,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes,
			bool                        forceParameters)
		{
			if (fieldExpression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				fieldExpression = ((UnaryExpression)fieldExpression).Operand;
				valueExpression = Expression.Convert(valueExpression, fieldExpression.Type);
			}

			var correctedField = builder.BuildSqlExpression(buildContext, fieldExpression);

			if (correctedField is SqlGenericConstructorExpression fieldGeneric)
			{
				var correctedValue = builder.BuildSqlExpression(buildContext, valueExpression);

				if (correctedValue is not SqlGenericConstructorExpression valueGeneric)
					throw SqlErrorExpression.CreateException(valueExpression, null);

				var pairs =
					from f in fieldGeneric.Assignments
					join v in valueGeneric.Assignments on f.MemberInfo equals v.MemberInfo
					select (f, v);

				foreach (var (f, v) in pairs)
				{
					var currentPath = Expression.MakeMemberAccess(targetPath, f.MemberInfo);
					ParseSet(builder, buildContext, currentPath, f.Expression, v.Expression, envelopes, false);
				}
			}
			else
			{
				var hasConversion = false;
				var targetColumn  = builder.BuildSqlExpression(buildContext, fieldExpression);

				ColumnDescriptor? columnDescriptor = null;
				if (targetColumn is SqlPlaceholderExpression placeholder)
				{
					columnDescriptor = QueryHelper.GetColumnDescriptor(placeholder.Sql);

					hasConversion = columnDescriptor?.ValueConverter != null;
				}

				using var saveDescriptor = builder.UsingColumnDescriptor(columnDescriptor);

				if (hasConversion)
				{
					envelopes.Add(new SetExpressionEnvelope(correctedField.UnwrapConvert(), valueExpression, true));
				}
				else
				{
					var correctedValue = builder.BuildSqlExpression(buildContext, valueExpression);

					if (correctedValue is SqlGenericConstructorExpression valueGeneric)
					{
						foreach (var assignment in valueGeneric.Assignments)
						{
							var currentPath = Expression.MakeMemberAccess(targetPath, assignment.MemberInfo);
							ParseSet(builder, buildContext, currentPath, currentPath, assignment.Expression, envelopes, false);
						}
					}
					else
						envelopes.Add(new SetExpressionEnvelope(correctedField.UnwrapConvert(), valueExpression, forceParameters));
				}
			}
		}

		internal static void ParseSet(
			ContextRefExpression        targetRef,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes,
			bool                        forceParameters)
		{
			ParseSet(targetRef.BuildContext.Builder, targetRef.BuildContext, targetRef, fieldExpression, valueExpression, envelopes, forceParameters);
		}

		internal static void ParseSetter(
			ExpressionBuilder           builder,
			ContextRefExpression        targetRef,
			ContextRefExpression        sourceRef,
			Expression                  setterExpression,
			List<SetExpressionEnvelope> envelopes)
		{
			var correctedSetter = builder.ParseGenericConstructor(setterExpression, ProjectFlags.SQL, null);

			if (correctedSetter is not SqlGenericConstructorExpression)
			{
				correctedSetter = builder.BuildSqlExpression(sourceRef.BuildContext, correctedSetter);
			}

			if (correctedSetter is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					var memberAccess = Expression.MakeMemberAccess(targetRef, assignment.MemberInfo);

					ParseSet(builder, sourceRef.BuildContext, memberAccess, memberAccess, assignment.Expression, envelopes, false);
				}
			}
			else
			{
				if (correctedSetter is SqlPlaceholderExpression { Sql: SqlValue { Value: null } })
					return;

				if (correctedSetter is ConstantExpression { Value: null })
					return;

				throw new NotImplementedException();
			}
		}

		[DebuggerDisplay("{FieldExpression} = {ValueExpression}")]
		public sealed class SetExpressionEnvelope
		{
			public SetExpressionEnvelope(Expression fieldExpression, Expression? valueExpression, bool forceParameter)
			{
				FieldExpression = fieldExpression;
				ValueExpression = valueExpression;
				ForceParameter  = forceParameter;
			}

			public Expression  FieldExpression { get; }
			public Expression? ValueExpression { get; }
			public bool        ForceParameter  { get; }
		}

		#endregion

		#region UpdateContext

		public sealed class UpdateContext : PassThroughContext
		{
			public UpdateContext(IBuildContext querySequence, UpdateTypeEnum updateType, SqlUpdateStatement updateStatement, bool createColumns)
				: base(querySequence, querySequence.SelectQuery)
			{
				UpdateStatement = updateStatement;
				UpdateType      = updateType;
				CreateColumns   = createColumns;
			}

			public SqlUpdateStatement         UpdateStatement { get; }
			public UpdateTypeEnum             UpdateType      { get; set; }
			public bool                       CreateColumns   { get; set; }

			public Expression? TablePath { get; set; }

			public ITableContext? TargetTable
			{
				get;
				set
				{
					field = value;

					UpdateStatement.Update.Table = value?.SqlTable;
				}
			}

			public IBuildContext QuerySequence
			{
				get => Context;
				set
				{
					Context     = value;
					SelectQuery = Context.SelectQuery;
				}
			}

			public BuildInfo?                  LastBuildInfo  { get; set; }
			public List<SetExpressionEnvelope> SetExpressions { get; } = new ();

			public LambdaExpression? OutputExpression { get; set; }

			public SelectQuery? OutputQuery { get; set; }

			public void FinalizeSetters()
			{
				var update = UpdateStatement.Update;

				if (update.Items.Count > 0 || LastBuildInfo == null)
					return;

				if (TargetTable == null)
				{
					throw new LinqToDBException("Update query has no defined target table.");
				}

				var tableContext = TargetTable;

				update.Table                = tableContext?.SqlTable;
				UpdateStatement.SelectQuery = QuerySequence.SelectQuery;

				SetExpressions.RemoveDuplicatesFromTail((s1, s2) =>
					ExpressionEqualityComparer.Instance.Equals(s1.FieldExpression, s2.FieldExpression));

				InitializeSetExpressions(Builder, TargetTable, QuerySequence, SetExpressions, update.Items, CreateColumns);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				switch (UpdateType)
				{
					case UpdateTypeEnum.Update:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}
					case UpdateTypeEnum.UpdateOutput:
					{
						var mapper = Builder.BuildMapper<T>(SelectQuery, expr);
						QueryRunner.SetRunQuery(query, mapper);
						break;
					}
					case UpdateTypeEnum.UpdateOutputInto:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}
					default:
						throw new InvalidOperationException($"Unexpected update type: {UpdateType}");
				}
			}

			static IEnumerable<(Expression path, SqlGenericConstructorExpression generic)> FindForRightProjectionPath(SqlGenericConstructorExpression generic, Expression currentPath, Type objecType)
			{
				if (generic.Type == objecType)
					yield return (currentPath, generic);

				// search for nesting
				foreach (var assignment in generic.Assignments)
				{
					if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
					{
						var newPath = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);
						foreach (var sub in FindForRightProjectionPath(subGeneric, newPath, objecType))
							yield return sub;
					}
				}
			}

			static Expression BuildDefaultOutputExpression(Type outputType, IBuildContext insertedContext, IBuildContext deletedContext)
			{
				var returnType   = typeof(UpdateOutput<>).MakeGenericType(outputType);

				var insertedExpr = new ContextRefExpression(outputType, insertedContext);
				var deletedExpr  = new ContextRefExpression(outputType, deletedContext);

				return
					// new UpdateOutput<T> { Deleted = deleted, Inserted = inserted, }
					Expression.MemberInit(
						Expression.New(returnType),
						Expression.Bind(returnType.GetProperty(nameof(UpdateOutput<object>.Deleted))!, deletedExpr),
						Expression.Bind(returnType.GetProperty(nameof(UpdateOutput<object>.Inserted))!, insertedExpr));
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					FinalizeSetters();

					if (UpdateType == UpdateTypeEnum.UpdateOutput)
					{
						if (TargetTable == null)
							throw new InvalidOperationException();

						UpdateStatement.Output ??= new SqlOutputClause();

						OutputQuery ??= new SelectQuery();
						var outputSelectQuery = OutputQuery;

						var insertedDeletedRoot = QuerySequence;
						if (TablePath != null)
						{
							insertedDeletedRoot = new SelectContext(Parent, TablePath, QuerySequence, QuerySequence.SelectQuery, false);
						}

						var insertedContext = new AnchorContext(Parent, insertedDeletedRoot, SqlAnchor.AnchorKindEnum.Inserted);
						var deletedContext  = new AnchorContext(Parent, insertedDeletedRoot, SqlAnchor.AnchorKindEnum.Deleted);

						var outputBody = OutputExpression == null
							? BuildDefaultOutputExpression(TargetTable.ObjectType, insertedContext, deletedContext)
							: SequenceHelper.PrepareBody(OutputExpression, QuerySequence, deletedContext, insertedContext);

						var selectContext     = new SelectContext(Parent, outputBody, insertedContext, false);
						var outputRef         = new ContextRefExpression(path.Type, selectContext);
						var outputExpressions = new List<SetExpressionEnvelope>();

						var sqlExpr = Builder.BuildSqlExpression(selectContext, outputRef);
						sqlExpr = SequenceHelper.CorrectSelectQuery(sqlExpr, outputSelectQuery);

						if (sqlExpr is SqlPlaceholderExpression)
							outputExpressions.Add(new SetExpressionEnvelope(sqlExpr, sqlExpr, false));
						else
							ParseSetter(Builder, outputRef, outputRef, sqlExpr, outputExpressions);

						var setItems = new List<SqlSetExpression>();
						InitializeSetExpressions(Builder, selectContext, selectContext, outputExpressions, setItems, false);

						UpdateStatement.Output!.OutputColumns = setItems.Select(c => c.Column).ToList();

						return sqlExpr;
					}

					return Expression.Default(path.Type);
				}

				return base.MakeExpression(path, flags);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return UpdateStatement;
			}
		}

		#endregion

		#region Set

		[BuildsMethodCall(nameof(LinqExtensions.Set))]
		internal sealed class Set : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call)
				=> call.IsQueryable();

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder,
				MethodCallExpression                                                 methodCall, BuildInfo buildInfo)
			{
				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				ExtractSequence(buildInfo, ref sequence, out var updateContext);

				var extract  = methodCall.Arguments[1].UnwrapLambda();
				var update   = methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null;

				var extractExpr = SequenceHelper.PrepareBody(extract, sequence);
				if (updateContext.TargetTable == null)
				{
					var tableContext = SequenceHelper.GetTableOrCteContext(builder, extractExpr);
					updateContext.TargetTable = tableContext;
				}

				if (update == null)
				{
					if (updateContext.TargetTable == null)
					{
						var tableContext = SequenceHelper.GetTableOrCteContext(sequence);
						updateContext.TargetTable = tableContext;
					}

					// we have first lambda as whole update field part

					updateContext.SetExpressions.Add(new SetExpressionEnvelope(extractExpr, null, false));
				}
				else
				{
					var updateExpr      = update;
					var forceParameters = true;

					if (updateExpr.Unwrap() is LambdaExpression lambda)
					{
						forceParameters = false;
						updateExpr      = SequenceHelper.PrepareBody(lambda, sequence);
					}

					ParseSet(builder, sequence, extractExpr, extractExpr, updateExpr, updateContext.SetExpressions, forceParameters);
				}

				return BuildSequenceResult.FromContext(updateContext);
			}
		}

		#endregion
	}
}
