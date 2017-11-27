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

	class UpdateBuilder : MethodCallBuilder
	{
		#region Update

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Update");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			switch (methodCall.Arguments.Count)
			{
				case 1 : // int Update<T>(this IUpdateable<T> source)
					CheckAssociation(sequence);
					break;

				case 2 : // int Update<T>(this IQueryable<T> source, Expression<Func<T,T>> setter)
					{
						CheckAssociation(sequence);

						if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
							sequence = new SubQueryContext(sequence);

						BuildSetter(
							builder,
							buildInfo,
							(LambdaExpression)methodCall.Arguments[1].Unwrap(),
							sequence,
							sequence.SelectQuery.Update.Items,
							sequence);
						break;
					}

				case 3 :
					{
						var expr = methodCall.Arguments[1].Unwrap();

						if (expr is LambdaExpression && ((LambdaExpression)expr).ReturnType == typeof(bool))
						{
							CheckAssociation(sequence);

							// int Update<T>(this IQueryable<T> source, Expression<Func<T,bool>> predicate, Expression<Func<T,T>> setter)
							//
							sequence = builder.BuildWhere(buildInfo.Parent, sequence, (LambdaExpression)methodCall.Arguments[1].Unwrap(), false);

							if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
								sequence = new SubQueryContext(sequence);

							BuildSetter(
								builder,
								buildInfo,
								(LambdaExpression)methodCall.Arguments[2].Unwrap(),
								sequence,
								sequence.SelectQuery.Update.Items,
								sequence);
						}
						else
						{
							IBuildContext into;

							if (expr is LambdaExpression)
							{
								// static int Update<TSource,TTarget>(this IQueryable<TSource> source, Expression<Func<TSource,TTarget>> target, Expression<Func<TSource,TTarget>> setter)
								//
								var body      = ((LambdaExpression)expr).Body;
								int level     = body.GetLevel();
								var tableInfo = sequence.IsExpression(body, level, RequestFor.Table);

								if (tableInfo.Result == false)
									throw new LinqException("Expression '{0}' must be a table.");

								into = tableInfo.Context;
							}
							else
							{
								// static int Update<TSource,TTarget>(this IQueryable<TSource> source, Table<TTarget> target, Expression<Func<TSource,TTarget>> setter)
								//
								into = builder.BuildSequence(new BuildInfo(buildInfo, expr, new SelectQuery()));
							}

							sequence.ConvertToIndex(null, 0, ConvertFlags.All);
							new SelectQueryOptimizer(builder.DataContext.SqlProviderFlags, sequence.SelectQuery)
								.ResolveWeakJoins(new List<ISqlTableSource>());
							sequence.SelectQuery.Select.Columns.Clear();

							BuildSetter(
								builder,
								buildInfo,
								(LambdaExpression)methodCall.Arguments[2].Unwrap(),
								into,
								sequence.SelectQuery.Update.Items,
								sequence);

							var sql = sequence.SelectQuery;

							sql.Select.Columns.Clear();

							foreach (var item in sql.Update.Items)
								sql.Select.Columns.Add(new SqlColumn(sql, item.Expression));

							sql.Update.Table = ((TableBuilder.TableContext)into).SqlTable;
						}

						break;
					}
			}

			sequence.SelectQuery.ChangeQueryType(QueryType.Update);

			return new UpdateContext(buildInfo.Parent, sequence);
		}

		static void CheckAssociation(IBuildContext sequence)
		{
			var ctx = sequence as SelectContext;

			if (ctx != null && ctx.IsScalar)
			{
				var res = ctx.IsExpression(null, 0, RequestFor.Association);

				if (res.Result && res.Context is TableBuilder.AssociatedTableContext)
				{
					var atc = (TableBuilder.AssociatedTableContext)res.Context;
					sequence.SelectQuery.Update.Table = atc.SqlTable;
				}
				else
				{
					res = ctx.IsExpression(null, 0, RequestFor.Table);

					if (res.Result && res.Context is TableBuilder.TableContext)
					{
						var tc = (TableBuilder.TableContext)res.Context;

						if (sequence.SelectQuery.From.Tables.Count == 0 || sequence.SelectQuery.From.Tables[0].Source != tc.SelectQuery)
							sequence.SelectQuery.Update.Table = tc.SqlTable;
					}
				}
			}
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#endregion

		#region Helpers

		internal static void BuildSetter(
			ExpressionBuilder               builder,
			BuildInfo                       buildInfo,
			LambdaExpression                setter,
			IBuildContext                   into,
			List<SqlSetExpression> items,
			IBuildContext                   sequence)
		{
			var path = Expression.Parameter(setter.Body.Type, "p");
			var ctx  = new ExpressionContext(buildInfo.Parent, sequence, setter);

			if (setter.Body.NodeType == ExpressionType.MemberInit)
			{
				var ex  = (MemberInitExpression)setter.Body;
				var p   = sequence.Parent;

				BuildSetter(builder, into, items, ctx, ex, path);

				builder.ReplaceParent(ctx, p);
			}
			else
			{
				var sqlInfo = ctx.ConvertToSql(setter.Body, 0, ConvertFlags.All);

				foreach (var info in sqlInfo)
				{
					if (info.Members.Count == 0)
						throw new LinqException("Object initializer expected for insert statement.");

					if (info.Members.Count != 1)
						throw new InvalidOperationException();

					var member = info.Members[0];
					var pe     = Expression.MakeMemberAccess(path, member);
					var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
					var expr   = info.Sql;

					items.Add(new SqlSetExpression(column[0].Sql, expr));
				}
			}
		}

		static void BuildSetter(
			ExpressionBuilder               builder,
			IBuildContext                   into,
			List<SqlSetExpression> items,
			IBuildContext                   ctx,
			MemberInitExpression            expression,
			Expression                      path)
		{
			foreach (var binding in expression.Bindings)
			{
				var member  = binding.Member;

				if (member is MethodInfo)
					member = ((MethodInfo)member).GetPropertyInfo();

				if (binding is MemberAssignment)
				{
					var ma = binding as MemberAssignment;
					var pe = Expression.MakeMemberAccess(path, member);

					if (ma.Expression is MemberInitExpression && !into.IsExpression(pe, 1, RequestFor.Field).Result)
					{
						BuildSetter(
							builder,
							into,
							items,
							ctx,
							(MemberInitExpression)ma.Expression, Expression.MakeMemberAccess(path, member));
					}
					else
					{
						var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
						var expr   = builder.ConvertToSqlExpression(ctx, ma.Expression);

						if (expr.ElementType == QueryElementType.SqlParameter)
						{
							var parm  = (SqlParameter)expr;
							var field = column[0].Sql is SqlField
								? (SqlField)column[0].Sql
								: (SqlField)((SqlColumn)column[0].Sql).Expression;

							if (parm.DataType == DataType.Undefined)
								parm.DataType = field.DataType;
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
			var ext = extract.Body;

			while (ext.NodeType == ExpressionType.Convert || ext.NodeType == ExpressionType.ConvertChecked)
				ext = ((UnaryExpression)ext).Operand;

			if (ext.NodeType != ExpressionType.MemberAccess || ext.GetRootObject(builder.MappingSchema) != extract.Parameters[0])
				throw new LinqException("Member expression expected for the 'Set' statement.");

			var body   = (MemberExpression)ext;
			var member = body.Member;

			if (member is MethodInfo)
				member = ((MethodInfo)member).GetPropertyInfo();

			var members = body.GetMembers();
			var name    = members
				.Skip(1)
				.Select(ex =>
				{
					var me = ex as MemberExpression;

					if (me == null)
						return null;

					var m = me.Member;

					if (m is MethodInfo)
						m = ((MethodInfo)m).GetPropertyInfo();

					return m;
				})
				.Where(m => m != null && !m.IsNullableValueMember())
				.Select(m => m.Name)
				.Aggregate((s1,s2) => s1 + "." + s2);

			if (table != null && !table.Fields.ContainsKey(name))
				throw new LinqException("Member '{0}.{1}' is not a table column.", member.DeclaringType.Name, name);

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
			var ext = extract.Body;

			if (!update.Type.IsConstantable() && !builder.AsParameters.Contains(update))
				builder.AsParameters.Add(update);

			while (ext.NodeType == ExpressionType.Convert || ext.NodeType == ExpressionType.ConvertChecked)
				ext = ((UnaryExpression)ext).Operand;

			if (ext.NodeType != ExpressionType.MemberAccess || ext.GetRootObject(builder.MappingSchema) != extract.Parameters[0])
				throw new LinqException("Member expression expected for the 'Set' statement.");

			var body   = (MemberExpression)ext;
			var member = body.Member;

			if (member is MethodInfo)
				member = ((MethodInfo)member).GetPropertyInfo();

			var column = select.ConvertToSql(body, 1, ConvertFlags.Field);

			if (column.Length == 0)
				throw new LinqException("Member '{0}.{1}' is not a table column.", member.DeclaringType.Name, member.Name);

			var expr = builder.ConvertToSql(select, update);

			items.Add(new SqlSetExpression(column[0].Sql, expr));
		}

		#endregion

		#region UpdateContext

		class UpdateContext : SequenceContextBase
		{
			public UpdateContext(IBuildContext parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
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
				return methodCall.IsQueryable("Set");
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				if (sequence.SelectQuery.Select.SkipValue != null || !sequence.SelectQuery.Select.OrderBy.IsEmpty)
					sequence = new SubQueryContext(sequence);

				var extract  = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var update   =                   methodCall.Arguments[2].Unwrap();

				if (update.NodeType == ExpressionType.Lambda)
					ParseSet(
						builder,
						buildInfo,
						extract,
						(LambdaExpression)update,
						sequence,
						sequence.SelectQuery.Update.Table,
						sequence.SelectQuery.Update.Items);
				else
					ParseSet(
						builder,
						buildInfo,
						extract,
						update,
						sequence,
						sequence.SelectQuery.Update.Items);

				return sequence;
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}

		#endregion
	}
}
