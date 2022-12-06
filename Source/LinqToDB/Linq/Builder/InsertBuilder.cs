using System;
using System.Linq.Expressions;
using System.Reflection;

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

		static void ExtractSequence(BuildInfo buildInfo, ref IBuildContext sequence, out InsertContext insertContext)
		{
			insertContext   = sequence as InsertContext;
			if (insertContext != null)
			{
				sequence = insertContext.Sequence;
			}
			else
			{
				insertContext = new InsertContext(buildInfo.Parent, sequence, InsertContext.InsertTypeEnum.Insert,
					new SqlInsertStatement(sequence.SelectQuery), null);
			}
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			ExtractSequence(buildInfo, ref sequence, out var insertContext);

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
				var argument = methodCall.Arguments[0];
				if (typeof(IValueInsertable<>).IsSameOrParentOf(argument.Type) ||
				    typeof(ISelectInsertable<,>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>              (this IValueInsertable<T> source)
					// static int Insert<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)

					//sequence.SelectQuery.Select.Columns.Clear();

					if (insertContext.SetExpressions.Count == 0)
					{
						//throw new NotImplementedException();
					}

					insertContext.Into ??= sequence;

					//AddInsertColumns(sequence.SelectQuery, insertStatement.Insert.Items);
				}
				else if (methodCall.Arguments.Count > 1                  &&
					typeof(IQueryable<>).IsSameOrParentOf(argument.Type) &&
					typeof(ITable<>).IsSameOrParentOf(methodCall.Arguments[1].Type))
				{
					// static int Insert<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)

					var into = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
					insertContext.Into = into;

					var setter     = methodCall.GetArgumentByName("setter")!.UnwrapLambda();
					var setterExpr = SequenceHelper.PrepareBody(setter, sequence);

					var targetType = methodCall.Method.GetGenericArguments()[1];
					var contextRef = new ContextRefExpression(targetType, into);

					UpdateBuilder.ParseSetter(builder, contextRef, setterExpr, insertContext.SetExpressions);
				}
				else if (typeof(ITable<>).IsSameOrParentOf(argument.Type))
				{
					// static int Insert<T>(this Table<T> target, Expression<Func<T>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter)
					// static TTarget InsertWithOutput<TTarget>(this ITable<TTarget> target, Expression<Func<TTarget>> setter, Expression<Func<TTarget,TOutput>> outputExpression)
					var argIndex = 1;
					var arg = methodCall.Arguments[argIndex].Unwrap();
					LambdaExpression? setter = null;
					switch (arg)
					{
						case LambdaExpression lambda:
						{
							Expression setterExpr;
							ContextRefExpression contextRef;
							insertContext.Into = sequence;
							if (lambda.Parameters.Count == 0)
							{
								var type = lambda.Body.Type;
								setterExpr = lambda.Body;
								contextRef = new ContextRefExpression(type, insertContext.Into);
							}	
							else
								throw new NotImplementedException();

							UpdateBuilder.ParseSetter(builder,
								contextRef, 
								setterExpr, 
								insertContext.SetExpressions);

							break;
						}
						default:
							{
								var objType = arg.Type;

								var ed   = builder.MappingSchema.GetEntityDescriptor(objType);
								var into = sequence;
								var ctx  = new TableBuilder.TableContext(builder, buildInfo, objType);

								var table = new SqlTable(objType);

								foreach (var c in ed.Columns.Where(c => !c.SkipOnInsert))
								{
									var field     = table[c.MemberName] ?? throw new InvalidOperationException($"Cannot find column {c.MemberName}({c.ColumnName})");
									var pe        = Expression.MakeMemberAccess(arg, c.MemberInfo);
									var column    = into.ConvertToSql(pe, 1, ConvertFlags.Field);
									var parameter = builder.ParametersContext.BuildParameterFromArgumentProperty(methodCall, argIndex, field.ColumnDescriptor);

									insertStatement.Insert.Items.Add(new SqlSetExpression(column[0].Sql, parameter.SqlParameter));
								}

								break;
							}
					}

					//sequence.SelectQuery.From.Tables.Clear();
				}

				if (insertType == InsertContext.InsertTypeEnum.InsertOutput || insertType == InsertContext.InsertTypeEnum.InsertOutputInto)
				{
					outputExpression =
						methodCall.GetArgumentByName("outputExpression")?.UnwrapLambda()
						?? BuildDefaultOutputExpression(methodCall.Method.GetGenericArguments().Last());

					insertStatement.Output = new SqlOutputClause();

					var insertedTable = builder.DataContext.SqlProviderFlags.OutputInsertUseSpecialTable ? SqlTable.Inserted(outputExpression.Parameters[0].Type) : insertStatement.Insert.Into;

					if (insertedTable == null)
						throw new InvalidOperationException("Cannot find target table for INSERT statement");

					outputContext = new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable);

					if (builder.DataContext.SqlProviderFlags.OutputInsertUseSpecialTable)
						insertStatement.Output.InsertedTable = insertedTable;

					if (insertType == InsertContext.InsertTypeEnum.InsertOutputInto)
					{
						var outputTable = methodCall.GetArgumentByName("outputTable")!;
						var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

						UpdateBuilder.BuildSetter(
							builder,
							buildInfo,
							outputExpression,
							destination,
							insertStatement.Output.OutputItems,
							outputContext);

						insertStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
					}
				}
			}

			if (insertContext.SetExpressions.Count == 0)
				throw new LinqToDBException("Insert query has no setters defined.");

			insertContext.LastBuildInfo = buildInfo;
			insertContext.FinalizeSetters();

			insertStatement.Insert.WithIdentity = insertType == InsertContext.InsertTypeEnum.InsertWithIdentity;

			return insertContext;
		}

		#endregion

		#region InsertContext

		sealed class InsertContext : SequenceContextBase
		{
			public SqlInsertStatement InsertStatement { get; }

			public enum InsertTypeEnum
			{
				Insert,
				InsertWithIdentity,
				InsertOutput,
				InsertOutputInto
			}

			public InsertContext(IBuildContext? parent, IBuildContext sequence, InsertTypeEnum insertType, SqlInsertStatement insertStatement, LambdaExpression? outputExpression)
				: base(parent, sequence, outputExpression)
			{
				InsertType        = insertType;
				_outputExpression = outputExpression;
				InsertStatement   = insertStatement;
			}

			public InsertTypeEnum InsertType { get; set; }

			readonly LambdaExpression? _outputExpression;

			public List<UpdateBuilder.SetExpressionEnvelope> SetExpressions { get; } = new ();

			public IBuildContext? Into          { get; set; }
			public BuildInfo?     LastBuildInfo { get; set; }

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					FinalizeSetters();
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
					throw new LinqToDBException("Insert query has no setters defined.");
				}

				var tableContext = SequenceHelper.GetTableContext(Into);
			
				insert.Into = tableContext?.SqlTable;

				if (insert.Into == null)
					throw new LinqToDBException("Insert query has no setters defined.");

				SetExpressions.RemoveDuplicatesFromTail((s1, s2) =>
					ExpressionEqualityComparer.Instance.Equals(s1.FieldExpression, s2.FieldExpression));

				UpdateBuilder.InitializeSetExpressions(Builder, LastBuildInfo, Sequence, insert.Into, SetExpressions, insert.Items, true);

				var q = insert.Into.IdentityFields
					.Except(insert.Items.Select(e => e.Column).OfType<SqlField>());

				foreach (var field in q)
				{
					var expr = Builder.DataContext.CreateSqlProvider().GetIdentityExpression(insert.Into);

					if (expr != null)
					{
						insert.Items.Insert(0, new SqlSetExpression(field, expr));

						/*if (methodCall.Arguments.Count == 3)
						{
							sequence.SelectQuery.Select.Columns.Insert(0, new SqlColumn(sequence.SelectQuery, insert.Items[0].Expression!));
						}*/
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
						var mapper = Builder.BuildMapper<T>(expr);

						var insertStatement = (SqlInsertStatement)Statement!;
						var outputQuery     = Sequence.SelectQuery;

						insertStatement.Output!.OutputColumns =
							outputQuery.Select.Columns.Select(c => c.Expression).ToList();

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
				return new InsertContext(null, context.CloneContext(Sequence), InsertType, context.CloneElement(InsertStatement), context.CloneExpression(_outputExpression));
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

		#endregion

		#region Into

		internal sealed class Into : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsQueryable("Into");
			}

			sealed class MatchInfo
			{
				public MatchInfo(SqlPlaceholderExpression placeholder, MemberInfo[] memberPath)
				{
					Placeholder = placeholder;
					MemberPath  = memberPath;
				}

				public SqlPlaceholderExpression Placeholder { get; }
				public MemberInfo[]             MemberPath  { get; }
			}

			static IEnumerable<(MatchInfo left, MatchInfo right)>
				MatchMembers(MemberInfo[] currentPath, SqlGenericConstructorExpression left, SqlGenericConstructorExpression right)
			{
				var matchedQuery =
					from leftAssignment in left.Assignments
					join rightAssignment in right.Assignments on leftAssignment.MemberInfo equals rightAssignment.MemberInfo
					select (leftAssignment, rightAssignment);

				foreach (var (la, ra) in matchedQuery)
				{
					var newPath  = currentPath.ArrayAppend(la.MemberInfo);

					if (la.Expression is SqlPlaceholderExpression leftPlaceholder && ra.Expression is SqlPlaceholderExpression rightPlaceholder)
					{
						yield return (new MatchInfo(leftPlaceholder, newPath), new MatchInfo(rightPlaceholder, newPath));
					}
					else if (la.Expression is SqlGenericConstructorExpression leftGeneric &&
					         ra.Expression is SqlGenericConstructorExpression rightGeneric)
					{
						foreach (var r in MatchMembers(newPath, leftGeneric, rightGeneric))
							yield return r;
					}
				}
			}

			static IEnumerable<(MatchInfo left, MatchInfo right)> 
				MatchSequences(ExpressionBuilder builder, ContextRefExpression source, ContextRefExpression destination)
			{
				var sourceExpr = builder.ConvertToSqlExpr(source.BuildContext, source, ProjectFlags.SQL);
				var destExpr   = builder.ConvertToSqlExpr(destination.BuildContext, destination, ProjectFlags.SQL);

				if (destExpr is not SqlGenericConstructorExpression destGeneric)
					throw new LinqToDBException("Could not convert destination to tale expression.");

				if (sourceExpr is not SqlGenericConstructorExpression sourceGeneric)
				{
					sourceGeneric = destGeneric;
				}

				var matched = MatchMembers(Array<MemberInfo>.Empty, sourceGeneric, destGeneric);

				return matched;
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
				insertContext = new InsertContext(buildInfo.Parent, sequence, InsertContext.InsertTypeEnum.Insert, insertStatement, null);
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

				ExtractSequence(buildInfo, ref sequence, out var insertContext);

				insertContext.Into ??= sequence;

				var tableType  = methodCall.Method.GetGenericArguments()[1];
				var contextRef = new ContextRefExpression(tableType, insertContext.Into);

				var extractExp = SequenceHelper.PrepareBody(extract, insertContext.Into);
				var updateExpr = update;
				if (updateExpr is LambdaExpression updateLambda)
					updateExpr = SequenceHelper.PrepareBody(updateLambda, sequence);

				UpdateBuilder.ParseSet(builder, contextRef, extractExp, updateExpr, insertContext.SetExpressions);
				insertContext.LastBuildInfo = buildInfo;

				return insertContext;
			}
		}

		#endregion
	}
}
