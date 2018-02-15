using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	partial class ExpressionBuilder
	{
		#region Build Where

		public IBuildContext BuildWhere(IBuildContext parent, IBuildContext sequence, LambdaExpression condition, bool checkForSubQuery, bool enforceHaving = false)
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

						if (ma.Member.IsNullableValueMember() || ma.Member.IsNullableHasValueMember())
							return true;

						if (Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member) != null)
							return true;

						var ctx = GetContext(context, expr);

						if (ctx == null)
							return true;

						var expres = ctx.IsExpression(expr, 0, RequestFor.Expression);

						if (expres.Result)
						{
							if (expres.Expression != null && IsGrouping(expres.Expression))
							{
								isHaving = true;
								return false;
							}

							makeSubQuery = true;
						}
						else
						{
							if (IsGrouping(expr))
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

							if (IsGrouping(e))
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

		bool IsGrouping(Expression expression)
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
			{
				if (context.SelectQuery.Select.SkipValue is SqlParameter && sql.Select.TakeValue is SqlValue)
				{
					var skip = (SqlParameter)sql.Select.SkipValue;
					var parm = (SqlParameter)sql.Select.SkipValue.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);

					parm.SetTakeConverter((int)((SqlValue)sql.Select.TakeValue).Value);

					sql.Select.Take(parm, hints);

					var ep = (from pm in CurrentSqlParameters where ReferenceEquals(pm.SqlParameter, skip) select pm).First();

					ep = new ParameterAccessor
					(
						ep.Expression,
						ep.Accessor,
						ep.DataTypeAccessor,
						parm
					);

					CurrentSqlParameters.Add(ep);
				}
				else
					sql.Select.Take(Convert(
						context,
						new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue, Precedence.Additive)), hints);
			}

			if (!DataContext.SqlProviderFlags.GetAcceptsTakeAsParameterFlag(sql))
			{
				if (sql.Select.TakeValue is SqlParameter p)
					p.IsQueryParameter = false;
			}
		}

		#endregion

		#region SubQueryToSql

		public IBuildContext GetSubQuery(IBuildContext context, MethodCallExpression expr)
		{
			var info = new BuildInfo(context, expr, new SelectQuery { ParentSelect = context.SelectQuery });
			var ctx  = BuildSequence(info);

			if (ctx.SelectQuery.Select.Columns.Count == 0 &&
				(ctx.IsExpression(null, 0, RequestFor.Expression).Result ||
				 ctx.IsExpression(null, 0, RequestFor.Field).     Result))
			{
				ctx.ConvertToIndex(null, 0, ConvertFlags.Field);
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
							subQuery.GroupBy.IsEmpty && !subQuery.Select.HasModifier && !subQuery.HasUnion &&
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

				var mc = arg as MethodCallExpression;

				while (mc != null)
				{
					if (!mc.IsQueryable())
						return GetTableFunctionAttribute(mc.Method) != null;

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

			while (expr != null && expr.NodeType == ExpressionType.MemberAccess)
				expr = ((MemberExpression)expr).Expression;

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
							var cm = ConvertMethod((MethodCallExpression)e);
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
								var expr = body.Transform(wpi => wpi.NodeType == ExpressionType.Parameter ? ma.Expression : wpi);

								if (expr.Type != e.Type)
									expr = new ChangeTypeExpression(expr, e.Type);

								return new TransformInfo(ConvertExpression(expr));
							}

							if (ma.Member.IsNullableValueMember())
							{
								var ntype  = typeof(ConvertHelper<>).MakeGenericType(ma.Type);
								var helper = (IConvertHelper)Activator.CreateInstance(ntype);
								var expr   = helper.ConvertNull(ma);

								return new TransformInfo(ConvertExpression(expr));
							}

							if (ma.Member.DeclaringType == typeof(TimeSpan))
							{
								switch (ma.Expression.NodeType)
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
				}

				return new TransformInfo(e);
			});
		}

		Expression ConvertMethod(MethodCallExpression pi)
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

						return n < 0 ? pi.Object : pi.Arguments[n];
					}
				}

				return wpi;
			});

			if (pi.Method.ReturnType != pie.Type)
				pie = new ChangeTypeExpression(pie, pi.Method.ReturnType);

			return pie;
		}

		Expression ConvertNew(NewExpression pi)
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

		public SqlInfo[] ConvertExpressions(IBuildContext context, Expression expression, ConvertFlags queryConvertFlag)
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

						return expr.Arguments
							.Select((arg,i) =>
							{
								var mi = expr.Members[i];
								if (mi is MethodInfo)
									mi = ((MethodInfo)mi).GetPropertyInfo();

								return ConvertExpressions(context, arg, queryConvertFlag).Select(si => si.Clone(mi));
							})
							.SelectMany(si => si)
							.ToArray();
					}

				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo, _ => _.i);

						return expr.Bindings
							.Where  (b => b is MemberAssignment)
							.Cast<MemberAssignment>()
							.OrderBy(b => dic[b.Member])
							.Select (a =>
							{
								var mi = a.Member;
								if (mi is MethodInfo)
									mi = ((MethodInfo)mi).GetPropertyInfo();

								return ConvertExpressions(context, a.Expression, queryConvertFlag).Select(si => si.Clone(mi));
							})
							.SelectMany(si => si)
							.ToArray();
					}
			}

			var ctx = GetContext(context, expression);

			if (ctx != null && ctx.IsExpression(expression, 0, RequestFor.Object).Result)
				return ctx.ConvertToSql(expression, 0, queryConvertFlag);

			return new[] { new SqlInfo { Sql = ConvertToSql(context, expression) } };
		}

		public ISqlExpression ConvertToSqlExpression(IBuildContext context, Expression expression)
		{
			var expr = ConvertExpression(expression);
			return ConvertToSql(context, expr);
		}

		public ISqlExpression ConvertToSql(IBuildContext context, Expression expression, bool unwrap = false)
		{
			if (!PreferServerSide(expression, false))
			{
				if (CanBeConstant(expression))
					return BuildConstant(expression);

				if (CanBeCompiled(expression))
					return BuildParameter(expression).SqlParameter;
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
						BuildSearchCondition(context, expression, condition.Conditions);
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
							case ExpressionType.AddChecked      : return Convert(context, new SqlBinaryExpression(t, l, "+", r, Precedence.Additive));
							case ExpressionType.And             : return Convert(context, new SqlBinaryExpression(t, l, "&", r, Precedence.Bitwise));
							case ExpressionType.Divide          : return Convert(context, new SqlBinaryExpression(t, l, "/", r, Precedence.Multiplicative));
							case ExpressionType.ExclusiveOr     : return Convert(context, new SqlBinaryExpression(t, l, "^", r, Precedence.Bitwise));
							case ExpressionType.Modulo          : return Convert(context, new SqlBinaryExpression(t, l, "%", r, Precedence.Multiplicative));
							case ExpressionType.Multiply:
							case ExpressionType.MultiplyChecked : return Convert(context, new SqlBinaryExpression(t, l, "*", r, Precedence.Multiplicative));
							case ExpressionType.Or              : return Convert(context, new SqlBinaryExpression(t, l, "|", r, Precedence.Bitwise));
							case ExpressionType.Power           : return Convert(context, new SqlFunction(t, "Power", l, r));
							case ExpressionType.Subtract        :
							case ExpressionType.SubtractChecked : return Convert(context, new SqlBinaryExpression(t, l, "-", r, Precedence.Subtraction));
							case ExpressionType.Coalesce        :
								{
									if (r is SqlFunction c)
									{
										if (c.Name == "Coalesce")
										{
											var parms = new ISqlExpression[c.Parameters.Length + 1];

											parms[0] = l;
											c.Parameters.CopyTo(parms, 1);

											return Convert(context, new SqlFunction(t, "Coalesce", parms));
										}
									}

									return Convert(context, new SqlFunction(t, "Coalesce", l, r));
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
								return Convert(context, new SqlBinaryExpression(t, new SqlValue(-1), "*", o, Precedence.Multiplicative));
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

						if (o.SystemType != null && s.Type == typeof(object))
						{
							t = o.SystemType;
							s = SqlDataType.GetDataType(t);
						}

						if (e.Type == t ||
							t.IsEnumEx()      && Enum.GetUnderlyingType(t)      == e.Type ||
							e.Type.IsEnumEx() && Enum.GetUnderlyingType(e.Type) == t)
							return o;

						return Convert(
							context,
							new SqlFunction(e.Type, "$Convert$", SqlDataType.GetDataType(e.Type), s, o));
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

							return Convert(context, new SqlFunction(e.Type, "CASE", parms));
						}

						return Convert(context, new SqlFunction(e.Type, "CASE", s, t, f));
					}

				case ExpressionType.MemberAccess :
					{
						var ma   = (MemberExpression)expression;
						var attr = GetExpressionAttribute(ma.Member);

						if (attr != null)
						{
							if (attr.ExpectExpression)
							{
								var exp = ConvertToSql(context, ma.Expression);
								return Convert(context, attr.GetExpression(ma.Member, exp));
							}
							else
							{
								return Convert(context, attr.GetExpression(ma.Member));
							}
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

				case ExpressionType.Call        :
					{
						var e = (MethodCallExpression)expression;
						var isAggregation = e.IsAggregate(MappingSchema);
						if ((isAggregation || e.IsQueryable()) && !ContainsBuilder.IsConstant(e))
						{
							if (IsSubQuery(context, e))
								return SubQueryToSql(context, e);

							if (isAggregation || CountBuilder.MethodNames.Contains(e.Method.Name))
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

							return SubQueryToSql(context, e);
						}

						var expr = ConvertMethod(e);

						if (expr != null)
							return ConvertToSql(context, expr, unwrap);

						var attr = GetExpressionAttribute(e.Method);

						if (attr != null)
						{
							var inlineParameters = DataContext.InlineParameters;

							if (attr.InlineParameters)
								DataContext.InlineParameters = true;

							var sqlExpression = attr.GetExpression(MappingSchema, context.SelectQuery, e, _ => ConvertToSql(context, _));
							if (sqlExpression != null)
								return Convert(context, sqlExpression);

							var parms = new List<ISqlExpression>();

							if (e.Object != null)
								parms.Add(ConvertToSql(context, e.Object));

							ParameterInfo[] pis = null;

							for (var i = 0; i < e.Arguments.Count; i++)
							{
								var arg = e.Arguments[i];

								if (arg is NewArrayExpression nae)
								{
									if (pis == null)
										pis = e.Method.GetParameters();

									var p = pis[i];

									if (p.GetCustomAttributesEx(true).OfType<ParamArrayAttribute>().Any())
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

							return Convert(context, attr.GetExpression(e.Method, parms.ToArray()));
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
						BuildSearchCondition(context, expression, condition.Conditions);
						return condition;
					}

				case ChangeTypeExpression.ChangeTypeType :
					return ConvertToSql(context, ((ChangeTypeExpression)expression).Expression);

				case ExpressionType.Extension :
					{
						if (expression.CanReduce)
							return ConvertToSql(context, expression.Reduce());
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

		readonly HashSet<Expression> _convertedPredicates = new HashSet<Expression>();

		#endregion

		#region IsServerSideOnly

		Expression _lastExpr3;
		bool       _lastResult3;

		bool IsServerSideOnly(Expression expr)
		{
			if (_lastExpr3 == expr)
				return _lastResult3;

			var result = false;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var ex = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, ex.Expression?.Type, ex.Member);

						if (l != null)
						{
							result = IsServerSideOnly(l.Body.Unwrap());
						}
						else
						{
							var attr = GetExpressionAttribute(ex.Member);
							result = attr != null && attr.ServerSideOnly;
						}

						break;
					}

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						if (e.Method.DeclaringType == typeof(Enumerable))
						{
							if (CountBuilder.MethodNames.Contains(e.Method.Name) || e.IsAggregate(MappingSchema))
								result = IsQueryMember(e.Arguments[0]);
						}
						else if (e.IsAggregate(MappingSchema) || e.IsAssociation(MappingSchema))
						{
							result = true;
						}
						else if (e.Method.DeclaringType == typeof(Queryable))
						{
							switch (e.Method.Name)
							{
								case "Any"      :
								case "All"      :
								case "Contains" : result = true; break;
							}
						}
						else
						{
							var l = Expressions.ConvertMember(MappingSchema, e.Object?.Type, e.Method);

							if (l != null)
							{
								result = l.Body.Unwrap().Find(IsServerSideOnly) != null;
							}
							else
							{
								var attr = GetExpressionAttribute(e.Method);
								result = attr != null && attr.ServerSideOnly;
							}
						}

						break;
					}
			}

			_lastExpr3 = expr;
			return _lastResult3 = result;
		}

		static bool IsQueryMember(Expression expr)
		{
			if (expr != null) switch (expr.NodeType)
			{
				case ExpressionType.Parameter    : return true;
				case ExpressionType.MemberAccess : return IsQueryMember(((MemberExpression)expr).Expression);
				case ExpressionType.Call         :
					{
						var call = (MethodCallExpression)expr;

						if (call.Method.DeclaringType == typeof(Queryable))
							return true;

						if (call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count > 0)
							return IsQueryMember(call.Arguments[0]);

						return IsQueryMember(call.Object);
					}
			}

			return false;
		}

		#endregion

		#region CanBeConstant

		Expression _lastExpr1;
		bool       _lastResult1;

		bool CanBeConstant(Expression expr)
		{
			if (_lastExpr1 == expr)
				return _lastResult1;

			var result = null == expr.Find(ex =>
			{
				if (ex is BinaryExpression || ex is UnaryExpression /*|| ex.NodeType == ExpressionType.Convert*/)
					return false;

				if (MappingSchema.GetConvertExpression(ex.Type, typeof(DataParameter), false, false) != null)
					return true;

				switch (ex.NodeType)
				{
					case ExpressionType.Constant     :
						{
							var c = (ConstantExpression)ex;

							if (c.Value == null || ex.Type.IsConstantable())
								return false;

							break;
						}

					case ExpressionType.MemberAccess :
						{
							var ma = (MemberExpression)ex;

							var l = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

							if (l != null)
								return l.Body.Unwrap().Find(CanBeConstant) == null;

							if (ma.Member.DeclaringType.IsConstantable() || ma.Member.IsNullableValueMember())
								return false;

							break;
						}

					case ExpressionType.Call         :
						{
							var mc = (MethodCallExpression)ex;

							if (mc.Method.DeclaringType.IsConstantable() || mc.Method.DeclaringType == typeof(object))
								return false;

							var attr = GetExpressionAttribute(mc.Method);

							if (attr != null && !attr.ServerSideOnly)
								return false;

							break;
						}
				}

				return true;
			});


			_lastExpr1 = expr;
			return _lastResult1 = result;
		}

		#endregion

		#region CanBeCompiled

		Expression _lastExpr2;
		bool       _lastResult2;

		bool CanBeCompiled(Expression expr)
		{
			if (_lastExpr2 == expr)
				return _lastResult2;

			var result = null == expr.Find(ex =>
			{
				if (IsServerSideOnly(ex))
					return true;

				switch (ex.NodeType)
				{
					case ExpressionType.Parameter    :
						return !ReferenceEquals(ex, ParametersParam);
/*
					case ExpressionType.MemberAccess :
						{
							var attr = GetExpressionAttribute(((MemberExpression)ex).Member);
							return attr != null && attr.ServerSideOnly;
						}

					case ExpressionType.Call         :
						{
							var attr = GetExpressionAttribute(((MethodCallExpression)ex).Method);
							return attr != null && attr.ServerSideOnly;
						}
*/
				}

				return false;
			});

			_lastExpr2 = expr;
			return _lastResult2 = result;
		}

		#endregion

		#region Build Constant

		readonly Dictionary<Expression,SqlValue> _constants = new Dictionary<Expression,SqlValue>();

		SqlValue BuildConstant(Expression expr)
		{
			if (_constants.TryGetValue(expr, out var value))
				return value;

			var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object)));
			var v      = lambda.Compile()();

			if (v != null && v.GetType().IsEnumEx())
			{
				var attrs = v.GetType().GetCustomAttributesEx(typeof(Sql.EnumAttribute), true);

				if (attrs.Length == 0)
					v = MappingSchema.EnumToValue((Enum)v);
			}

			value = MappingSchema.GetSqlValue(expr.Type, v);

			_constants.Add(expr, value);

			return value;
		}

		#endregion

		#region Build Parameter

		readonly Dictionary<Expression,ParameterAccessor> _parameters = new Dictionary<Expression, ParameterAccessor>();

		public readonly HashSet<Expression> AsParameters = new HashSet<Expression>();

		internal enum BuildParameterType
		{
			Default,
			InPredicate
		}

		ParameterAccessor BuildParameter(Expression expr, BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			if (_parameters.TryGetValue(expr, out var p))
				return p;

			string name = null;

			var newExpr = ReplaceParameter(_expressionAccessors, expr, nm => name = nm);

			p = CreateParameterAccessor(
				DataContext, newExpr.ValueExpression, newExpr.DataTypeExpression, expr, ExpressionParam, ParametersParam, name, buildParameterType);

			_parameters.Add(expr, p);
			CurrentSqlParameters.Add(p);

			return p;
		}

		class ValueTypeExpression
		{
			public Expression ValueExpression;
			public Expression DataTypeExpression;
		}

		ValueTypeExpression ReplaceParameter(IDictionary<Expression,Expression> expressionAccessors, Expression expression, Action<string> setName)
		{
			var resullt = new ValueTypeExpression() { DataTypeExpression = Expression.Constant(DataType.Undefined) };

			resullt.ValueExpression = expression.Transform(expr =>
			{
				if (expr.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)expr;

					if (!expr.Type.IsConstantable() || AsParameters.Contains(c))
					{
						if (expressionAccessors.TryGetValue(expr, out var val))
						{
							expr = Expression.Convert(val, expr.Type);

							if (expression.NodeType == ExpressionType.MemberAccess)
							{
								var ma = (MemberExpression)expression;

								var mt = GetMemberDataType(ma.Member);

								if (mt != null)
									resullt.DataTypeExpression = Expression.Constant(mt.Value);

								setName(ma.Member.Name);
							}
						}
					}
				}

				return expr;
			});

			return resullt;
		}

		#endregion

		#region Predicate Converter

		ISqlPredicate ConvertPredicate(IBuildContext context, Expression expression)
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

						ISqlPredicate predicate = null;

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
								typeof(IList).        IsSameOrParentOf(e.Method.DeclaringType) ||
								typeof(ICollection<>).IsSameOrParentOf(e.Method.DeclaringType))
							{
								predicate = ConvertInPredicate(context, e);
							}
						}
						else if (e.Method.Name == "ContainsValue" && typeof(Dictionary<,>).IsSameOrParentOf(e.Method.DeclaringType))
						{
							var args = e.Method.DeclaringType.GetGenericArguments(typeof(Dictionary<,>));
							var minf = EnumerableMethods
								.First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[1]);

							var expr = Expression.Call(
								minf,
								Expression.PropertyOrField(e.Object, "Values"),
								e.Arguments[0]);

							predicate = ConvertInPredicate(context, expr);
						}
						else if (e.Method.Name == "ContainsKey" && typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType))
						{
							var args = e.Method.DeclaringType.GetGenericArguments(typeof(IDictionary<,>));
							var minf = EnumerableMethods
								.First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[0]);

							var expr = Expression.Call(
								minf,
								Expression.PropertyOrField(e.Object, "Keys"),
								e.Arguments[0]);

							predicate = ConvertInPredicate(context, expr);
						}
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
						else if (e.Method == ReflectionHelper.Functions.String.Like11) predicate = ConvertLikePredicate(context, e);
						else if (e.Method == ReflectionHelper.Functions.String.Like12) predicate = ConvertLikePredicate(context, e);
