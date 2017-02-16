using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

#if !SILVERLIGHT && !NETFX_CORE || NETSTANDARD
using System.Data.SqlTypes;
#endif

using LinqToDB.Data;

namespace LinqToDB.Linq.Builder
{
	using Common;
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

			BuildSearchCondition(
				ctx,
				expr,
				enforceHaving || makeHaving && !ctx.SelectQuery.GroupBy.IsEmpty?
					ctx.SelectQuery.Having.SearchCondition.Conditions :
					ctx.SelectQuery.Where. SearchCondition.Conditions);

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

						if (Expressions.ConvertMember(MappingSchema, ma.Expression == null ? null : ma.Expression.Type, ma.Member) != null)
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

							if (Expressions.ConvertMember(MappingSchema, e.Object == null ? null : e.Object.Type, e.Method) != null)
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

		public void BuildTake(IBuildContext context, ISqlExpression expr)
		{
			var sql = context.SelectQuery;

			sql.Select.Take(expr);

			if (sql.Select.SkipValue != null &&
				 DataContextInfo.SqlProviderFlags.IsTakeSupported &&
				!DataContextInfo.SqlProviderFlags.GetIsSkipSupportedFlag(sql))
			{
				if (context.SelectQuery.Select.SkipValue is SqlParameter && sql.Select.TakeValue is SqlValue)
				{
					var skip = (SqlParameter)sql.Select.SkipValue;
					var parm = (SqlParameter)sql.Select.SkipValue.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);

					parm.SetTakeConverter((int)((SqlValue)sql.Select.TakeValue).Value);

					sql.Select.Take(parm);

					var ep = (from pm in CurrentSqlParameters where ReferenceEquals(pm.SqlParameter, skip) select pm).First();

					ep = new ParameterAccessor
					{
						Expression   = ep.Expression,
						Accessor     = ep.Accessor,
						SqlParameter = parm
					};

					CurrentSqlParameters.Add(ep);
				}
				else
					sql.Select.Take(Convert(
						context,
						new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue, Precedence.Additive)));
			}

			if (!DataContextInfo.SqlProviderFlags.GetAcceptsTakeAsParameterFlag(sql))
			{
				var p = sql.Select.TakeValue as SqlParameter;

				if (p != null)
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

			if (subSql != null)
				return subSql;

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
							query.GroupBy.Items.Count == ((SelectQuery.SearchCondition)cond.Predicate).Conditions.Count)
						{
							var func = (SqlFunction)subQuery.Select.Columns[0].Expression;

							if (CountBuilder.MethodNames.Contains(func.Name))
								return SqlFunction.CreateCount(func.SystemType, query);
						}
					}
				}
			}

			return sequence.SelectQuery;
		}

		#endregion

		#region IsSubQuery

		bool IsSubQuery(IBuildContext context, MethodCallExpression call)
		{
			if (call.IsQueryable())
			{
				var info = new BuildInfo(context, call, new SelectQuery { ParentSelect = context.SelectQuery });

				if (!IsSequence(info))
					return false;

				var arg = call.Arguments[0];

				if (AggregationBuilder.MethodNames.Contains(call.Method.Name))
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

		Expression ConvertExpression(Expression expression)
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

							var equalityLeft  = b.Left as BinaryExpression;
							var constantRight = b.Right as ConstantExpression;

							if (equalityLeft != null && constantRight != null)
								if (equalityLeft.Type.GetGenericTypeDefinition() == typeof (System.Nullable<>))
									if (equalityLeft.NodeType == ExpressionType.Equal && equalityLeft.Left.Type == equalityLeft.Right.Type)
										if (constantRight.Value is bool && (bool) constantRight.Value == false)
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
							var l  = Expressions.ConvertMember(MappingSchema, ma.Expression == null ? null : ma.Expression.Type, ma.Member);

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
			var l = Expressions.ConvertMember(MappingSchema, pi.Object == null ? null : pi.Object.Type, pi.Method);
			return l == null ? null : ConvertMethod(pi, l);
		}

		static Expression ConvertMethod(MethodCallExpression pi, LambdaExpression lambda)
		{
			var ef    = lambda.Body.Unwrap();
			var parms = new Dictionary<string,int>(lambda.Parameters.Count);
			var pn    = pi.Method.IsStatic ? 0 : -1;

			foreach (var p in lambda.Parameters)
				parms.Add(p.Name, pn++);

			var pie = ef.Transform(wpi =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					int n;
					if (parms.TryGetValue(((ParameterExpression)wpi).Name, out n))
						return n < 0 ? pi.Object : pi.Arguments[n];
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

				case ExpressionType.MemberAccess :
					{
						if (queryConvertFlag == ConvertFlags.All)
						{
							var expr = (MemberExpression) expression;
							if (!MappingSchema.IsScalarType(expr.Type) && !expr.Type.IsNullable())
							{
								var descriptor = MappingSchema.GetEntityDescriptor(expr.Type);
								if (descriptor.Columns.Count > 0)
								{
									return descriptor.Columns
										.Where (c => c.MemberInfo.DeclaringType == expr.Type) // filtering inheritance columns
										.Select(c =>
										{
											return ConvertExpressions(context, Expression.MakeMemberAccess(expr, c.MemberInfo), ConvertFlags.Field)
												.Select(si => si.Clone(c.MemberInfo));
										})
										.SelectMany(si => si)
										.ToArray();
								}
							}
						}
						break;
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
			if (!PreferServerSide(expression))
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
						var condition = new SelectQuery.SearchCondition();
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
									if (r is SqlFunction)
									{
										var c = (SqlFunction)r;

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

#if !SILVERLIGHT && !NETFX_CORE  || NETSTANDARD
						if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
							return o;
#endif

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

						if (f is SqlFunction)
						{
							var c = (SqlFunction)f;

							if (c.Name == "CASE")
							{
								var parms = new ISqlExpression[c.Parameters.Length + 2];

								parms[0] = s;
								parms[1] = t;
								c.Parameters.CopyTo(parms, 2);

								return Convert(context, new SqlFunction(e.Type, "CASE", parms));
							}
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

						if (e.IsQueryable())
						{
							if (IsSubQuery(context, e))
								return SubQueryToSql(context, e);

							if (CountBuilder.MethodNames.Concat(AggregationBuilder.MethodNames).Contains(e.Method.Name))
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
							var inlineParameters = DataContextInfo.DataContext.InlineParameters;

							if (attr.InlineParameters)
								DataContextInfo.DataContext.InlineParameters = true;

							var parms = new List<ISqlExpression>();

							if (e.Object != null)
								parms.Add(ConvertToSql(context, e.Object));

							ParameterInfo[] pis = null;

							for (var i = 0; i < e.Arguments.Count; i++)
							{
								var arg = e.Arguments[i];

								if (arg is NewArrayExpression)
								{
									if (pis == null)
										pis = e.Method.GetParameters();

									var p = pis[i];

									if (p.GetCustomAttributesEx(true).OfType<ParamArrayAttribute>().Any())
									{
										var nae = (NewArrayExpression)arg;

										parms.AddRange(nae.Expressions.Select(a => ConvertToSql(context, a)));
									}
									else
									{
										parms.Add(ConvertToSql(context, arg));
									}
								}
								else
								{
									parms.Add(ConvertToSql(context, arg));
								}
							}

							DataContextInfo.DataContext.InlineParameters = inlineParameters;

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

							var pie = l.Body.Transform(wpi =>
							{
								Expression ppi;
								return dic.TryGetValue(wpi, out ppi) ? ppi : wpi;
							});

							return ConvertToSql(context, pie);
						}

						break;
					}

				case ExpressionType.TypeIs :
					{
						var condition = new SelectQuery.SearchCondition();
						BuildSearchCondition(context, expression, condition.Conditions);
						return condition;
					}

				case ChangeTypeExpression.ChangeTypeType :
					return ConvertToSql(context, ((ChangeTypeExpression)expression).Expression);
			}

			if (expression.Type == typeof(bool) && _convertedPredicates.Add(expression))
			{
				var predicate = ConvertPredicate(context, expression);
				_convertedPredicates.Remove(expression);
				if (predicate != null)
					return new SelectQuery.SearchCondition(new SelectQuery.Condition(false, predicate));
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
						var l  = Expressions.ConvertMember(MappingSchema, ex.Expression == null ? null : ex.Expression.Type, ex.Member);

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
							if (CountBuilder.MethodNames.Concat(AggregationBuilder.MethodNames).Contains(e.Method.Name))
								result = IsQueryMember(e.Arguments[0]);
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
							var l = Expressions.ConvertMember(MappingSchema, e.Object == null ? null : e.Object.Type, e.Method);

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

							var l = Expressions.ConvertMember(MappingSchema, ma.Expression == null ? null : ma.Expression.Type, ma.Member);

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
			SqlValue value;

			if (_constants.TryGetValue(expr, out value))
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
			ParameterAccessor p;

			if (_parameters.TryGetValue(expr, out p))
				return p;

			string name = null;

			var newExpr = ReplaceParameter(_expressionAccessors, expr, nm => name = nm);

			p = CreateParameterAccessor(
				DataContextInfo.DataContext, newExpr, expr, ExpressionParam, ParametersParam, name, buildParameterType);

			_parameters.Add(expr, p);
			CurrentSqlParameters.Add(p);

			return p;
		}

		Expression ReplaceParameter(IDictionary<Expression,Expression> expressionAccessors, Expression expression, Action<string> setName)
		{
			return expression.Transform(expr =>
			{
				if (expr.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)expr;

					if (!expr.Type.IsConstantable() || AsParameters.Contains(c))
					{
						Expression val;
						
						if (expressionAccessors.TryGetValue(expr, out val))
						{
							expr = Expression.Convert(val, expr.Type);

							if (expression.NodeType == ExpressionType.MemberAccess)
							{
								var ma = (MemberExpression)expression;
								setName(ma.Member.Name);
							}
						}
					}
				}

				return expr;
			});
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
#if !SILVERLIGHT && !NETFX_CORE && !NETSTANDARD
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
						new SelectQuery.Predicate.ExprExpr(
							ConvertToSql(context, expression),
							SelectQuery.Predicate.Operator.Equal,
							new SqlValue(true)));

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expression;

						if (e.Member.Name == "HasValue" && 
							e.Member.DeclaringType.IsGenericTypeEx() && 
							e.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
						{
							var expr = ConvertToSql(context, e.Expression);
							return Convert(context, new SelectQuery.Predicate.IsNull(expr, true));
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
#if !SILVERLIGHT && !NETFX_CORE
						var e = (UnaryExpression)expression;

						if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
							return ConvertPredicate(context, e.Operand);
#endif

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ChangeTypeExpression.ChangeTypeType:
					return ConvertPredicate(context, AddEqualTrue(expression));
			}

			var ex = ConvertToSql(context, expression);

			if (SqlExpression.NeedsEqual(ex))
				return Convert(context, new SelectQuery.Predicate.ExprExpr(ex, SelectQuery.Predicate.Operator.Equal, new SqlValue(true)));

			return Convert(context, new SelectQuery.Predicate.Expr(ex));
		}

		Expression AddEqualTrue(Expression expr)
		{
			return Equal(MappingSchema, Expression.Constant(true), expr);
		}

		#region ConvertCompare

		ISqlPredicate ConvertCompare(IBuildContext context, ExpressionType nodeType, Expression left, Expression right)
		{
			if (left.NodeType == ExpressionType.Convert && left.Type == typeof(int) && right.NodeType == ExpressionType.Constant)
			{
				var conv = (UnaryExpression)left;

				if (conv.Operand.Type == typeof(char))
				{
					left  = conv.Operand;
					right = Expression.Constant(ConvertTo<char>.From(((ConstantExpression)right).Value));
				}
			}

			if (right.NodeType == ExpressionType.Convert && right.Type == typeof(int) && left.NodeType == ExpressionType.Constant)
			{
				var conv = (UnaryExpression)right;

				if (conv.Operand.Type == typeof(char))
				{
					right = conv.Operand;
					left  = Expression.Constant(ConvertTo<char>.From(((ConstantExpression)left).Value));
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

			SelectQuery.Predicate.Operator op;

			switch (nodeType)
			{
				case ExpressionType.Equal             : op = SelectQuery.Predicate.Operator.Equal;          break;
				case ExpressionType.NotEqual          : op = SelectQuery.Predicate.Operator.NotEqual;       break;
				case ExpressionType.GreaterThan       : op = SelectQuery.Predicate.Operator.Greater;        break;
				case ExpressionType.GreaterThanOrEqual: op = SelectQuery.Predicate.Operator.GreaterOrEqual; break;
				case ExpressionType.LessThan          : op = SelectQuery.Predicate.Operator.Less;           break;
				case ExpressionType.LessThanOrEqual   : op = SelectQuery.Predicate.Operator.LessOrEqual;    break;
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

					if (q != null)
					{
						q.Select.Columns.Add(new SelectQuery.Column(q, new SqlValue(1)));
					}

					break;
			}

			if (l is SelectQuery.SearchCondition)
				l = Convert(context, new SqlFunction(typeof(bool), "CASE", l, new SqlValue(true), new SqlValue(false)));

			if (r is SelectQuery.SearchCondition)
				r = Convert(context, new SqlFunction(typeof(bool), "CASE", r, new SqlValue(true), new SqlValue(false)));

			return Convert(context, new SelectQuery.Predicate.ExprExpr(l, op, r));
		}

		#endregion

		#region ConvertEnumConversion

		ISqlPredicate ConvertEnumConversion(IBuildContext context, Expression left, SelectQuery.Predicate.Operator op, Expression right)
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

						var    origValue = Enum.Parse(type, name, false);
						object mapValue;

						if (!dic.TryGetValue(origValue, out mapValue))
							return null;

						ISqlExpression l, r;

						if (left.NodeType == ExpressionType.Convert)
						{
							l = ConvertToSql(context, operand);
							r = MappingSchema.GetSqlValue(type, mapValue);
						}
						else
						{
							r = ConvertToSql(context, operand);
							l = MappingSchema.GetSqlValue(type, mapValue);
						}

						return Convert(context, new SelectQuery.Predicate.ExprExpr(l, op, r));
					}

				case ExpressionType.Convert:
					{
						value = ((UnaryExpression)value).Operand;

						var l = ConvertToSql(context, operand);
						var r = ConvertToSql(context, value);

						return Convert(context, new SelectQuery.Predicate.ExprExpr(l, op, r));
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
						return new SelectQuery.Predicate.Expr(new SqlValue(!isEqual));
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

				isNull = right is ConstantExpression && ((ConstantExpression)right).Value == null;
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

				isNull = right is ConstantExpression && ((ConstantExpression)right).Value == null;
				lcols  = qsl.ConvertToSql(left, 0, ConvertFlags.Key);

				if (!sr)
					ProcessProjection(rmembers, right);
			}

			if (lcols.Length == 0)
				return null;

			var condition = new SelectQuery.SearchCondition();

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

				var predicate = Convert(leftContext, new SelectQuery.Predicate.ExprExpr(
					lcol.Sql,
					nodeType == ExpressionType.Equal ? SelectQuery.Predicate.Operator.Equal : SelectQuery.Predicate.Operator.NotEqual,
					rex));

				condition.Conditions.Add(new SelectQuery.Condition(false, predicate));
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

			var condition = new SelectQuery.SearchCondition();

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
					new SelectQuery.Predicate.ExprExpr(
						lex,
						nodeType == ExpressionType.Equal ? SelectQuery.Predicate.Operator.Equal : SelectQuery.Predicate.Operator.NotEqual,
						rex));

				condition.Conditions.Add(new SelectQuery.Condition(false, predicate));
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

			var par  = ReplaceParameter(_expressionAccessors, ex, _ => {});
			var expr = Expression.MakeMemberAccess(par.Type == typeof(object) ? Expression.Convert(par, member.DeclaringType) : par, member);
			var p    = CreateParameterAccessor(DataContextInfo.DataContext, expr, expr, ExpressionParam, ParametersParam, member.Name);

			_parameters.Add(expr, p);
			CurrentSqlParameters.Add(p);

			return p.SqlParameter;
		}

		internal static ParameterAccessor CreateParameterAccessor(
			IDataContext        dataContext,
			Expression          accessorExpression,
			Expression          expression,
			ParameterExpression expressionParam,
			ParameterExpression parametersParam,
			string              name,
			BuildParameterType  buildParameterType = BuildParameterType.Default)
		{
			var type        = accessorExpression.Type;
			var defaultType = Converter.GetDefaultMappingFromEnumType(dataContext.MappingSchema, type);

			if (defaultType != null)
			{
				var enumMapExpr = dataContext.MappingSchema.GetConvertExpression(type, defaultType);
				accessorExpression = enumMapExpr.GetBody(accessorExpression);
			}

			LambdaExpression expr = null;
			if (buildParameterType != BuildParameterType.InPredicate)
				expr = dataContext.MappingSchema.GetConvertExpression(type, typeof(DataParameter), createDefault: false);

			if (expr != null)
				accessorExpression = Expression.PropertyOrField(expr.GetBody(accessorExpression), "Value");

			var mapper = Expression.Lambda<Func<Expression,object[],object>>(
				Expression.Convert(accessorExpression, typeof(object)),
				new [] { expressionParam, parametersParam });

			return new ParameterAccessor
			{
				Expression   = expression,
				Accessor     = mapper.Compile(),
				SqlParameter = new SqlParameter(accessorExpression.Type, name, null) { IsQueryParameter = !dataContext.InlineParameters }
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
							return new SelectQuery.Predicate.Expr(new SqlValue(false));

						var exprs  = new ISqlExpression[newArr.Expressions.Count];

						for (var i = 0; i < newArr.Expressions.Count; i++)
							exprs[i] = ConvertToSql(context, newArr.Expressions[i]);

						return new SelectQuery.Predicate.InList(expr, false, exprs);
					}

				default :

					if (CanBeCompiled(arr))
					{
						var p = BuildParameter(arr, BuildParameterType.InPredicate).SqlParameter;
						p.IsQueryParameter = false;
						return new SelectQuery.Predicate.InList(expr, false, p);
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

			if (a is SqlValue)
			{
				var value = ((SqlValue)a).Value;

				if (value == null)
					throw new LinqException("NULL cannot be used as a LIKE predicate parameter.");

				return value.ToString().IndexOfAny(new[] { '%', '_' }) < 0?
					new SelectQuery.Predicate.Like(o, false, new SqlValue(start + value + end), null):
					new SelectQuery.Predicate.Like(o, false, new SqlValue(start + EscapeLikeText(value.ToString()) + end), new SqlValue('~'));
			}

			if (a is SqlParameter)
			{
				var p  = (SqlParameter)a;
				var ep = (from pm in CurrentSqlParameters where pm.SqlParameter == p select pm).First();

				ep = new ParameterAccessor
				{
					Expression   = ep.Expression,
					Accessor     = ep.Accessor,
					SqlParameter = new SqlParameter(ep.Expression.Type, p.Name, p.Value)
					{
						LikeStart        = start,
						LikeEnd          = end,
						ReplaceLike      = p.ReplaceLike,
						IsQueryParameter = !DataContextInfo.DataContext.InlineParameters
					},
				};

				CurrentSqlParameters.Add(ep);

				return new SelectQuery.Predicate.Like(o, false, ep.SqlParameter, new SqlValue('~'));
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

			return new SelectQuery.Predicate.Like(o, false, expr, new SqlValue('~'));
		}

		ISqlPredicate ConvertLikePredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e  = expression;
			var a1 = ConvertToSql(context, e.Arguments[0]);
			var a2 = ConvertToSql(context, e.Arguments[1]);

			ISqlExpression a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2]);

			return new SelectQuery.Predicate.Like(a1, false, a2, a3);
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
				return Convert(table, new SelectQuery.Predicate.Expr(new SqlValue(true)));

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
						var cond = new SelectQuery.SearchCondition();

						if (inheritanceMapping.Any(m => m.Type == toType))
						{
							foreach (var m in inheritanceMapping.Where(m => !m.IsDefault))
							{
								cond.Conditions.Add(
									new SelectQuery.Condition(
										false, 
										Convert(context,
											new SelectQuery.Predicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SelectQuery.Predicate.Operator.NotEqual,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code)))));
							}
						}
						else
						{
							foreach (var m in inheritanceMapping.Where(m => toType.IsSameOrParentOf(m.Type)))
							{
								cond.Conditions.Add(
									new SelectQuery.Condition(
										false,
										Convert(context,
											new SelectQuery.Predicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SelectQuery.Predicate.Operator.Equal,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
										true));
							}
						}

						return cond;
					}

				case 1 :
					return Convert(context,
						new SelectQuery.Predicate.ExprExpr(
							getSql(mapping[0].DiscriminatorName),
							SelectQuery.Predicate.Operator.Equal,
							MappingSchema.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code)));

				default:
					{
						var cond = new SelectQuery.SearchCondition();

						foreach (var m in mapping)
						{
							cond.Conditions.Add(
								new SelectQuery.Condition(
									false,
									Convert(context,
										new SelectQuery.Predicate.ExprExpr(
											getSql(m.DiscriminatorName),
											SelectQuery.Predicate.Operator.Equal,
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
				return Convert(table, new SelectQuery.Predicate.Expr(new SqlValue(true)));

			var mapping = table.InheritanceMapping.Select((m,i) => new { m, i }).Where(m => m.m.Type == typeOperand && !m.m.IsDefault).ToList();
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

				expr = expr != null ? Expression.AndAlso(expr, e) : e;
			}

			return ConvertPredicate(context, expr);
		}

		#endregion

		#endregion

		#region Search Condition Builder

		void BuildSearchCondition(IBuildContext context, Expression expression, List<SelectQuery.Condition> conditions)
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

				case ExpressionType.Or     :
				case ExpressionType.OrElse :
					{
						var e           = (BinaryExpression)expression;
						var orCondition = new SelectQuery.SearchCondition();

						BuildSearchCondition(context, e.Left,  orCondition.Conditions);
						orCondition.Conditions[orCondition.Conditions.Count - 1].IsOr = true;
						BuildSearchCondition(context, e.Right, orCondition.Conditions);

						conditions.Add(new SelectQuery.Condition(false, orCondition));

						break;
					}

				case ExpressionType.Not    :
					{
						var e            = expression as UnaryExpression;
						var notCondition = new SelectQuery.SearchCondition();

						BuildSearchCondition(context, e.Operand, notCondition.Conditions);

						if (notCondition.Conditions.Count == 1 && notCondition.Conditions[0].Predicate is SelectQuery.Predicate.NotExpr)
						{
							var p = notCondition.Conditions[0].Predicate as SelectQuery.Predicate.NotExpr;
							p.IsNot = !p.IsNot;
							conditions.Add(notCondition.Conditions[0]);
						}
						else
							conditions.Add(new SelectQuery.Condition(true, notCondition));

						break;
					}

				default                    :
					var predicate = ConvertPredicate(context, expression);

					if (predicate is SelectQuery.Predicate.Expr)
					{
						var expr = ((SelectQuery.Predicate.Expr)predicate).Expr1;

						if (expr.ElementType == QueryElementType.SearchCondition)
						{
							var sc = (SelectQuery.SearchCondition)expr;

							if (sc.Conditions.Count == 1)
							{
								conditions.Add(sc.Conditions[0]);
								break;
							}
						}
					}

					conditions.Add(new SelectQuery.Condition(false, predicate));

					break;
			}
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
			var root = expression.GetRootObject();

			for (; current != null; current = current.Parent)
				if (current.IsExpression(root, 0, RequestFor.Root).Result)
					return current;

			return null;
		}

		Sql.ExpressionAttribute GetExpressionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.ExpressionAttribute>(member, a => a.Configuration);
		}

		internal Sql.TableFunctionAttribute GetTableFunctionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member, a => a.Configuration);
		}

		public ISqlExpression Convert(IBuildContext context, ISqlExpression expr)
		{
			return DataContextInfo.GetSqlOptimizer().ConvertExpression(expr);
		}

		public ISqlPredicate Convert(IBuildContext context, ISqlPredicate predicate)
		{
			return DataContextInfo.GetSqlOptimizer().ConvertPredicate(context.SelectQuery, predicate);
		}

		internal ISqlExpression ConvertSearchCondition(IBuildContext context, ISqlExpression sqlExpression)
		{
			if (sqlExpression is SelectQuery.SearchCondition)
			{
				if (sqlExpression.CanBeNull)
				{
					var notExpr = new SelectQuery.SearchCondition
					{
						Conditions = { new SelectQuery.Condition(true, new SelectQuery.Predicate.Expr(sqlExpression)) }
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

							if (member is MethodInfo)
								members.Add(((MethodInfo)member).GetPropertyInfo(), expr.Arguments[i]);
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

							if (binding.Member is MethodInfo)
								members.Add(((MethodInfo)binding.Member).GetPropertyInfo(), binding.Expression);
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
						context.Parent = newParent;
		}

		#endregion
	}
}
