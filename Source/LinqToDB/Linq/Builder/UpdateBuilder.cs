using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;
	using Common;

	class UpdateBuilder : MethodCallBuilder
	{
		private static readonly string[] Methods = new []
		{
			nameof(LinqExtensions.Update),
			nameof(LinqExtensions.UpdateWithOutput),
			nameof(LinqExtensions.UpdateWithOutputInto)
		};

		#region Update

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(Methods);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var updateType = methodCall.Method.Name switch
			{
				nameof(LinqExtensions.UpdateWithOutput) => UpdateContext.UpdateType.UpdateOutput,
				nameof(LinqExtensions.UpdateWithOutputInto) => UpdateContext.UpdateType.UpdateOutputInto,
				_ => UpdateContext.UpdateType.Update,
			};

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var updateStatement = sequence.Statement as SqlUpdateStatement ?? new SqlUpdateStatement(sequence.SelectQuery);
			sequence.Statement  = updateStatement;

			switch (GetOutputMethod(methodCall))
			{
				case OutputMethod.IUpdatable:
					{
						// int Update<T>(this IUpdateable<T> source)
						CheckAssociation(sequence);
						break;
					}

				case OutputMethod.QueryableSetter:
					{
						// int Update<T>(this IQueryable<T> source, Expression<Func<T,T>> setter)
						// int Update<T>(this IQueryable<T> source, Expression<Func<T,bool>> predicate, Expression<Func<T,T>> setter)
						CheckAssociation(sequence);

						var expr = methodCall.Arguments[1].Unwrap();
						if (expr is LambdaExpression lex && lex.ReturnType == typeof(bool))
							sequence = builder.BuildWhere(buildInfo.Parent, sequence, (LambdaExpression)methodCall.Arguments[1].Unwrap(), false);

							if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
								sequence = new SubQueryContext(sequence);

							updateStatement.SelectQuery = sequence.SelectQuery;
							sequence.Statement = updateStatement;

							BuildSetter(
								builder,
								buildInfo,
								(LambdaExpression)methodCall.Arguments[2].Unwrap(),
								sequence,
								updateStatement.Update.Items,
								sequence);

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
							new SelectQueryOptimizer(builder.DataContext.SqlProviderFlags, updateStatement, updateStatement.SelectQuery, 0)
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

						break;
						}
			}

			var indexedParameters
				= methodCall.Method.GetParameters().Select((p, i) => Tuple.Create(p, i)).ToDictionary(t => t.Item1.Name, t => t.Item2);

			Expression GetArgumentByName(string name) =>
				methodCall.Arguments[indexedParameters[name]];

			LambdaExpression GetOutputExpression(Type outputType)
			{
				if (!indexedParameters.TryGetValue("outputExpression", out var index))
				{
					var param1 = Expression.Parameter(outputType, "source");
					var param2 = Expression.Parameter(outputType, "deleted");
					var param3 = Expression.Parameter(outputType, "inserted");
					var returnType = typeof(UpdateOutput<>).MakeGenericType(outputType);
					return Expression.Lambda(
						Expression.MemberInit(
							Expression.New(returnType),
							Expression.Bind(returnType.GetProperty("Deleted"), param2),
							Expression.Bind(returnType.GetProperty("Inserted"), param3)),
						param1, param2, param3);
					}

				return (LambdaExpression)methodCall.Arguments[index].Unwrap();
			}

			if (updateType != UpdateContext.UpdateType.Update)
			{
				var outputExpression = GetOutputExpression(methodCall.Method.GetGenericArguments().Last());

				var objectType    = methodCall.Method.GetGenericArguments()[1];
				var insertedTable = SqlTable.Inserted(objectType);
				var deletedTable  = SqlTable.Deleted(objectType);

				updateStatement.Output = new SqlOutputClause()
				{
					InsertedTable = insertedTable,
					DeletedTable  = deletedTable,
				};

				var outputContext = new UpdateOutputContext(
					buildInfo.Parent,
					outputExpression,
					sequence,
					new TableBuilder.TableContext(builder, new SelectQuery(), deletedTable),
					new TableBuilder.TableContext(builder, new SelectQuery(), insertedTable));

				if (updateType != UpdateContext.UpdateType.UpdateOutputInto)
					return outputContext;

				var outputTable = GetArgumentByName("outputTable");
				var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

				BuildSetter(
					builder,
					buildInfo,
					outputExpression,
					destination,
					updateStatement.Output.OutputItems,
					outputContext);

				updateStatement.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
			}

			return new UpdateContext(buildInfo.Parent, sequence);
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
				"setter" => OutputMethod.QueryableSetter,
				_ => OutputMethod.QueryableTarget,
			};
		}

		static void CheckAssociation(IBuildContext sequence)
		{
			if (sequence is SelectContext ctx && ctx.IsScalar)
			{
				var res = ctx.IsExpression(null, 0, RequestFor.Association);

				if (res.Result)
				{
					var atc = res.Context!.IsExpression(null, 0, RequestFor.Table);
					if (atc.Result && atc.Context is TableBuilder.TableContext tableContext)
					{
						ctx.Statement!.RequireUpdateClause().Table = tableContext.SqlTable;
					}
				}
				else
				{
					res = ctx.IsExpression(null, 0, RequestFor.Table);

					if (res.Result && res.Context is TableBuilder.TableContext tc)
					{
						if (ctx.Statement!.SelectQuery!.From.Tables.Count == 0 || ctx.Statement.SelectQuery.From.Tables[0].Source != tc.SelectQuery)
							ctx.Statement.RequireUpdateClause().Table = tc.SqlTable;
					}
				}
			}
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
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
			var ctx = new ExpressionContext(buildInfo.Parent, sequences, setter);

			void BuildSetter(MemberExpression memberExpression, Expression expression)
			{
				var column     = into.ConvertToSql(memberExpression, 1, ConvertFlags.Field);
				var columnExpr = column[0].Sql;
				var expr       = builder.ConvertToSqlExpression(ctx, expression, QueryHelper.GetColumnDescriptor(columnExpr), false);

				if (expr.ElementType == QueryElementType.SqlParameter)
				{
					var parm  = (SqlParameter)expr;
					var field = QueryHelper.GetUnderlyingField(columnExpr);

					if (parm.Type.DataType == DataType.Undefined)
						parm.Type = parm.Type.WithDataType(field!.Type!.Value.DataType);
				}

				items.Add(new SqlSetExpression(columnExpr, expr));
			}

			void BuildNew(NewExpression expression, Expression path)
			{
				for (var i = 0; i < expression.Members.Count; i++)
				{
					var member   = expression.Members[i];
					var argument = expression.Arguments[i];

					if (member is MethodInfo mi)
						member = mi.GetPropertyInfo();

					var pe = Expression.MakeMemberAccess(path, member);

					if (argument is NewExpression newExpr && newExpr.Type.IsAnonymous())
					{
						BuildNew(newExpr, Expression.MakeMemberAccess(path, member));
					}
					else if (argument is MemberInitExpression initExpr && !into.IsExpression(pe, 1, RequestFor.Field).Result)
					{
						BuildMemberInit(initExpr, Expression.MakeMemberAccess(path, member));
					}
					else
					{
						BuildSetter(pe, argument);
					}
				}
			}

			void BuildMemberInit(MemberInitExpression expression, Expression path)
			{
				foreach (var binding in expression.Bindings)
				{
					var member = binding.Member;

					if (member is MethodInfo mi)
						member = mi.GetPropertyInfo();

					if (binding is MemberAssignment ma)
					{
						var pe = Expression.MakeMemberAccess(path, member);

						if (ma.Expression is NewExpression newExpr && newExpr.Type.IsAnonymous())
						{
							BuildNew(newExpr, Expression.MakeMemberAccess(path, member));
						}
						else if (ma.Expression is MemberInitExpression initExpr && !into.IsExpression(pe, 1, RequestFor.Field).Result)
						{
							BuildMemberInit(initExpr, Expression.MakeMemberAccess(path, member));
						}
						else
						{
							BuildSetter(pe, ma.Expression);
						}
					}
					else
						throw new InvalidOperationException();
				}
			}

			var bodyPath = Expression.Parameter(setter.Body.Type, "p");
			var bodyExpr = setter.Body;

			if (bodyExpr.NodeType == ExpressionType.New && bodyExpr.Type.IsAnonymous())
			{
				var ex = (NewExpression)bodyExpr;
				var p  = sequences[0].Parent;

				BuildNew(ex, bodyPath);

				builder.ReplaceParent(ctx, p);
			}
			else if (bodyExpr.NodeType == ExpressionType.MemberInit)
			{
				var ex = (MemberInitExpression)bodyExpr;
				var p  = sequences[0].Parent;

				BuildMemberInit(ex, bodyPath);

				builder.ReplaceParent(ctx, p);
			}
			else
			{
				var sqlInfo = ctx.ConvertToSql(bodyExpr, 0, ConvertFlags.All);

				foreach (var info in sqlInfo)
				{
					if (info.MemberChain.Length == 0)
						throw new LinqException("Object initializer expected for insert statement.");

					if (info.MemberChain.Length != 1)
						throw new InvalidOperationException();

					var member = info.MemberChain[0];
					var pe     = Expression.MakeMemberAccess(bodyPath, member);
					var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
					var expr   = info.Sql;

					items.Add(new SqlSetExpression(column[0].Sql, expr));
				}
			}
		}

		internal static void ParseSet(
			ExpressionBuilder               builder,
			BuildInfo                       buildInfo,
			LambdaExpression                extract,
			LambdaExpression                update,
			IBuildContext                   fieldsContext,
			IBuildContext                   valuesContext,
			SqlTable?                       table,
			List<SqlSetExpression> items)
		{
			extract = (LambdaExpression)builder.ConvertExpression(extract);
			var ext = extract.Body.Unwrap();

			var sp    = fieldsContext.Parent;
			var ctx   = new ExpressionContext(buildInfo.Parent, fieldsContext, extract);
			var sql   = ctx.ConvertToSql(ext, 0, ConvertFlags.Field);
			var field = sql.Select(s => QueryHelper.GetUnderlyingField(s.Sql)).FirstOrDefault(f => f != null);
			builder.ReplaceParent(ctx, sp);

			if (sql.Length != 1)
				throw new LinqException($"Expression '{extract}' can not be used as Update Field.");

			var column = table != null && field != null ? table[field.Name]! : sql[0].Sql;

			sp       = valuesContext.Parent;
			ctx      = new ExpressionContext(buildInfo.Parent, valuesContext, update);
			var expr = builder.ConvertToSqlExpression(ctx, update.Body, QueryHelper.GetColumnDescriptor(column), false);

			builder.ReplaceParent(ctx, sp);

			items.Add(new SqlSetExpression(column, expr));
		}

		internal static void ParseSet(
			ExpressionBuilder               builder,
			LambdaExpression                extract,
			MethodCallExpression            updateMethod,
			int                             valueIndex,
			IBuildContext                   select,
			List<SqlSetExpression> items)
		{
			var ext        = extract.Body.Unwrap();
			var rootObject = builder.GetRootObject(ext);

			ISqlExpression columnSql;
			MemberInfo     member;
			if (ext.NodeType == ExpressionType.MemberAccess)
			{
				var body = (MemberExpression)ext;

				member = body.Member;

				if (!member.IsPropertyEx() && !member.IsFieldEx() || rootObject != extract.Parameters[0])
					throw new LinqException("Member expression expected for the 'Set' statement.");

				if (member is MethodInfo info)
					member = info.GetPropertyInfo();

				var columnExpr = body;
				var column     = select.ConvertToSql(columnExpr, 1, ConvertFlags.Field);

				if (column.Length == 0)
					throw new LinqException("Member '{0}.{1}' is not a table column.", member.DeclaringType?.Name, member.Name);
				columnSql = column[0].Sql;
			}
			else
			{
				member = MemberHelper.GetMemberInfo(ext);
				if (member == null)
					throw new LinqException("Member expression expected for the 'Set' statement.");

				var memberExpr = Expression.MakeMemberAccess(rootObject, member);
				var column     = select.ConvertToSql(memberExpr, 1, ConvertFlags.Field);
				if (column.Length == 0)
					throw new LinqException($"Expression '{ext}' is not a table column.");
				columnSql = column[0].Sql;
			}

			var columnDescriptor = QueryHelper.GetColumnDescriptor(columnSql);

			var p = builder.BuildParameter(updateMethod.Arguments[valueIndex], columnDescriptor, true);

			items.Add(new SqlSetExpression(columnSql, p.SqlParameter));
		}

		#endregion

		#region UpdateContext

		class UpdateContext : SequenceContextBase
		{
			public enum UpdateType
			{
				Update,
				UpdateOutput,
				UpdateOutputInto,
			}

			public UpdateContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
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

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}

		class UpdateOutputContext : SelectContext
		{
			public UpdateOutputContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext source, IBuildContext deletedTable, IBuildContext insertedTable)
				: base(parent, lambda, source, deletedTable, insertedTable)
			{
				Statement = source.Statement;
				Sequence[0].SelectQuery.Select.Columns.Clear();
				Sequence[1].SelectQuery = Sequence[0].SelectQuery;
				Sequence[2].SelectQuery = Sequence[0].SelectQuery;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				var updateStatement = (SqlUpdateStatement)Statement!;

				updateStatement.Output!.OutputQuery = Sequence[0].SelectQuery;

				QueryRunner.SetRunQuery(query, mapper);
			}
		}
		#endregion

		#region Set

		internal class Set : MethodCallBuilder
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

				var extract  = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var update   =  methodCall.Arguments.Count > 2 ? methodCall.Arguments[2].Unwrap() : null;

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
				else if (update.NodeType == ExpressionType.Lambda)
					ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						sequence,
						sequence,
						updateStatement.Update.Table,
						updateStatement.Update.Items);
				else
					ParseSet(
						builder,
						extract,
						methodCall,
						2,
						sequence,
						updateStatement.Update.Items);

				updateStatement.Update.Items.RemoveDuplicatesFromTail((s1, s2) => s1.Column.Equals(s2.Column));

				return sequence;
			}

			protected override SequenceConvertInfo? Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			{
				return null;
			}
		}

		#endregion
	}
}