#endif
						else if (e.Method == ReflectionHelper.Functions.String.Like21) predicate = ConvertLikePredicate(context, e);
						else if (e.Method == ReflectionHelper.Functions.String.Like22) predicate = ConvertLikePredicate(context, e);

						if (predicate != null)
							return Convert(context, predicate);

						var attr = GetExpressionAttribute(e.Method);

						if (attr != null && attr.IsPredicate)
							break;

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ExpressionType.Conditional  :
					return Convert(context,
						new SqlPredicate.ExprExpr(
							ConvertToSql(context, expression),
							SqlPredicate.Operator.Equal,
							new SqlValue(true)));

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expression;

						if (e.Member.Name == "HasValue" &&
							e.Member.DeclaringType.IsGenericTypeEx() &&
							e.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
						{
							var expr = ConvertToSql(context, e.Expression);
							return Convert(context, new SqlPredicate.IsNull(expr, true));
						}

						var attr = GetExpressionAttribute(e.Member);

						if (attr != null && attr.IsPredicate)
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
				return Convert(context, new SqlPredicate.ExprExpr(ex, SqlPredicate.Operator.Equal, new SqlValue(true)));

			return Convert(context, new SqlPredicate.Expr(ex));
		}

		Expression AddEqualTrue(Expression expr)
		{
			return Equal(MappingSchema, Expression.Constant(true), expr);
		}

		#region ConvertCompare

		ISqlPredicate ConvertCompare(IBuildContext context, ExpressionType nodeType, Expression left, Expression right)
		{
			if (left.NodeType == ExpressionType.Convert && left.Type == typeof(int) && (right.NodeType == ExpressionType.Constant || right.NodeType == ExpressionType.Convert))
			{
				var conv  = (UnaryExpression)left;

				if (conv.Operand.Type == typeof(char))
				{
					left  = conv.Operand;
					right = right.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression) right).Value))
						: ((UnaryExpression) right).Operand;
				}
			}

			if (right.NodeType == ExpressionType.Convert && right.Type == typeof(int) && (left.NodeType == ExpressionType.Constant || left.NodeType == ExpressionType.Convert))
			{
				var conv = (UnaryExpression)right;

				if (conv.Operand.Type == typeof(char))
				{
					right = conv.Operand;
					left  = left.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression) left).Value))
						: ((UnaryExpression) left).Operand;
				}
			}

			switch (nodeType)
			{
				case ExpressionType.Equal    :
				case ExpressionType.NotEqual :

					var p = ConvertObjectComparison(nodeType, context, left, context, right);
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
						p = ConvertNewObjectComparison(context, nodeType, left, right);
						if (p != null)
							return p;
					}

					break;
			}

			SqlPredicate.Operator op;

			switch (nodeType)
			{
				case ExpressionType.Equal             : op = SqlPredicate.Operator.Equal;          break;
				case ExpressionType.NotEqual          : op = SqlPredicate.Operator.NotEqual;       break;
				case ExpressionType.GreaterThan       : op = SqlPredicate.Operator.Greater;        break;
				case ExpressionType.GreaterThanOrEqual: op = SqlPredicate.Operator.GreaterOrEqual; break;
				case ExpressionType.LessThan          : op = SqlPredicate.Operator.Less;           break;
				case ExpressionType.LessThanOrEqual   : op = SqlPredicate.Operator.LessOrEqual;    break;
				default: throw new InvalidOperationException();
			}

			if (left.NodeType == ExpressionType.Convert || right.NodeType == ExpressionType.Convert)
			{
				var p = ConvertEnumConversion(context, left, op, right);
				if (p != null)
					return p;
			}

			var l = ConvertToSql(context, left);
			var r = ConvertToSql(context, right, true);

			var lValue = l as SqlValue;
			var rValue = r as SqlValue;

			if (lValue != null)
				lValue.DataType = GetDataType(r);

			if (rValue != null)
				rValue.DataType = GetDataType(l);

			switch (nodeType)
			{
				case ExpressionType.Equal   :
				case ExpressionType.NotEqual:

					if (!context.SelectQuery.IsParameterDependent &&
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
				l = Convert(context, new SqlFunction(typeof(bool), "CASE", l, new SqlValue(true), new SqlValue(false)));

			if (r is SqlSearchCondition)
				r = Convert(context, new SqlFunction(typeof(bool), "CASE", r, new SqlValue(true), new SqlValue(false)));

			return Convert(context, new SqlPredicate.ExprExpr(l, op, r));
		}

		#endregion

		#region ConvertEnumConversion

		ISqlPredicate ConvertEnumConversion(IBuildContext context, Expression left, SqlPredicate.Operator op, Expression right)
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

			if (!type.ToNullableUnderlying().IsEnumEx())
				return null;

			var dic = new Dictionary<object, object>();

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
							mapValue = origValue; //((ConstantExpression)value).Value;

						ISqlExpression l, r;

						SqlValue sqlvalue;
						var ce = MappingSchema.GetConverter(type, typeof(DataParameter), false);
						if (ce != null)
						{
							mapValue = ((DataParameter) ce.Lambda.Compile().DynamicInvoke(origValue)).Value;
							sqlvalue = new SqlValue(mapValue);
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

		ISqlPredicate ConvertObjectNullComparison(IBuildContext context, Expression left, Expression right, bool isEqual)
		{
			if (right.NodeType == ExpressionType.Constant && ((ConstantExpression)right).Value == null)
			{
				if (left.NodeType == ExpressionType.MemberAccess || left.NodeType == ExpressionType.Parameter)
				{
					var ctx = GetContext(context, left);

					if (ctx != null &&
						(ctx.IsExpression(left, 0, RequestFor.Object).Result ||
						 left.NodeType == ExpressionType.Parameter && ctx.IsExpression(left, 0, RequestFor.Field).Result))
					{
						return new SqlPredicate.Expr(new SqlValue(!isEqual));
					}
				}
			}

			return null;
		}

		#endregion

		#region ConvertObjectComparison

		public ISqlPredicate ConvertObjectComparison(
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
				lcols  = lmembers.Select(m => new SqlInfo(m.Key) { Sql = ConvertToSql(leftContext, m.Value) }).ToArray();
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
				lcols  = qsl.ConvertToSql(left, 0, ConvertFlags.Key);

				if (!sr)
					ProcessProjection(rmembers, right);
			}

			if (lcols.Length == 0)
				return null;

			var condition = new SqlSearchCondition();

			foreach (var lcol in lcols)
			{
				if (lcol.Members.Count == 0)
					throw new InvalidOperationException();

				ISqlExpression rcol = null;

				var lmember = lcol.Members[lcol.Members.Count - 1];

				if (sr)
					rcol = ConvertToSql(rightContext, Expression.MakeMemberAccess(right, lmember));
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

		internal ISqlPredicate ConvertNewObjectComparison(IBuildContext context, ExpressionType nodeType, Expression left, Expression right)
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

			var newRight = right as NewExpression;
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
					newRight != null ?
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
			if (member is MethodInfo)
				member = ((MethodInfo)member).GetPropertyInfo();

			var vte  = ReplaceParameter(_expressionAccessors, ex, _ => { });
			var par  = vte.ValueExpression;
			var expr = Expression.MakeMemberAccess(par.Type == typeof(object) ? Expression.Convert(par, member.DeclaringType) : par, member);
			var p    = CreateParameterAccessor(DataContext, expr, vte.DataTypeExpression, expr, ExpressionParam, ParametersParam, member.Name);

			_parameters.Add(expr, p);
			CurrentSqlParameters.Add(p);

			return p.SqlParameter;
		}

		DataType? GetMemberDataType(MemberInfo member)
		{
			var dta      = MappingSchema.GetAttribute<DataTypeAttribute>(member.ReflectedTypeEx(), member);
			var ca       = MappingSchema.GetAttribute<ColumnAttribute>  (member.ReflectedTypeEx(), member);
			var dataType = null as DataType?;

			if (ca != null)
				dataType = ca.DataType;

			if (dataType == null && dta?.DataType != null)
				dataType = dta.DataType.Value;

			return dataType;
		}

		static DataType? GetDataType(ISqlExpression expr)
		{
			DataType? result = null;

			QueryVisitor.Find(expr, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField:
						result = ((SqlField)e).DataType;
						return true;
					case QueryElementType.SqlParameter:
						result = ((SqlParameter)e).DataType;
						return true;
					case QueryElementType.SqlDataType:
						result = ((SqlDataType)e).DataType;
						return true;
					case QueryElementType.SqlValue:
						result = ((SqlValue)e).DataType;
						return true;
					default:
						return false;
				}
			});

			if (result == DataType.Undefined)
				result = null;

			return result;
		}

		internal static ParameterAccessor CreateParameterAccessor(
			IDataContext        dataContext,
			Expression          accessorExpression,
			Expression          dataTypeAccessorExpression,
			Expression          expression,
			ParameterExpression expressionParam,
			ParameterExpression parametersParam,
			string              name,
			BuildParameterType  buildParameterType = BuildParameterType.Default)
		{
			var type        = accessorExpression.Type;
			
			LambdaExpression expr = null;
			if (buildParameterType != BuildParameterType.InPredicate)
				expr = dataContext.MappingSchema.GetConvertExpression(type, typeof(DataParameter), createDefault: false);

			if (expr != null)
			{
				var body = expr.GetBody(accessorExpression);

				accessorExpression         = Expression.PropertyOrField(body, "Value");
				dataTypeAccessorExpression = Expression.PropertyOrField(body, "DataType");
			}
			else
			{
				var defaultType = Converter.GetDefaultMappingFromEnumType(dataContext.MappingSchema, type);

				if (defaultType != null)
				{
					var enumMapExpr = dataContext.MappingSchema.GetConvertExpression(type, defaultType);
					accessorExpression = enumMapExpr.GetBody(accessorExpression);
				}
			}

			// see #820
			accessorExpression = accessorExpression.Transform(e =>
			{
				switch (e.NodeType)
				{
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

			});


			var mapper = Expression.Lambda<Func<Expression,object[],object>>(
				Expression.Convert(accessorExpression, typeof(object)),
				new [] { expressionParam, parametersParam });

			var dataTypeAccessor = Expression.Lambda<Func<Expression,object[],DataType>>(
				Expression.Convert(dataTypeAccessorExpression, typeof(DataType)),
				new [] { expressionParam, parametersParam });

			return new ParameterAccessor
			(
				expression,
				mapper.Compile(),
				dataTypeAccessor.Compile(),
				new SqlParameter(accessorExpression.Type, name, null) { IsQueryParameter = !(dataContext.InlineParameters && accessorExpression.Type.IsScalar(false)) }
			);
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

			ISqlExpression expr = null;

			var ctx = GetContext(context, arg);

			if (ctx is TableBuilder.TableContext &&
				ctx.SelectQuery != context.SelectQuery &&
				ctx.IsExpression(arg, 0, RequestFor.Object).Result)
			{
				expr = ctx.SelectQuery;
			}

			if (expr == null)
			{
				var sql = ConvertExpressions(context, arg, ConvertFlags.Key);

				if (sql.Length == 1 && sql[0].Members.Count == 0)
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
						var p = BuildParameter(arr, BuildParameterType.InPredicate).SqlParameter;
						p.IsQueryParameter = false;
						return new SqlPredicate.InList(expr, false, p);
					}

					break;
			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		#endregion

		#region LIKE predicate

		ISqlPredicate ConvertLikePredicate(IBuildContext context, MethodCallExpression expression, string start, string end)
		{
			var e = expression;
			var o = ConvertToSql(context, e.Object);
			var a = ConvertToSql(context, e.Arguments[0]);

			if (a is SqlValue sqlValue)
			{
				var value = sqlValue.Value;

				if (value == null)
					throw new LinqException("NULL cannot be used as a LIKE predicate parameter.");

				return value.ToString().IndexOfAny(new[] { '%', '_' }) < 0?
					new SqlPredicate.Like(o, false, new SqlValue(start + value + end), null):
					new SqlPredicate.Like(o, false, new SqlValue(start + EscapeLikeText(value.ToString()) + end), new SqlValue('~'));
			}

			if (a is SqlParameter p)
			{
				var ep = (from pm in CurrentSqlParameters where pm.SqlParameter == p select pm).First();

				ep = new ParameterAccessor
				(
					ep.Expression,
					ep.Accessor,
					ep.DataTypeAccessor,
					new SqlParameter(ep.Expression.Type, p.Name, p.Value)
					{
						LikeStart        = start,
						LikeEnd          = end,
						ReplaceLike      = p.ReplaceLike,
						IsQueryParameter = !(DataContext.InlineParameters && ep.Expression.Type.IsScalar(false))
					}
				);

				CurrentSqlParameters.Add(ep);

				return new SqlPredicate.Like(o, false, ep.SqlParameter, new SqlValue('~'));
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

			return new SqlPredicate.Like(o, false, expr, new SqlValue('~'));
		}

		ISqlPredicate ConvertLikePredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e  = expression;
			var a1 = ConvertToSql(context, e.Arguments[0]);
			var a2 = ConvertToSql(context, e.Arguments[1]);

			ISqlExpression a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2]);

			return new SqlPredicate.Like(a1, false, a2, a3);
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
			var table       = new TableBuilder.TableContext(this, new BuildInfo((IBuildContext)null, Expression.Constant(null), new SelectQuery()), typeOperand);

			if (typeOperand == table.ObjectType && table.InheritanceMapping.All(m => m.Type != typeOperand))
				return Convert(table, new SqlPredicate.Expr(new SqlValue(true)));

			var mapping = table.InheritanceMapping.Select((m,i) => new { m, i }).Where(m => typeOperand.IsAssignableFrom(m.m.Type) && !m.m.IsDefault).ToList();
			var isEqual = true;

			if (mapping.Count == 0)
			{
				mapping = table.InheritanceMapping.Select((m,i) => new { m, i }).Where(m => !m.m.IsDefault).ToList();
				isEqual = false;
			}

			Expression expr = null;

			foreach (var m in mapping)
			{
				var field = table.SqlTable.Fields[table.InheritanceMapping[m.i].DiscriminatorName];
				var ttype = field.ColumnDescriptor.MemberAccessor.TypeAccessor.Type;
				var obj   = expression.Expression;

				if (obj.Type != ttype)
					obj = Expression.Convert(expression.Expression, ttype);

				var left = Expression.PropertyOrField(obj, field.Name);
				var code = m.m.Code;

				if (code == null)
					code = left.Type.GetDefaultValue();
				else if (left.Type != code.GetType())
					code = Converter.ChangeType(code, left.Type, MappingSchema);

				Expression right = Expression.Constant(code, left.Type);

				var e = isEqual ? Expression.Equal(left, right) : Expression.NotEqual(left, right);

				expr = expr != null ? Expression.OrElse(expr, e) : e;
			}

			return ConvertPredicate(context, expr);
		}

		#endregion

		#endregion

		#region Search Condition Builder

		internal void BuildSearchCondition(IBuildContext context, Expression expression, List<SqlCondition> conditions)
		{
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
						var e            = expression as UnaryExpression;
						var notCondition = new SqlSearchCondition();

						BuildSearchCondition(context, e.Operand, notCondition.Conditions);

						if (notCondition.Conditions.Count == 1 && notCondition.Conditions[0].Predicate is SqlPredicate.NotExpr)
						{
							var p           = notCondition.Conditions[0].Predicate as SqlPredicate.NotExpr;
							p.IsNot         = !p.IsNot;

							var checkIsNull = CheckIsNull(notCondition.Conditions[0].Predicate, true);

							conditions.Add(checkIsNull ?? notCondition.Conditions[0]);
						}
						else
						{
							var checkIsNull = CheckIsNull(notCondition.Conditions[0].Predicate, true);

							conditions.Add(checkIsNull ?? new SqlCondition(true, notCondition));
						}

						break;
					}

				case ExpressionType.Equal :
					{
						if (expression.Type == typeof(bool))
						{
							var e = (BinaryExpression)expression;

							Expression ce = null, ee = null;

							if      (e.Left.NodeType  == ExpressionType.Constant) { ce = e.Left;  ee = e.Right; }
							else if (e.Right.NodeType == ExpressionType.Constant) { ce = e.Right; ee = e.Left; }

							if (ce != null)
							{
								var value = ((ConstantExpression)ce).Value;

								if (value is bool b && b == false)
								{
									BuildSearchCondition(context, Expression.Not(ee), conditions);
									return;
								}
							}
						}

						goto default;
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

					conditions.Add(CheckIsNull(predicate, false) ?? new SqlCondition(false, predicate));

					break;
			}
		}

		static SqlCondition CheckIsNull(ISqlPredicate predicate, bool isNot)
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

			if (predicate.CanBeNull && predicate is SqlPredicate.ExprExpr || inList != null)
			{
				var exprExpr = predicate as SqlPredicate.ExprExpr;

				if (exprExpr != null &&
					(
						exprExpr.Operator == SqlPredicate.Operator.NotEqual && isNot == false ||
						exprExpr.Operator == SqlPredicate.Operator.Equal    && isNot == true
					) ||
					inList != null && inList.IsNot || isNot)
				{
					var expr1 = exprExpr != null ? exprExpr.Expr1 : inList.Expr1;
					var expr2 = exprExpr?.Expr2;

					var nullValue1 =                 QueryVisitor.Find(expr1, _ => _ is IValueContainer);
					var nullValue2 = expr2 != null ? QueryVisitor.Find(expr2, _ => _ is IValueContainer) : null;

					var hasNullValue =
						   nullValue1 != null && ((IValueContainer) nullValue1).Value == null
						|| nullValue2 != null && ((IValueContainer) nullValue2).Value == null;

					if (!hasNullValue)
					{
						var expr1IsField =                  expr1.CanBeNull && QueryVisitor.Find(expr1, _ => _.ElementType == QueryElementType.SqlField) != null;
						var expr2IsField = expr2 != null && expr2.CanBeNull && QueryVisitor.Find(expr2, _ => _.ElementType == QueryElementType.SqlField) != null;

						var nullableField = expr1IsField
							? expr1
							: expr2IsField ? expr2 : null;

						if (nullableField != null)
						{
							var checkNullPredicate = new SqlPredicate.IsNull(nullableField, false);

							var orCondition = new SqlSearchCondition(
								new SqlCondition(false,                   checkNullPredicate),
								new SqlCondition(isNot && inList == null, predicate));

							orCondition.Conditions[0].IsOr = true;

							return new SqlCondition(false, orCondition);
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
			List<Expression> ignoredMembers = null;

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

							Expression obj = null;

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
				}

				return false;
			});
		}

		#endregion

		#region Helpers

		public IBuildContext GetContext([JetBrains.Annotations.NotNull] IBuildContext current, Expression expression)
		{
			var root = expression.GetRootObject(MappingSchema);

			for (; current != null; current = current.Parent)
				if (current.IsExpression(root, 0, RequestFor.Root).Result)
					return current;

			return null;
		}

		Sql.ExpressionAttribute GetExpressionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedTypeEx(), member, a => a.Configuration);
		}

		internal Sql.TableFunctionAttribute GetTableFunctionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member.ReflectedTypeEx(), member, a => a.Configuration);
		}

		public ISqlExpression Convert(IBuildContext context, ISqlExpression expr)
		{
			return DataContext.GetSqlOptimizer().ConvertExpression(expr);
		}

		public ISqlPredicate Convert(IBuildContext context, ISqlPredicate predicate)
		{
			return DataContext.GetSqlOptimizer().ConvertPredicate(context.SelectQuery, predicate);
		}

		internal ISqlExpression ConvertSearchCondition(IBuildContext context, ISqlExpression sqlExpression)
		{
			if (sqlExpression is SqlSearchCondition)
			{
				if (sqlExpression.CanBeNull)
				{
					var notExpr = new SqlSearchCondition
					{
						Conditions = { new SqlCondition(true, new SqlPredicate.Expr(sqlExpression)) }
					};

					return Convert(context, new SqlFunction(sqlExpression.SystemType, "CASE", sqlExpression, new SqlValue(1), notExpr, new SqlValue(0), new SqlValue(null)));
				}

				return Convert(context, new SqlFunction(sqlExpression.SystemType, "CASE", sqlExpression, new SqlValue(1), new SqlValue(0)));
			}

			return sqlExpression;
		}

		public bool ProcessProjection(Dictionary<MemberInfo,Expression> members, Expression expression)
		{
			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
						if (expr.Members == null)
							return false;
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore ConditionIsAlwaysTrueOrFalse

						for (var i = 0; i < expr.Members.Count; i++)
						{
							var member = expr.Members[i];

							members.Add(member, expr.Arguments[i]);

							if (member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), expr.Arguments[i]);
						}

						return true;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo.Name, _ => _.i);

						foreach (var binding in expr.Bindings.Cast<MemberAssignment>().OrderBy(b => dic.ContainsKey(b.Member.Name) ? dic[b.Member.Name] : 1000000))
						{
							members.Add(binding.Member, binding.Expression);

							if (binding.Member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), binding.Expression);
						}

						return true;
					}

				// .Select(p => everything else)
				//
				default                        :
					return false;
			}
		}

		public void ReplaceParent(IBuildContext oldParent, IBuildContext newParent)
		{
			foreach (var context in Contexts)
				if (context != newParent)
					if (context.Parent == oldParent)
						if (newParent != null && newParent.Parent != context)
							context.Parent = newParent;
		}

		#endregion

		#region CTE

		readonly Dictionary<Expression, Tuple<CteClause,IBuildContext>> _ctes = new Dictionary<Expression, Tuple<CteClause,IBuildContext>>(new ExpressionEqualityComparer());
		readonly Dictionary<IQueryable, Expression> _ctesObjectMapping = new Dictionary<IQueryable, Expression>();

		public Tuple<CteClause, IBuildContext, Expression> RegisterCte(IQueryable queryable, Expression cteExpression, Func<CteClause> buildFunc)
		{
			if (cteExpression != null && queryable != null && !_ctesObjectMapping.ContainsKey(queryable))
			{
				_ctesObjectMapping.Add(queryable, cteExpression);
			}

			if (cteExpression == null)
			{
				cteExpression = _ctesObjectMapping[queryable];
			}

			if (!_ctes.TryGetValue(cteExpression, out var value))
			{
				var cte = buildFunc();
				value = Tuple.Create<CteClause, IBuildContext>(cte, null);
				_ctes.Add(cteExpression, value);
			}

			return Tuple.Create(value.Item1, value.Item2, cteExpression);
		}

		public Tuple<CteClause, IBuildContext> BuildCte(Expression cteExpression, Func<CteClause, Tuple<CteClause, IBuildContext>> buildFunc)
		{
			if (_ctes.TryGetValue(cteExpression, out var value))
				if (value.Item2 != null)
					return value;

			value = buildFunc(value?.Item1);
			_ctes.Remove(cteExpression);
			_ctes.Add(cteExpression, value);
			return value;
		}

		public IBuildContext GetCteContext(Expression cteExpression)
		{
			if (_ctes.TryGetValue(cteExpression, out var value))
				return value.Item2;
			return null;
		}

		#endregion
	}
}
