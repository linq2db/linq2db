using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Common.Internal;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using System.Threading;
	using System.Runtime.CompilerServices;

	partial class ExpressionBuilder
	{
		#region Build Where

		public IBuildContext BuildWhere(IBuildContext? parent, IBuildContext sequence, LambdaExpression condition, bool checkForSubQuery, bool enforceHaving = false)
		{
			var prevParent = sequence.Parent;
			var ctx        = new ExpressionContext(parent, sequence, condition);
			var expr       = ConvertExpression(condition.Body.Unwrap());
			var makeHaving = false;

			if (checkForSubQuery && CheckSubQueryForWhere(ctx, expr, out makeHaving))
			{
				ReplaceParent(ctx, prevParent);

				sequence = new SubQueryContext(sequence);
				prevParent = sequence.Parent;

				ctx = new ExpressionContext(parent, sequence, condition);
			}

			var conditions = enforceHaving || makeHaving && !ctx.SelectQuery.GroupBy.IsEmpty?
				ctx.SelectQuery.Having.SearchCondition.Conditions :
				ctx.SelectQuery.Where. SearchCondition.Conditions;

			BuildSearchCondition(ctx, expr, conditions);

			ReplaceParent(ctx, prevParent);

			return sequence;
		}

		class CheckSubQueryForWhereContext
		{
			public CheckSubQueryForWhereContext(ExpressionBuilder builder, IBuildContext buildContext)
			{
				Builder = builder;
				BuildContext = buildContext;
			}

			public bool MakeSubQuery;
			public bool IsHaving;
			public bool IsWhere;

			public readonly ExpressionBuilder Builder;
			public readonly IBuildContext     BuildContext;
		}

		bool CheckSubQueryForWhere(IBuildContext context, Expression expression, out bool makeHaving)
		{
			var ctx = new CheckSubQueryForWhereContext(this, context);

			expression.Visit(ctx, static (context, expr) =>
			{
				if (context.MakeSubQuery)
					return false;

				if (context.Builder._subQueryExpressions?.Contains(expr) == true)
				{
					context.MakeSubQuery = true;
					context.IsWhere      = true;
					return false;
				}

				switch (expr.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression)expr;

						if (ma.Member.IsNullableValueMember())
							return true;

						if (Expressions.ConvertMember(context.Builder.MappingSchema, ma.Expression?.Type, ma.Member) != null)
							return true;

						var ctx = context.Builder.GetContext(context.BuildContext, expr);

						if (ctx == null)
							return true;

						var expres = ctx.IsExpression(expr, 0, RequestFor.Expression);

						if (expres.Result)
						{
							if (expres.Expression != null && context.Builder.IsGrouping(expres.Expression, context.Builder.MappingSchema))
							{
								context.IsHaving = true;
								return false;
							}

							context.MakeSubQuery = true;
						}
						else
						{
							if (context.Builder.IsGrouping(expr, context.Builder.MappingSchema))
							{
								context.IsHaving = true;
								return false;
							}

							context.IsWhere = ctx.IsExpression(expr, 0, RequestFor.Field).Result;
						}

						return false;
					}

					case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						if (Expressions.ConvertMember(context.Builder.MappingSchema, e.Object?.Type, e.Method) != null)
							return true;

						if (context.Builder.IsGrouping(e, context.Builder.MappingSchema))
						{
							context.IsHaving = true;
							return false;
						}

						break;
					}

					case ExpressionType.Parameter:
					{
						var ctx = context.Builder.GetContext(context.BuildContext, expr);

						if (ctx != null)
						{
							if (ctx.IsExpression(expr, 0, RequestFor.Expression).Result)
								context.MakeSubQuery = true;
						}

						context.IsWhere = true;

						break;
					}
				}

				return true;
			});

			makeHaving = ctx.IsHaving && !ctx.IsWhere;
			return ctx.MakeSubQuery || ctx.IsHaving && ctx.IsWhere;
		}

		bool IsGrouping(Expression expression, MappingSchema mappingSchema)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.MemberAccess:
				{
					var ma = (MemberExpression)expression;
					return ma.Expression != null && typeof(IGrouping<,>).IsSameOrParentOf(ma.Expression.Type);
				}

				case ExpressionType.Call:
				{
					var mce = (MethodCallExpression)expression;

					if (mce.Object != null && typeof(IGrouping<,>).IsSameOrParentOf(mce.Object.Type))
						return true;

					if (mce.Method == Methods.LinqToDB.GroupBy.Grouping)
						return true;

					return mce.Arguments.Any(static a => typeof(IGrouping<,>).IsSameOrParentOf(a.Type));
				}
			}

			return false;
		}

		#endregion

		#region BuildTake

		public void BuildTake(IBuildContext context, ISqlExpression expr, TakeHints? hints)
		{
			var sql = context.SelectQuery;

			sql.Select.Take(expr, hints);

			if (sql.Select.SkipValue != null &&
				 DataContext.SqlProviderFlags.IsTakeSupported &&
				!DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql.Select.TakeValue, sql.Select.SkipValue))
				sql.Select.Take(
					new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue!, Precedence.Additive), hints);
		}

		#endregion

		#region SubQueryToSql

		public IBuildContext GetSubQuery(IBuildContext context, MethodCallExpression expr)
		{
			var info = new BuildInfo(context, expr, new SelectQuery { ParentSelect = context.SelectQuery });
			var ctx  = BuildSequence(info);

			if (ctx.SelectQuery.Select.Columns.Count == 0)
			{
				if (ctx.IsExpression(null, 0, RequestFor.Field).Result)
					ctx.ConvertToIndex(null, 0, ConvertFlags.Field);
				if (ctx.IsExpression(null, 0, RequestFor.Expression).Result)
					ctx.ConvertToIndex(null, 0, ConvertFlags.All);
			}

			return ctx;
		}

		internal ISqlExpression SubQueryToSql(IBuildContext context, MethodCallExpression expression)
		{
			var subQueryCtx = GetSubQueryContext(context, expression);
			var sequence    = subQueryCtx.Context;
			var subSql      = sequence.GetSubQuery(context);

			if (subSql == null)
			{
				var query    = context.SelectQuery;
				var subQuery = sequence.SelectQuery;

				// This code should be moved to context.
				//
				if (!query.GroupBy.IsEmpty && !subQuery.Where.IsEmpty)
				{
					var fromGroupBy = false;
					foreach (var p in sequence.SelectQuery.Properties.OfType<Tuple<string, SelectQuery>>())
					{
						if (p.Item1 == "from_group_by" && ReferenceEquals(p.Item2, context.SelectQuery))
						{
							fromGroupBy = true;
							break;
						}
					}

					if (fromGroupBy)
					{
						if (subQuery.Select.Columns.Count == 1 &&
							subQuery.Select.Columns[0].Expression.ElementType == QueryElementType.SqlFunction &&
							subQuery.GroupBy.IsEmpty && !subQuery.Select.HasModifier && !subQuery.HasSetOperators &&
							subQuery.Where.SearchCondition.Conditions.Count == 1)
						{
							var cond = subQuery.Where.SearchCondition.Conditions[0];

							if (cond.Predicate.ElementType == QueryElementType.ExprExprPredicate && query.GroupBy.Items.Count == 1 ||
								cond.Predicate.ElementType == QueryElementType.SearchCondition &&
								query.GroupBy.Items.Count == ((SqlSearchCondition)cond.Predicate).Conditions.Count)
							{
								var func = (SqlFunction)subQuery.Select.Columns[0].Expression;

								if (CountBuilder.MethodNames.Contains(func.Name))
									return SqlFunction.CreateCount(func.SystemType, query);
							}
						}
					}
				}

				subSql = sequence.SelectQuery;
			}

			return subSql;
		}

		#endregion

		#region IsSubQuery

		internal bool IsSubQuery(IBuildContext context, MethodCallExpression call)
		{
			var isAggregate = call.IsAggregate(MappingSchema);

			if (isAggregate || call.IsQueryable())
			{
				var info = new BuildInfo(context, call, new SelectQuery { ParentSelect = context.SelectQuery });

				if (!IsSequence(info))
					return false;

				var arg = call.Arguments[0];

				if (isAggregate)
					while (arg.NodeType == ExpressionType.Call && ((MethodCallExpression)arg).Method.Name == "Select")
						arg = ((MethodCallExpression)arg).Arguments[0];

				arg = arg.SkipPathThrough();
				arg = arg.SkipMethodChain(MappingSchema);

				var mc = arg as MethodCallExpression;

				while (mc != null)
				{
					if (!mc.IsQueryable())
					{
						if (mc.IsAssociation(MappingSchema))
							return true;

						return GetTableFunctionAttribute(mc.Method) != null;
					}

					mc = mc.Arguments[0] as MethodCallExpression;
				}

				return arg.NodeType == ExpressionType.Call || IsSubQuerySource(context, arg);
			}

			return false;
		}

		bool IsSubQuerySource(IBuildContext context, Expression? expr)
		{
			if (expr == null)
				return false;

			var ctx = GetContext(context, expr);

			if (ctx != null && ctx.IsExpression(expr, 0, RequestFor.Object).Result)
				return true;

			while (expr != null)
			{
				switch (expr)
				{
					case MemberExpression me:
						expr = me.Expression;
						continue;
					case MethodCallExpression mc when mc.IsQueryable("AsQueryable"):
						expr = mc.Arguments[0];
						continue;
				}

				break;
			}

			return expr != null && expr.NodeType == ExpressionType.Constant;
		}

		bool IsGroupJoinSource(IBuildContext context, MethodCallExpression call)
		{
			if (!call.IsQueryable() || CountBuilder.MethodNames.Contains(call.Method.Name))
				return false;

			Expression expr = call;

			while (expr.NodeType == ExpressionType.Call)
				expr = ((MethodCallExpression)expr).Arguments[0];

			var ctx = GetContext(context, expr);

			return ctx != null && ctx.IsExpression(expr, 0, RequestFor.GroupJoin).Result;
		}

		#endregion

		#region ConvertExpression

		interface IConvertHelper
		{
			Expression ConvertNull(MemberExpression expression);
		}

		class ConvertHelper<T> : IConvertHelper
			where T : struct
		{
			public Expression ConvertNull(MemberExpression expression)
			{
				return Expression.Call(
					null,
					MemberHelper.MethodOf<T?>(p => Sql.ToNotNull(p)),
					expression.Expression!);
			}
		}

		internal Expression ConvertExpression(Expression expression)
		{
			return (_convertExpressionTransformer ??= TransformInfoVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.ConvertExpressionTransformer(e)))
				.Transform(expression);
		}

		private TransformInfoVisitor<ExpressionBuilder>? _convertExpressionTransformer;

		private TransformInfo ConvertExpressionTransformer(Expression e)
		{
			if (CanBeConstant(e) || CanBeCompiled(e))
				//if ((CanBeConstant(e) || CanBeCompiled(e)) && !PreferServerSide(e))
				return new TransformInfo(e, true);

			switch (e.NodeType)
			{
				//This is to handle VB's weird expression generation when dealing with nullable properties.
				case ExpressionType.Coalesce:
				{
					var b = (BinaryExpression)e;

					if (b.Left is BinaryExpression equalityLeft && b.Right is ConstantExpression constantRight)
						if (equalityLeft.Type.IsNullable())
							if (equalityLeft.NodeType == ExpressionType.Equal && equalityLeft.Left.Type == equalityLeft.Right.Type)
								if (constantRight.Value is bool val && val == false)
									return new TransformInfo(equalityLeft, false);

					break;
				}

				case ExpressionType.New:
				{
					var ex = ConvertNew((NewExpression)e);
					if (ex != null)
						return new TransformInfo(ConvertExpression(ex));
					break;
				}

				case ExpressionType.Call:
				{
					var expr = (MethodCallExpression)e;

					if (expr.Method.IsSqlPropertyMethodEx())
					{
						// transform Sql.Property into member access
						if (expr.Arguments[1].Type != typeof(string))
							throw new ArgumentException("Only strings are allowed for member name in Sql.Property expressions.");

						var entity           = ConvertExpression(expr.Arguments[0]);
						var memberName       = (string)expr.Arguments[1].EvaluateExpression()!;
						var entityDescriptor = MappingSchema.GetEntityDescriptor(entity.Type);

						var memberInfo = entityDescriptor[memberName]?.MemberInfo;
						if (memberInfo == null)
						{
							foreach (var a in entityDescriptor.Associations)
							{
								if (a.MemberInfo.Name == memberName)
								{
									if (memberInfo != null)
										throw new InvalidOperationException("Sequence contains more than one element");
									memberInfo = a.MemberInfo;
								}
							}
						}

						if (memberInfo == null)
							memberInfo = MemberHelper.GetMemberInfo(expr);

						return new TransformInfo(ConvertExpression(Expression.MakeMemberAccess(entity, memberInfo)));
					}

					var cm = ConvertMethod(expr);
					if (cm != null)
						//TODO: looks like a mess: ConvertExpression can not work without OptimizeExpression
						return new TransformInfo(OptimizeExpression(ConvertExpression(cm)));
					break;
				}

				case ExpressionType.MemberAccess:
				{
					var ma = (MemberExpression)e;
					var l  = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

					if (l != null)
					{
						var body = l.Body.Unwrap();
						var expr = body.Transform(ma, static (ma, wpi) => wpi.NodeType == ExpressionType.Parameter ? ma.Expression! : wpi);

						if (expr.Type != e.Type)
							expr = new ChangeTypeExpression(expr, e.Type);

						//TODO: looks like a mess: ConvertExpression can not work without OptimizeExpression
						return new TransformInfo(OptimizeExpression(ConvertExpression(expr)));
					}

					if (ma.Member.IsNullableValueMember())
					{
						var ntype  = typeof(ConvertHelper<>).MakeGenericType(ma.Type);
						var helper = (IConvertHelper)Activator.CreateInstance(ntype)!;
						var expr   = helper.ConvertNull(ma);

						return new TransformInfo(ConvertExpression(expr));
					}

					if (ma.Member.DeclaringType == typeof(TimeSpan))
					{
						switch (ma.Expression!.NodeType)
						{
							case ExpressionType.Subtract:
							case ExpressionType.SubtractChecked:

								Sql.DateParts datePart;

								switch (ma.Member.Name)
								{
									case "TotalMilliseconds": datePart = Sql.DateParts.Millisecond; break;
									case "TotalSeconds": datePart = Sql.DateParts.Second; break;
									case "TotalMinutes": datePart = Sql.DateParts.Minute; break;
									case "TotalHours": datePart = Sql.DateParts.Hour; break;
									case "TotalDays": datePart = Sql.DateParts.Day; break;
									default: return new TransformInfo(e);
								}

								var ex = (BinaryExpression)ma.Expression;
								if (ex.Left.Type == typeof(DateTime)
									&& ex.Right.Type == typeof(DateTime))
								{
									var method = MemberHelper.MethodOf(
												() => Sql.DateDiff(Sql.DateParts.Day, DateTime.MinValue, DateTime.MinValue));

									var call   =
												Expression.Convert(
													Expression.Call(
														null,
														method,
														Expression.Constant(datePart),
														Expression.Convert(ex.Right, typeof(DateTime?)),
														Expression.Convert(ex.Left,  typeof(DateTime?))),
													typeof(double));

									return new TransformInfo(ConvertExpression(call));
								}
								else
								{
									var method = MemberHelper.MethodOf(
												() => Sql.DateDiff(Sql.DateParts.Day, DateTimeOffset.MinValue, DateTimeOffset.MinValue));

									var call   =
												Expression.Convert(
													Expression.Call(
														null,
														method,
														Expression.Constant(datePart),
														Expression.Convert(ex.Right, typeof(DateTimeOffset?)),
														Expression.Convert(ex.Left,  typeof(DateTimeOffset?))),
													typeof(double));

									return new TransformInfo(ConvertExpression(call));
								}
						}
					}

					break;
				}

				default:
				{
					if (e is BinaryExpression binary)
					{
						var l = Expressions.ConvertBinary(MappingSchema, binary);
						if (l != null)
						{
							var body = l.Body.Unwrap();
							var expr = body.Transform((l, binary), static (context, wpi) =>
								{
									if (wpi.NodeType == ExpressionType.Parameter)
									{
										if (context.l.Parameters[0] == wpi)
											return context.binary.Left;
										if (context.l.Parameters[1] == wpi)
											return context.binary.Right;
									}

									return wpi;
								});

							if (expr.Type != e.Type)
								expr = new ChangeTypeExpression(expr, e.Type);

							return new TransformInfo(ConvertExpression(expr));
						}
					}
					break;
				}
			}

			return new TransformInfo(e);
		}

		Expression? ConvertMethod(MethodCallExpression pi)
		{
			LambdaExpression? lambda = null;

			if (!pi.Method.IsStatic && pi.Object != null && pi.Object.Type != pi.Method.DeclaringType)
			{
				var concreteTypeMemberInfo = pi.Object.Type.GetMemberEx(pi.Method);
				if (concreteTypeMemberInfo != null)
					lambda = Expressions.ConvertMember(MappingSchema, pi.Object.Type, concreteTypeMemberInfo);
			}

			if (lambda == null)
				lambda = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

			return lambda == null ? null : ConvertMethod(pi, lambda);
		}

		static Expression ConvertMethod(MethodCallExpression pi, LambdaExpression lambda)
		{
			var ef    = lambda.Body.Unwrap();
			var parms = new Dictionary<ParameterExpression,int>(lambda.Parameters.Count);
			var pn    = pi.Method.IsStatic ? 0 : -1;

			foreach (var p in lambda.Parameters)
				parms.Add(p, pn++);

			var pie = ef.Transform((pi, parms), static (context, wpi) =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (context.parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						if (n >= context.pi.Arguments.Count)
						{
							if (DataContextParam.Type.IsSameOrParentOf(wpi.Type))
							{
								if (DataContextParam.Type != wpi.Type)
									return Expression.Convert(DataContextParam, wpi.Type);
								return DataContextParam;
							}

							throw new LinqToDBException($"Can't convert {wpi} to expression.");
						}

						var result = n < 0 ? context.pi.Object! : context.pi.Arguments[n];

						if (result.Type != wpi.Type)
						{
							if (result.Type.IsEnum)
								result = Expression.Convert(result, wpi.Type);
						}

						return result;
					}
				}

				return wpi;
			});

			if (pi.Method.ReturnType != pie.Type)
				pie = new ChangeTypeExpression(pie, pi.Method.ReturnType);

			return pie;
		}

		Expression? ConvertNew(NewExpression pi)
		{
			var lambda = Expressions.ConvertMember(MappingSchema, pi.Type, pi.Constructor!);

			if (lambda != null)
			{
				var ef    = lambda.Body.Unwrap();
				var parms = new Dictionary<string,int>(lambda.Parameters.Count);
				var pn    = 0;

				foreach (var p in lambda.Parameters)
					parms.Add(p.Name!, pn++);

				return ef.Transform((pi, parms), static (context, wpi) =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						var pe   = (ParameterExpression)wpi;
						var n    = context.parms[pe.Name!];
						return context.pi.Arguments[n];
					}

					return wpi;
				});
			}

			return null;
		}

		#endregion

		#region BuildExpression

		public SqlInfo[] ConvertExpressions(IBuildContext context, Expression expression, ConvertFlags queryConvertFlag, ColumnDescriptor? columnDescriptor)
		{
			expression = ConvertExpression(expression).UnwrapConvertToObject();

			switch (expression.NodeType)
			{
				case ExpressionType.New:
				{
					var expr = (NewExpression)expression;

					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					// ReSharper disable HeuristicUnreachableCode
					if (expr.Members == null)
						return Array<SqlInfo>.Empty;
					// ReSharper restore HeuristicUnreachableCode
					// ReSharper restore ConditionIsAlwaysTrueOrFalse

					var ed = context.Builder.MappingSchema.GetEntityDescriptor(expr.Type);
					if (expr.Arguments.Count == 0)
						return Array<SqlInfo>.Empty;
					var sqlInfos = new List<SqlInfo>();
					for (var i = 0; i < expr.Arguments.Count; i++)
					{
						var arg = expr.Arguments[i];
						var mi = expr.Members[i];
						if (mi is MethodInfo info)
							mi = info.GetPropertyInfo();

						var descriptor = ed.FindColumnDescriptor(mi);

						if (descriptor == null && EagerLoading.IsDetailsMember(context, arg))
							continue;

						foreach (var si in ConvertExpressions(context, arg.UnwrapConvertToObject(), queryConvertFlag, descriptor))
							sqlInfos.Add(si.Clone(mi));
					}

					return sqlInfos.ToArray();
				}

				case ExpressionType.MemberInit:
				{
					var expr = (MemberInitExpression)expression;
					var ed   = context.Builder.MappingSchema.GetEntityDescriptor(expr.Type);
					var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select(static (m,i) => (m, i))
							.ToDictionary(static _ => _.m.MemberInfo, static _ => _.i);

					var result = new List<SqlInfo>();

					var assignments = new List<(MemberAssignment ma, int order)>();

					foreach (var ma in expr.Bindings.Where(static b => b is MemberAssignment).Cast<MemberAssignment>())
						assignments.Add((ma, dic[expr.Type.GetMemberEx(ma.Member)!]));

					foreach (var (a, _) in assignments.OrderBy(static a => a.order))
					{
						var mi = a.Member;
						if (mi is MethodInfo info)
							mi = info.GetPropertyInfo();

						var descriptor = ed.FindColumnDescriptor(mi);

						if (descriptor == null && EagerLoading.IsDetailsMember(context, a.Expression))
							return Array<SqlInfo>.Empty;

						foreach (var si in ConvertExpressions(context, a.Expression, queryConvertFlag, descriptor))
							result.Add(si.Clone(mi));
					}

					return result.ToArray();
				}
				case ExpressionType.Call:
				{
					var callCtx = GetContext(context, expression);
					if (callCtx != null)
					{
						var mc = (MethodCallExpression)expression;
						if (IsSubQuery(callCtx, mc))
						{
							var subQueryContextInfo = GetSubQueryContext(callCtx, mc);
							if (subQueryContextInfo.Context.IsExpression(null, 0, RequestFor.Object).Result)
							{
								var info = subQueryContextInfo.Context.ConvertToSql(null, 0, ConvertFlags.All);
								return info;
							}

							var sql = subQueryContextInfo.Context.GetSubQuery(context);
							if (sql != null)
								return new[] { new SqlInfo(sql) };

							return new[] { new SqlInfo(subQueryContextInfo.Context.SelectQuery) };
						}
					}
					break;
				}
				case ExpressionType.NewArrayInit:
				{
					var expr = (NewArrayExpression)expression;
					var sql = new List<SqlInfo>();
					foreach (var arg in expr.Expressions)
						sql.AddRange(ConvertExpressions(context, arg, queryConvertFlag, columnDescriptor));
					return sql.ToArray();
				}
				case ExpressionType.ListInit:
				{
					var expr = (ListInitExpression)expression;

					var sql = new List<SqlInfo>();
					foreach (var init in expr.Initializers)
						foreach (var arg in init.Arguments)
							sql.AddRange(ConvertExpressions(context, arg, queryConvertFlag, columnDescriptor));
					return sql.ToArray();
				}
			}

			var ctx = GetContext(context, expression);

			if (ctx != null && ctx.IsExpression(expression, 0, RequestFor.Object).Result)
				return ctx.ConvertToSql(expression, 0, queryConvertFlag);

			return new[] { new SqlInfo(ConvertToSql(context, expression, false, columnDescriptor)) };
		}

		public ISqlExpression ConvertToSqlExpression(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor, bool isPureExpression)
		{
			var expr = ConvertExpression(expression);
			return ConvertToSql(context, expr, false, columnDescriptor, isPureExpression);
		}

		public ISqlExpression ConvertToExtensionSql(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor)
		{
			expression = expression.UnwrapConvertToObject();
			var unwrapped = expression.Unwrap();

			if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(unwrapped.Type))
			{
				Expression preparedExpression;
				if (unwrapped.NodeType == ExpressionType.Call)
					preparedExpression = ((MethodCallExpression)unwrapped).Arguments[0];
				else
					preparedExpression = ((Sql.IQueryableContainer)unwrapped.EvaluateExpression()!).Query.Expression;
				return ConvertToExtensionSql(context, preparedExpression, columnDescriptor);
			}

			if (unwrapped is LambdaExpression lambda)
			{
				IBuildContext valueSequence = context;

				if (context is SelectContext sc && sc.Sequence[0] is GroupByBuilder.GroupByContext)
					valueSequence = sc.Sequence[0];

				if (valueSequence is GroupByBuilder.GroupByContext groupByContext)
				{
					valueSequence = groupByContext.Element;
				}

				var contextRefExpression = new ContextRefExpression(lambda.Parameters[0].Type, valueSequence);

				var body = lambda.GetBody(contextRefExpression);

				var result = ConvertToSql(context, body, false, columnDescriptor);

				if (!(result is SqlField field) || field.Table!.All != field)
					return result;
				result = context.ConvertToSql(null, 0, ConvertFlags.Field).Select(static _ => _.Sql).First();
				return result;
			}

			if (context is SelectContext selectContext)
			{
				if (null != expression.Find(selectContext.Body))
					return context.ConvertToSql(null, 0, ConvertFlags.Field).Select(static _ => _.Sql).First();
			}

			if (context is MethodChainBuilder.ChainContext chainContext)
			{
				if (expression is MethodCallExpression mc && IsSubQuery(context, mc))
					return context.ConvertToSql(null, 0, ConvertFlags.Field).Select(static _ => _.Sql).First();
			}

			return ConvertToSql(context, expression, false, columnDescriptor);
		}

		public ISqlExpression ConvertToSql(IBuildContext? context, Expression expression, bool unwrap = false, ColumnDescriptor? columnDescriptor = null, bool isPureExpression = false)
		{
			if (typeof(IToSqlConverter).IsSameOrParentOf(expression.Type))
			{
				var sql = ConvertToSqlConvertible(expression);
				if (sql != null)
					return sql;
			}

			if (!PreferServerSide(expression, false))
			{
				if (columnDescriptor?.ValueConverter == null && CanBeConstant(expression))
					return BuildConstant(expression, columnDescriptor);

				if (CanBeCompiled(expression))
					return ParametersContext.BuildParameter(expression, columnDescriptor).SqlParameter;
			}

			if (unwrap)
				expression = expression.Unwrap();

			switch (expression.NodeType)
			{
				case ExpressionType.AndAlso:
				case ExpressionType.OrElse:
				case ExpressionType.Not:
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					var condition = new SqlSearchCondition();
					BuildSearchCondition(context, expression, condition.Conditions);
					return condition;
				}

				case ExpressionType.And:
				case ExpressionType.Or:
				{
					if (expression.Type == typeof(bool))
						goto case ExpressionType.AndAlso;
					goto case ExpressionType.Add;
				}

				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Divide:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Power:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Coalesce:
				{
					var e = (BinaryExpression)expression;

					ISqlExpression l;
					ISqlExpression r;
					var shouldCheckColumn =
							e.Left.Type.ToNullableUnderlying() == e.Right.Type.ToNullableUnderlying();

					if (shouldCheckColumn)
					{
						var ls = GetContext(context, e.Left);
						if (ls?.IsExpression(e.Left, 0, RequestFor.Field).Result == true)
						{
							l = ConvertToSql(context, e.Left);
							r = ConvertToSql(context, e.Right, true, QueryHelper.GetColumnDescriptor(l) ?? columnDescriptor);
						}
						else
						{
							r = ConvertToSql(context, e.Right, true);
							l = ConvertToSql(context, e.Left, false, QueryHelper.GetColumnDescriptor(r) ?? columnDescriptor);
						}
					}
					else
					{
						l = ConvertToSql(context, e.Left, true, columnDescriptor);
						r = ConvertToSql(context, e.Right, true, null);
					}

					var t = e.Type;

					switch (expression.NodeType)
					{
						case ExpressionType.Add:
						case ExpressionType.AddChecked: return new SqlBinaryExpression(t, l, "+", r, Precedence.Additive);
						case ExpressionType.And: return new SqlBinaryExpression(t, l, "&", r, Precedence.Bitwise);
						case ExpressionType.Divide: return new SqlBinaryExpression(t, l, "/", r, Precedence.Multiplicative);
						case ExpressionType.ExclusiveOr: return new SqlBinaryExpression(t, l, "^", r, Precedence.Bitwise);
						case ExpressionType.Modulo: return new SqlBinaryExpression(t, l, "%", r, Precedence.Multiplicative);
						case ExpressionType.Multiply:
						case ExpressionType.MultiplyChecked: return new SqlBinaryExpression(t, l, "*", r, Precedence.Multiplicative);
						case ExpressionType.Or: return new SqlBinaryExpression(t, l, "|", r, Precedence.Bitwise);
						case ExpressionType.Power: return new SqlFunction(t, "Power", l, r);
						case ExpressionType.Subtract:
						case ExpressionType.SubtractChecked: return new SqlBinaryExpression(t, l, "-", r, Precedence.Subtraction);
						case ExpressionType.Coalesce:
						{
							if (QueryHelper.UnwrapExpression(r) is SqlFunction c)
							{
								if (c.Name == "Coalesce")
								{
									var parms = new ISqlExpression[c.Parameters.Length + 1];

									parms[0] = l;
									c.Parameters.CopyTo(parms, 1);

									return new SqlFunction(t, "Coalesce", parms);
								}
							}

							return new SqlFunction(t, "Coalesce", l, r);
						}
					}

					break;
				}

				case ExpressionType.UnaryPlus:
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				{
					var e = (UnaryExpression)expression;
					var o = ConvertToSql(context, e.Operand);
					var t = e.Type;

					switch (expression.NodeType)
					{
						case ExpressionType.UnaryPlus: return o;
						case ExpressionType.Negate:
						case ExpressionType.NegateChecked:
							return new SqlBinaryExpression(t, new SqlValue(-1), "*", o, Precedence.Multiplicative);
					}

					break;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var e = (UnaryExpression)expression;

					var o = ConvertToSql(context, e.Operand);

					if (e.Method == null && e.IsLifted)
						return o;

					if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
						return o;

					var t = e.Operand.Type;
					var s = SqlDataType.GetDataType(t);

					if (o.SystemType != null && s.Type.SystemType == typeof(object))
					{
						t = o.SystemType;
						s = SqlDataType.GetDataType(t);
					}

					if (e.Type == t ||
						t.IsEnum && Enum.GetUnderlyingType(t) == e.Type ||
						e.Type.IsEnum && Enum.GetUnderlyingType(e.Type) == t)
						return o;

					return new SqlFunction(e.Type, "$Convert$", SqlDataType.GetDataType(e.Type), s, o);
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expression;
					var s = ConvertToSql(context, e.Test);
					var t = ConvertToSql(context, e.IfTrue);
					var f = ConvertToSql(context, e.IfFalse);

					if (QueryHelper.UnwrapExpression(f) is SqlFunction c && c.Name == "CASE")
					{
						var parms = new ISqlExpression[c.Parameters.Length + 2];

						parms[0] = s;
						parms[1] = t;
						c.Parameters.CopyTo(parms, 2);

						return new SqlFunction(e.Type, "CASE", parms) { CanBeNull = false };
					}

					return new SqlFunction(e.Type, "CASE", s, t, f) { CanBeNull = false };
				}

				case ExpressionType.MemberAccess:
				{
					var ma   = (MemberExpression)expression;
					var attr = GetExpressionAttribute(ma.Member);

					var converted = attr?.GetExpression((this_: this, context: context!), DataContext, context!.SelectQuery, ma,
							static (context, e, descriptor) => context.this_.ConvertToExtensionSql(context.context, e, descriptor));

					if (converted != null)
						return converted;

					var ctx = GetContext(context, expression);

					if (ctx != null)
					{
						var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

						switch (sql.Length)
						{
							case 0: break;
							case 1: return sql[0].Sql;
							default: throw new InvalidOperationException();
						}
					}

					break;
				}

				case ExpressionType.Parameter:
				{
					var ctx = GetContext(context, expression);

					if (ctx != null)
					{
						var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

						switch (sql.Length)
						{
							case 0: break;
							case 1: return sql[0].Sql;
							default: throw new InvalidOperationException();
						}
					}

					break;
				}

				case ExpressionType.Extension:
				{
					var ctx = GetContext(context, expression);

					if (ctx != null)
					{
						var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

						switch (sql.Length)
						{
							case 0: break;
							case 1: return sql[0].Sql;
							default: throw new InvalidOperationException();
						}
					}

					break;
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expression;

					var isAggregation = e.IsAggregate(MappingSchema);
					if (isAggregation && !e.IsQueryable())
					{
						var arg = e.Arguments[0];
						var enumerableType = arg.Type;
						if (EagerLoading.IsEnumerableType(enumerableType, MappingSchema))
						{
							var elementType = EagerLoading.GetEnumerableElementType(enumerableType, MappingSchema);
							if (!e.Method.GetParameters()[0].ParameterType.IsSameOrParentOf(typeof(IEnumerable<>).MakeGenericType(elementType)))
								isAggregation = false;
						}
					}

					if ((isAggregation || e.IsQueryable()) && !ContainsBuilder.IsConstant(e))
					{
						if (IsSubQuery(context!, e))
							return SubQueryToSql(context!, e);

						if (isAggregation)
						{
							var ctx = GetContext(context, expression);

							if (ctx != null)
							{
								var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

								if (sql.Length != 1)
									throw new InvalidOperationException();

								return sql[0].Sql;
							}

							break;
						}

						return SubQueryToSql(context!, e);
					}

					var expr = ConvertMethod(e);

					if (expr != null)
						return ConvertToSql(context, expr, unwrap);

					var attr = GetExpressionAttribute(e.Method);

					if (attr != null)
					{
						return ConvertExtensionToSql(context!, attr, e);
					}

					if (e.Method.IsSqlPropertyMethodEx())
						return ConvertToSql(context, ConvertExpression(expression), unwrap);

					if (e.Method.DeclaringType == typeof(string) && e.Method.Name == "Format")
					{
						return ConvertFormatToSql(context, e, isPureExpression);
					}

					if (e.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
					{
						var sql = ConvertToSql(context, e.Arguments[0], unwrap);
						return sql;
					}

					break;
				}

				case ExpressionType.Invoke:
				{
					var pi = (InvocationExpression)expression;
					var ex = pi.Expression;

					if (ex.NodeType == ExpressionType.Quote)
						ex = ((UnaryExpression)ex).Operand;

					if (ex.NodeType == ExpressionType.Lambda)
					{
						var l   = (LambdaExpression)ex;
						var dic = new Dictionary<Expression,Expression>();

						for (var i = 0; i < l.Parameters.Count; i++)
							dic.Add(l.Parameters[i], pi.Arguments[i]);

						var pie = l.Body.Transform(dic, static (dic, wpi) => dic.TryGetValue(wpi, out var ppi) ? ppi : wpi);

						return ConvertToSql(context, pie);
					}

					break;
				}

				case ExpressionType.TypeIs:
				{
					var condition = new SqlSearchCondition();
					BuildSearchCondition(context, expression, condition.Conditions);
					return condition;
				}

				case ChangeTypeExpression.ChangeTypeType:
					return ConvertToSql(context, ((ChangeTypeExpression)expression).Expression);

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)expression;
					if (cnt.Value is ISqlExpression sql)
						return sql;
					break;
				}
			}

			if (expression.Type == typeof(bool) && _convertedPredicates.Add(expression))
			{
				var predicate = ConvertPredicate(context, expression);
				_convertedPredicates.Remove(expression);
				if (predicate != null)
					return new SqlSearchCondition(new SqlCondition(false, predicate));
			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		public ISqlExpression ConvertFormatToSql(IBuildContext? context, MethodCallExpression mc, bool isPureExpression)
		{
			// TODO: move PrepareRawSqlArguments to more correct location
			TableBuilder.PrepareRawSqlArguments(mc, null,
				out var format, out var arguments);

			var sqlArguments = new List<ISqlExpression>();
			foreach (var a in arguments)
				sqlArguments.Add(ConvertToSql(context, a));

			if (isPureExpression)
				return new SqlExpression(mc.Type, format, Precedence.Primary, sqlArguments.ToArray());

			return QueryHelper.ConvertFormatToConcatenation(format, sqlArguments);
		}

		public ISqlExpression ConvertExtensionToSql(IBuildContext context, Sql.ExpressionAttribute attr, MethodCallExpression mc)
		{
			var inlineParameters = DataContext.InlineParameters;

			if (attr.InlineParameters)
				DataContext.InlineParameters = true;

			var sqlExpression =
				attr.GetExpression(
					(this_: this, context: context),
					DataContext,
					context!.SelectQuery,
					mc,
					static (context, e, descriptor) => context.this_.ConvertToExtensionSql(context.context, e, descriptor));
			if (sqlExpression == null)
				throw new LinqToDBException($"Cannot convert to SQL method '{mc}'.");

			DataContext.InlineParameters = inlineParameters;

			return sqlExpression;
		}

		public static ISqlExpression ConvertToSqlConvertible(Expression expression)
		{
			var l = Expression.Lambda<Func<IToSqlConverter>>(expression);
			var f = l.CompileExpression();
			var c = f();

			return c.ToSql(expression);
		}

		readonly HashSet<Expression> _convertedPredicates = new ();

		#endregion

		#region IsServerSideOnly

		public bool IsServerSideOnly(Expression expr)
		{
			return _optimizationContext.IsServerSideOnly(expr);
		}

		#endregion

		#region CanBeConstant

		bool CanBeConstant(Expression expr)
		{
			return _optimizationContext.CanBeConstant(expr);
		}

		#endregion

		#region CanBeCompiled

		public bool CanBeCompiled(Expression expr)
		{
			return _optimizationContext.CanBeCompiled(expr);
		}

		#endregion

		#region Build Constant

		readonly Dictionary<Tuple<Expression, ColumnDescriptor?>,SqlValue> _constants = new ();

		SqlValue BuildConstant(Expression expr, ColumnDescriptor? columnDescriptor)
		{
			var key = Tuple.Create(expr, columnDescriptor);
			if (_constants.TryGetValue(key, out var sqlValue))
				return sqlValue;

			var columnType = columnDescriptor?.GetDbDataType(true);
			var dbType = columnType != null && columnType.Value.SystemType == expr.Type
				? columnType.Value.WithSystemType(expr.Type)
				: new DbDataType(expr.Type);

			var unwrapped = expr.Unwrap();
			if (unwrapped != expr && !MappingSchema.ValueToSqlConverter.CanConvert(dbType.SystemType) &&
				MappingSchema.ValueToSqlConverter.CanConvert(unwrapped.Type))
			{
				dbType = dbType.WithSystemType(unwrapped.Type);
				expr   = unwrapped;
			}

			dbType = dbType.WithSystemType(expr.Type);

			if (columnDescriptor != null)
			{
				expr = columnDescriptor.ApplyConversions(expr, dbType, true);
			}
			else
			{
				if (!MappingSchema.ValueToSqlConverter.CanConvert(dbType.SystemType))
					expr = ColumnDescriptor.ApplyConversions(MappingSchema, expr, dbType, null, true);
			}

			var value = expr.EvaluateExpression();

			sqlValue = MappingSchema.GetSqlValue(expr.Type, value);

			_constants.Add(key, sqlValue);

			return sqlValue;
		}

		#endregion

		#region Predicate Converter

		ISqlPredicate ConvertPredicate(IBuildContext? context, Expression expression)
		{
			ISqlExpression IsCaseSensitive(MethodCallExpression mc)
			{
				if (mc.Arguments.Count <= 1)
					return new SqlValue(typeof(bool?), null);

				if (!typeof(StringComparison).IsSameOrParentOf(mc.Arguments[1].Type))
					return new SqlValue(typeof(bool?), null);

				var arg = mc.Arguments[1];

				if (arg.NodeType == ExpressionType.Constant || arg.NodeType == ExpressionType.Default)
				{
					var comparison = (StringComparison)(arg.EvaluateExpression() ?? throw new InvalidOperationException());
					return new SqlValue(comparison == StringComparison.CurrentCulture   ||
					                    comparison == StringComparison.InvariantCulture ||
					                    comparison == StringComparison.Ordinal);
				}

				var variable   = Expression.Variable(typeof(StringComparison), "c");
				var assignment = Expression.Assign(variable, arg);
				var expr       = (Expression)Expression.Equal(variable, Expression.Constant(StringComparison.CurrentCulture));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.InvariantCulture)));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.Ordinal)));
				expr = Expression.Block(new[] {variable}, assignment, expr);

				var parameter = ParametersContext.BuildParameter(expr, columnDescriptor: null, forceConstant: true);
				parameter.SqlParameter.IsQueryParameter = false;

				return parameter.SqlParameter;
			}

			switch (expression.NodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					var e = (BinaryExpression)expression;
					return ConvertCompare(context, expression.NodeType, e.Left, e.Right);
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expression;

					ISqlPredicate? predicate = null;

					if (e.Method.Name == "Equals" && e.Object != null && e.Arguments.Count == 1)
						return ConvertCompare(context, ExpressionType.Equal, e.Object, e.Arguments[0]);

					if (e.Method.DeclaringType == typeof(string))
					{
						switch (e.Method.Name)
						{
								case "Contains"   : predicate = CreateStringPredicate(context, e, SqlPredicate.SearchString.SearchKind.Contains,   IsCaseSensitive(e)); break;
								case "StartsWith" : predicate = CreateStringPredicate(context, e, SqlPredicate.SearchString.SearchKind.StartsWith, IsCaseSensitive(e)); break;
								case "EndsWith"   : predicate = CreateStringPredicate(context, e, SqlPredicate.SearchString.SearchKind.EndsWith,   IsCaseSensitive(e)); break;
						}
					}
					else if (e.Method.Name == "Contains")
					{
						if (e.Method.DeclaringType == typeof(Enumerable) ||
							typeof(IList).IsSameOrParentOf(e.Method.DeclaringType!) ||
							typeof(ICollection<>).IsSameOrParentOf(e.Method.DeclaringType!) ||
							typeof(IReadOnlyCollection<>).IsSameOrParentOf(e.Method.DeclaringType!))
						{
							predicate = ConvertInPredicate(context!, e);
						}
					}
					else if (e.Method.Name == "ContainsValue" && typeof(Dictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!))
					{
						var args = e.Method.DeclaringType!.GetGenericArguments(typeof(Dictionary<,>))!;
						var minf = EnumerableMethods
								.First(static m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[1]);

						var expr = Expression.Call(
								minf,
								ExpressionHelper.PropertyOrField(e.Object!, "Values"),
								e.Arguments[0]);

						predicate = ConvertInPredicate(context!, expr);
					}
					else if (e.Method.Name == "ContainsKey" && typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!))
					{
						var args = e.Method.DeclaringType!.GetGenericArguments(typeof(IDictionary<,>))!;
						var minf = EnumerableMethods
								.First(static m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[0]);

						var expr = Expression.Call(
								minf,
								ExpressionHelper.PropertyOrField(e.Object!, "Keys"),
								e.Arguments[0]);

						predicate = ConvertInPredicate(context!, expr);
					}
