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

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var updateType = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.UpdateWithOutput)     => UpdateType.UpdateOutput,
				nameof(LinqExtensions.UpdateWithOutputInto) => UpdateType.UpdateOutputInto,
				_                                           => UpdateType.Update,
			};

			var sequence         = builder.BuildSequence(new (buildInfo, methodCall.Arguments[0]));
			var updateStatement  = sequence.Statement as SqlUpdateStatement ?? new SqlUpdateStatement(sequence.SelectQuery);
			var genericArguments = methodCall.Method.GetGenericArguments();
			var outputExpression = (LambdaExpression?)methodCall.GetArgumentByName("outputExpression")?.Unwrap();

			Type? objectType;

			sequence.Statement = updateStatement;

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

					var expr = methodCall.Arguments[1].Unwrap();
					if (expr is LambdaExpression lex && lex.ReturnType == typeof(bool))
					{
						sequence = builder.BuildWhere(buildInfo.Parent, sequence, (LambdaExpression)methodCall.Arguments[1].Unwrap(), false, false, buildInfo.AggregationTest);
						expr     = methodCall.Arguments[2].Unwrap();
					}

					if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
						sequence = new SubQueryContext(sequence);

					updateStatement.SelectQuery = sequence.SelectQuery;
					sequence.Statement = updateStatement;

					BuildSetter(
						builder,
						buildInfo,
						(LambdaExpression)expr,
						sequence,
						updateStatement.Update.Items,
						sequence);

					objectType       = genericArguments[0];
					outputExpression = RewriteOutputExpression(outputExpression);

					break;
				}

				case OutputMethod.QueryableTarget:
				{
					// int Update<TSource,TTarget>(this IQueryable<TSource> source, ITable<TTarget> target, Expression<Func<TSource,TTarget>> setter)
					// int Update<TSource,TTarget>(this IQueryable<TSource> source, Expression<Func<TSource,TTarget>> target, Expression<Func<TSource,TTarget>> setter)

					var expr = methodCall.Arguments[1].Unwrap();
					IBuildContext into;

					if (expr is LambdaExpression expression)
					{
						var body  = expression.Body;
						var level = body.GetLevel(builder.MappingSchema);

						var tableInfo = sequence.IsExpression(body, level, RequestFor.Table);

						if (tableInfo.Result == false)
							throw new LinqException("Expression '{0}' must be a table.", body);

						into = tableInfo.Context!;
					}
					else
					{
						into = builder.BuildSequence(new BuildInfo(buildInfo, expr, new SelectQuery()));
					}

					sequence.ConvertToIndex(null, 0, ConvertFlags.All);
					new SelectQueryOptimizer(builder.DataContext.SqlProviderFlags, new EvaluationContext(), updateStatement, updateStatement.SelectQuery, 0)
						.ResolveWeakJoins();
					updateStatement.SelectQuery.Select.Columns.Clear();

					BuildSetter(
						builder,
						buildInfo,
						(LambdaExpression)methodCall.Arguments[2].Unwrap(),
						into,
						updateStatement.Update.Items,
						sequence);

					updateStatement.SelectQuery.Select.Columns.Clear();

					foreach (var item in updateStatement.Update.Items)
						updateStatement.SelectQuery.Select.Columns.Add(new SqlColumn(updateStatement.SelectQuery, item.Expression!));

					updateStatement.Update.Table = ((TableBuilder.TableContext)into!).SqlTable;

					objectType       = genericArguments[1];

					break;
				}

				default:
					throw new InvalidOperationException("Unknown Output Method");
			}

			if (updateStatement.Update.Items.Count == 0)
				throw new LinqToDBException("Update query has no setters defined.");

			if (updateType == UpdateType.Update)
				return new UpdateContext(buildInfo.Parent, sequence);

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

			if (updateType == UpdateType.UpdateOutput)
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

				var outputContext = new UpdateOutputContext(
					buildInfo.Parent,
					outputExpression,
					sequence,
					new TableBuilder.TableContext(builder, new SelectQuery(), deletedTable),
					new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable));

				return outputContext;
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
				return new UpdateContext(buildInfo.Parent, sequence);
			}
		}

		enum UpdateType
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
			IBuildContext               context,
			SqlTable?                   table,
			List<SetExpressionEnvelope> envelopes,
			List<SqlSetExpression>      items)
		{
			ISqlExpression GetField(Expression fieldExpr)
			{
				var sql   = builder.ConvertToSql(context, fieldExpr);
				var field = QueryHelper.GetUnderlyingField(sql);
				
				if (field == null)
					throw new LinqException($"Expression '{SqlErrorExpression.PrepareExpression(fieldExpr)}' can not be used as Update Field.");

				return table == null ? field : table[field.Name]!;
			}

			SqlSetExpression  setExpression;
			ColumnDescriptor? columnDescriptor = null;

			foreach (var envelope in envelopes)
			{
				var fieldExpression = builder.ConvertExpression(envelope.FieldExpression);
				var valueExpression = builder.ConvertExpression(envelope.ValueExpression);

				if (fieldExpression.IsSqlRow())
				{
					var row = fieldExpression.GetSqlRowValues()
						.Select(GetField)
						.ToArray();

					var rowExpression = new SqlRow(row);

					setExpression = new SqlSetExpression(rowExpression, null);
				}
				else
				{
					var column = GetField(fieldExpression);
					columnDescriptor = QueryHelper.GetColumnDescriptor(column);
					setExpression    = new SqlSetExpression(column, null);
				}

				var sqlExpr = builder.ConvertToSqlExpr(context, valueExpression,
					columnDescriptor: columnDescriptor, unwrap: false);

				if (sqlExpr is not SqlPlaceholderExpression placeholder)
					throw SqlErrorExpression.CreateError(valueExpression);

				var sql = context.SelectQuery.Select.AddNewColumn(placeholder.Sql);
				setExpression.Expression = sql;

				items.Add(setExpression);
			}
		}

		static void ParseSet(
			Expression                  targetPath,
			ExpressionBuilder           builder,
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
					ParseSet(currentPath, builder, f.Expression, v.Expression, envelopes);
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
						ParseSet(currentPath, builder, currentPath, assignment.Expression, envelopes);
					}
				}
				else
					envelopes.Add(new SetExpressionEnvelope(correctedField, valueExpression));
			}
		}

		internal static void ParseSet(
			ExpressionBuilder           builder,
			ContextRefExpression        targetRef,
			Expression                  fieldExpression,
			Expression                  valueExpression,
			List<SetExpressionEnvelope> envelopes)
		{
			ParseSet(targetRef, builder, fieldExpression, valueExpression, envelopes);
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
					ParseSet(Expression.MakeMemberAccess(targetRef, assignment.MemberInfo), builder, Expression.MakeMemberAccess(targetRef, assignment.MemberInfo), assignment.Expression, envelopes);
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		internal static void ParseSet(
			ExpressionBuilder      builder,
			BuildInfo              buildInfo,
			LambdaExpression       extract,
			LambdaExpression       update,
			IBuildContext          fieldsContext,
			IBuildContext          valuesContext,
			SqlTable?              table,
			List<SqlSetExpression> items)
		{
			extract = (LambdaExpression)builder.ConvertExpression(extract);
			var ext        = SequenceHelper.PrepareBody(extract, fieldsContext);
			var updateBody = SequenceHelper.PrepareBody(update, valuesContext);

			ColumnDescriptor? columnDescriptor = null;
			SqlSetExpression 		  setExpression;

			if (ext.IsSqlRow())
			{
				var row = ext.GetSqlRowValues()
					.Select(GetField)
					.ToArray();

				var rowExpression = new SqlRow(row);

				setExpression = new SqlSetExpression(rowExpression, null);
			}
			else
			{
				var column = GetField(ext);
				columnDescriptor = QueryHelper.GetColumnDescriptor(column);
				setExpression    = new SqlSetExpression(column, null);
			}

			var sqlExpr = builder.ConvertToSqlExpr(valuesContext, updateBody, columnDescriptor: columnDescriptor, unwrap: false);

			var withColumns = sqlExpr;

			if (valuesContext != fieldsContext)
			{
				withColumns = builder.ToColumns(valuesContext, withColumns);
			}

			if (withColumns is not SqlPlaceholderExpression placeholder)
				throw SqlErrorExpression.CreateError(withColumns);

			setExpression.Expression = placeholder.Sql;

			items.Insert(placeholder.Index ?? items.Count, setExpression);

			ISqlExpression GetField(Expression fieldExpr)
			{
				var sql   = builder.ConvertToSql(fieldsContext, fieldExpr);
				var field = QueryHelper.GetUnderlyingField(sql);
				
				if (field == null)
					throw new LinqException($"Expression '{SqlErrorExpression.PrepareExpression(extract)}' can not be used as Update Field.");

				return table == null ? field : table[field.Name]!;
			}
		}

		[DebuggerDisplay("{FieldExpression} = {ValueExpression}")]
		public sealed class SetExpressionEnvelope
		{
			public SetExpressionEnvelope(Expression fieldExpression, Expression valueExpression)
			{
				FieldExpression = fieldExpression;
				ValueExpression = valueExpression;
			}

			public Expression FieldExpression { get; }
			public Expression ValueExpression { get; }
		}


		#endregion

		#region UpdateContext

		sealed class UpdateContext : SequenceContextBase
		{
			public UpdateContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

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

			public override IBuildContext Clone(CloningContext context)
			{
				return new UpdateContext(null, context.CloneContext(Sequence));
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}

		sealed class UpdateOutputContext : SelectContext
		{
			public UpdateOutputContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext source, IBuildContext deletedTable, IBuildContext insertedTable)
				: base(parent, lambda, false, source, deletedTable, insertedTable)
			{
				Statement = source.Statement;

				Sequence[0].SelectQuery.Select.Columns.Clear();
				Sequence[1].SelectQuery = Sequence[0].SelectQuery;
				Sequence[2].SelectQuery = Sequence[0].SelectQuery;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var updateStatement = (SqlUpdateStatement)Statement!;

				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				if (updateStatement.SelectQuery.From.Tables.Count > 0
					&& updateStatement.SelectQuery.From.Tables[0].Source is SelectQuery sourceQuery
					&& sourceQuery.Select.Columns.Count > 0)
				{
					// TODO: better fix?
					// for "UPDATE qry FROM qry(T)" we must check that output doesn't include missing field from qry
					// e.g. see Issue3044UpdateOutputWithTake2 test and TableWithData.Value field
					var setColumns = new HashSet<string>();

					foreach (var col in sourceQuery.Select.Columns)
						setColumns.Add(col.Alias!);

					var columns = new List<ISqlExpression>(Sequence[0].SelectQuery.Select.Columns.Count);

					foreach (var c in Sequence[0].SelectQuery.Select.Columns)
					{
						if (c.Expression is SqlField f && !setColumns.Contains(f.PhysicalName))
							columns.Add(new SqlExpression(c.Expression.SystemType!, $"NULL /* {f.PhysicalName} */"));
						else
							columns.Add(c.Expression);
					}

					updateStatement.Output!.OutputColumns = columns;

					QueryRunner.SetRunQuery(query, mapper);
				}
				else
				{

					var columns = new List<ISqlExpression>(Sequence[0].SelectQuery.Select.Columns.Count);

					foreach (var c in Sequence[0].SelectQuery.Select.Columns)
						columns.Add(c.Expression);

					updateStatement.Output!.OutputColumns = columns;

					QueryRunner.SetRunQuery(query, mapper);
				}
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

				//if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
				//	sequence = new SubQueryContext(sequence);

				var extract  = methodCall.Arguments[1].UnwrapLambda();
				var update   = methodCall.Arguments.Count > 2 ? methodCall.Arguments[2].Unwrap() : null;

				var updateStatement = sequence.Statement as SqlUpdateStatement ?? new SqlUpdateStatement(sequence.SelectQuery);
				sequence.Statement  = updateStatement;

				if (update == null)
				{
					// we have first lambda as whole update field part
					var sp     = sequence.Parent;
					var ctx    = new ExpressionContext(buildInfo.Parent, sequence, extract);
					var expr   = builder.ConvertToSqlExpression(ctx, extract.Body, null, true);

					builder.ReplaceParent(ctx, sp);

					updateStatement.Update.Items.Add(new SqlSetExpression(expr, null));
				}
				else 
				{
					var updateLambda = update as LambdaExpression ?? Expression.Lambda(update);

					ParseSet(
						builder,
						buildInfo,
						extract,
						updateLambda,
						sequence,
						sequence,
						updateStatement.Update.Table,
						updateStatement.Update.Items);
				}

				// TODO: remove in v4?
				updateStatement.Update.Items.RemoveDuplicatesFromTail((s1, s2) => s1.Column.Equals(s2.Column));

				return sequence;
			}
		}

		#endregion
	}
}
