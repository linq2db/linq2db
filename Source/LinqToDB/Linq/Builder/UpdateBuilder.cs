using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Common;
	using LinqToDB.Expressions;
	using static LinqToDB.Linq.Builder.TableBuilder;

	sealed class UpdateBuilder : MethodCallBuilder
	{
		static readonly string[] _methods =
		{
			nameof(LinqExtensions.Update),
			nameof(LinqExtensions.UpdateWithOutput),
			nameof(LinqExtensions.UpdateWithOutputInto)
		};

		#region Update

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(_methods);
		}

		static void ExtractSequence(BuildInfo buildInfo, ref IBuildContext sequence, out UpdateContext updateContext)
		{
			updateContext = sequence as UpdateContext;
			if (updateContext != null)
			{
				sequence = updateContext.Sequence;
			}
			else
			{
				updateContext = new UpdateContext(buildInfo.Parent, sequence, UpdateTypeEnum.Update, new SqlUpdateStatement(sequence.SelectQuery));
			}

			updateContext.LastBuildInfo = buildInfo;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

			Type? objectType;

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

					objectType       = genericArguments[0];
					outputExpression = RewriteOutputExpression(outputExpression);

					break;
				}

				case OutputMethod.QueryableSetter:
				{
					// int Update<T>(this IQueryable<T> source, Expression<Func<T,T>> setter)
					// int Update<T>(this IQueryable<T> source, Expression<Func<T,bool>> predicate, Expression<Func<T,T>> setter)
					//

					if (updateContext.TargetTable == null)
					{
						var tableContext = SequenceHelper.GetTableContext(sequence);
						if (tableContext == null)
							throw new LinqToDBException("Cannot find target table for UPDATE statement");

						updateContext.TargetTable = tableContext;
					}

					var setterExpr = methodCall.Arguments[1].Unwrap();
					if (setterExpr is LambdaExpression && methodCall.Arguments.Count == 3)
					{
						sequence = builder.BuildWhere(buildInfo.Parent, sequence, methodCall.Arguments[1].UnwrapLambda(), false, false, buildInfo.AggregationTest);
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

					ParseSetter(builder, targetRef, setterExpr, updateContext.SetExpressions);

					outputExpression = RewriteOutputExpression(outputExpression);

					break;
				}

				case OutputMethod.QueryableTarget:
				{
					// int Update<TSource,TTarget>(this IQueryable<TSource> source, ITable<TTarget> target, Expression<Func<TSource,TTarget>> setter)
					// int Update<TSource,TTarget>(this IQueryable<TSource> source, Expression<Func<TSource,TTarget>> target, Expression<Func<TSource,TTarget>> setter)
					//

					var expr = methodCall.Arguments[1].Unwrap();
					IBuildContext into;

					var setter     = methodCall.Arguments[2].UnwrapLambda();
					var setterExpr = SequenceHelper.PrepareBody(setter, sequence);

					if (expr is LambdaExpression lambda)
					{
						var body = SequenceHelper.PrepareBody(lambda, sequence);

						var tableContext = SequenceHelper.GetTableContext(builder, body);

						if (tableContext == null)
						{
							throw new LinqException("Cannot retrieve Table for update.");
						}

						into = tableContext;

						updateContext.TargetTable = tableContext;
					}
					else
					{
						into = builder.BuildSequence(new BuildInfo(buildInfo, expr, new SelectQuery()));
						var sequenceTableContext = SequenceHelper.GetTableContext(sequence);
						var intoTableContext = SequenceHelper.GetTableContext(into);

						if (intoTableContext == null)
						{
							throw new LinqException("Cannot retrieve Table for update.");
						}

						if (sequenceTableContext != null)
						{
							// copy information form target table
							//
							sequenceTableContext.SqlTable.TableName    = intoTableContext.SqlTable.TableName;
							sequenceTableContext.SqlTable.TableOptions = intoTableContext.SqlTable.TableOptions;

							updateContext.TargetTable = sequenceTableContext;							
						}
						else
						{
							updateContext.TargetTable = intoTableContext;
						}
					}

					objectType = genericArguments[1];

					var targetRef = new ContextRefExpression(objectType, updateContext.TargetTable);

					ParseSetter(builder, targetRef, setterExpr, updateContext.SetExpressions);

					break;
				}

				default:
					throw new InvalidOperationException("Unknown Output Method");
			}

			if (updateContext.SetExpressions.Count == 0)
				throw new LinqToDBException("Update query has no setters defined.");

			if (updateType == UpdateTypeEnum.Update)
				return updateContext;

			var insertedTable = builder.DataContext.SqlProviderFlags.OutputUpdateUseSpecialTables ? SqlTable.Inserted(objectType) : updateStatement.GetUpdateTable();
			var deletedTable  = SqlTable.Deleted(objectType);

			if (insertedTable == null)
				throw new InvalidOperationException("Cannot find target table for UPDATE statement");

			updateStatement.Output = new SqlOutputClause();

			if (builder.DataContext.SqlProviderFlags.OutputUpdateUseSpecialTables)
			{
				updateStatement.Output.InsertedTable = insertedTable;
				updateStatement.Output.DeletedTable  = deletedTable;
			}

			if (updateType == UpdateTypeEnum.UpdateOutput)
			{
				static LambdaExpression BuildDefaultOutputExpression(Type outputType)
				{
					var param1 = Expression.Parameter(outputType, "source");
					var param2 = Expression.Parameter(outputType, "deleted");
					var param3 = Expression.Parameter(outputType, "inserted");
					var returnType = typeof(UpdateOutput<>).MakeGenericType(outputType);

					return Expression.Lambda(
						// (source, deleted, inserted) => new UpdateOutput<T> { Deleted = deleted, Inserted = inserted, }
						Expression.MemberInit(
							Expression.New(returnType),
							Expression.Bind(returnType.GetProperty(nameof(UpdateOutput<object>.Deleted))!, param2),
							Expression.Bind(returnType.GetProperty(nameof(UpdateOutput<object>.Inserted))!, param3)),
						param1, param2, param3);
				}

				outputExpression ??= BuildDefaultOutputExpression(objectType);

				throw new NotImplementedException();
				/*var outputContext = new UpdateOutputContext(
					buildInfo.Parent,
					outputExpression,
					sequence,
					new TableBuilder.TableContext(builder, new SelectQuery(), deletedTable),
					new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable));

				return outputContext;*/
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

				outputExpression ??= BuildDefaultOutputExpression(objectType);

				throw new NotImplementedException();
				BuildSetterWithContext(
					builder,
					buildInfo,
					outputExpression,
					destination,
					updateStatement.Output.OutputItems,
					sequence,
					new TableBuilder.TableContext(builder, new SelectQuery(), deletedTable),
					new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable));

				updateStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;

				return updateContext;
			}
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

		internal static void BuildSetter(
			ExpressionBuilder builder,
			BuildInfo buildInfo,
			LambdaExpression setter,
			IBuildContext into,
			List<SqlSetExpression> items,
			IBuildContext sequence)
		{
			BuildSetterWithContext(builder, buildInfo, setter, into, items, sequence);
		}

		internal static void BuildSetterWithContext(
			ExpressionBuilder      builder,
			BuildInfo              buildInfo,
			LambdaExpression       setter,
			IBuildContext          into,
			List<SqlSetExpression> items,
			params IBuildContext[] sequences)
		{
			var setterBody     = SequenceHelper.PrepareBody(setter, sequences);
			var sourceSequence = sequences[0];
			var setterExpr     = builder.ConvertToSqlExpr(sourceSequence, setterBody);
			if (setterExpr is not SqlGenericConstructorExpression)
			{
				// try again in Keys mode
				setterExpr = builder.ConvertToSqlExpr(into, setterBody, ProjectFlags.SQL | ProjectFlags.Keys);
			}

			void BuildSetter(MemberExpression memberExpression, Expression expression)
			{
				var column = builder.ConvertToSql(into, memberExpression);
				var expr   = builder.ConvertToSqlExpr(sourceSequence, expression, ProjectFlags.SQL, columnDescriptor: QueryHelper.GetColumnDescriptor(column));
				var withColumns = expr;

				// if there are joins we have to make columns
				if (sourceSequence != into || QueryHelper.EnumerateAccessibleSources(sourceSequence.SelectQuery).Skip(1).Any())
				{
					withColumns = builder.ToColumns(sourceSequence, expr);
				}

				if (withColumns is not SqlPlaceholderExpression placeholder)
					throw SqlErrorExpression.CreateError(withColumns);
				
				items.Insert(placeholder.Index ?? items.Count, new SqlSetExpression(column, placeholder.Sql));
			}

			void BuildGeneric(SqlGenericConstructorExpression generic, Expression path)
			{
				foreach (var assignment in generic.Assignments)
				{
					var member   = assignment.MemberInfo;
					var argument = assignment.Expression;

					if (member is MethodInfo mi)
						member = mi.GetPropertyInfo();

					var pe = Expression.MakeMemberAccess(path, member);

					if (argument is SqlGenericConstructorExpression genericArgument)
					{
						BuildGeneric(genericArgument, Expression.MakeMemberAccess(path, member));
					}
					else
					{
						BuildSetter(pe, argument);
					}
				}
			}

			var bodyPath = new ContextRefExpression(setterExpr.Type, into);
			var bodyExpr = setterExpr;

			if (bodyExpr is SqlGenericConstructorExpression generic)
			{
				BuildGeneric(generic, bodyPath);
			}
			else
				throw new LinqException($"Setter expression '{setterExpr}' cannot be used for build SQL.");
		}

		internal static void InitializeSetExpressions(
			ExpressionBuilder           builder,
			BuildInfo                   buildInfo, 
			IBuildContext               fieldsContext,
			IBuildContext               valuesContext,
			List<SetExpressionEnvelope> envelopes,
			List<SqlSetExpression>      items,
			bool createColumns)
		{
			ISqlExpression GetFieldExpression(Expression fieldExpr, bool isPureExpression)
			{
				var sql = builder.ConvertToSql(fieldsContext, fieldExpr, isPureExpression: isPureExpression);
				return sql;
			}

			SqlSetExpression  setExpression;
			ColumnDescriptor? columnDescriptor = null;

			foreach (var envelope in envelopes)
			{
				var fieldExpression = builder.ConvertExpression(envelope.FieldExpression);
				var valueExpression = envelope.ValueExpression != null ? builder.ConvertExpression(envelope.ValueExpression) : null;

				if (fieldExpression.IsSqlRow())
				{
					var row = fieldExpression.GetSqlRowValues()
						.Select(e => GetFieldExpression(e, false))
						.ToArray();

					var rowExpression = new SqlRow(row);

					setExpression = new SqlSetExpression(rowExpression, null);
				}
				else
				{
					var column = GetFieldExpression(fieldExpression, valueExpression == null);
					columnDescriptor = QueryHelper.GetColumnDescriptor(column);
					setExpression    = new SqlSetExpression(column, null);
				}

				if (valueExpression != null)
				{
					var sqlExpr = builder.ConvertToSqlExpr(valuesContext, valueExpression,
						columnDescriptor: columnDescriptor, unwrap: false);

					if (sqlExpr is not SqlPlaceholderExpression placeholder)
						throw SqlErrorExpression.CreateError(valueExpression);

					var sql = createColumns
						? valuesContext.SelectQuery.Select.AddNewColumn(placeholder.Sql)
						: placeholder.Sql;

					setExpression.Expression = sql;
				}

				items.Add(setExpression);
			}
		}

		static void ParseSet(
			Expression                  targetPath,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes)
		{
			var correctedField = SqlGenericConstructorExpression.Parse(fieldExpression);

			if (correctedField is SqlGenericConstructorExpression fieldGeneric)
			{
				var correctedValue = SqlGenericConstructorExpression.Parse(valueExpression);

				if (correctedValue is not SqlGenericConstructorExpression valueGeneric)
					throw SqlErrorExpression.CreateError(valueExpression);

				var pairs =
					from f in fieldGeneric.Assignments
					join v in valueGeneric.Assignments on f.MemberInfo equals v.MemberInfo
					select (f, v);

				foreach (var (f, v) in pairs)
				{
					var currentPath = Expression.MakeMemberAccess(targetPath, f.MemberInfo);
					ParseSet(currentPath, f.Expression, v.Expression, envelopes);
				}
			}
			else
			{
				var correctedValue = SqlGenericConstructorExpression.Parse(valueExpression);

				if (correctedValue is SqlGenericConstructorExpression valueGeneric)
				{
					foreach (var assignment in valueGeneric.Assignments)
					{
						var currentPath = Expression.MakeMemberAccess(targetPath, assignment.MemberInfo);
						ParseSet(currentPath, currentPath, assignment.Expression, envelopes);
					}
				}
				else
					envelopes.Add(new SetExpressionEnvelope(correctedField, valueExpression));
			}
		}

		internal static void ParseSet(
			ContextRefExpression        targetRef,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes)
		{
			ParseSet(targetRef, fieldExpression, valueExpression, envelopes);
		}

		internal static void ParseSetter(
			ExpressionBuilder           builder,
			ContextRefExpression        targetRef,
			Expression                  setterExpression,
			List<SetExpressionEnvelope> envelopes)
		{
			var correctedSetter = SqlGenericConstructorExpression.Parse(setterExpression);

			if (correctedSetter is not SqlGenericConstructorExpression)
			{
				correctedSetter = builder.ConvertToSqlExpr(targetRef.BuildContext, correctedSetter);
			}

			if (correctedSetter is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					ParseSet(Expression.MakeMemberAccess(targetRef, assignment.MemberInfo), Expression.MakeMemberAccess(targetRef, assignment.MemberInfo), assignment.Expression, envelopes);
				}
			}
			else
			{
				if (correctedSetter is SqlPlaceholderExpression { Sql: SqlValue { Value: null } })
					return;

				throw new NotImplementedException();
			}
		}

		[DebuggerDisplay("{FieldExpression} = {ValueExpression}")]
		public sealed class SetExpressionEnvelope
		{
			public SetExpressionEnvelope(Expression fieldExpression, Expression? valueExpression)
			{
				FieldExpression = fieldExpression;
				ValueExpression = valueExpression;
			}

			public Expression  FieldExpression { get; }
			public Expression? ValueExpression { get; }
		}


		#endregion

		#region UpdateContext

		public sealed class UpdateContext : SequenceContextBase
		{
			TableBuilder.TableContext? _targetTable;

			public UpdateContext(IBuildContext? parent, IBuildContext sequence, UpdateTypeEnum updateType, SqlUpdateStatement updateStatement) : base(parent, sequence, null)
			{
				UpdateStatement = updateStatement;
				UpdateType      = updateType;
			}

			public SqlUpdateStatement         UpdateStatement { get; }
			public UpdateTypeEnum             UpdateType      { get; set; }

			public TableBuilder.TableContext? TargetTable
			{
				get => _targetTable;
				set
				{
					_targetTable = value;

					UpdateStatement.Update.Table = _targetTable?.SqlTable;
				}
			}

			public IBuildContext               QuerySequence  { get => Sequences[0]; set => Sequences[0] = value; }
			public BuildInfo?                  LastBuildInfo  { get;                 set; }
			public List<SetExpressionEnvelope> SetExpressions { get; } = new ();

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public void FinalizeSetters()
			{
				var update = UpdateStatement.Update;

				if (update.Items.Count > 0 || LastBuildInfo == null)
					return;

				if (TargetTable == null)
				{
					throw new LinqToDBException("Insert query has no defined target table.");
				}

				var tableContext = TargetTable;
			
				update.Table                = tableContext?.SqlTable;
				UpdateStatement.SelectQuery = QuerySequence.SelectQuery;

				SetExpressions.RemoveDuplicatesFromTail((s1, s2) =>
					ExpressionEqualityComparer.Instance.Equals(s1.FieldExpression, s2.FieldExpression));

				InitializeSetExpressions(Builder, LastBuildInfo, TargetTable, Sequence, SetExpressions, update.Items, true);

				/*var q = update.Keys.IdentityFields
					.Except(update.Items.Select(e => e.Column).OfType<SqlField>());

				foreach (var field in q)
				{
					var expr = Builder.DataContext.CreateSqlProvider().GetIdentityExpression(update.Into);

					if (expr != null)
					{
						update.Items.Insert(0, new SqlSetExpression(field, expr));

						/*if (methodCall.Arguments.Count == 3)
						{
							sequence.SelectQuery.Select.Columns.Insert(0, new SqlColumn(sequence.SelectQuery, insert.Items[0].Expression!));
						}#2#
					}
				}*/
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
						var mapper = Builder.BuildMapper<T>(expr);
						QueryRunner.SetRunQuery(query, mapper);
						break;
					}					
					case UpdateTypeEnum.UpdateOutputInto:
					{
						QueryRunner.SetNonQueryQuery(query);
						break;
					}	
					default:
						throw new InvalidOperationException($"Unexpected insert type: {UpdateType}");
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					FinalizeSetters();
					/*
					if (InsertType == InsertTypeEnum.InsertOutput)
					{
						if (OutputExpression == null || OutputContext == null || LastBuildInfo == null)
							throw new InvalidOperationException();

						var selectContext = new SelectContext(Parent, OutputExpression, false, OutputContext);
						var outputRef     = new ContextRefExpression(path.Type, selectContext);

						var outputExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();

						var sqlExpr = Builder.ConvertToSqlExpr(selectContext, outputRef);
						if (sqlExpr is SqlPlaceholderExpression)
							outputExpressions.Add(new UpdateBuilder.SetExpressionEnvelope(sqlExpr, sqlExpr));
						else
							UpdateBuilder.ParseSetter(Builder, outputRef, sqlExpr, outputExpressions);

						var setItems = new List<SqlSetExpression>();
						UpdateBuilder.InitializeSetExpressions(Builder, LastBuildInfo, selectContext, outputExpressions, setItems, false);

						InsertStatement.Output!.OutputColumns = setItems.Select(c => c.Expression!).ToList();

						return sqlExpr;

					}*/

					return Expression.Default(path.Type);
				}

				return base.MakeExpression(path, flags);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return null;
			}

			public override SqlStatement GetResultStatement()
			{
				return UpdateStatement;
			}
		}

		#endregion

		#region Set

		internal sealed class Set : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable(nameof(LinqExtensions.Set));
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				ExtractSequence(buildInfo, ref sequence, out var updateContext);

				var extract  = methodCall.Arguments[1].UnwrapLambda();
				var update   = methodCall.Arguments.Count > 2 ? methodCall.Arguments[2].Unwrap() : null;

				var extractExpr = SequenceHelper.PrepareBody(extract, sequence);
				if (updateContext.TargetTable == null)
				{
					var tableContext = SequenceHelper.GetTableContext(builder, extractExpr);
					updateContext.TargetTable = tableContext;
				}

				if (update == null)
				{
					if (updateContext.TargetTable == null)
					{
						var tableContext = SequenceHelper.GetTableContext(sequence);
						updateContext.TargetTable = tableContext;
					}

					// we have first lambda as whole update field part

					updateContext.SetExpressions.Add(new SetExpressionEnvelope(extractExpr, null));
				}
				else
				{
					var updateExpr = update;

					if (updateExpr is ConstantExpression constExpr)
					{
						builder.ParametersContext.MarkAsParameter(constExpr);
					}
					else if (updateExpr is LambdaExpression lambda)
					{
						updateExpr = SequenceHelper.PrepareBody(lambda, sequence);
					}

					ParseSet(extractExpr, extractExpr, updateExpr, updateContext.SetExpressions);
				}

				return updateContext;
			}
		}

		#endregion
	}
}