#if NETFRAMEWORK
					else if (e.Method == ReflectionHelper.Functions.String.Like11) predicate = ConvertLikePredicate(context!, e);
					else if (e.Method == ReflectionHelper.Functions.String.Like12) predicate = ConvertLikePredicate(context!, e);
#endif
					else if (e.Method == ReflectionHelper.Functions.String.Like21) predicate = ConvertLikePredicate(context!, e);
					else if (e.Method == ReflectionHelper.Functions.String.Like22) predicate = ConvertLikePredicate(context!, e);

					if (predicate != null)
						return predicate;

					var attr = GetExpressionAttribute(e.Method);

					if (attr != null && attr.GetIsPredicate(expression))
						break;

					break;
				}

				case ExpressionType.Conditional:
					return new SqlPredicate.ExprExpr(
							ConvertToSql(context, expression),
							SqlPredicate.Operator.Equal,
							new SqlValue(true), null);

				case ExpressionType.TypeIs:
				{
					var e   = (TypeBinaryExpression)expression;
					var ctx = GetContext(context, e.Expression);

					if (ctx != null && ctx.IsExpression(e.Expression, 0, RequestFor.Table).Result)
						return MakeIsPredicate(ctx, e);

					break;
				}

				case ExpressionType.Convert:
				{
					var e = (UnaryExpression)expression;

					if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
						return ConvertPredicate(context, e.Operand);

					break;
				}
			}

			var ex = ConvertToSql(context, expression);

			if (SqlExpression.NeedsEqual(ex))
			{
				var descriptor = QueryHelper.GetColumnDescriptor(ex);
				var trueValue  = ConvertToSql(context, ExpressionInstances.True,  false, descriptor);
				var falseValue = ConvertToSql(context, ExpressionInstances.False, false, descriptor);

				return new SqlPredicate.IsTrue(ex, trueValue, falseValue, Configuration.Linq.CompareNullsAsValues ? false : null, false);
			}

			return new SqlPredicate.Expr(ex);
		}

		#region ConvertCompare

		ISqlPredicate ConvertCompare(IBuildContext? context, ExpressionType nodeType, Expression left, Expression right)
		{
			if (!RestoreCompare(ref left, ref right))
				RestoreCompare(ref right, ref left);

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					var p = ConvertObjectComparison(nodeType, context!, left, context!, right);
					if (p != null)
						return p;

					p = ConvertObjectNullComparison(context, left, right, nodeType == ExpressionType.Equal);
					if (p != null)
						return p;

					p = ConvertObjectNullComparison(context, right, left, nodeType == ExpressionType.Equal);
					if (p != null)
						return p;

					if (left.NodeType == ExpressionType.New || right.NodeType == ExpressionType.New)
					{
						p = ConvertNewObjectComparison(context!, nodeType, left, right);
						if (p != null)
							return p;
					}

					break;
			}

			var op = nodeType switch
			{
				ExpressionType.Equal              => SqlPredicate.Operator.Equal,
				ExpressionType.NotEqual           => SqlPredicate.Operator.NotEqual,
				ExpressionType.GreaterThan        => SqlPredicate.Operator.Greater,
				ExpressionType.GreaterThanOrEqual => SqlPredicate.Operator.GreaterOrEqual,
				ExpressionType.LessThan           => SqlPredicate.Operator.Less,
				ExpressionType.LessThanOrEqual    => SqlPredicate.Operator.LessOrEqual,
				_                                 => throw new InvalidOperationException(),
			};
			if ((left.NodeType == ExpressionType.Convert || right.NodeType == ExpressionType.Convert) && (op == SqlPredicate.Operator.Equal || op == SqlPredicate.Operator.NotEqual))
			{
				var p = ConvertEnumConversion(context!, left, op, right);
				if (p != null)
					return p;
			}

			var cd = SuggestColumnDescriptor(context, left, right);
			var l  = ConvertToSql(context, left,  unwrap: false, cd);
			var r  = ConvertToSql(context, right, unwrap: true,  cd);

			l = QueryHelper.UnwrapExpression(l);
			r = QueryHelper.UnwrapExpression(r);

			if (l is SqlValue lValue)
				lValue.ValueType = GetDataType(r, lValue.ValueType);

			if (r is SqlValue rValue)
				rValue.ValueType = GetDataType(l, rValue.ValueType);

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					if (!context!.SelectQuery.IsParameterDependent &&
						(l is SqlParameter && l.CanBeNull || r is SqlParameter && r.CanBeNull))
						context.SelectQuery.IsParameterDependent = true;

					// | (SqlQuery(Select([]) as q), SqlValue(null))
					// | (SqlValue(null), SqlQuery(Select([]) as q))  =>

					var q =
						l.ElementType == QueryElementType.SqlQuery &&
						r.ElementType == QueryElementType.SqlValue &&
						((SqlValue)r).Value == null &&
						((SelectQuery)l).Select.Columns.Count == 0 ?
							(SelectQuery)l :
						r.ElementType == QueryElementType.SqlQuery &&
						l.ElementType == QueryElementType.SqlValue &&
						((SqlValue)l).Value == null &&
						((SelectQuery)r).Select.Columns.Count == 0 ?
							(SelectQuery)r :
							null;

					q?.Select.Columns.Add(new SqlColumn(q, new SqlValue(1)));

					break;
			}

			if (l is SqlSearchCondition)
			{
				l = new SqlFunction(typeof(bool), "CASE", l, new SqlValue(true), new SqlValue(false))
				{
					CanBeNull = false
				};
			}

			if (r is SqlSearchCondition)
			{
				r = new SqlFunction(typeof(bool), "CASE", r, new SqlValue(true), new SqlValue(false))
				{
					CanBeNull = false
				};
			}

			ISqlPredicate? predicate = null;
			if (op == SqlPredicate.Operator.Equal || op == SqlPredicate.Operator.NotEqual)
			{
				bool?           value      = null;
				ISqlExpression? expression = null;
				var             isNullable = false;
				if (IsBooleanConstant(left, out value))
				{
					isNullable = typeof(bool?) == left.Type || r.CanBeNull;
					expression = r;
				}
				else if (IsBooleanConstant(right, out value))
				{
					isNullable = typeof(bool?) == right.Type || l.CanBeNull;
					expression = l;
				}

				if (value != null
					&& expression != null
					&& !(expression.ElementType == QueryElementType.SqlValue && ((SqlValue)expression).Value == null))
				{
					var isNot = !value.Value;
					var withNull = false;
					if (op == SqlPredicate.Operator.NotEqual)
					{
						isNot = !isNot;
						withNull = true;
					}
					var descriptor = QueryHelper.GetColumnDescriptor(expression);
					var trueValue  = ConvertToSql(context, ExpressionInstances.True,  false, descriptor);
					var falseValue = ConvertToSql(context, ExpressionInstances.False, false, descriptor);

					var withNullValue = Configuration.Linq.CompareNullsAsValues &&
										(isNullable || NeedNullCheck(expression))
						? withNull
						: (bool?)null;
					predicate = new SqlPredicate.IsTrue(expression, trueValue, falseValue, withNullValue, isNot);
				}
			}

			if (predicate == null)
				predicate = new SqlPredicate.ExprExpr(l, op, r, Configuration.Linq.CompareNullsAsValues ? true : null);
			return predicate;
		}

		private static bool IsBooleanConstant(Expression expr, out bool? value)
		{
			value = null;
			if (expr.Type == typeof(bool) || expr.Type == typeof(bool?))
			{
				expr = expr.Unwrap();
				if (expr is ConstantExpression c)
				{
					value = c.Value as bool?;
					return true;
				}
				else if (expr is DefaultExpression)
				{
					value = expr.Type == typeof(bool) ? false : null;
					return true;
				}
			}
			return false;
		}

		// restores original types, lost due to C# compiler optimizations
		// e.g. see https://github.com/linq2db/linq2db/issues/2041
		private static bool RestoreCompare(ref Expression op1, ref Expression op2)
		{
			if (op1.NodeType == ExpressionType.Convert)
			{
				var op1conv = (UnaryExpression)op1;

				// handle char replaced with int
				// (int)chr op CONST
				if (op1.Type == typeof(int) && op1conv.Operand.Type == typeof(char)
					&& (op2.NodeType == ExpressionType.Constant || op2.NodeType == ExpressionType.Convert))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)op2).Operand;
					return true;
				}
				// (int?)chr? op CONST
				else if (op1.Type == typeof(int?) && op1conv.Operand.Type == typeof(char?)
					&& (op2.NodeType == ExpressionType.Constant
						|| (op2.NodeType == ExpressionType.Convert && ((UnaryExpression)op2).Operand.NodeType == ExpressionType.Convert)))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)((UnaryExpression)op2).Operand).Operand;
					return true;
				}
				// handle enum replaced with integer
				// here byte/short values replaced with int, int+ values replaced with actual underlying type
				// (int)enum op const
				else if (op1conv.Operand.Type.IsEnum
					&& op2.NodeType == ExpressionType.Constant
						&& (op2.Type == Enum.GetUnderlyingType(op1conv.Operand.Type) || op2.Type == typeof(int)))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(Enum.ToObject(op1conv.Operand.Type, ((ConstantExpression)op2).Value!), op1conv.Operand.Type);
					return true;
				}
				// here underlying type used
				// (int?)enum? op (int?)enum
				else if (op1conv.Operand.Type.IsNullable() && Nullable.GetUnderlyingType(op1conv.Operand.Type)!.IsEnum
					&& op2.NodeType == ExpressionType.Convert
					&& op2 is UnaryExpression op2conv2
					&& op2conv2.Operand.NodeType == ExpressionType.Constant
					&& op2conv2.Operand.Type == Nullable.GetUnderlyingType(op1conv.Operand.Type))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Convert(op2conv2.Operand, op1conv.Operand.Type);
					return true;
				}
				// https://github.com/linq2db/linq2db/issues/2039
				// byte, sbyte and ushort comparison operands upcasted to int
				else if (op2.NodeType == ExpressionType.Convert
					&& op2 is UnaryExpression op2conv1
					&& op1conv.Operand.Type == op2conv1.Operand.Type)
				{
					op1 = op1conv.Operand;
					op2 = op2conv1.Operand;
					return true;
				}

				// https://github.com/linq2db/linq2db/issues/2166
				// generates expression:
				// Convert(member, int) == const(value, int)
				// we must replace it with:
				// member == const(value, member_type)
				if (op2 is ConstantExpression const2
					&& const2.Type == typeof(int)
					&& ConvertUtils.TryConvert(const2.Value, op1conv.Operand.Type, out var convertedValue))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(convertedValue, op1conv.Operand.Type);
					return true;
				}
			}

			return false;
		}

		#endregion

		#region ConvertEnumConversion

		ISqlPredicate? ConvertEnumConversion(IBuildContext context, Expression left, SqlPredicate.Operator op, Expression right)
		{
			Expression value;
			Expression operand;

			if (left is MemberExpression)
			{
				operand = left;
				value   = right;
			}
			else if (left.NodeType == ExpressionType.Convert && ((UnaryExpression)left).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else if (right is MemberExpression)
			{
				operand = right;
				value   = left;
			}
			else if (right.NodeType == ExpressionType.Convert && ((UnaryExpression)right).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)right).Operand;
				value   = left;
			}
			else if (left.NodeType == ExpressionType.Convert)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else
			{
				operand = ((UnaryExpression)right).Operand;
				value = left;
			}

			var type = operand.Type;

			if (!type.ToNullableUnderlying().IsEnum)
				return null;

			var dic = new Dictionary<object, object?>();

			var mapValues = MappingSchema.GetMapValues(type);

			if (mapValues != null)
				foreach (var mv in mapValues)
					if (!dic.ContainsKey(mv.OrigValue))
						dic.Add(mv.OrigValue, mv.MapValues[0].Value);

			switch (value.NodeType)
			{
				case ExpressionType.Constant:
				{
					var name = Enum.GetName(type, ((ConstantExpression)value).Value!);

					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					// ReSharper disable HeuristicUnreachableCode
					if (name == null)
						return null;
					// ReSharper restore HeuristicUnreachableCode
					// ReSharper restore ConditionIsAlwaysTrueOrFalse

					var origValue = Enum.Parse(type, name, false);

					if (!dic.TryGetValue(origValue, out var mapValue))
						mapValue = origValue;

					ISqlExpression l, r;

					SqlValue sqlvalue;
					var ce = MappingSchema.GetConverter(new DbDataType(type), new DbDataType(typeof(DataParameter)), false);

					if (ce != null)
					{
						sqlvalue = new SqlValue(ce.ConvertValueToParameter(origValue).Value!);
					}
					else
					{
						sqlvalue = MappingSchema.GetSqlValue(type, mapValue);
					}

					if (left.NodeType == ExpressionType.Convert)
					{
						l = ConvertToSql(context, operand);
						r = sqlvalue;
					}
					else
					{
						r = ConvertToSql(context, operand);
						l = sqlvalue;
					}

					return new SqlPredicate.ExprExpr(l, op, r, true);
				}

				case ExpressionType.Convert:
				{
					value = ((UnaryExpression)value).Operand;

					var cd = SuggestColumnDescriptor(context, operand, value);

					var l = ConvertToSql(context, operand, columnDescriptor: cd);
					var r = ConvertToSql(context, value, columnDescriptor: cd);

					return new SqlPredicate.ExprExpr(l, op, r, true);
				}
			}

			return null;
		}

		#endregion

		#region ConvertObjectNullComparison

		ISqlPredicate? ConvertObjectNullComparison(IBuildContext? context, Expression left, Expression right, bool isEqual)
		{
			if (right.IsNullValue())
			{
				if (left.NodeType == ExpressionType.MemberAccess || left.NodeType == ExpressionType.Parameter)
				{
					var ctx = GetContext(context, left);

					if (ctx != null && ctx.IsExpression(left, 0, RequestFor.Object).Result)
					{
						return new SqlPredicate.Expr(new SqlValue(!isEqual));
					}
				}
			}

			return null;
		}

		#endregion

		#region ConvertObjectComparison

		static Expression? ConstructMemberPath(MemberInfo[] memberPath, Expression ob, bool throwOnError)
		{
			Expression result = ob;
			foreach (var memberInfo in memberPath)
			{
				if (memberInfo.DeclaringType!.IsAssignableFrom(result.Type))
				{
					result = Expression.MakeMemberAccess(result, memberInfo);
				}
			}

			if (ReferenceEquals(result, ob) && throwOnError)
				throw new LinqToDBException($"Type {result.Type.Name} does not have member {memberPath.Last().Name}.");

			return result;
		}

		public ISqlPredicate? ConvertObjectComparison(
			ExpressionType nodeType,
			IBuildContext leftContext,
			Expression left,
			IBuildContext rightContext,
			Expression right)
		{
			var qsl = GetContext(leftContext,  left);
			var qsr = GetContext(rightContext, right);

			var sl = qsl != null && qsl.IsExpression(left,  0, RequestFor.Object).Result;
			var sr = qsr != null && qsr.IsExpression(right, 0, RequestFor.Object).Result;

			bool      isNull;
			SqlInfo[] lcols;

			var rmembers = new Dictionary<MemberInfo,Expression>(new MemberInfoComparer());

			if (sl == false && sr == false)
			{
				var lmembers = new Dictionary<MemberInfo,Expression>(new MemberInfoComparer());

				var isl = ProcessProjection(lmembers, left);
				var isr = ProcessProjection(rmembers, right);

				if (!isl && !isr)
					return null;

				if (lmembers.Count == 0)
				{
					(left, right)               = (right, left);
					(leftContext, rightContext) = (rightContext, leftContext);
					var q                       = qsr;
					qsl                         = q;

					sr = false;

					(rmembers, lmembers) = (lmembers, rmembers);
				}

				isNull = right.IsNullValue();
				lcols  = new SqlInfo[lmembers.Count];
				var idx = 0;
				foreach (var m in lmembers)
				{
					lcols[idx] = new SqlInfo(m.Key, ConvertToSql(leftContext, m.Value));
					idx++;
				}
			}
			else
			{
				if (sl == false)
				{
					(left, right)               = (right, left);
					// leftContext value not used below: https://pvs-studio.com/ru/blog/posts/csharp/0887/
					// but! we have test that fails in this place (TestDefaultExpression_08)
					// so it could be incomplete implementation
					(leftContext, rightContext) = (rightContext, leftContext);

					var q = qsr;
					qsl   = q;

					sr = false;
				}

				isNull = right.IsNullValue();
				lcols  = qsl!.ConvertToSql(left, 0, ConvertFlags.Key);

				if (!sr)
					ProcessProjection(rmembers, right);
			}

			if (lcols.Length == 0)
				return null;

			var condition = new SqlSearchCondition();

			foreach (var lcol in lcols)
			{
				if (lcol.Sql is SelectQuery innerQuery && isNull)
				{
					var existsPredicate = new SqlPredicate.FuncLike(SqlFunction.CreateExists(innerQuery));
					condition.Conditions.Add(new SqlCondition(nodeType == ExpressionType.Equal, existsPredicate));
					continue;
				}

				if (lcol.MemberChain.Length == 0)
					throw new InvalidOperationException();

				ISqlExpression? rcol = null;

				var lmember = lcol.MemberChain[lcol.MemberChain.Length - 1];

				var columnDescriptor = QueryHelper.GetColumnDescriptor(lcol.Sql);

				if (sr)
				{
					var memeberPath = ConstructMemberPath(lcol.MemberChain, right, true)!;
					rcol = ConvertToSql(rightContext, memeberPath, unwrap: false, columnDescriptor);
				}
				else if (rmembers.Count != 0)
					rcol = ConvertToSql(rightContext, rmembers[lmember], unwrap: false, columnDescriptor);

				var rex =
					isNull ?
						MappingSchema.GetSqlValue(right.Type, null) :
						rcol ?? ParametersContext.GetParameter(right, lmember, columnDescriptor);

				var predicate = new SqlPredicate.ExprExpr(
					lcol.Sql,
					nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
					rex, Configuration.Linq.CompareNullsAsValues ? true : null);

				condition.Conditions.Add(new SqlCondition(false, predicate));
			}

			if (nodeType == ExpressionType.NotEqual)
				foreach (var c in condition.Conditions)
					c.IsOr = true;

			return condition;
		}

		internal ISqlPredicate? ConvertNewObjectComparison(IBuildContext context, ExpressionType nodeType, Expression left, Expression right)
		{
			left  = FindExpression(left);
			right = FindExpression(right);

			var condition = new SqlSearchCondition();

			if (left.NodeType != ExpressionType.New)
			{
				(right, left) = (left, right);
			}

			var newExpr  = (NewExpression)left;

			// ReSharper disable ConditionIsAlwaysTrueOrFalse
			// ReSharper disable HeuristicUnreachableCode
			if (newExpr.Members == null)
				return null;
			// ReSharper restore HeuristicUnreachableCode
			// ReSharper restore ConditionIsAlwaysTrueOrFalse

			for (var i = 0; i < newExpr.Arguments.Count; i++)
			{
				var lex = ConvertToSql(context, newExpr.Arguments[i]);
				var rex =
					right is NewExpression newRight ?
						ConvertToSql(context, newRight.Arguments[i]) :
						ParametersContext.GetParameter(right, newExpr.Members[i], QueryHelper.GetColumnDescriptor(lex));

				var predicate =
					new SqlPredicate.ExprExpr(
						lex,
						nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
						rex, Configuration.Linq.CompareNullsAsValues ? true : null);

				condition.Conditions.Add(new SqlCondition(false, predicate));
			}

			if (nodeType == ExpressionType.NotEqual)
				foreach (var c in condition.Conditions)
					c.IsOr = true;

			return condition;
		}

		static Expression FindExpression(Expression expr)
		{
			var ret = _findExpressionVisitor.Find(expr);

			if (ret == null)
				throw new NotImplementedException();

			return ret;
		}

		private static readonly FindVisitor<object?> _findExpressionVisitor = FindVisitor<object?>.Create(FindExpressionFind);

		static bool FindExpressionFind(Expression pi)
		{
			switch (pi.NodeType)
			{
				case ExpressionType.Convert:
				{
					var e = (UnaryExpression)pi;

					return
						e.Operand.NodeType == ExpressionType.ArrayIndex &&
						ReferenceEquals(((BinaryExpression)e.Operand).Left, ParametersParam);
				}
				case ExpressionType.MemberAccess:
				case ExpressionType.New         :
					return true;
			}

			return false;
		}

		#endregion

		#region Parameters

		public static DbDataType GetMemberDataType(MappingSchema mappingSchema, MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());

			var dta = mappingSchema.GetAttribute<DataTypeAttribute>(member.ReflectedType!, member);
			var ca  = mappingSchema.GetAttribute<ColumnAttribute>  (member.ReflectedType!, member);

			var dataType = ca?.DataType ?? dta?.DataType;

			if (dataType != null)
				typeResult = typeResult.WithDataType(dataType.Value);

			var dbType = ca?.DbType ?? dta?.DbType;
			if (dbType != null)
				typeResult = typeResult.WithDbType(dbType);

			if (ca != null && ca.HasLength())
				typeResult = typeResult.WithLength(ca.Length);

			return typeResult;
		}

		private class GetDataTypeContext
		{
			public GetDataTypeContext(DbDataType baseType)
			{
				DataType   = baseType.DataType;
				DbType     = baseType.DbType;
				Length     = baseType.Length;
				Precision  = baseType.Precision;
				Scale      = baseType.Scale;
			}

			public DataType DataType;
			public string?  DbType;
			public int?     Length;
			public int?     Precision;
			public int?     Scale;
		}

		static DbDataType GetDataType(ISqlExpression expr, DbDataType baseType)
		{
			var ctx = new GetDataTypeContext(baseType);

			expr.Find(ctx, static (context, e) =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField:
						{
							var fld = (SqlField)e;
							context.DataType     = fld.Type.DataType;
							context.DbType       = fld.Type.DbType;
							context.Length       = fld.Type.Length;
							context.Precision    = fld.Type.Precision;
							context.Scale        = fld.Type.Scale;
							return true;
						}
					case QueryElementType.SqlParameter:
						context.DataType     = ((SqlParameter)e).Type.DataType;
						context.DbType       = ((SqlParameter)e).Type.DbType;
						context.Length       = ((SqlParameter)e).Type.Length;
						context.Precision    = ((SqlParameter)e).Type.Precision;
						context.Scale        = ((SqlParameter)e).Type.Scale;
						return true;
					case QueryElementType.SqlDataType:
						context.DataType     = ((SqlDataType)e).Type.DataType;
						context.DbType       = ((SqlDataType)e).Type.DbType;
						context.Length       = ((SqlDataType)e).Type.Length;
						context.Precision    = ((SqlDataType)e).Type.Precision;
						context.Scale        = ((SqlDataType)e).Type.Scale;
						return true;
					case QueryElementType.SqlValue:
						context.DataType     = ((SqlValue)e).ValueType.DataType;
						context.DbType       = ((SqlValue)e).ValueType.DbType;
						context.Length      = ((SqlValue)e).ValueType.Length;
						context.Precision    = ((SqlValue)e).ValueType.Precision;
						context.Scale        = ((SqlValue)e).ValueType.Scale;
						return true;
					default:
						return false;
				}
			});

			return new DbDataType(
				baseType.SystemType,
				ctx.DataType == DataType.Undefined ? baseType.DataType : ctx.DataType,
				string.IsNullOrEmpty(ctx.DbType)   ? baseType.DbType   : ctx.DbType,
				ctx.Length     ?? baseType.Length,
				ctx.Precision  ?? baseType.Precision,
				ctx.Scale      ?? baseType.Scale
			);
		}

		#endregion

		#region ConvertInPredicate

		private ISqlPredicate ConvertInPredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e        = expression;
			var argIndex = e.Object != null ? 0 : 1;
			var arr      = e.Object ?? e.Arguments[0];
			var arg      = e.Arguments[argIndex];

			ISqlExpression? expr = null;

			var ctx = GetContext(context, arg);

			if (ctx is TableBuilder.TableContext &&
				ctx.SelectQuery != context.SelectQuery &&
				ctx.IsExpression(arg, 0, RequestFor.Object).Result)
			{
				expr = ctx.SelectQuery;
			}

			if (expr == null)
			{
				var sql = ConvertExpressions(context, arg, ConvertFlags.Key, null);

				if (sql.Length == 1 && sql[0].MemberChain.Length == 0)
					expr = sql[0].Sql;
				else
					expr = new SqlObjectExpression(MappingSchema, sql);
			}

			var columnDescriptor = QueryHelper.GetColumnDescriptor(expr);

			switch (arr.NodeType)
			{
				case ExpressionType.NewArrayInit :
					{
						var newArr = (NewArrayExpression)arr;

						if (newArr.Expressions.Count == 0)
							return new SqlPredicate.Expr(new SqlValue(false));

						var exprs  = new ISqlExpression[newArr.Expressions.Count];

						for (var i = 0; i < newArr.Expressions.Count; i++)
							exprs[i] = ConvertToSql(context, newArr.Expressions[i], columnDescriptor: columnDescriptor);

						return new SqlPredicate.InList(expr, Configuration.Linq.CompareNullsAsValues ? false : null, false, exprs);
					}

				default :

					if (CanBeCompiled(arr))
					{
						var p = ParametersContext.BuildParameter(arr, columnDescriptor, false, ParametersContext.BuildParameterType.InPredicate).SqlParameter;
						p.IsQueryParameter = false;
						return new SqlPredicate.InList(expr, Configuration.Linq.CompareNullsAsValues ? false : null, false, p);
					}

					break;
			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		#endregion

		#region ColumnDescriptor Helpers

		public ColumnDescriptor? SuggestColumnDescriptor(IBuildContext? context, Expression expr)
		{
			expr = expr.Unwrap();
			var ctx = GetContext(context, expr);
			if (ctx != null && ctx.IsExpression(expr, 0, RequestFor.Field).Result)
			{
				var descriptor = QueryHelper.GetColumnDescriptor(ConvertToSql(context, expr));
				if (descriptor != null)
				{
					return descriptor;
				}
			}

			return null;
		}

		public ColumnDescriptor? SuggestColumnDescriptor(IBuildContext? context, Expression expr1, Expression expr2)
		{
			return SuggestColumnDescriptor(context, expr1) ?? SuggestColumnDescriptor(context, expr2);
		}

		public ColumnDescriptor? SuggestColumnDescriptor(IBuildContext? context, ReadOnlyCollection<Expression> expressions)
		{
			foreach (var expr in expressions)
			{
				var descriptor = SuggestColumnDescriptor(context, expr);
				if (descriptor != null)
					return descriptor;
			}

			return null;
		}
	

		#endregion

		#region LIKE predicate

		ISqlPredicate CreateStringPredicate(IBuildContext? context, MethodCallExpression expression, SqlPredicate.SearchString.SearchKind kind, ISqlExpression caseSensitive)
		{
			var e = expression;

			var descriptor = SuggestColumnDescriptor(context, e.Object!, e.Arguments[0]);

			var o = ConvertToSql(context, e.Object!,      unwrap: false, descriptor);
			var a = ConvertToSql(context, e.Arguments[0], unwrap: false, descriptor);

			return new SqlPredicate.SearchString(o, false, a, kind, caseSensitive);
		}

		ISqlPredicate ConvertLikePredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e  = expression;

			var descriptor = SuggestColumnDescriptor(context, e.Arguments);

			var a1 = ConvertToSql(context, e.Arguments[0], unwrap: false, descriptor);
			var a2 = ConvertToSql(context, e.Arguments[1], unwrap: false, descriptor);

			ISqlExpression? a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2], unwrap: false, descriptor);

			return new SqlPredicate.Like(a1, false, a2, a3);
		}

		#endregion

		#region MakeIsPredicate

		internal ISqlPredicate MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
		{
			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return new SqlPredicate.Expr(new SqlValue(true));
			}

			return MakeIsPredicate(table, table, table.InheritanceMapping, typeOperand, static (table, name) => table.SqlTable[name] ?? throw new LinqException($"Field {name} not found in table {table.SqlTable}"));
		}

		internal ISqlPredicate MakeIsPredicate<TContext>(
			TContext                              getSqlContext,
			IBuildContext                         context,
			List<InheritanceMapping>              inheritanceMapping,
			Type                                  toType,
			Func<TContext,string, ISqlExpression> getSql)
		{
			var mapping = new List<InheritanceMapping>(inheritanceMapping.Count);
			foreach (var m in inheritanceMapping)
				if (m.Type == toType && !m.IsDefault)
					mapping.Add(m);

			switch (mapping.Count)
			{
				case 0 :
					{
						var cond = new SqlSearchCondition();

						var found = false;
						foreach (var m in inheritanceMapping)
						{
							if (m.Type == toType)
							{
								found = true;
								break;
							}
						}

						if (found)
						{
							foreach (var m in inheritanceMapping.Where(static m => !m.IsDefault))
							{
								cond.Conditions.Add(
									new SqlCondition(
										false,
											new SqlPredicate.ExprExpr(
												getSql(getSqlContext, m.DiscriminatorName),
												SqlPredicate.Operator.NotEqual,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code), Configuration.Linq.CompareNullsAsValues ? true : null)));
							}
						}
						else
						{
							foreach (var m in inheritanceMapping)
							{
								if (toType.IsSameOrParentOf(m.Type))
								{
									cond.Conditions.Add(
										new SqlCondition(
											false,
												new SqlPredicate.ExprExpr(
													getSql(getSqlContext, m.DiscriminatorName),
													SqlPredicate.Operator.Equal,
													MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code), Configuration.Linq.CompareNullsAsValues ? true : null),
											true));
								}
							}
						}

						return cond;
					}

				case 1 :
					return new SqlPredicate.ExprExpr(
							getSql(getSqlContext, mapping[0].DiscriminatorName),
							SqlPredicate.Operator.Equal,
							MappingSchema.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code), Configuration.Linq.CompareNullsAsValues ? true : null);

				default:
					{
						var cond = new SqlSearchCondition();

						foreach (var m in mapping)
						{
							cond.Conditions.Add(
								new SqlCondition(
									false,
										new SqlPredicate.ExprExpr(
											getSql(getSqlContext, m.DiscriminatorName),
											SqlPredicate.Operator.Equal,
											MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code), Configuration.Linq.CompareNullsAsValues ? true : null),
									true));
						}

						return cond;
					}
			}
		}

		ISqlPredicate MakeIsPredicate(IBuildContext context, TypeBinaryExpression expression)
		{
			var typeOperand = expression.TypeOperand;
			var table       = new TableBuilder.TableContext(this, new BuildInfo((IBuildContext?)null, ExpressionInstances.UntypedNull, new SelectQuery()), typeOperand);

			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return new SqlPredicate.Expr(new SqlValue(true));
			}

			var mapping = new List<(InheritanceMapping m, int i)>(table.InheritanceMapping.Count);

			for (var i = 0; i < table.InheritanceMapping.Count; i++)
			{
				var m = table.InheritanceMapping[i];
				if (typeOperand.IsAssignableFrom(m.Type) && !m.IsDefault)
					mapping.Add((m, i));
			}

			var isEqual = true;

			if (mapping.Count == 0)
			{
				for (var i = 0; i < table.InheritanceMapping.Count; i++)
				{
					var m = table.InheritanceMapping[i];
					if (!m.IsDefault)
						mapping.Add((m, i));
				}

				isEqual = false;
			}

			Expression? expr = null;

			foreach (var m in mapping)
			{
				var field = table.SqlTable[table.InheritanceMapping[m.i].DiscriminatorName] ?? throw new LinqException($"Field {table.InheritanceMapping[m.i].DiscriminatorName} not found in table {table.SqlTable}");
				var ttype = field.ColumnDescriptor.MemberAccessor.TypeAccessor.Type;
				var obj   = expression.Expression;

				if (obj.Type != ttype)
					obj = Expression.Convert(expression.Expression, ttype);

				var left = ExpressionHelper.PropertyOrField(obj, field.Name);
				var code = m.m.Code;

				if (code == null)
					code = left.Type.GetDefaultValue();
				else if (left.Type != code.GetType())
					code = Converter.ChangeType(code, left.Type, MappingSchema);

				Expression right = Expression.Constant(code, left.Type);

				var e = isEqual ? Expression.Equal(left, right) : Expression.NotEqual(left, right);

				if (!isEqual)
					expr = expr != null ? Expression.AndAlso(expr, e) : e;
				else
					expr = expr != null ? Expression.OrElse(expr, e) : e;
			}

			return ConvertPredicate(context, expr!);
		}

		#endregion

		#endregion

		#region Search Condition Builder

		internal void BuildSearchCondition(IBuildContext? context, Expression expression, List<SqlCondition> conditions)
		{
			expression = GetRemoveNullPropagationTransformer(true).Transform(expression);

			switch (expression.NodeType)
			{
				case ExpressionType.And     :
				case ExpressionType.AndAlso :
					{
						var e = (BinaryExpression)expression;

						BuildSearchCondition(context, e.Left,  conditions);
						BuildSearchCondition(context, e.Right, conditions);

						break;
					}

				case ExpressionType.Extension :
					{
						break;
					}

				case ExpressionType.Or     :
				case ExpressionType.OrElse :
					{
						var e           = (BinaryExpression)expression;
						var orCondition = new SqlSearchCondition();

						BuildSearchCondition(context, e.Left,  orCondition.Conditions);
						orCondition.Conditions[orCondition.Conditions.Count - 1].IsOr = true;
						BuildSearchCondition(context, e.Right, orCondition.Conditions);

						conditions.Add(new SqlCondition(false, orCondition));

						break;
					}

				case ExpressionType.Not    :
					{
						var e            = (UnaryExpression)expression;
						var notCondition = new SqlSearchCondition();

						BuildSearchCondition(context, e.Operand, notCondition.Conditions);

						conditions.Add(new SqlCondition(true, notCondition));

						break;
					}

				default                    :
					var predicate = ConvertPredicate(context, expression);

					conditions.Add(new SqlCondition(false, predicate));

					break;
			}
		}


		static bool NeedNullCheck(ISqlExpression expr)
		{
			if (!expr.CanBeNull)
				return false;

			if (null != expr.Find(QueryElementType.SelectClause))
				return false;
			return true;
		}

		#endregion

		#region CanBeTranslatedToSql

		private class CanBeTranslatedToSqlContext
		{
			public CanBeTranslatedToSqlContext(ExpressionBuilder builder, IBuildContext buildContext, bool canBeCompiled)
			{
				Builder       = builder;
				BuildContext  = buildContext;
				CanBeCompiled = canBeCompiled;
			}

			public readonly ExpressionBuilder Builder;
			public readonly IBuildContext     BuildContext;
			public readonly bool              CanBeCompiled;

			public List<Expression>? IgnoredMembers;
		}

		bool CanBeTranslatedToSql(IBuildContext context, Expression expr, bool canBeCompiled)
		{
			var ctx = new CanBeTranslatedToSqlContext(this, context, canBeCompiled);

			return null == expr.Find(ctx, static (context, pi) =>
			{
				if (context.IgnoredMembers != null)
				{
					if (pi != context.IgnoredMembers[context.IgnoredMembers.Count - 1])
						throw new InvalidOperationException();

					if (context.IgnoredMembers.Count == 1)
						context.IgnoredMembers = null;
					else
						context.IgnoredMembers.RemoveAt(context.IgnoredMembers.Count - 1);

					return false;
				}

				switch (pi.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var ma   = (MemberExpression)pi;
							var attr = context.Builder.GetExpressionAttribute(ma.Member);

							if (attr == null && !ma.Member.IsNullableValueMember())
							{
								if (context.CanBeCompiled)
								{
									var ctx = context.Builder.GetContext(context.BuildContext, pi);

									if (ctx == null)
										return !context.Builder.CanBeCompiled(pi);

									if (ctx.IsExpression(pi, 0, RequestFor.Object).Result)
										return !context.Builder.CanBeCompiled(pi);

									context.IgnoredMembers = ma.Expression.GetMembers();
								}
							}

							break;
						}

					case ExpressionType.Parameter    :
						{
							var ctx = context.Builder.GetContext(context.BuildContext, pi);

							if (ctx == null)
								if (context.CanBeCompiled)
									return !context.Builder.CanBeCompiled(pi);

							break;
						}

					case ExpressionType.Call         :
						{
							var e = (MethodCallExpression)pi;

							if (e.Method.DeclaringType != typeof(Enumerable))
							{
								var attr = context.Builder.GetExpressionAttribute(e.Method);

								if (attr == null && context.CanBeCompiled)
									return !context.Builder.CanBeCompiled(pi);
							}

							break;
						}

					case ExpressionType.TypeIs       : return context.CanBeCompiled;
					case ExpressionType.TypeAs       :
					case ExpressionType.New          : return true;

					case ExpressionType.NotEqual     :
					case ExpressionType.Equal        :
						{
							var e = (BinaryExpression)pi;

							Expression? obj = null;

							if (e.Left.IsNullValue())
								obj = e.Right;
							else if (e.Right.IsNullValue())
								obj = e.Left;

							if (obj != null)
							{
								var ctx = context.Builder.GetContext(context.BuildContext, obj);

								if (ctx != null)
								{
									if (ctx.IsExpression(obj, 0, RequestFor.Table).      Result ||
										ctx.IsExpression(obj, 0, RequestFor.Association).Result)
									{
										context.IgnoredMembers = obj.GetMembers();
									}
								}
							}

							break;
						}

					case ExpressionType.Conditional:
						{
							var cond = (ConditionalExpression)pi;
							if (!cond.Type.IsScalar())
								return true;
							break;
						}
				}

				return false;
			});
		}

		#endregion

		#region Helpers

		public IBuildContext? GetContext(IBuildContext? current, Expression? expression)
		{
			var root = GetRootObject(expression);
			root = root.Unwrap();

			if (root is ContextRefExpression refExpression)
				return refExpression.BuildContext;

			for (; current != null; current = current.Parent)
				if (current.IsExpression(root, 0, RequestFor.Root).Result)
					return current;

			return null;
		}

		Sql.ExpressionAttribute? GetExpressionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedType!, member, static a => a.Configuration);
		}

		internal Sql.TableFunctionAttribute? GetTableFunctionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member.ReflectedType!, member, static a => a.Configuration);
		}

		bool IsNullConstant(Expression expr)
		{
			// TODO: is it correct to return true for DefaultValueExpression for non-reference type or when default value
			// set to non-null value?
			return expr.IsNullValue()
				|| expr is DefaultValueExpression;
		}

		private TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformer;
		private TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformerForSearch;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TransformVisitor<ExpressionBuilder> GetRemoveNullPropagationTransformer(bool forSearch)
		{
			if (forSearch)
				return _removeNullPropagationTransformerForSearch ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, true));
			else
				return _removeNullPropagationTransformer ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, false));
		}

		Expression RemoveNullPropagation(Expression expr, bool forSearch)
		{
			bool IsAcceptableType(Type type)
			{
				if (!forSearch)
					return type.IsNullableType();

				if (type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type))
					return true;

				if (!MappingSchema.IsScalarType(type))
					return true;

				return false;
			}

			// Do not modify parameters
			//
			if (CanBeCompiled(expr))
				return expr;

			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
							else if (IsNullConstant(conditional.IfFalse)
								&& ((nullRight && IsAcceptableType(binary.Left.Type) ||
									(nullLeft  && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
							else if (IsNullConstant(conditional.IfTrue)
							         && ((nullRight && IsAcceptableType(binary.Left.Type) ||
							              (nullLeft && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
						}
					}
					break;
			}

			return expr;
		}

		public bool ProcessProjection(Dictionary<MemberInfo,Expression> members, Expression expression)
		{
			void CollectParameters(Type forType, MethodBase method, ReadOnlyCollection<Expression> arguments)
			{
				var pms = method.GetParameters();

				var typeMembers = TypeAccessor.GetAccessor(forType).Members;

				for (var i = 0; i < pms.Length; i++)
				{
					var param = pms[i];
					MemberAccessor? foundMember = null;
					foreach (var tm in typeMembers)
					{
						if (tm.Name == param.Name)
						{
							foundMember = tm;
							break;
						}
					}

					if (foundMember == null)
					{
						foreach (var tm in typeMembers)
						{
							if (tm.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
							{
								foundMember = tm;
								break;
							}
						}
					}

					if (foundMember == null)
						continue;

					if (members.ContainsKey(foundMember.MemberInfo))
						continue;

					var converted = arguments[i];

					members.Add(foundMember.MemberInfo, converted);
				}
			}

			expression = GetRemoveNullPropagationTransformer(false).Transform(expression);

			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

						if (expr.Members != null)
						{
							for (var i = 0; i < expr.Members.Count; i++)
							{
								var member = expr.Members[i];

								var converted = expr.Arguments[i];
								members.Add(member, converted);

								if (member is MethodInfo info)
									members.Add(info.GetPropertyInfo(), converted);
							}
						}

						var isScalar = MappingSchema.IsScalarType(expr.Type);
						if (!isScalar)
							CollectParameters(expr.Type, expr.Constructor!, expr.Arguments);

						return members.Count > 0 || !isScalar;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr        = (MemberInitExpression)expression;
						var typeMembers = TypeAccessor.GetAccessor(expr.Type).Members;

						var dic  = typeMembers
							.Select(static (m,i) => new { m, i })
							.ToDictionary(static _ => _.m.MemberInfo.Name, static _ => _.i);

						var assignments = new List<(MemberAssignment ma, int order)>();
						foreach (var ma in expr.Bindings.Cast<MemberAssignment>())
							assignments.Add((ma, dic.ContainsKey(ma.Member.Name) ? dic[ma.Member.Name] : 1000000));

						foreach (var (binding, _) in assignments.OrderBy(static a => a.order))
						{
							var converted = binding.Expression;
							members.Add(binding.Member, converted);

							if (binding.Member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), converted);
						}

						return true;
					}

				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expression;

						// process fabric methods

						if (!MappingSchema.IsScalarType(mc.Type))
							CollectParameters(mc.Type, mc.Method, mc.Arguments);

						return members.Count > 0;
					}

				case ExpressionType.NewArrayInit:
				case ExpressionType.ListInit:
					{
						return true;
					}
				// .Select(p => everything else)
				//
				default                        :
					return false;
			}
		}

		public void ReplaceParent(IBuildContext oldParent, IBuildContext? newParent)
		{
			foreach (var context in Contexts)
				if (context != newParent)
					if (context.Parent == oldParent)
						if (newParent != null && newParent.Parent != context)
							context.Parent = newParent;
		}

		public static void EnsureAggregateColumns(IBuildContext context, SelectQuery query)
		{
			if (query.Select.Columns.Count == 0)
			{
				var sql = context.ConvertToSql(null, 0, ConvertFlags.All);
				if (sql.Length > 0)
				{
					// Handling case when all columns are aggregates, it cause query to produce only single record and we have to include at least one aggregation in Select statement.
					// 
					var allAggregate = sql.All(static s => QueryHelper.IsAggregationOrWindowFunction(s.Sql));
					if (allAggregate)
					{
						query.Select.Add(sql[0].Sql, sql[0].MemberChain.FirstOrDefault()?.Name);
					}
				}
			}
		}


		#endregion

		#region CTE

		List<Tuple<Expression, Tuple<CteClause, IBuildContext?>>>? _ctes;
		Dictionary<IQueryable, Expression>?                        _ctesObjectMapping;

		public Tuple<CteClause, IBuildContext?, Expression> RegisterCte(IQueryable? queryable, Expression? cteExpression, Func<CteClause> buildFunc)
		{
			if (cteExpression != null && queryable != null && (_ctesObjectMapping == null || !_ctesObjectMapping.ContainsKey(queryable)))
			{
				_ctesObjectMapping ??= new Dictionary<IQueryable, Expression>();

				_ctesObjectMapping.Add(queryable, cteExpression);
			}

			if (cteExpression == null)
			{
				if (_ctesObjectMapping == null)
					throw new InvalidOperationException();
				cteExpression = _ctesObjectMapping[queryable!];
			}

			var value = FindRegisteredCteByExpression(cteExpression, out _);

			if (value == null)
			{
				var cte = buildFunc();
				value = Tuple.Create<CteClause, IBuildContext?>(cte, null);

				_ctes ??= new List<Tuple<Expression, Tuple<CteClause, IBuildContext?>>>();
				_ctes.Add(Tuple.Create(cteExpression, value));
			}

			return Tuple.Create(value.Item1, value.Item2, cteExpression);
		}

		Tuple<CteClause, IBuildContext?>? FindRegisteredCteByExpression(Expression cteExpression, out int? idx)
		{
			if (_ctes != null)
			{
				for (var index = 0; index < _ctes.Count; index++)
				{
					var tuple = _ctes[index];
					if (tuple.Item1.EqualsTo(cteExpression, OptimizationContext.GetSimpleEqualsToContext(false)))
					{
						idx = index;
						return tuple.Item2;
					}
				}
			}

			idx = null;
			return null;
		}
		

		public Tuple<CteClause, IBuildContext?> BuildCte(Expression cteExpression, Func<CteClause?, Tuple<CteClause, IBuildContext?>> buildFunc)
		{
			var value = FindRegisteredCteByExpression(cteExpression, out var idx);
			if (value?.Item2 != null)
				return value;

			value = buildFunc(value?.Item1);

			if (idx != null)
			{
				_ctes!.RemoveAt(idx.Value);
			}
			else
			{
				_ctes ??= new List<Tuple<Expression, Tuple<CteClause, IBuildContext?>>>();
			}

			_ctes.Add(Tuple.Create(cteExpression, value));

			return value;
		}

		public IBuildContext? GetCteContext(Expression cteExpression)
		{
			return FindRegisteredCteByExpression(cteExpression, out _)?.Item2;
		}

		#endregion

		#region Eager Loading

		private List<Tuple<
			object?,
			Func<object?, IDataContext, Expression, object?[]?, object?>,
			Func<object?, IDataContext, Expression, object?[]?, CancellationToken, Task<object?>>>>? _preambles;

		public static readonly ParameterExpression PreambleParam =
			Expression.Parameter(typeof(object[]), "preamble");

		public int RegisterPreamble<T>(
			object? data,
			Func<object?, IDataContext, Expression, object?[]?, T> func,
			Func<object?, IDataContext, Expression, object?[]?, CancellationToken, Task<T>> funcAsync
			)
		{
			_preambles ??= new();
			_preambles.Add(
				Tuple.Create<object?, 
					Func<object?, IDataContext, Expression, object?[]?, object?>, 
					Func<object?, IDataContext, Expression, object?[]?, CancellationToken, Task<object?>>
				>
				(
					data,
					(d, dc, e, ps) => func(d, dc, e, ps),
					async (d, dc, e, ps, ct) => await funcAsync(d, dc, e, ps, ct).ConfigureAwait(Configuration.ContinueOnCapturedContext))
				);
			return _preambles.Count - 1;
		}

		#endregion

		#region Query Filter

		private Stack<Type[]>? _disabledFilters;

		public void AddDisabledQueryFilters(Type[] disabledFilters)
		{
			if (_disabledFilters == null)
				_disabledFilters = new Stack<Type[]>();
			_disabledFilters.Push(disabledFilters);
		}

		public bool IsFilterDisabled(Type entityType)
		{
			if (_disabledFilters == null || _disabledFilters.Count == 0)
				return false;
			var filter = _disabledFilters.Peek();
			if (filter.Length == 0)
				return true;
			return Array.IndexOf(filter, entityType) >= 0;
		}

		public void RemoveDisabledFilter()
		{
			if (_disabledFilters == null)
				throw new InvalidOperationException();

			_ = _disabledFilters.Pop();
		}

		#endregion

		#region Grouping Guard

		public bool IsGroupingGuardDisabled { get; set; }

		#endregion
	}
}
