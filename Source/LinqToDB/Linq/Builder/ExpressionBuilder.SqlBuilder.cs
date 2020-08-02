using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using SqlProvider;
	using Tools;

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

			BuildSearchCondition(ctx, expr, conditions, false);

			ReplaceParent(ctx, prevParent);

			return sequence;
		}

		bool CheckSubQueryForWhere(IBuildContext context, Expression expression, out bool makeHaving)
		{
			var makeSubQuery = false;
			var isHaving     = false;
			var isWhere      = false;

			expression.Visit(expr =>
			{
				if (makeSubQuery)
					return false;

				if (_subQueryExpressions != null && _subQueryExpressions.Contains(expr))
				{
					makeSubQuery = true;
					isWhere      = true;
					return false;
				}

				switch (expr.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression)expr;

						if (ma.Member.IsNullableValueMember())
							return true;

						if (Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member) != null)
							return true;

						var ctx = GetContext(context, expr);

						if (ctx == null)
							return true;

						var expres = ctx.IsExpression(expr, 0, RequestFor.Expression);

						if (expres.Result)
						{
							if (expres.Expression != null && IsGrouping(expres.Expression, MappingSchema))
							{
								isHaving = true;
								return false;
							}

							makeSubQuery = true;
						}
						else
						{
							if (IsGrouping(expr, MappingSchema))
							{
								isHaving = true;
								return false;
							}

							isWhere = ctx.IsExpression(expr, 0, RequestFor.Field).Result;
						}

						return false;
					}

					case ExpressionType.Call:
						{
							var e = (MethodCallExpression)expr;

							if (Expressions.ConvertMember(MappingSchema, e.Object?.Type, e.Method) != null)
								return true;

							if (IsGrouping(e, MappingSchema))
							{
								isHaving = true;
								return false;
							}

							break;
						}

					case ExpressionType.Parameter:
						{
							var ctx = GetContext(context, expr);

							if (ctx != null)
							{
								if (ctx.IsExpression(expr, 0, RequestFor.Expression).Result)
									makeSubQuery = true;
							}

							isWhere = true;

							break;
						}
				}

				return true;
			});

			makeHaving = isHaving && !isWhere;
			return makeSubQuery || isHaving && isWhere;
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

					return mce.Arguments.Any(a => typeof(IGrouping<,>).IsSameOrParentOf(a.Type));
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
				!DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql))
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
			var sequence = GetSubQuery(context, expression);
			var subSql   = sequence.GetSubQuery(context);

			if (subSql == null)
			{
				var query    = context.SelectQuery;
				var subQuery = sequence.SelectQuery;

				// This code should be moved to context.
				//
				if (!query.GroupBy.IsEmpty && !subQuery.Where.IsEmpty)
				{
					var fromGroupBy = sequence.SelectQuery.Properties
						.OfType<Tuple<string,SelectQuery>>()
						.Any(p => p.Item1 == "from_group_by" && ReferenceEquals(p.Item2, context.SelectQuery));

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

		bool IsSubQuery(IBuildContext context, MethodCallExpression call)
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

		bool IsSubQuerySource(IBuildContext context, Expression expr)
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
					expression.Expression);
			}
		}

		internal Expression ConvertExpression(Expression expression)
		{
			return expression.Transform(e =>
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
								if (equalityLeft.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
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

								var memberInfo = entityDescriptor[memberName]?.MemberInfo ?? entityDescriptor.Associations
													 .SingleOrDefault(a => a.MemberInfo.Name == memberName)?.MemberInfo;
								if (memberInfo == null)
									memberInfo = MemberHelper.GetMemberInfo(expr);

								return new TransformInfo(ConvertExpression(Expression.MakeMemberAccess(entity, memberInfo)));
							}

							var cm = ConvertMethod(expr);
							if (cm != null)
								return new TransformInfo(ConvertExpression(cm));
							break;
						}

					case ExpressionType.MemberAccess:
						{
							var ma = (MemberExpression)e;
							var l  = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

							if (l != null)
							{
								var body = l.Body.Unwrap();
								var expr = body.Transform(wpi => wpi.NodeType == ExpressionType.Parameter ? ma.Expression! : wpi);

								if (expr.Type != e.Type)
									expr = new ChangeTypeExpression(expr, e.Type);

								return new TransformInfo(ConvertExpression(expr));
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
									case ExpressionType.Subtract       :
									case ExpressionType.SubtractChecked:

										Sql.DateParts datePart;

										switch (ma.Member.Name)
										{
											case "TotalMilliseconds" : datePart = Sql.DateParts.Millisecond; break;
											case "TotalSeconds"      : datePart = Sql.DateParts.Second;      break;
											case "TotalMinutes"      : datePart = Sql.DateParts.Minute;      break;
											case "TotalHours"        : datePart = Sql.DateParts.Hour;        break;
											case "TotalDays"         : datePart = Sql.DateParts.Day;         break;
											default                  : return new TransformInfo(e);
										}

										var ex     = (BinaryExpression)ma.Expression;
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
									var expr = body.Transform(wpi =>
									{
										if (wpi.NodeType == ExpressionType.Parameter)
										{
											if (l.Parameters[0] == wpi)
												return binary.Left;
											if (l.Parameters[1] == wpi)
												return binary.Right;
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
			});
		}

		Expression? ConvertMethod(MethodCallExpression pi)
		{
			var l = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);
			return l == null ? null : ConvertMethod(pi, l);
		}

		static Expression ConvertMethod(MethodCallExpression pi, LambdaExpression lambda)
		{
			var ef    = lambda.Body.Unwrap();
			var parms = new Dictionary<ParameterExpression,int>(lambda.Parameters.Count);
			var pn    = pi.Method.IsStatic ? 0 : -1;

			foreach (var p in lambda.Parameters)
				parms.Add(p, pn++);

			var pie = ef.Transform(wpi =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						if (n >= pi.Arguments.Count)
						{
							if (DataContextParam.Type.IsSameOrParentOf(wpi.Type))
							{
								if (DataContextParam.Type != wpi.Type)
									return Expression.Convert(DataContextParam, wpi.Type);
								return DataContextParam;
							}

							throw new LinqToDBException($"Can't convert {wpi} to expression.");
						}

						var result = n < 0 ? pi.Object : pi.Arguments[n];
						
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
			var lambda = Expressions.ConvertMember(MappingSchema, pi.Type, pi.Constructor);

			if (lambda != null)
			{
				var ef    = lambda.Body.Unwrap();
				var parms = new Dictionary<string,int>(lambda.Parameters.Count);
				var pn    = 0;

				foreach (var p in lambda.Parameters)
					parms.Add(p.Name, pn++);

				return ef.Transform(wpi =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						var pe   = (ParameterExpression)wpi;
						var n    = parms[pe.Name];
						return pi.Arguments[n];
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
			expression = ConvertExpression(expression);

			switch (expression.NodeType)
			{
				case ExpressionType.New :
					{
						var expr = (NewExpression)expression;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
						if (expr.Members == null)
							return Array<SqlInfo>.Empty;
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore ConditionIsAlwaysTrueOrFalse

						var ed = context.Builder.MappingSchema.GetEntityDescriptor(expr.Type);
						return expr.Arguments
							.Select((arg,i) =>
							{
								var mi = expr.Members[i];
								if (mi is MethodInfo info)
									mi = info.GetPropertyInfo();

								var descriptor = ed.FindColumnDescriptor(mi);

								return ConvertExpressions(context, arg, queryConvertFlag, descriptor).Select(si => si.Clone(mi));
							})
							.SelectMany(si => si)
							.ToArray();
					}

				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var ed   = context.Builder.MappingSchema.GetEntityDescriptor(expr.Type);
						var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo, _ => _.i);

						return expr.Bindings
							.Where  (b => b is MemberAssignment)
							.Cast<MemberAssignment>()
							.OrderBy(b => dic[expr.Type.GetMemberEx(b.Member)!])
							.Select (a =>
							{
								var mi = a.Member;
								if (mi is MethodInfo info)
									mi = info.GetPropertyInfo();

								var descriptor = ed.FindColumnDescriptor(mi);

								return ConvertExpressions(context, a.Expression, queryConvertFlag, descriptor).Select(si => si.Clone(mi));
							})
							.SelectMany(si => si)
							.ToArray();
					}
				case ExpressionType.Call         :
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

								return new[] { new SqlInfo (subQueryContextInfo.Context.SelectQuery) };
							}
						}
						break;
					}
				case ExpressionType.NewArrayInit:
					{
						var expr = (NewArrayExpression)expression;
						var sql  = expr.Expressions
							.Select(arg => ConvertExpressions(context, arg, queryConvertFlag, columnDescriptor))
							.SelectMany(si => si)
							.ToArray();

						return sql;
					}
				case ExpressionType.ListInit:
					{
						var expr = (ListInitExpression)expression;
						var sql  = expr.Initializers
							.SelectMany(init => init.Arguments)
							.Select(arg => ConvertExpressions(context, arg, queryConvertFlag, columnDescriptor))
							.SelectMany(si => si)
							.ToArray();

						return sql;
					}
			}

			var ctx = GetContext(context, expression);

			if (ctx != null && ctx.IsExpression(expression, 0, RequestFor.Object).Result)
				return ctx.ConvertToSql(expression, 0, queryConvertFlag);

			return new[] { new SqlInfo(ConvertToSql(context, expression, false, columnDescriptor)) };
		}

		public ISqlExpression ConvertToSqlExpression(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor)
		{
			var expr = ConvertExpression(expression);
			return ConvertToSql(context, expr, false, columnDescriptor);
		}

		public ISqlExpression ConvertToExtensionSql(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor)
		{
			expression = expression.Unwrap();

			if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(expression.Type))
			{
				Expression preparedExpression;
				if (expression.NodeType == ExpressionType.Call)
					preparedExpression = ((MethodCallExpression)expression).Arguments[0];
				else 
					preparedExpression = ((Sql.IQueryableContainer)expression.EvaluateExpression()!).Query.Expression;
				return ConvertToExtensionSql(context, preparedExpression, columnDescriptor);
			}

			if (expression is LambdaExpression lambda)
			{
				var saveParent = context.Parent;
				ExpressionContext exprCtx;

				IBuildContext valueSequence = context;

				if (context is SelectContext sc && sc.Sequence[0] is GroupByBuilder.GroupByContext)
					valueSequence = sc.Sequence[0];

				if (valueSequence is GroupByBuilder.GroupByContext groupByContext)
				{
					valueSequence = groupByContext.Element;
				}

				exprCtx = new ExpressionContext(valueSequence.Parent, valueSequence, lambda);

				var result = ConvertToSql(exprCtx, lambda.Body, false, columnDescriptor);
				ReplaceParent(context.Parent!, saveParent);
				if (!(result is SqlField field) || field.Table!.All != field)
					return result;
				result = context.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).FirstOrDefault();
				return result;
			}

			if (!MappingSchema.IsScalarType(expression.Type) && typeof(IQueryable<>).IsSameOrParentOf(expression.Type))
				return context.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).FirstOrDefault();

			return ConvertToSql(context, expression, false, columnDescriptor);
		}

		public ISqlExpression ConvertToSql(IBuildContext? context, Expression expression, bool unwrap = false, ColumnDescriptor? columnDescriptor = null)
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
					return BuildParameter(expression, columnDescriptor).SqlParameter;
			}

			if (unwrap)
				expression = expression.Unwrap();

			switch (expression.NodeType)
			{
				case ExpressionType.AndAlso            :
				case ExpressionType.OrElse             :
				case ExpressionType.Not                :
				case ExpressionType.Equal              :
				case ExpressionType.NotEqual           :
				case ExpressionType.GreaterThan        :
				case ExpressionType.GreaterThanOrEqual :
				case ExpressionType.LessThan           :
				case ExpressionType.LessThanOrEqual    :
					{
						var condition = new SqlSearchCondition();
						BuildSearchCondition(context, expression, condition.Conditions, expression.NodeType == ExpressionType.Not);
						return condition;
					}

				case ExpressionType.And                :
				case ExpressionType.Or                 :
					{
						if (expression.Type == typeof(bool))
							goto case ExpressionType.AndAlso;
						goto case ExpressionType.Add;
					}

				case ExpressionType.Add                :
				case ExpressionType.AddChecked         :
				case ExpressionType.Divide             :
				case ExpressionType.ExclusiveOr        :
				case ExpressionType.Modulo             :
				case ExpressionType.Multiply           :
				case ExpressionType.MultiplyChecked    :
				case ExpressionType.Power              :
				case ExpressionType.Subtract           :
				case ExpressionType.SubtractChecked    :
				case ExpressionType.Coalesce           :
					{
						var e = (BinaryExpression)expression;
						var l = ConvertToSql(context, e.Left);
						var r = ConvertToSql(context, e.Right);
						var t = e.Type;

						switch (expression.NodeType)
						{
							case ExpressionType.Add             :
							case ExpressionType.AddChecked      : return Convert(new SqlBinaryExpression(t, l, "+", r, Precedence.Additive));
							case ExpressionType.And             : return Convert(new SqlBinaryExpression(t, l, "&", r, Precedence.Bitwise));
							case ExpressionType.Divide          : return Convert(new SqlBinaryExpression(t, l, "/", r, Precedence.Multiplicative));
							case ExpressionType.ExclusiveOr     : return Convert(new SqlBinaryExpression(t, l, "^", r, Precedence.Bitwise));
							case ExpressionType.Modulo          : return Convert(new SqlBinaryExpression(t, l, "%", r, Precedence.Multiplicative));
							case ExpressionType.Multiply:
							case ExpressionType.MultiplyChecked : return Convert(new SqlBinaryExpression(t, l, "*", r, Precedence.Multiplicative));
							case ExpressionType.Or              : return Convert(new SqlBinaryExpression(t, l, "|", r, Precedence.Bitwise));
							case ExpressionType.Power           : return Convert(new SqlFunction(t, "Power", l, r));
							case ExpressionType.Subtract        :
							case ExpressionType.SubtractChecked : return Convert(new SqlBinaryExpression(t, l, "-", r, Precedence.Subtraction));
							case ExpressionType.Coalesce        :
								{
									if (r is SqlFunction c)
									{
										if (c.Name == "Coalesce")
										{
											var parms = new ISqlExpression[c.Parameters.Length + 1];

											parms[0] = l;
											c.Parameters.CopyTo(parms, 1);

											return Convert(new SqlFunction(t, "Coalesce", parms));
										}
									}

									return Convert(new SqlFunction(t, "Coalesce", l, r));
								}
						}

						break;
					}

				case ExpressionType.UnaryPlus      :
				case ExpressionType.Negate         :
				case ExpressionType.NegateChecked  :
					{
						var e = (UnaryExpression)expression;
						var o = ConvertToSql(context, e.Operand);
						var t = e.Type;

						switch (expression.NodeType)
						{
							case ExpressionType.UnaryPlus     : return o;
							case ExpressionType.Negate        :
							case ExpressionType.NegateChecked :
								return Convert(new SqlBinaryExpression(t, new SqlValue(-1), "*", o, Precedence.Multiplicative));
						}

						break;
					}

				case ExpressionType.Convert        :
				case ExpressionType.ConvertChecked :
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
							t.IsEnum      && Enum.GetUnderlyingType(t)      == e.Type ||
							e.Type.IsEnum && Enum.GetUnderlyingType(e.Type) == t)
							return o;

						return Convert(new SqlFunction(e.Type, "$Convert$", SqlDataType.GetDataType(e.Type), s, o));
					}

				case ExpressionType.Conditional    :
					{
						var e = (ConditionalExpression)expression;
						var s = ConvertToSql(context, e.Test);
						var t = ConvertToSql(context, e.IfTrue);
						var f = ConvertToSql(context, e.IfFalse);

						if (f is SqlFunction c && c.Name == "CASE")
						{
							var parms = new ISqlExpression[c.Parameters.Length + 2];

							parms[0] = s;
							parms[1] = t;
							c.Parameters.CopyTo(parms, 2);

							return Convert(new SqlFunction(e.Type, "CASE", parms));
						}

						return Convert(new SqlFunction(e.Type, "CASE", s, t, f));
					}

				case ExpressionType.MemberAccess :
					{
						var ma   = (MemberExpression)expression;
						var attr = GetExpressionAttribute(ma.Member);

						if (attr != null)
						{
							var converted = attr.GetExpression(DataContext, context!.SelectQuery, ma,
								(e, descriptor) => ConvertToExtensionSql(context, e, descriptor));

							if (converted == null)
							{
								if (attr.ExpectExpression)
								{
									var exp = ConvertToSql(context, ma.Expression);
									converted = Convert(attr.GetExpression(ma.Member, exp));
								}
								else
								{
									converted = Convert(attr.GetExpression(ma.Member));
								}
							}

							return converted;
						}

						var ctx = GetContext(context, expression);

						if (ctx != null)
						{
							var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

							switch (sql.Length)
							{
								case 0  : break;
								case 1  : return sql[0].Sql;
								default : throw new InvalidOperationException();
							}
						}

						break;
					}

				case ExpressionType.Parameter   :
					{
						var ctx = GetContext(context, expression);

						if (ctx != null)
						{
							var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

							switch (sql.Length)
							{
								case 0  : break;
								case 1  : return sql[0].Sql;
								default : throw new InvalidOperationException();
							}
						}

						break;
					}

				case ExpressionType.Extension   :
					{
						var ctx = GetContext(context, expression);

						if (ctx != null)
						{
							var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);

							switch (sql.Length)
							{
								case 0  : break;
								case 1  : return sql[0].Sql;
								default : throw new InvalidOperationException();
							}
						}

						break;
					}

				case ExpressionType.Call        :
					{
						var e = (MethodCallExpression)expression;

						var isAggregation = e.IsAggregate(MappingSchema);
						if (isAggregation && !e.IsQueryable())
						{
							var arg = e.Arguments[0];
							var enumerableType = arg.Type;
							if (!EagerLoading.IsEnumerableType(enumerableType, MappingSchema))
								isAggregation = false;
							else
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
							// TODO: move PrepareRawSqlArguments to more correct location
							TableBuilder.PrepareRawSqlArguments(e, null,
								out var format, out var arguments);
							var sqlArguments = arguments.Select(a => ConvertToSql(context, a)).ToArray();

							return new SqlExpression(e.Type, format, Precedence.Primary, sqlArguments);
						}

						break;
					}

				case ExpressionType.Invoke :
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

							var pie = l.Body.Transform(wpi => dic.TryGetValue(wpi, out var ppi) ? ppi : wpi);

							return ConvertToSql(context, pie);
						}

						break;
					}

				case ExpressionType.TypeIs :
					{
						var condition = new SqlSearchCondition();
						BuildSearchCondition(context, expression, condition.Conditions, false);
						return condition;
					}

				case ChangeTypeExpression.ChangeTypeType :
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

		public ISqlExpression ConvertExtensionToSql(IBuildContext context, Sql.ExpressionAttribute attr, MethodCallExpression mc)
		{
			var inlineParameters = DataContext.InlineParameters;

			if (attr.InlineParameters)
				DataContext.InlineParameters = true;

			var sqlExpression =
				attr.GetExpression(DataContext, context!.SelectQuery, mc, (e, descriptor) => ConvertToExtensionSql(context, e, descriptor));
			if (sqlExpression != null)
				return Convert(sqlExpression);

			var parms = new List<ISqlExpression>();

			if (mc.Object != null)
				parms.Add(ConvertToSql(context, mc.Object));

			ParameterInfo[]? pis = null;

			for (var i = 0; i < mc.Arguments.Count; i++)
			{
				var arg = mc.Arguments[i];

				if (arg is NewArrayExpression nae)
				{
					if (pis == null)
						pis = mc.Method.GetParameters();

					var p = pis[i];

					if (p.GetCustomAttributes(true).OfType<ParamArrayAttribute>().Any())
					{
						parms.AddRange(nae.Expressions.Select(a => ConvertToSql(context, a)));
					}
					else
					{
						parms.Add(ConvertToSql(context, nae));
					}
				}
				else
				{
					parms.Add(ConvertToSql(context, arg));
				}
			}

			DataContext.InlineParameters = inlineParameters;

			return Convert(attr.GetExpression(mc.Method, parms.ToArray()));
		}

		internal static ISqlExpression ConvertToSqlConvertible(Expression expression)
		{
			var l = Expression.Lambda<Func<IToSqlConverter>>(expression);
			var f = l.Compile();
			var c = f();

			return c.ToSql(expression);
		}

		readonly HashSet<Expression> _convertedPredicates = new HashSet<Expression>();

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

		bool CanBeCompiled(Expression expr)
		{
			return _optimizationContext.CanBeCompiled(expr);
		}

		#endregion

		#region Build Constant

		readonly Dictionary<Tuple<Expression, ColumnDescriptor?>,SqlValue> _constants = new Dictionary<Tuple<Expression, ColumnDescriptor?>,SqlValue>();

		SqlValue BuildConstant(Expression expr, ColumnDescriptor? columnDescriptor)
		{
			var key = Tuple.Create(expr, columnDescriptor);
			if (_constants.TryGetValue(key, out var sqlValue))
				return sqlValue;

			var dbType = columnDescriptor?.GetDbDataType() ?? new DbDataType(expr.Type);

			if (columnDescriptor != null)
			{
				expr = columnDescriptor.ApplyConversions(expr, dbType, false);
			}
			else
			{
				expr = ColumnDescriptor.ApplyConversions(MappingSchema, expr, dbType, null, false);
			}

			var value = expr.EvaluateExpression();

			if (value != null && MappingSchema.ValueToSqlConverter.CanConvert(dbType.SystemType))
				sqlValue = new SqlValue(dbType, value);
			else
			{
				if (value != null && dbType.SystemType.IsEnum)
				{
					var attrs = dbType.SystemType.GetCustomAttributes(typeof(Sql.EnumAttribute), true);

					if (attrs.Length == 0)
						value = MappingSchema.EnumToValue((Enum)value);
				}

				sqlValue = MappingSchema.GetSqlValue(expr.Type, value);
			}

			_constants.Add(key, sqlValue);

			return sqlValue;
		}

		#endregion

		#region Build Parameter

		internal readonly Dictionary<Expression,ParameterAccessor> _parameters = new Dictionary<Expression,ParameterAccessor>();

		internal void AddCurrentSqlParameter(ParameterAccessor parameterAccessor)
		{
			var idx = CurrentSqlParameters.Count;
			CurrentSqlParameters.Add(parameterAccessor);
			parameterAccessor.SqlParameter.AccessorId = idx;
		}

		internal enum BuildParameterType
		{
			Default,
			InPredicate
		}

		ParameterAccessor BuildParameter(Expression expr, ColumnDescriptor? columnDescriptor,
			BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			if (_parameters.TryGetValue(expr, out var p))
				return p;

			string? name = null;

			ValueTypeExpression newExpr;
			newExpr = ReplaceParameter(_expressionAccessors, expr, nm => name = nm);

			p = PrepareConvertersAndCreateParameter(newExpr, expr, name, columnDescriptor, buildParameterType);

			var found = p;
			foreach (var accessor in _parameters)
			{
				if (accessor.Value.SqlParameter.Type.Equals(p.SqlParameter.Type))
					continue;

				if (accessor.Key.EqualsTo(expr, DataContext, new Dictionary<Expression, QueryableAccessor>(), null,
					null, compareConstantValues: true))
				{
					found = accessor.Value;
					break;
				}
			}

			// We already have registered parameter for the same expression
			//
			if (!ReferenceEquals(found, p))
				return found;

			_parameters.Add(expr, p);
			AddCurrentSqlParameter(p);

			return p;
		}

		public ParameterAccessor BuildParameterFromArgumentProperty(MethodCallExpression methodCall, int argumentIndex, ColumnDescriptor columnDescriptor,
			BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			var valueAccessor = GenerateArgumentAccessor(methodCall, argumentIndex, null);

			valueAccessor = Expression.MakeMemberAccess(valueAccessor, columnDescriptor.MemberInfo);

			var dataType = columnDescriptor.GetDbDataType();
			var newExpr  = new ValueTypeExpression
			{
				DataType             = dataType,
				DbDataTypeExpression = Expression.Constant(dataType),
				ValueExpression      = valueAccessor
			};

			var p = PrepareConvertersAndCreateParameter(newExpr, valueAccessor, null, columnDescriptor,
				buildParameterType);
			AddCurrentSqlParameter(p);

			return p;
		}

		public ParameterAccessor BuildParameterFromArgument(MethodCallExpression methodCall, int argumentIndex, ColumnDescriptor? columnDescriptor,
			BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			var valueAccessor = GenerateArgumentAccessor(methodCall, argumentIndex, columnDescriptor);

			var dataType = new DbDataType(valueAccessor.Type);
			var newExpr  = new ValueTypeExpression
			{
				DataType             = dataType,
				DbDataTypeExpression = Expression.Constant(dataType),
				ValueExpression      = valueAccessor
			};

			var p = PrepareConvertersAndCreateParameter(newExpr, valueAccessor, null, columnDescriptor,
				buildParameterType);
			AddCurrentSqlParameter(p);

			return p;
		}

		Expression? GetActualMethodAccessor(MethodCallExpression methodCall)
		{
			Expression? current = methodCall;
			if (_expressionAccessors.TryGetValue(current, out var methodAccessor))
				return methodAccessor;

			// Looking in known accessors for method with only changed first parameter.
			// Typical case when we have transformed Queryable method chain.
			foreach (var accessorPair in _expressionAccessors)
			{
				if (accessorPair.Key.NodeType != ExpressionType.Call)
					continue;

				var mc = (MethodCallExpression)accessorPair.Key;
				if (mc.Method != methodCall.Method)
					continue;

				var isEqual = true;
				for (int i = 1; i < mc.Arguments.Count; i++)
				{
					isEqual = (mc.Arguments[i].Equals(methodCall.Arguments[i]));
					if (!isEqual)
						break;
				}
				if (isEqual)
				{
					return accessorPair.Value;
				}
			}

			return null;
		}

		Expression GenerateArgumentAccessor(MethodCallExpression methodCall, int argumentIndex, ColumnDescriptor? columnDescriptor)
		{
			var arg = methodCall.Arguments[argumentIndex];
			var methodAccessor = GetActualMethodAccessor(methodCall);
			if (methodAccessor == null)
			{
				// compiled query case
				//
				if (null == arg.Find(e => e == ParametersParam))
					throw new InvalidOperationException($"Method '{methodCall}' does not have accessor.");
				return arg;
			}

			var prop = Expression.Property(methodAccessor, ReflectionHelper.MethodCall.Arguments);
			var valueAccessorExpr = Expression.Call(prop, ReflectionHelper.IndexExpressor<Expression>.Item,
				Expression.Constant(argumentIndex));

			var expectedType = columnDescriptor?.MemberType ?? arg.Type;

			var evaluatedExpr = Expression.Call(null,
				MemberHelper.MethodOf(() => InternalExtensions.EvaluateExpression(null)),
				valueAccessorExpr);

			var valueAccessor = (Expression)evaluatedExpr;
			valueAccessor = Expression.Convert(valueAccessor, expectedType);

			return valueAccessor;
		}

		ParameterAccessor PrepareConvertersAndCreateParameter(ValueTypeExpression newExpr, Expression valueExpression, string? name, ColumnDescriptor? columnDescriptor, BuildParameterType buildParameterType)
		{
			if (buildParameterType != BuildParameterType.InPredicate)
			{
				if (!typeof(DataParameter).IsSameOrParentOf(newExpr.ValueExpression.Type))
				{
					if (columnDescriptor != null)
					{
						newExpr.DataType        = columnDescriptor.GetDbDataType();
						if (newExpr.ValueExpression.Type != columnDescriptor.MemberType)
							newExpr.ValueExpression = Expression.Convert(newExpr.ValueExpression, columnDescriptor.MemberType);
						newExpr.ValueExpression = columnDescriptor.ApplyConversions(newExpr.ValueExpression, newExpr.DataType, true);

						if (name == null)
						{
							if (columnDescriptor.MemberName.Contains('.'))
								name = columnDescriptor.ColumnName;
							else
								name = columnDescriptor.MemberName;

						}

						newExpr.DbDataTypeExpression = Expression.Constant(newExpr.DataType);
					}
					else
					{
						newExpr.ValueExpression = ColumnDescriptor.ApplyConversions(MappingSchema, newExpr.ValueExpression, newExpr.DataType, null, true);
					}
				}

				if (typeof(DataParameter).IsSameOrParentOf(newExpr.ValueExpression.Type))
				{
					newExpr.DbDataTypeExpression = Expression.PropertyOrField(newExpr.ValueExpression, nameof(DataParameter.DbDataType));
					newExpr.ValueExpression      = Expression.PropertyOrField(newExpr.ValueExpression, nameof(DataParameter.Value));
				}
			}

			var p = CreateParameterAccessor(
				DataContext, newExpr.ValueExpression, newExpr.DbDataTypeExpression, valueExpression, ExpressionParam,
				ParametersParam, DataContextParam, name!);

			return p;
		}

		class ValueTypeExpression
		{
			public Expression ValueExpression      = null!;
			public Expression DbDataTypeExpression = null!;

			public DbDataType DataType;
		}

		ValueTypeExpression ReplaceParameter(IDictionary<Expression,Expression> expressionAccessors, Expression expression, Action<string> setName)
		{
			var result = new ValueTypeExpression
			{
				DataType             = new DbDataType(expression.Type),
				DbDataTypeExpression = Expression.Constant(new DbDataType(expression.Type), typeof(DbDataType)),
			};

			var unwrapped = expression.Unwrap();
			if (unwrapped.NodeType == ExpressionType.MemberAccess)
			{
				var ma = (MemberExpression)unwrapped;
				setName(ma.Member.Name);
			}

			result.ValueExpression = expression.Transform(expr =>
			{
				if (expr.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)expr;

					if (!expr.Type.IsConstantable(false))
					{
						if (expressionAccessors.TryGetValue(expr, out var val))
						{
							expr = Expression.Convert(val, expr.Type);

							if (expression.NodeType == ExpressionType.MemberAccess)
							{
								var ma = (MemberExpression)expression;

								var mt = GetMemberDataType(ma.Member);

								if (mt.DataType != DataType.Undefined)
								{
									result.DataType             = result.DataType.WithDataType(mt.DataType);
									result.DbDataTypeExpression = Expression.Constant(mt);
								}

								if (mt.DbType != null)
								{
									result.DataType             = result.DataType.WithDbType(mt.DbType);
									result.DbDataTypeExpression = Expression.Constant(mt);
								}

								if (mt.Length != null)
								{
									result.DataType             = result.DataType.WithLength(mt.Length);
									result.DbDataTypeExpression = Expression.Constant(mt);
								}

								setName(ma.Member.Name);
							}
						}
					}
				}

				return expr;
			});

			return result;
		}

		#endregion

		#region Predicate Converter

		ISqlPredicate ConvertPredicate(IBuildContext? context, Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Equal              :
				case ExpressionType.NotEqual           :
				case ExpressionType.GreaterThan        :
				case ExpressionType.GreaterThanOrEqual :
				case ExpressionType.LessThan           :
				case ExpressionType.LessThanOrEqual    :
					{
						var e = (BinaryExpression)expression;
						return ConvertCompare(context, expression.NodeType, e.Left, e.Right);
					}

				case ExpressionType.Call               :
					{
						var e = (MethodCallExpression)expression;

						ISqlPredicate? predicate = null;

						if (e.Method.Name == "Equals" && e.Object != null && e.Arguments.Count == 1)
							return ConvertCompare(context, ExpressionType.Equal, e.Object, e.Arguments[0]);

						if (e.Method.DeclaringType == typeof(string))
						{
							switch (e.Method.Name)
							{
								case "Contains"   : predicate = ConvertLikePredicate(context, e, "%", "%"); break;
								case "StartsWith" : predicate = ConvertLikePredicate(context, e, "",  "%"); break;
								case "EndsWith"   : predicate = ConvertLikePredicate(context, e, "%", "");  break;
							}
						}
						else if (e.Method.Name == "Contains")
						{
							if (e.Method.DeclaringType == typeof(Enumerable) ||
								typeof(IList).        IsSameOrParentOf(e.Method.DeclaringType!) ||
								typeof(ICollection<>).IsSameOrParentOf(e.Method.DeclaringType!))
							{
								predicate = ConvertInPredicate(context!, e);
							}
						}
						else if (e.Method.Name == "ContainsValue" && typeof(Dictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!))
						{
							var args = e.Method.DeclaringType!.GetGenericArguments(typeof(Dictionary<,>))!;
							var minf = EnumerableMethods
								.First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
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
								.First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
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
							return Convert(context!, predicate);

						var attr = GetExpressionAttribute(e.Method);

						if (attr != null && attr.GetIsPredicate(expression))
							break;

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ExpressionType.Conditional  :
					return Convert(context!,
						new SqlPredicate.ExprExpr(
							ConvertToSql(context, expression),
							SqlPredicate.Operator.Equal,
							new SqlValue(true)));

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expression;

						var attr = GetExpressionAttribute(e.Member);

						if (attr != null && attr.GetIsPredicate(expression))
							break;

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

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

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ChangeTypeExpression.ChangeTypeType:
					return ConvertPredicate(context, AddEqualTrue(expression));
			}

			var ex = ConvertToSql(context, expression);

			if (SqlExpression.NeedsEqual(ex))
				return Convert(context!, new SqlPredicate.ExprExpr(ex, SqlPredicate.Operator.Equal, new SqlValue(true)));

			return Convert(context!, new SqlPredicate.Expr(ex));
		}

		Expression AddEqualTrue(Expression expr)
		{
			return Equal(MappingSchema, Expression.Constant(true), expr);
		}

		#region ConvertCompare

		ISqlPredicate ConvertCompare(IBuildContext? context, ExpressionType nodeType, Expression left, Expression right)
		{
			if (!RestoreCompare(ref left, ref right))
				RestoreCompare(ref right, ref left);

			switch (nodeType)
			{
				case ExpressionType.Equal    :
				case ExpressionType.NotEqual :

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
			if ((left.NodeType == ExpressionType.Convert || right.NodeType == ExpressionType.Convert) && op.In(SqlPredicate.Operator.Equal, SqlPredicate.Operator.NotEqual))
			{
				var p = ConvertEnumConversion(context!, left, op, right);
				if (p != null)
					return p;
			}

			ISqlExpression l;
			ISqlExpression r;
			var ls = GetContext(context, left);
			if (ls?.IsExpression(left, 0, RequestFor.Field).Result == true)
			{
				l = ConvertToSql(context, left);
				r = ConvertToSql(context, right, true, QueryHelper.GetColumnDescriptor(l));
			}
			else
			{
				r = ConvertToSql(context, right, true);
				l = ConvertToSql(context, left, false, QueryHelper.GetColumnDescriptor(r));
			}

			if (l is SqlValue lValue)
				lValue.ValueType = GetDataType(r, lValue.ValueType);

			if (r is SqlValue rValue)
				rValue.ValueType = GetDataType(l, rValue.ValueType);

			switch (nodeType)
			{
				case ExpressionType.Equal   :
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
				l = Convert(new SqlFunction(typeof(bool), "CASE", l, new SqlValue(true), new SqlValue(false)));

			if (r is SqlSearchCondition)
				r = Convert(new SqlFunction(typeof(bool), "CASE", r, new SqlValue(true), new SqlValue(false)));

			ISqlPredicate? predicate = null;
			if (op.In(SqlPredicate.Operator.Equal, SqlPredicate.Operator.NotEqual))
			{
				bool?           value      = null;
				ISqlExpression? expression = null;
				var             isNullable = false;
				if ((typeof(bool) == left.Type || typeof(bool?) == left.Type) && left.Unwrap() is ConstantExpression lc)
				{
					value      = lc.Value as bool?;
					isNullable = typeof(bool?) == left.Type;
					expression = r;
				}
				else if ((typeof(bool) == right.Type || typeof(bool?) == right.Type) && right.Unwrap() is ConstantExpression rc)
				{
					value      = rc.Value as bool?;
					isNullable = typeof(bool?) == right.Type;
					expression = l;
				}

				if (value != null && expression != null)
				{
					var isNot = !value.Value;
					var withNull = false;
					if (op == SqlPredicate.Operator.NotEqual)
					{
						isNot    = !isNot;
						withNull = true;
					}
					var descriptor = QueryHelper.GetColumnDescriptor(expression);
					var trueValue  = ConvertToSql(context, Expression.Constant(true),  false, descriptor);
					var falseValue = ConvertToSql(context, Expression.Constant(false), false, descriptor);

					predicate = new SqlPredicate.IsTrue(expression, trueValue, falseValue, isNullable || NeedNullCheck(expression) ? withNull : (bool?)null, isNot); 
				}
			}

			if (predicate == null)
				predicate = new SqlPredicate.ExprExpr(l, op, r);
			return Convert(context!, predicate);
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
					op2 = Expression.Constant(Enum.ToObject(op1conv.Operand.Type, ((ConstantExpression)op2).Value), op1conv.Operand.Type);
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
					op1 = op1conv .Operand;
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
						var name = Enum.GetName(type, ((ConstantExpression)value).Value);

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

						return Convert(context, new SqlPredicate.ExprExpr(l, op, r));
					}

				case ExpressionType.Convert:
					{
						value = ((UnaryExpression)value).Operand;

						var l = ConvertToSql(context, operand);
						var r = ConvertToSql(context, value);

						return Convert(context, new SqlPredicate.ExprExpr(l, op, r));
					}
			}

			return null;
		}

		#endregion

		#region ConvertObjectNullComparison

		ISqlPredicate? ConvertObjectNullComparison(IBuildContext? context, Expression left, Expression right, bool isEqual)
		{
			if (right.NodeType == ExpressionType.Constant && ((ConstantExpression)right).Value == null)
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

		static Expression? ConstructMemberPath(IEnumerable<MemberInfo> memberPath, Expression ob, bool throwOnError)
		{
			Expression result = ob;
			var skipCount     = 0;
			foreach (var memberInfo in memberPath)
			{
				if (!memberInfo.DeclaringType!.IsAssignableFrom(result.Type))
				{
					// first element may have inappropriate nesting
					if (skipCount-- == 0)
						continue;

					if (throwOnError)
						throw new LinqToDBException($"Type {result.Type.Name} does not have member {memberInfo.Name}.");
					return null;
				}
				result = Expression.MakeMemberAccess(result, memberInfo);
			}

			return result;
		}
		
		public ISqlPredicate? ConvertObjectComparison(
			ExpressionType nodeType,
			IBuildContext  leftContext,
			Expression     left,
			IBuildContext  rightContext,
			Expression     right)
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
					var r = right;
					right = left;
					left  = r;

					var c = rightContext;
					rightContext = leftContext;
					leftContext  = c;

					var q = qsr;
					qsl = q;

					sr = false;

					var lm = lmembers;
					lmembers = rmembers;
					rmembers = lm;
				}

				isNull = right is ConstantExpression expression && expression.Value == null;
				lcols  = lmembers.Select(m => new SqlInfo(m.Key, ConvertToSql(leftContext, m.Value))).ToArray();
			}
			else
			{
				if (sl == false)
				{
					var r = right;
					right = left;
					left  = r;

					var c = rightContext;
					rightContext = leftContext;
					leftContext  = c;

					var q = qsr;
					qsl = q;

					sr = false;
				}

				isNull = right is ConstantExpression expression && expression.Value == null;
				
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

				if (sr)
				{
					var memeberPath = ConstructMemberPath(lcol.MemberChain, right, true)!;
					rcol = ConvertToSql(rightContext, memeberPath);
				}	
				else if (rmembers.Count != 0)
					rcol = ConvertToSql(rightContext, rmembers[lmember]);

				var rex =
					isNull ?
						MappingSchema.GetSqlValue(right.Type, null) :
						rcol ?? GetParameter(right, lmember);

				var predicate = Convert(leftContext, new SqlPredicate.ExprExpr(
					lcol.Sql,
					nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
					rex));

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
				var temp = left;
				left  = right;
				right = temp;
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
						GetParameter(right, newExpr.Members[i]);

				var predicate = Convert(context,
					new SqlPredicate.ExprExpr(
						lex,
						nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
						rex));

				condition.Conditions.Add(new SqlCondition(false, predicate));
			}

			if (nodeType == ExpressionType.NotEqual)
				foreach (var c in condition.Conditions)
					c.IsOr = true;

			return condition;
		}

		ISqlExpression GetParameter(Expression ex, MemberInfo member)
		{
			if (member is MethodInfo mi)
				member = mi.GetPropertyInfo();

			var vte  = ReplaceParameter(_expressionAccessors, ex, _ => { });
			var par  = vte.ValueExpression;
			var expr = Expression.MakeMemberAccess(par.Type == typeof(object) ? Expression.Convert(par, member.DeclaringType) : par, member);
			var p    = CreateParameterAccessor(DataContext, expr, vte.DbDataTypeExpression, expr, ExpressionParam, ParametersParam, DataContextParam, member.Name);

			_parameters.Add(expr, p);
			AddCurrentSqlParameter(p);

			return p.SqlParameter;
		}

		DbDataType GetMemberDataType(MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());

			var dta      = MappingSchema.GetAttribute<DataTypeAttribute>(member.ReflectedType!, member);
			var ca       = MappingSchema.GetAttribute<ColumnAttribute>  (member.ReflectedType!, member);

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

		static DbDataType GetDataType(ISqlExpression expr, DbDataType baseType)
		{
			var systemType = baseType.SystemType;
			var dataType   = baseType.DataType;
			var dbType     = baseType.DbType;
			var length     = baseType.Length;
			var precision  = baseType.Precision;
			var scale      = baseType.Scale;

			new QueryVisitor().Find(expr, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField:
						{
							var fld = (SqlField)e;
							if (fld.Type != null)
							{
								dataType     = fld.Type.Value.DataType;
								dbType       = fld.Type.Value.DbType;
								length       = fld.Type.Value.Length;
								precision    = fld.Type.Value.Precision;
								scale        = fld.Type.Value.Scale;
								//systemType = ((SqlField)e).SystemType;
							}
							return true;
						}
					case QueryElementType.SqlParameter:
						dataType     = ((SqlParameter)e).Type.DataType;
						dbType       = ((SqlParameter)e).Type.DbType;
						length       = ((SqlParameter)e).Type.Length;
						precision    = ((SqlParameter)e).Type.Precision;
						scale        = ((SqlParameter)e).Type.Scale;
						//systemType = ((SqlParameter)e).SystemType;
						return true;
					case QueryElementType.SqlDataType:
						dataType     = ((SqlDataType)e).Type.DataType;
						dbType       = ((SqlDataType)e).Type.DbType;
						length       = ((SqlDataType)e).Type.Length;
						precision    = ((SqlDataType)e).Type.Precision;
						scale        = ((SqlDataType)e).Type.Scale;
						//systemType = ((SqlDataType)e).SystemType;
						return true;
					case QueryElementType.SqlValue:
						dataType     = ((SqlValue)e).ValueType.DataType;
						dbType       = ((SqlValue)e).ValueType.DbType;
						length       = ((SqlValue)e).ValueType.Length;
						precision    = ((SqlValue)e).ValueType.Precision;
						scale        = ((SqlValue)e).ValueType.Scale;
						//systemType = ((SqlValue)e).ValueType.SystemType;
						return true;
					default:
						return false;
				}
			});

			return new DbDataType(
				systemType ?? baseType.SystemType,
				dataType == DataType.Undefined ? baseType.DataType : dataType,
				string.IsNullOrEmpty(dbType)   ? baseType.DbType   : dbType,
				length     ?? baseType.Length,
				precision  ?? baseType.Precision,
				scale      ?? baseType.Scale
			);
		}

		internal static ParameterAccessor CreateParameterAccessor(
			IDataContext        dataContext,
			Expression          accessorExpression,
			Expression          dbDataTypeAccessorExpression,
			Expression          expression,
			ParameterExpression expressionParam,
			ParameterExpression parametersParam,
			ParameterExpression dataContextParam,
			string?             name)
		{
			// Extracting name for parameter
			//
			if (name == null && expression.Type == typeof(DataParameter))
			{
				var dp = expression.EvaluateExpression<DataParameter>();
				if (dp?.Name?.IsNullOrEmpty() == false)
					name = dp.Name;
			}

			// see #820
			accessorExpression = accessorExpression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Parameter:
						{
							// DataContext creates DataConnection which is not compatible with QueryRunner and parameter evaluation.
							// It can be fixed by adding additional parameter to execution path, but it's may slowdown performance.
							// So for now decided to throw exception.
							if (e == dataContextParam && !typeof(DataConnection).IsSameOrParentOf(dataContext.GetType()))
								throw new LinqException("Only DataConnection descendants can be used as source of parameters.");
							return e;
						}
					case ExpressionType.MemberAccess:
						var ma = (MemberExpression) e;

						if (ma.Member.IsNullableValueMember())
						{
							return Expression.Condition(
								Expression.Equal(ma.Expression, Expression.Constant(null, ma.Expression.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					case ExpressionType.Convert:
						var ce = (UnaryExpression) e;
						if (ce.Operand.Type.IsNullable() && !ce.Type.IsNullable())
						{
							return Expression.Condition(
								Expression.Equal(ce.Operand, Expression.Constant(null, ce.Operand.Type)),
								Expression.Default(e.Type),
								e);
						}
						return e;
					default:
						return e;
				}
			})!;

			var mapper = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,object?>>(
				Expression.Convert(accessorExpression, typeof(object)),
				expressionParam, dataContextParam, parametersParam);

			var dbDataTypeAccessor = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,DbDataType>>(
				Expression.Convert(dbDataTypeAccessorExpression, typeof(DbDataType)),
				expressionParam, dataContextParam, parametersParam);

			return new ParameterAccessor
			(
				expression,
				mapper.Compile(),
				dbDataTypeAccessor.Compile(),
				new SqlParameter(new DbDataType(accessorExpression.Type), name, null)
				{
					IsQueryParameter = !(dataContext.InlineParameters && dataContext.MappingSchema.ValueToSqlConverter.CanConvert(accessorExpression.Type))
				}
			);
		}

		internal static SqlParameter CreateParameter(
			object value,
			IDataContext dataContext,
			Expression accessorExpression,
			Expression dbDataTypeAccessorExpression,
			Expression expression,
			ParameterExpression expressionParam,
			ParameterExpression parametersParam,
			ParameterExpression dataContextParam,
			string? name)
		{
			// Extracting name for parameter
			//
			if (name == null && expression.Type == typeof(DataParameter))
			{
				var dp = expression.EvaluateExpression<DataParameter>();
				if (dp?.Name?.IsNullOrEmpty() == false)
					name = dp.Name;
			}

			return new SqlParameter(new DbDataType(accessorExpression.Type), name, null)
			{
				IsQueryParameter = !(dataContext.InlineParameters && dataContext.MappingSchema.ValueToSqlConverter.CanConvert(accessorExpression.Type))
			};
		}

		static Expression FindExpression(Expression expr)
		{
			var ret = expr.Find(pi =>
			{
				switch (pi.NodeType)
				{
					case ExpressionType.Convert      :
						{
							var e = (UnaryExpression)expr;

							return
								e.Operand.NodeType == ExpressionType.ArrayIndex &&
								ReferenceEquals(((BinaryExpression)e.Operand).Left, ParametersParam);
						}

					case ExpressionType.MemberAccess :
					case ExpressionType.New          :
						return true;
				}

				return false;
			});

			if (ret == null)
				throw new NotImplementedException();

			return ret;
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
					expr = new ObjectSqlExpression(MappingSchema, sql);
			}

			switch (arr.NodeType)
			{
				case ExpressionType.NewArrayInit :
					{
						var newArr = (NewArrayExpression)arr;

						if (newArr.Expressions.Count == 0)
							return new SqlPredicate.Expr(new SqlValue(false));

						var exprs  = new ISqlExpression[newArr.Expressions.Count];

						for (var i = 0; i < newArr.Expressions.Count; i++)
							exprs[i] = ConvertToSql(context, newArr.Expressions[i]);

						return new SqlPredicate.InList(expr, false, exprs);
					}

				default :

					if (CanBeCompiled(arr))
					{
						var p = BuildParameter(arr, null, BuildParameterType.InPredicate).SqlParameter;
						p.IsQueryParameter = false;
						return new SqlPredicate.InList(expr, false, p);
					}

					break;
			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		#endregion

		#region LIKE predicate

		ISqlPredicate ConvertLikePredicate(IBuildContext? context, MethodCallExpression expression, string start, string end)
		{
			var e = expression;
			var o = ConvertToSql(context, e.Object);
			var a = ConvertToSql(context, e.Arguments[0]);

			if (a is SqlValue sqlValue)
			{
				var value = sqlValue.Value;

				if (value == null)
					throw new LinqException("NULL cannot be used as a LIKE predicate parameter.");

				return value.ToString()!.IndexOfAny(new[] { '%', '_' }) < 0?
					new SqlPredicate.Like(o, false, new SqlValue(start + value + end), null, false):
					new SqlPredicate.Like(o, false, new SqlValue(start + EscapeLikeText(value.ToString()!) + end), new SqlValue('~'), false);
			}

			if (a is SqlParameter p)
			{
				var ep = (from pm in CurrentSqlParameters where pm.SqlParameter == p select pm).First();

				ep = new ParameterAccessor
				(
					ep.Expression,
					ep.Accessor,
					ep.DbDataTypeAccessor,
					new SqlParameter(p.Type.WithSystemType(ep.Expression.Type), p.Name, p.Value)
					{
						LikeStart        = start,
						LikeEnd          = end,
						ReplaceLike      = p.ReplaceLike,
						IsQueryParameter = !(DataContext.InlineParameters && ep.Expression.Type.IsScalar(false))
					}
				);

				AddCurrentSqlParameter(ep);

				return new SqlPredicate.Like(o, false, ep.SqlParameter, new SqlValue('~'), false);
			}

			var mi = MemberHelper.MethodOf(() => "".Replace("", ""));
			var ex =
				Expression.Call(
				Expression.Call(
				Expression.Call(
					e.Arguments[0],
						mi, Expression.Constant("~"), Expression.Constant("~~")),
						mi, Expression.Constant("%"), Expression.Constant("~%")),
						mi, Expression.Constant("_"), Expression.Constant("~_"));

			var expr = ConvertToSql(context, ConvertExpression(ex));

			if (!string.IsNullOrEmpty(start))
				expr = new SqlBinaryExpression(typeof(string), new SqlValue("%"), "+", expr);

			if (!string.IsNullOrEmpty(end))
				expr = new SqlBinaryExpression(typeof(string), expr, "+", new SqlValue("%"));

			return new SqlPredicate.Like(o, false, expr, new SqlValue('~'), false);
		}

		ISqlPredicate ConvertLikePredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e  = expression;
			var a1 = ConvertToSql(context, e.Arguments[0]);
			var a2 = ConvertToSql(context, e.Arguments[1]);

			ISqlExpression? a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2]);

			return new SqlPredicate.Like(a1, false, a2, a3, true);
		}

		static string EscapeLikeText(string text)
		{
			if (text.IndexOfAny(new[] { '%', '_' }) < 0)
				return text;

			var builder = new StringBuilder(text.Length);

			foreach (var ch in text)
			{
				switch (ch)
				{
					case '%':
					case '_':
					case '~':
						builder.Append('~');
						break;
				}

				builder.Append(ch);
			}

			return builder.ToString();
		}

		#endregion

		#region MakeIsPredicate

		internal ISqlPredicate MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
		{
			if (typeOperand == table.ObjectType && table.InheritanceMapping.All(m => m.Type != typeOperand))
				return Convert(table, new SqlPredicate.Expr(new SqlValue(true)));

			return MakeIsPredicate(table, table.InheritanceMapping, typeOperand, name => table.SqlTable.Fields.Values.First(f => f.Name == name));
		}

		internal ISqlPredicate MakeIsPredicate(
			IBuildContext               context,
			List<InheritanceMapping>    inheritanceMapping,
			Type                        toType,
			Func<string,ISqlExpression> getSql)
		{
			var mapping = inheritanceMapping
				.Where (m => m.Type == toType && !m.IsDefault)
				.ToList();

			switch (mapping.Count)
			{
				case 0 :
					{
						var cond = new SqlSearchCondition();

						if (inheritanceMapping.Any(m => m.Type == toType))
						{
							foreach (var m in inheritanceMapping.Where(m => !m.IsDefault))
							{
								cond.Conditions.Add(
									new SqlCondition(
										false,
										Convert(context,
											new SqlPredicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SqlPredicate.Operator.NotEqual,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code)))));
							}
						}
						else
						{
							foreach (var m in inheritanceMapping.Where(m => toType.IsSameOrParentOf(m.Type)))
							{
								cond.Conditions.Add(
									new SqlCondition(
										false,
										Convert(context,
											new SqlPredicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SqlPredicate.Operator.Equal,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
										true));
							}
						}

						return cond;
					}

				case 1 :
					return Convert(context,
						new SqlPredicate.ExprExpr(
							getSql(mapping[0].DiscriminatorName),
							SqlPredicate.Operator.Equal,
							MappingSchema.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code)));

				default:
					{
						var cond = new SqlSearchCondition();

						foreach (var m in mapping)
						{
							cond.Conditions.Add(
								new SqlCondition(
									false,
									Convert(context,
										new SqlPredicate.ExprExpr(
											getSql(m.DiscriminatorName),
											SqlPredicate.Operator.Equal,
											MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
									true));
						}

						return cond;
					}
			}
		}

		ISqlPredicate MakeIsPredicate(IBuildContext context, TypeBinaryExpression expression)
		{
			var typeOperand = expression.TypeOperand;
			var table       = new TableBuilder.TableContext(this, new BuildInfo((IBuildContext?)null, Expression.Constant(null), new SelectQuery()), typeOperand);

			if (typeOperand == table.ObjectType && table.InheritanceMapping.All(m => m.Type != typeOperand))
				return Convert(table, new SqlPredicate.Expr(new SqlValue(true)));

			var mapping = table.InheritanceMapping.Select((m, i) => new { m, i }).Where(m => typeOperand.IsAssignableFrom(m.m.Type) && !m.m.IsDefault).ToList();
			var isEqual = true;

			if (mapping.Count == 0)
			{
				mapping = table.InheritanceMapping.Select((m,i) => new { m, i }).Where(m => !m.m.IsDefault).ToList();
				isEqual = false;
			}

			Expression? expr = null;

			foreach (var m in mapping)
			{
				var field = table.SqlTable.Fields[table.InheritanceMapping[m.i].DiscriminatorName];
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

		internal void BuildSearchCondition(IBuildContext? context, Expression expression, List<SqlCondition> conditions, bool isNotExpression)
		{
			expression = expression.Transform(RemoveNullPropagation);

			switch (expression.NodeType)
			{
				case ExpressionType.And     :
				case ExpressionType.AndAlso :
					{
						var e = (BinaryExpression)expression;

						BuildSearchCondition(context, e.Left,  conditions, isNotExpression);
						BuildSearchCondition(context, e.Right, conditions, isNotExpression);

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

						BuildSearchCondition(context, e.Left,  orCondition.Conditions, isNotExpression);
						orCondition.Conditions[orCondition.Conditions.Count - 1].IsOr = true;
						BuildSearchCondition(context, e.Right, orCondition.Conditions, isNotExpression);

						conditions.Add(new SqlCondition(false, orCondition));

						break;
					}

				case ExpressionType.Not    :
					{
						var e            = (UnaryExpression)expression;
						var notCondition = new SqlSearchCondition();

						BuildSearchCondition(context, e.Operand, notCondition.Conditions, true);

						var isNot = true;

						if (notCondition.Conditions.Count == 1)
						{
							var sqlCondition = notCondition.Conditions[0];
							if (sqlCondition.Predicate is SqlPredicate.NotExpr p)
							{
								p.Invert();
								var checkIsNullLocal = CheckIsNull(sqlCondition.Predicate, true, isNotExpression);
								conditions.Add(checkIsNullLocal ?? sqlCondition);
							}
							else
							{
								sqlCondition.Predicate = BasicSqlOptimizer.OptimizePredicate(sqlCondition.Predicate, ref isNot);
								var checkIsNullLocal   = CheckIsNull(sqlCondition.Predicate, isNot, isNotExpression);
								conditions.Add(checkIsNullLocal ?? new SqlCondition(isNot, sqlCondition.Predicate));
							}

							break;
						}

						for (var i = 0; i < notCondition.Conditions.Count; i++)
						{
							var cond = notCondition.Conditions[i];
							var checkIsNull = CheckIsNull(cond.Predicate, false, isNotExpression);
							if (checkIsNull != null)
								cond = checkIsNull;
							notCondition.Conditions[i] = cond;
						}

						conditions.Add(new SqlCondition(true, notCondition));

						break;
					}

				default                    :
					var predicate = ConvertPredicate(context, expression);

					if (predicate is SqlPredicate.Expr ex)
					{
						var expr = ex.Expr1;

						if (expr.ElementType == QueryElementType.SearchCondition)
						{
							var sc = (SqlSearchCondition)expr;

							if (sc.Conditions.Count == 1)
							{
								conditions.Add(sc.Conditions[0]);
								break;
							}
						}
					}

					conditions.Add(CheckIsNull(predicate, false, isNotExpression) ?? new SqlCondition(false, predicate));

					break;
			}
		}


		static bool NeedNullCheck(ISqlExpression expr)
		{
			if (null != new QueryVisitor().Find(expr, e => e.ElementType == QueryElementType.SelectClause))
				return false;
			return expr.CanBeNull;
		}

		static SqlCondition? CheckIsNull(ISqlPredicate predicate, bool isNot, bool isNotExpression)
		{
			if (Configuration.Linq.CompareNullsAsValues == false)
				return null;

			var inList = predicate as SqlPredicate.InList;

			// ili this will fail https://github.com/linq2db/linq2db/issues/909
			//
			//if (predicate is SelectQuery.SearchCondition)
			//{
			//	var sc = (SelectQuery.SearchCondition) predicate;

			//	inList = QueryVisitor
			//		.Find(sc, _ => _.ElementType == QueryElementType.InListPredicate) as SelectQuery.Predicate.InList;

			//	if (inList != null)
			//	{
			//		isNot = QueryVisitor.Find(sc, _ =>
			//		{
			//			var condition = _ as SelectQuery.Condition;
			//			return condition != null && condition.IsNot;
			//		}) != null;
			//	}
			//}

			if (null != new QueryVisitor().Find(predicate, e => e.ElementType == QueryElementType.SelectClause))
				return null;

			if (predicate.CanBeNull && predicate is SqlPredicate.ExprExpr || inList != null)
			{
				var exprExpr = predicate as SqlPredicate.ExprExpr;

				if (exprExpr != null &&
					(
						exprExpr.Operator == SqlPredicate.Operator.NotEqual /*&& isNot == false*/ ||
						exprExpr.Operator == SqlPredicate.Operator.Equal    /*&& isNot == true */
					) ||
					inList != null && inList.IsNot || isNot)
				{
					var expr1 = exprExpr != null ? exprExpr.Expr1 : inList!.Expr1;
					var expr2 = exprExpr?.Expr2;

					var nullValue1 =                 new QueryVisitor().Find(expr1, _ => _ is IValueContainer);
					var nullValue2 = expr2 != null ? new QueryVisitor().Find(expr2, _ => _ is IValueContainer) : null;

					var hasNullValue =
						   nullValue1 != null && ((IValueContainer) nullValue1).Value == null
						|| nullValue2 != null && ((IValueContainer) nullValue2).Value == null;

					if (!hasNullValue)
					{
						var expr1IsField =                  expr1.CanBeNull && new QueryVisitor().Find(expr1, _ => _.ElementType == QueryElementType.SqlField) != null;
						var expr2IsField = expr2 != null && expr2.CanBeNull && new QueryVisitor().Find(expr2, _ => _.ElementType == QueryElementType.SqlField) != null;

						var nullableField = expr1IsField
							? expr1
							: expr2IsField ? expr2 : null;

						if (nullableField != null)
						{
							var checkNullPredicate = new SqlPredicate.IsNull(nullableField, exprExpr != null && exprExpr.Operator == SqlPredicate.Operator.Equal);

							var predicateIsNot = isNot && inList == null;
							predicate = BasicSqlOptimizer.OptimizePredicate(predicate, ref predicateIsNot);

							if (predicate is SqlPredicate.ExprExpr ee &&
								(!ee.Expr1.CanBeNull || !ee.Expr2.CanBeNull) &&
								(
									ee.Operator != SqlPredicate.Operator.NotEqual && !isNot && !isNotExpression ||
									ee.Operator == SqlPredicate.Operator.NotEqual &&  isNot &&  isNotExpression
								))
							{
								return null;
							}

							var orCondition = new SqlSearchCondition(
								new SqlCondition(false,          checkNullPredicate),
								new SqlCondition(predicateIsNot, predicate));

							orCondition.Conditions[0].IsOr = exprExpr == null || exprExpr.Operator == SqlPredicate.Operator.NotEqual;

							var ret = new SqlCondition(false, orCondition);

							return ret;
						}
					}
				}
			}

			return null;
		}

		#endregion

		#region CanBeTranslatedToSql

		bool CanBeTranslatedToSql(IBuildContext context, Expression expr, bool canBeCompiled)
		{
			List<Expression>? ignoredMembers = null;

			return null == expr.Find(pi =>
			{
				if (ignoredMembers != null)
				{
					if (pi != ignoredMembers[ignoredMembers.Count - 1])
						throw new InvalidOperationException();

					if (ignoredMembers.Count == 1)
						ignoredMembers = null;
					else
						ignoredMembers.RemoveAt(ignoredMembers.Count - 1);

					return false;
				}

				switch (pi.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var ma   = (MemberExpression)pi;
							var attr = GetExpressionAttribute(ma.Member);

							if (attr == null && !ma.Member.IsNullableValueMember())
							{
								if (canBeCompiled)
								{
									var ctx = GetContext(context, pi);

									if (ctx == null)
										return !CanBeCompiled(pi);

									if (ctx.IsExpression(pi, 0, RequestFor.Object).Result)
										return !CanBeCompiled(pi);

									ignoredMembers = ma.Expression.GetMembers();
								}
							}

							break;
						}

					case ExpressionType.Parameter    :
						{
							var ctx = GetContext(context, pi);

							if (ctx == null)
								if (canBeCompiled)
									return !CanBeCompiled(pi);

							break;
						}

					case ExpressionType.Call         :
						{
							var e = (MethodCallExpression)pi;

							if (e.Method.DeclaringType != typeof(Enumerable))
							{
								var attr = GetExpressionAttribute(e.Method);

								if (attr == null && canBeCompiled)
									return !CanBeCompiled(pi);
							}

							break;
						}

					case ExpressionType.TypeIs       : return canBeCompiled;
					case ExpressionType.TypeAs       :
					case ExpressionType.New          : return true;

					case ExpressionType.NotEqual     :
					case ExpressionType.Equal        :
						{
							var e = (BinaryExpression)pi;

							Expression? obj = null;

							if (e.Left.NodeType == ExpressionType.Constant && ((ConstantExpression)e.Left).Value == null)
								obj = e.Right;
							else if (e.Right.NodeType == ExpressionType.Constant && ((ConstantExpression)e.Right).Value == null)
								obj = e.Left;

							if (obj != null)
							{
								var ctx = GetContext(context, obj);

								if (ctx != null)
								{
									if (ctx.IsExpression(obj, 0, RequestFor.Table).      Result ||
										ctx.IsExpression(obj, 0, RequestFor.Association).Result)
									{
										ignoredMembers = obj.GetMembers();
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
			return MappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedType!, member, a => a.Configuration);
		}

		internal Sql.TableFunctionAttribute? GetTableFunctionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member.ReflectedType!, member, a => a.Configuration);
		}

		public ISqlExpression Convert(ISqlExpression expr)
		{
			return DataContext.GetSqlOptimizer().ConvertExpression(expr);
		}

		public ISqlPredicate Convert(IBuildContext context, ISqlPredicate predicate)
		{
			return DataContext.GetSqlOptimizer().ConvertPredicate(context.SelectQuery, predicate);
		}

		internal ISqlExpression ConvertSearchCondition(ISqlExpression sqlExpression)
		{
			if (sqlExpression is SqlSearchCondition)
			{
				if (sqlExpression.CanBeNull)
				{
					var notExpr = new SqlSearchCondition
					{
						Conditions = { new SqlCondition(true, new SqlPredicate.Expr(sqlExpression)) }
					};

					return Convert(new SqlFunction(sqlExpression.SystemType!, "CASE", sqlExpression, new SqlValue(1), notExpr, new SqlValue(0), new SqlValue(sqlExpression.SystemType!, null)));
				}

				return Convert(new SqlFunction(sqlExpression.SystemType!, "CASE", sqlExpression, new SqlValue(1), new SqlValue(0)));
			}

			return sqlExpression;
		}

		bool IsNullConstant(Expression expr)
		{
			return expr.NodeType == ExpressionType.Constant  && ((ConstantExpression)expr).Value == null 
				|| expr.NodeType == ExpressionType.Extension && expr is DefaultValueExpression;
		}

		Expression RemoveNullPropagation(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary = (BinaryExpression)conditional.Test;
						if (IsNullConstant(binary.Right) || IsNullConstant(binary.Left))
						{
							if (IsNullConstant(conditional.IfFalse))
							{
								return conditional.IfTrue.Transform(e => RemoveNullPropagation(e));
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary = (BinaryExpression)conditional.Test;
						if (IsNullConstant(binary.Right) || IsNullConstant(binary.Left))
						{
							if (IsNullConstant(conditional.IfTrue))
							{
								return conditional.IfFalse.Transform(e => RemoveNullPropagation(e));
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
					var foundMember = typeMembers.Find(tm => tm.Name == param.Name);
					if (foundMember == null)
						foundMember = typeMembers.Find(tm =>
							tm.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
					if (foundMember == null)
						continue;

					if (members.ContainsKey(foundMember.MemberInfo))
						continue;

					var converted = arguments[i].Transform(e => RemoveNullPropagation(e));

					if (!foundMember.MemberInfo.GetMemberType().IsAssignableFrom(converted.Type))
						continue;

					members.Add(foundMember.MemberInfo, converted);
				}
			}

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

								var converted = expr.Arguments[i].Transform(e => RemoveNullPropagation(e));
								members.Add(member, converted);

								if (member is MethodInfo info)
									members.Add(info.GetPropertyInfo(), converted);
							}
						}

						var isScalar = MappingSchema.IsScalarType(expr.Type);
						if (!isScalar)
							CollectParameters(expr.Type, expr.Constructor, expr.Arguments);

						return members.Count > 0 || !isScalar;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var typeMembers = TypeAccessor.GetAccessor(expr.Type).Members;

						var dic  = typeMembers
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo.Name, _ => _.i);

						foreach (var binding in expr.Bindings.Cast<MemberAssignment>().OrderBy(b => dic.ContainsKey(b.Member.Name) ? dic[b.Member.Name] : 1000000))
						{
							var converted = binding.Expression.Transform(e => RemoveNullPropagation(e));
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
				var queryableAccessorDic = new Dictionary<Expression, QueryableAccessor>();
				for (var index = 0; index < _ctes.Count; index++)
				{
					var tuple = _ctes[index];
					if (tuple.Item1.EqualsTo(cteExpression, DataContext, queryableAccessorDic, null, null,
						compareConstantValues: false))
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

		private List<Tuple<Func<IDataContext, Expression, object?[]?, object?>, Func<IDataContext, Expression, object?[]?, Task<object?>>>>? _preambles;

		public static readonly ParameterExpression PreambleParam =
			Expression.Parameter(typeof(object[]), "preamble");

		public int RegisterPreamble<T>(Func<IDataContext, Expression, object?[]?, T> func, Func<IDataContext, Expression, object?[]?, Task<T>> funcAsync)
		{
			if (_preambles == null)
				_preambles = new List<Tuple<Func<IDataContext, Expression, object?[]?, object?>,Func<IDataContext, Expression, object?[]?, Task<object?>>>>();
			_preambles.Add(
				Tuple.Create< Func<IDataContext, Expression, object?[]?, object?>, Func<IDataContext, Expression, object?[]?, Task<object?>>>(
					(dc, e, ps) => func(dc, e, ps),
					async (dc, e, ps) => await funcAsync(dc, e, ps)));
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
