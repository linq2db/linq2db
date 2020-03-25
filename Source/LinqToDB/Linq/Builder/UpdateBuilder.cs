﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	class UpdateBuilder : MethodCallBuilder
	{
		#region Update

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(nameof(LinqExtensions.Update));
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var updateStatement = sequence.Statement as SqlUpdateStatement ?? new SqlUpdateStatement(sequence.SelectQuery);
			sequence.Statement  = updateStatement;

			switch (methodCall.Arguments.Count)
			{
				case 1: // int Update<T>(this IUpdateable<T> source)
					{
						CheckAssociation(sequence);
						break;
					}

				case 2 : // int Update<T>(this IQueryable<T> source, Expression<Func<T,T>> setter)
					{
						CheckAssociation(sequence);

						if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
						{
							sequence = new SubQueryContext(sequence);
							updateStatement.SelectQuery = sequence.SelectQuery;
							sequence.Statement = updateStatement;
						}

						BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[1].Unwrap(),
							sequence,
							updateStatement.Update.Items,
							sequence);
						break;
					}

				case 3 :
					{
						var expr = methodCall.Arguments[1].Unwrap();

						if (expr is LambdaExpression lex && lex.ReturnType == typeof(bool))
						{
							CheckAssociation(sequence);

							// int Update<T>(this IQueryable<T> source, Expression<Func<T,bool>> predicate, Expression<Func<T,T>> setter)
							//
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
						}
						else
						{
							IBuildContext into;

							if (expr is LambdaExpression expression)
							{
								// static int Update<TSource,TTarget>(this IQueryable<TSource> source, Expression<Func<TSource,TTarget>> target, Expression<Func<TSource,TTarget>> setter)
								//
								var body      = expression.Body;
								var level = body.GetLevel();


								var tableInfo = sequence.IsExpression(body, level, RequestFor.Table);

								if (tableInfo.Result == false)
									throw new LinqException("Expression '{0}' must be a table.", body);

								into = tableInfo.Context!;
							}
							else
							{
								// static int Update<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)
								//
								into = builder.BuildSequence(new BuildInfo(buildInfo, expr, new SelectQuery()));
							}

							sequence.ConvertToIndex(null, 0, ConvertFlags.All);
							new SelectQueryOptimizer(builder.DataContext.SqlProviderFlags, updateStatement, updateStatement.SelectQuery)
								.ResolveWeakJoins(new List<ISqlTableSource>());
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
						}

						break;
					}
			}

			return new UpdateContext(buildInfo.Parent, sequence);
		}

		static void CheckAssociation(IBuildContext sequence)
		{
			if (sequence is SelectContext ctx && ctx.IsScalar)
			{
				var res = ctx.IsExpression(null, 0, RequestFor.Association);

				if (res.Result && res.Context is TableBuilder.AssociatedTableContext)
				{
					var atc = (TableBuilder.AssociatedTableContext)res.Context;
					ctx.Statement!.RequireUpdateClause().Table = atc.SqlTable;
				}
				else
				{
					res = ctx.IsExpression(null, 0, RequestFor.Table);

					if (res.Result && res.Context is TableBuilder.TableContext)
					{
						var tc = (TableBuilder.TableContext)res.Context;

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
				var column = into.ConvertToSql(memberExpression, 1, ConvertFlags.Field);
				var expr   = builder.ConvertToSqlExpression(ctx, expression);

				if (expr.ElementType == QueryElementType.SqlParameter)
				{
					var parm  = (SqlParameter)expr;
					var field = QueryHelper.GetUnderlyingField(column[0].Sql);

					if (parm.Type.DataType == DataType.Undefined)
						parm.Type = parm.Type.WithDataType(field!.Type!.Value.DataType);
				}

				items.Add(new SqlSetExpression(column[0].Sql, expr));
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
					if (info.MemberChain.Count == 0)
						throw new LinqException("Object initializer expected for insert statement.");

					if (info.MemberChain.Count != 1)
						throw new InvalidOperationException();

					var member = info.MemberChain[0];
					var pe     = Expression.MakeMemberAccess(bodyPath, member);
					var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
					var expr   = info.Sql;

					items.Add(new SqlSetExpression(column[0].Sql, expr));
				}
			}
		}

		static void BuildSetter(
			ExpressionBuilder      builder,
			IBuildContext          into,
			List<SqlSetExpression> items,
			IBuildContext          ctx,
			MemberInitExpression   expression,
			Expression             path)
		{
			foreach (var binding in expression.Bindings)
			{
				var member = binding.Member;

				if (member is MethodInfo mi)
					member = mi.GetPropertyInfo();

				if (binding is MemberAssignment ma)
				{
					var pe = Expression.MakeMemberAccess(path, member);

					if (ma.Expression is MemberInitExpression initExpr && !into.IsExpression(pe, 1, RequestFor.Field).Result)
					{
						BuildSetter(
							builder,
							into,
							items,
							ctx,
							initExpr, Expression.MakeMemberAccess(path, member));
					}
					else
					{
						var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
						var expr   = builder.ConvertToSqlExpression(ctx, ma.Expression);

						if (expr.ElementType == QueryElementType.SqlParameter)
						{
							var parm  = (SqlParameter)expr;
							var field = column[0].Sql is SqlField sqlField
								? sqlField
								: (SqlField)((SqlColumn)column[0].Sql).Expression;

							if (parm.Type.DataType == DataType.Undefined)
								parm.Type = parm.Type.WithDataType(field.Type!.Value.DataType);
						}

						items.Add(new SqlSetExpression(column[0].Sql, expr));
					}
				}
				else
					throw new InvalidOperationException();
			}
		}

		internal static void ParseSet(
			ExpressionBuilder               builder,
			BuildInfo                       buildInfo,
			LambdaExpression                extract,
			LambdaExpression                update,
			IBuildContext                   select,
			SqlTable                        table,
			List<SqlSetExpression> items)
		{
			var member = MemberHelper.GetMemberInfo(extract.Body);

			var ext = extract.Body.Unwrap();

			var rootObject = ext.GetRootObject(builder.MappingSchema);

			if (!member.IsPropertyEx() && !member.IsFieldEx() || rootObject != extract.Parameters[0])
				throw new LinqException("Member expression expected for the 'Set' statement.");

			var body = ext is MemberExpression mex ? mex : Expression.MakeMemberAccess(rootObject, member);

			if (member is MethodInfo)
				member = ((MethodInfo)member).GetPropertyInfo();

			var members = body.GetMembers();
			var name    = members
				.Skip(1)
				.Select(ex =>
				{
					if (!(ex is MemberExpression me))
						return null;

					var m = me.Member;

					if (m is MethodInfo)
						m = ((MethodInfo)m).GetPropertyInfo();

					return m;
				})
				.Where(m => m != null && !m.IsNullableValueMember())
				.Select(m => m!.Name)
				.Aggregate((s1,s2) => s1 + "." + s2);

			if (table != null && !table.Fields.ContainsKey(name))
				throw new LinqException("Member '{0}.{1}' is not a table column.", member.DeclaringType?.Name, name);

			var column = table != null ?
				table.Fields[name] :
				select.ConvertToSql(
					body, 1, ConvertFlags.Field)[0].Sql;
					//Expression.MakeMemberAccess(Expression.Parameter(member.DeclaringType, "p"), member), 1, ConvertFlags.Field)[0].Sql;
			var sp     = select.Parent;
			var ctx    = new ExpressionContext(buildInfo.Parent, select, update);
			var expr   = builder.ConvertToSqlExpression(ctx, update.Body);

			builder.ReplaceParent(ctx, sp);

			items.Add(new SqlSetExpression(column, expr));
		}

		internal static void ParseSet(
			ExpressionBuilder               builder,
			BuildInfo                       buildInfo,
			LambdaExpression                extract,
			Expression                      update,
			IBuildContext                   select,
			List<SqlSetExpression> items)
		{
			var ext        = extract.Body.Unwrap();
			var rootObject = ext.GetRootObject(builder.MappingSchema);

			ISqlExpression columnSql;
			MemberInfo     member;
			if (ext.NodeType == ExpressionType.MemberAccess)
			{
				var body = (MemberExpression)ext;

				member = body.Member;

				if (!member.IsPropertyEx() && !member.IsFieldEx() || rootObject != extract.Parameters[0])
					throw new LinqException("Member expression expected for the 'Set' statement.");

				if (member is MethodInfo)
					member = ((MethodInfo)member).GetPropertyInfo();

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

			var memberType = member.GetMemberType().ToNullableUnderlying();
			var updateType = update.Type.ToNullableUnderlying();
			if (memberType.IsEnum && updateType != memberType)
				update = Expression.Convert(update, member.GetMemberType());

			if (!update.Type.IsConstantable() && !builder.AsParameters.Contains(update))
				builder.AsParameters.Add(update);


			var expr = builder.ConvertToSql(select, update);

			items.Add(new SqlSetExpression(columnSql, expr));
		}

		#endregion

		#region UpdateContext

		class UpdateContext : SequenceContextBase
		{
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
				var update   =                   methodCall.Arguments[2].Unwrap();

				var updateStatement = sequence.Statement as SqlUpdateStatement ?? new SqlUpdateStatement(sequence.SelectQuery);
				sequence.Statement  = updateStatement;

				if (update.NodeType == ExpressionType.Lambda)
					ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						sequence,
						updateStatement.Update.Table!,
						updateStatement.Update.Items);
				else
					ParseSet(
						builder,
						buildInfo,
						extract,
						update,
						sequence,
						updateStatement.Update.Items);

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
