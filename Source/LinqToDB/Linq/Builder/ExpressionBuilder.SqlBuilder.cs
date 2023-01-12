using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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

	partial class ExpressionBuilder
	{
		#region CompareNullsAsValues

		public bool CompareNullsAsValues { get; set; } = Configuration.Linq.CompareNullsAsValues;

		#endregion

		#region Build Where

		bool? IsHavingSql(bool forGroup, ISqlExpression expr, SelectQuery parentSql)
		{
			var isHaving = (bool?)null;
			expr.VisitParentFirst(e =>
		{
				if (isHaving == false)
					return false;

				if (e is SqlFunction func)
				{
					if (forGroup)
						isHaving = func.IsAggregate;
					else
						isHaving = false;
				}
				else if (e is SqlColumn column && column.Parent == parentSql)
				{
					isHaving = IsHavingSql(forGroup, column.Expression, parentSql);
					return false;
				} else if (e is SelectQuery)
				{
					isHaving = false;
				} else  if (e is SqlExpression sqlExpr)
				{
					isHaving = !sqlExpr.IsWindowFunction;
				}

				return isHaving != false;
			});

			return isHaving;
		}

		public IBuildContext BuildWhere(IBuildContext? parent, IBuildContext sequence, LambdaExpression condition,
			bool checkForSubQuery, bool enforceHaving, bool isTest)
		{
			if (sequence is not SubQueryContext)
			{
				sequence = new SubQueryContext(sequence);
			}

			var body = SequenceHelper.PrepareBody(condition, sequence);
			var expr = ConvertExpression(body.Unwrap());

			var sc = new SqlSearchCondition();

			var flags = ProjectFlags.SQL;
			if (isTest)
				flags |= ProjectFlags.Test;

			BuildSearchCondition(sequence, expr, flags, sc.Conditions);

			if (!isTest)
			{
				sequence.SelectQuery.Where.ConcatSearchCondition(sc);
			}

			return sequence;
		}

		sealed class CheckSubQueryForWhereContext
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

		public IBuildContext? GetSubQuery(IBuildContext context, Expression expr, bool isTest)
		{
			var testInfo =
				new BuildInfo(context, expr, new SelectQuery { ParentSelect = context.SelectQuery }) { IsTest = true };

			if (!IsSequence(testInfo))
				return null;
			
			var info = new BuildInfo(context, expr, new SelectQuery {ParentSelect = context.SelectQuery})
			{
				CreateSubQuery = true,
				IsTest         = isTest
			};

			var ctx = BuildSequence(info);

			return ctx;
		}

		#endregion

		#region IsSubQuery

		public bool IsSubQuery(IBuildContext context, MethodCallExpression call)
		{
			var isAggregate = call.IsAggregate(MappingSchema);

			if (isAggregate || call.IsQueryable())
			{
				/*
				var infoOnAggregation = new BuildInfo(context, call, new SelectQuery { ParentSelect = context.SelectQuery }) { InAggregation = true };
				if (IsSequence(infoOnAggregation))
					return false;
					*/

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

						return mc.Method.GetTableFunctionAttribute(MappingSchema) != null;
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

		sealed class ConvertHelper<T> : IConvertHelper
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

		public Expression ConvertExpression(Expression expression)
		{
			return (_convertExpressionTransformer ??= TransformInfoVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.ConvertExpressionTransformer(e)))
				.Transform(expression);
		}

		public Expression ConvertSingleExpression(Expression expression)
		{
			return ConvertExpressionTransformer(expression).Expression;
		}

		private TransformInfoVisitor<ExpressionBuilder>? _convertExpressionTransformer;

		private TransformInfo ConvertExpressionTransformer(Expression e)
		{
			if (CanBeConstant(e) || CanBeCompiled(e, false))
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
						{
							//expr = new ChangeTypeExpression(expr, e.Type);
							expr = Expression.Convert(expr, e.Type);
						}

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
									case "TotalSeconds"     : datePart = Sql.DateParts.Second;      break;
									case "TotalMinutes"     : datePart = Sql.DateParts.Minute;      break;
									case "TotalHours"       : datePart = Sql.DateParts.Hour;        break;
									case "TotalDays"        : datePart = Sql.DateParts.Day;         break;
									default                 : return new TransformInfo(e);
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

			lambda ??= Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

			return lambda == null ? null : OptimizationContext.ConvertMethod(pi, lambda);
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

		public ISqlExpression ConvertToSqlExpression(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor, bool isPureExpression)
		{
			var expr = ConvertExpression(expression);
			return ConvertToSql(context, expr, unwrap: false, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
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

				/*
				if (context is SelectContext sc && sc.Sequence is GroupByBuilder.GroupByContext)
					valueSequence = sc.Sequence;
					*/

				if (valueSequence is GroupByBuilder.GroupByContext groupByContext)
				{
					valueSequence = groupByContext.Element;
				}

				var contextRefExpression = new ContextRefExpression(lambda.Parameters[0].Type, valueSequence);

				var body = lambda.GetBody(contextRefExpression);

				var result = ConvertToSql(context, body, unwrap: false, columnDescriptor: columnDescriptor);

				if (!(result is SqlField field) || field.Table!.All != field)
					return result;
				result = context.ConvertToSql(null, 0, ConvertFlags.Field).Select(static _ => _.Sql).First();
				return result;
			}

			/*if (context is SelectContext selectContext)
			{
				var result = ConvertToSql(context, expression, false, columnDescriptor);

				if (!(result is SqlField field) || field.Table!.All != field)
					return result;

				if (null != expression.Find(selectContext.Body))
					return context.ConvertToSql(null, 0, ConvertFlags.Field).Select(static _ => _.Sql).First();
			}*/

			if (context is MethodChainBuilder.ChainContext chainContext)
			{
				if (expression is MethodCallExpression mc && IsSubQuery(context, mc))
					return context.ConvertToSql(null, 0, ConvertFlags.Field).Select(static _ => _.Sql).First();
			}

			return ConvertToSql(context, expression, unwrap: false, columnDescriptor: columnDescriptor);
		}

		[DebuggerDisplay("S: {SelectQuery?.SourceID} F: {Flags}, E: {Expression}, C: {Context}")]
		readonly struct SqlCacheKey
		{
			public SqlCacheKey(Expression? expression, IBuildContext? context, ColumnDescriptor? columnDescriptor, SelectQuery? selectQuery, ProjectFlags flags)
			{
				Expression       = expression;
				Context          = context;
				ColumnDescriptor = columnDescriptor;
				SelectQuery      = selectQuery;
				Flags            = flags;
			}

			public Expression?       Expression       { get; }
			public IBuildContext?    Context          { get; }
			public ColumnDescriptor? ColumnDescriptor { get; }
			public SelectQuery?      SelectQuery      { get; }
			public ProjectFlags      Flags            { get; }

			private sealed class SqlCacheKeyEqualityComparer : IEqualityComparer<SqlCacheKey>
			{
				public bool Equals(SqlCacheKey x, SqlCacheKey y)
				{
					return ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression) &&
					       Equals(x.Context, y.Context)                                           &&
					       Equals(x.SelectQuery, y.SelectQuery)                                   &&
					       Equals(x.ColumnDescriptor, y.ColumnDescriptor)                         &&
					       x.Flags == y.Flags;
				}

				public int GetHashCode(SqlCacheKey obj)
				{
					unchecked
					{
						var hashCode = (obj.Expression != null ? ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression) : 0);
						hashCode = (hashCode * 397) ^ (obj.Context          != null ? obj.Context.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (obj.SelectQuery      != null ? obj.SelectQuery.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (obj.ColumnDescriptor != null ? obj.ColumnDescriptor.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (int)obj.Flags;
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<SqlCacheKey> SqlCacheKeyComparer { get; } = new SqlCacheKeyEqualityComparer();
		}

		[DebuggerDisplay("S: {SelectQuery?.SourceID}, E: {Expression}")]
		readonly struct ColumnCacheKey
		{
			public ColumnCacheKey(Expression? expression, Type resultType, SelectQuery selectQuery, SelectQuery? parentQuery)
			{
				Expression  = expression;
				ResultType  = resultType;
				SelectQuery = selectQuery;
				ParentQuery = parentQuery;
			}

			public Expression?  Expression  { get; }
			public Type         ResultType  { get; }
			public SelectQuery  SelectQuery { get; }
			public SelectQuery? ParentQuery { get; }

			private sealed class ColumnCacheKeyEqualityComparer : IEqualityComparer<ColumnCacheKey>
			{
				public bool Equals(ColumnCacheKey x, ColumnCacheKey y)
				{
					return x.ResultType == y.ResultType                                           &&
					       ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression) &&
					       ReferenceEquals(x.SelectQuery, y.SelectQuery) &&
					       ReferenceEquals(x.ParentQuery, y.ParentQuery);
				}

				public int GetHashCode(ColumnCacheKey obj)
				{
					unchecked
					{
						var hashCode = obj.ResultType.GetHashCode();
						hashCode = (hashCode * 397) ^ (obj.Expression != null ? ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression) : 0);
						hashCode = (hashCode * 397) ^ obj.SelectQuery?.GetHashCode() ?? 0;
						hashCode = (hashCode * 397) ^ (obj.ParentQuery != null ? obj.ParentQuery.GetHashCode() : 0);
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<ColumnCacheKey> ColumnCacheKeyComparer { get; } = new ColumnCacheKeyEqualityComparer();
		}

		Dictionary<SqlCacheKey, Expression> _preciseCachedSql = new(SqlCacheKey.SqlCacheKeyComparer);
		Dictionary<SqlCacheKey, Expression> _cachedSql        = new(SqlCacheKey.SqlCacheKeyComparer);

		public SqlPlaceholderExpression ConvertToSqlPlaceholder(IBuildContext context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, ColumnDescriptor? columnDescriptor = null, bool isPureExpression = false)
		{
			var expr = ConvertToSqlExpr(context, expression, flags, unwrap, columnDescriptor, isPureExpression: isPureExpression);

			if (expr is not SqlPlaceholderExpression placeholder)
			{
				if (expr is SqlErrorExpression errorExpression)
					throw errorExpression.CreateError();

				throw CreateSqlError(context, expression).CreateError();
			}

			return placeholder;
		}

		public ISqlExpression ConvertToSql(IBuildContext context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, ColumnDescriptor? columnDescriptor = null, bool isPureExpression = false)
		{
			var placeholder = ConvertToSqlPlaceholder(context, expression, flags, unwrap: unwrap, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

			return placeholder.Sql;
		}

		public static SqlPlaceholderExpression CreatePlaceholder(IBuildContext context, ISqlExpression sqlExpression,
			Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			var placeholder = new SqlPlaceholderExpression(context.SelectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
			return placeholder;
		}

		public static SqlPlaceholderExpression CreatePlaceholder(SelectQuery? selectQuery, ISqlExpression sqlExpression,
			Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			var placeholder = new SqlPlaceholderExpression(selectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
			return placeholder;
		}

		/// <summary>
		/// Converts to Expression which may contain SQL or convert error.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="expression"></param>
		/// <param name="flags"></param>
		/// <param name="unwrap"></param>
		/// <param name="columnDescriptor"></param>
		/// <param name="isPureExpression"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public Expression ConvertToSqlExpr(IBuildContext context, Expression expression, ProjectFlags flags = ProjectFlags.SQL,
			bool unwrap = false, ColumnDescriptor? columnDescriptor = null, bool isPureExpression = false,
			string? alias = null)
		{
			// remove keys flag. We can cache SQL
			var cacheFlags = flags & ~ProjectFlags.Keys;

			var cacheKey        = new SqlCacheKey(expression, null, columnDescriptor, null, cacheFlags);
			var preciseCacheKey = new SqlCacheKey(expression, null, columnDescriptor, context?.SelectQuery, cacheFlags);

			if (_preciseCachedSql.TryGetValue(preciseCacheKey, out var sqlExpr))
			{
				return sqlExpr;
			}

			var cache = expression is SqlPlaceholderExpression ||
			            null != expression.Find(1, (_, e) => e is ContextRefExpression);

			ISqlExpression? sql = null;
			Expression?     result = null;

			var isFirst = true;

			var newExpr = expression;

			while (true)
			{
				if (typeof(IToSqlConverter).IsSameOrParentOf(newExpr.Type))
				{
					sql = ConvertToSqlConvertible(newExpr);
				}

				if (sql == null && !flags.IsExpression())
				{
					if (!PreferServerSide(newExpr, false))
					{
						if (columnDescriptor?.ValueConverter == null && CanBeConstant(newExpr))
						{
							sql = BuildConstant(newExpr, columnDescriptor);
						}
						else if (CanBeCompiled(newExpr, false))
						{
							if ((newExpr.NodeType == ExpressionType.MemberInit ||
							     newExpr.NodeType == ExpressionType.New) 
							    && !MappingSchema.IsScalarType(newExpr.Type) && flags.HasFlag(ProjectFlags.Keys))
							{
								// expression will be needed for comparison
								newExpr = SqlGenericConstructorExpression.Parse(newExpr);
							}
							else
								if (!isFirst)
									sql = ParametersContext.BuildParameter(newExpr, columnDescriptor, alias: alias).SqlParameter;
						}
					}
				}

				if (!isFirst)
					break;

				if (sql == null)
				{
					newExpr = MakeExpression(context, newExpr, flags);
					if (newExpr is SqlPlaceholderExpression)
					{
						result = newExpr;
						break;
					}
				}
				else
					break;

				isFirst = false;
			}

			if (result == null)
			{
				if (sql != null)
				{
					result = CreatePlaceholder(context.SelectQuery, sql, newExpr, alias: alias);
				}
				else
				{
					result = ConvertToSqlInternal(context, newExpr, flags, unwrap: unwrap, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression, alias: alias);
				}
			} 

			// nesting for Expressions updated in finalization
			var updateNesting = !flags.HasFlag(ProjectFlags.Test);

			_cachedSql[cacheKey] = result;

			if (updateNesting)
			{
				result = UpdateNesting(context, result);
			}

			if (result is SqlPlaceholderExpression placeholder)
			{
				if (expression is not SqlPlaceholderExpression)
					placeholder = placeholder.WithTrackingPath(expression);

				if (cache)
				{
					if (updateNesting && placeholder.SelectQuery != context.SelectQuery &&
					    placeholder.Sql is not SqlColumn)
					{
						// recreate placeholder
						placeholder = CreatePlaceholder(context.SelectQuery, placeholder.Sql, placeholder.Path,
							placeholder.ConvertType, placeholder.Alias, placeholder.Index, trackingPath: placeholder.TrackingPath);
					}

					if (expression is not SqlPlaceholderExpression)
						placeholder = placeholder.WithTrackingPath(expression);

					_preciseCachedSql[preciseCacheKey] = placeholder;
				}

				result = placeholder;
			}

			return result;
		}


		Expression ConvertToSqlInternal(IBuildContext context, Expression expression, ProjectFlags flags, bool unwrap = false, ColumnDescriptor? columnDescriptor = null, bool isPureExpression = false, string? alias = null)
		{
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
					if (!BuildSearchCondition(context, expression, flags, condition.Conditions))
						return expression;
					return CreatePlaceholder(context, condition, expression, alias: alias);
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

					columnDescriptor = SuggestColumnDescriptor(context, e.Left, e.Right, flags);

					var leftExpr  = ConvertToSqlExpr(context, e.Left,  flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
					var rightExpr = ConvertToSqlExpr(context, e.Right, flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

					if (leftExpr is not SqlPlaceholderExpression || rightExpr is not SqlPlaceholderExpression)
						return e;

					var leftPlaceholder  = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.Left,  flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
					var rightPlaceholder = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.Right, flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

					var l = leftPlaceholder.Sql;
					var r = rightPlaceholder.Sql;
					var t = e.Type;

					switch (expression.NodeType)
					{
						case ExpressionType.Add:
						case ExpressionType.AddChecked: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "+", r, Precedence.Additive), expression, alias: alias);
						case ExpressionType.And: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "&", r, Precedence.Bitwise), expression, alias: alias);
						case ExpressionType.Divide: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "/", r, Precedence.Multiplicative), expression, alias: alias);
						case ExpressionType.ExclusiveOr: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "^", r, Precedence.Bitwise), expression, alias: alias);
						case ExpressionType.Modulo: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "%", r, Precedence.Multiplicative), expression, alias: alias);
						case ExpressionType.Multiply:
						case ExpressionType.MultiplyChecked: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "*", r, Precedence.Multiplicative), expression, alias: alias);
						case ExpressionType.Or: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "|", r, Precedence.Bitwise), expression, alias: alias);
						case ExpressionType.Power: return CreatePlaceholder(context, new SqlFunction(t, "Power", l, r), expression, alias: alias);
						case ExpressionType.Subtract:
						case ExpressionType.SubtractChecked: return CreatePlaceholder(context, new SqlBinaryExpression(t, l, "-", r, Precedence.Subtraction), expression, alias: alias);
						case ExpressionType.Coalesce:
						{
							if (QueryHelper.UnwrapExpression(r, checkNullability: true) is SqlFunction c)
							{
								if (c.Name is "Coalesce" or PseudoFunctions.COALESCE)
								{
									var parms = new ISqlExpression[c.Parameters.Length + 1];

									parms[0] = l;
									c.Parameters.CopyTo(parms, 1);

									return CreatePlaceholder(context, PseudoFunctions.MakeCoalesce(t, parms), expression, alias: alias);
								}
							}

							return CreatePlaceholder(context, PseudoFunctions.MakeCoalesce(t, l, r), expression, alias: alias);
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
						case ExpressionType.UnaryPlus: return CreatePlaceholder(context, o, expression);
						case ExpressionType.Negate:
						case ExpressionType.NegateChecked:
							return CreatePlaceholder(context, new SqlBinaryExpression(t, new SqlValue(-1), "*", o, Precedence.Multiplicative), expression, alias: alias);
					}

					break;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var e = (UnaryExpression)expression;

					if (!TryConvertToSql(context, flags, e.Operand, columnDescriptor, out var o, out var oError))
						return oError;

					if (e.Method == null && (e.IsLifted || e.Type == typeof(object)))
						return CreatePlaceholder(context, o, expression, alias: alias);

					if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
						return CreatePlaceholder(context, o, expression, alias: alias);

					if (e.Type == typeof(Enum) && e.Operand.Type.IsEnum)
						return CreatePlaceholder(context, o, expression, alias: alias);

					var t = e.Operand.Type;
					var s = MappingSchema.GetDataType(t);

					if (o.SystemType != null && s.Type.SystemType == typeof(object))
					{
						t = o.SystemType;
						s = MappingSchema.GetDataType(t);
					}

					if (e.Type == t ||
						t.IsEnum && Enum.GetUnderlyingType(t) == e.Type ||
						e.Type.IsEnum && Enum.GetUnderlyingType(e.Type) == t)
					{
						return CreatePlaceholder(context, o, expression, alias: alias);
					}

					return CreatePlaceholder(context, PseudoFunctions.MakeConvert(MappingSchema.GetDataType(e.Type), s, o), expression, alias: alias);
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expression;

					/*if (flags.IsSql() && e.Test is SqlReaderIsNullExpression reader)
					{
						//TODO: not so elegant, prediction that expression was modified by DefaultIfEmptyContext
						var defaultValue = reader.IsNot ? e.IfFalse : e.IfTrue;
						if (defaultValue.NodeType == ExpressionType.Default || defaultValue is DefaultValueExpression)
						{
							var wrapped = reader.IsNot ? e.IfTrue : e.IfFalse;
							return ConvertToSqlExpr(context, wrapped, flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
						}
					}*/

					var testExpr  = ConvertToSqlExpr(context, e.Test, flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
					var trueExpr  = ConvertToSqlExpr(context, e.IfTrue, flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
					var falseExpr = ConvertToSqlExpr(context, e.IfFalse, flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

					if (testExpr is SqlPlaceholderExpression && 
					    trueExpr is SqlPlaceholderExpression &&
					    falseExpr is SqlPlaceholderExpression)
					{
						var testSql  = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.Test, flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
						var trueSql  = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.IfTrue, flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
						var falseSql = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.IfFalse, flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

						if (testSql.Sql is SqlSearchCondition sc)
						{
							sc = SelectQueryOptimizer.OptimizeSearchCondition(sc, new EvaluationContext());

							if (sc.Conditions.Count == 1                                 && !sc.Conditions[0].IsNot &&
							    sc.Conditions[0].Predicate is SqlPredicate.IsNull isnull && isnull.IsNot)
							{
								if (QueryHelper.IsNullValue(falseSql.Sql) && trueSql.Sql.Equals(isnull.Expr1))
									return CreatePlaceholder(context, isnull.Expr1, expression);
							}

						}

						if (QueryHelper.UnwrapExpression(falseSql.Sql, checkNullability: false) is SqlFunction c && c.Name == "CASE")
						{
							var parms = new ISqlExpression[c.Parameters.Length + 2];

							parms[0] = testSql.Sql;
							parms[1] = trueSql.Sql;
							c.Parameters.CopyTo(parms, 2);

							return CreatePlaceholder(context, new SqlFunction(e.Type, "CASE", parms) { CanBeNull = trueSql.Sql.CanBeNull || falseSql.Sql.CanBeNull || c.CanBeNull}, expression, alias: alias);
						}

						return CreatePlaceholder(context, new SqlFunction(e.Type, "CASE", testSql.Sql, trueSql.Sql, falseSql.Sql) { CanBeNull = trueSql.Sql.CanBeNull || falseSql.Sql.CanBeNull }, expression, alias: alias);
					}

					return e;
				}

				case ExpressionType.Extension:
				{
					if (expression is SqlPlaceholderExpression placeholder)
					{
						return placeholder;
					}

					if (expression is SqlGenericConstructorExpression genericConstructor)
					{
						var newConstructor = genericConstructor.ReplaceAssignments(genericConstructor.Assignments.Select(a =>
							a.WithExpression(ConvertToSqlExpr(context, a.Expression, flags, unwrap, columnDescriptor,
								isPureExpression, a.MemberInfo.Name))).ToList());

						newConstructor = newConstructor.ReplaceParameters(genericConstructor.Parameters.Select(p =>
							p.WithExpression(ConvertToSqlExpr(context, p.Expression, flags, unwrap, columnDescriptor,
								isPureExpression, p.MemberInfo?.Name ?? alias))).ToList());

						return newConstructor;
					}

					/*
					if (expression is SqlReaderIsNullExpression readerIsNullExpression && flags.HasFlag(ProjectFlags.SQL))
					{
						var sc = new SqlSearchCondition();

						var notNullPlaceholder = readerIsNullExpression.Placeholder;
						if (!flags.IsTest())
							notNullPlaceholder = (SqlPlaceholderExpression)UpdateNesting(context, notNullPlaceholder);
						sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(notNullPlaceholder.Sql, readerIsNullExpression.IsNot)));
						return CreatePlaceholder(context, sc, expression, alias: readerIsNullExpression.IsNot ? "not_null" : "null");
					}
					*/

					break;
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expression;

					var expr = ConvertMethod(e);

					if (expr != null)
						return ConvertToSqlExpr(context, expr, unwrap: unwrap, alias: alias, flags: flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

					var attr = e.Method.GetExpressionAttribute(MappingSchema);

					if (attr != null)
					{
						return CreatePlaceholder(context, ConvertExtensionToSql(context!, attr, e), expression, alias: alias);
					}

					if (e.Method.IsSqlPropertyMethodEx())
						return CreatePlaceholder(context, ConvertToSql(context, ConvertExpression(expression), unwrap: unwrap), expression, alias: alias);

					if (e.Method.DeclaringType == typeof(string) && e.Method.Name == "Format")
					{
						var sqlExpression = TryConvertFormatToSql(context, e, isPureExpression, flags);
						if (sqlExpression != null)
							return CreatePlaceholder(context, sqlExpression, expression, alias: alias);
						break;
					}

					if (e.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
					{
						var sqlExpr = ConvertToSqlExpr(context, e.Arguments[0], unwrap: unwrap, flags: flags,
							columnDescriptor: columnDescriptor, isPureExpression: isPureExpression, alias: alias);

						if (sqlExpr is SqlPlaceholderExpression placeholderExpression)
						{
							sqlExpr = placeholderExpression.WithAlias(alias);
						}
						return sqlExpr;
					}

					var newExpr = HandleExtension(context, e, flags);

					return newExpr;
				}

				case ExpressionType.MemberAccess:
				{
					var newExpr = HandleExtension(context, expression, flags) ;

					return newExpr;
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

						return CreatePlaceholder(context, ConvertToSql(context, pie), expression, alias: alias);
					}

					break;
				}

				case ExpressionType.TypeIs:
				{
					var condition = new SqlSearchCondition();
					BuildSearchCondition(context, expression, flags, condition.Conditions);
					return CreatePlaceholder(context, condition, expression, alias: alias);
				}

				case ExpressionType.TypeAs:
				{
					var unary     = (UnaryExpression)expression;
					var testExpr  = MakeIsPredicateExpression(context, Expression.TypeIs(unary.Operand, unary.Type));
					var trueCase  = Expression.Convert(unary.Operand, unary.Type);
					var falseCase = Expression.Default(unary.Type);

					var cond = Expression.Condition(testExpr, trueCase, falseCase);

					return ConvertToSqlExpr(context, cond, unwrap: unwrap, alias: alias, flags: flags, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
				}

				case ChangeTypeExpression.ChangeTypeType:
					return CreatePlaceholder(context, ConvertToSql(context, ((ChangeTypeExpression)expression).Expression), expression, alias: alias);

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)expression;
					if (cnt.Value is ISqlExpression sql)
						return CreatePlaceholder(context, sql, expression, alias: alias);
					break;
				}

				case ExpressionType.New:
				case ExpressionType.MemberInit:
				{
					if (SqlGenericConstructorExpression.Parse(expression) is SqlGenericConstructorExpression transformed)
					{
						return ConvertToSqlExpr(context, transformed, flags, unwrap, columnDescriptor, isPureExpression,
							alias);
					}

					break;
				}

				/*default:
				{
					expression = BuildSqlExpression(new Dictionary<Expression, Expression>(), context, expression,
						flags, alias);

					break;
				}*/
			}

			if (expression.Type == typeof(bool) && _convertedPredicates.Add(expression))
			{
				var predicate = ConvertPredicate(context, expression, flags);
				if (predicate == null)
					return expression;

				_convertedPredicates.Remove(expression);
				return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, predicate)), expression, alias: alias);
			}

			return expression;
		}


		public ISqlExpression? TryConvertFormatToSql(IBuildContext? context, MethodCallExpression mc, bool isPureExpression, ProjectFlags flags)
		{
			// TODO: move PrepareRawSqlArguments to more correct location
			TableBuilder.PrepareRawSqlArguments(mc, null,
				out var format, out var arguments);

			var sqlArguments = new List<ISqlExpression>();
			foreach (var a in arguments)
			{
				if (!TryConvertToSql(context, flags, a, null, out var sqlExpr, out _))
					return null;

				sqlArguments.Add(sqlExpr);
			}

			if (isPureExpression)
				return new SqlExpression(mc.Type, format, Precedence.Primary, sqlArguments.ToArray());

			return QueryHelper.ConvertFormatToConcatenation(format, sqlArguments);
		}

		public ISqlExpression ConvertExtensionToSql(IBuildContext context, Sql.ExpressionAttribute attr, MethodCallExpression mc)
		{
			var inlineParameters = DataContext.InlineParameters;

			if (attr.InlineParameters)
				DataContext.InlineParameters = true;

			var sqlExpression = attr.GetExpression(
				(this_: this, context),
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
			var l = Expression.Lambda<Func<IToSqlConverter>>(Expression.Convert(expression, typeof(IToSqlConverter)));
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
			if (!ParametersContext.CanBeConstant(expr))
			{
				return false;
			}
			return _optimizationContext.CanBeConstant(expr);
		}

		#endregion

		#region CanBeCompiled

		public bool CanBeCompiled(Expression expr, bool inProjection)
		{
			return _optimizationContext.CanBeCompiled(expr, inProjection);
		}

		#endregion

		#region Build Constant

		readonly Dictionary<Tuple<Expression, ColumnDescriptor?>,SqlValue> _constants = new ();

		SqlValue BuildConstant(Expression expr, ColumnDescriptor? columnDescriptor)
		{
			var key = Tuple.Create(expr, columnDescriptor);
			if (_constants.TryGetValue(key, out var sqlValue))
				return sqlValue;

			var dbType = columnDescriptor?.GetDbDataType(true).WithSystemType(expr.Type) ?? new DbDataType(expr.Type);

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

		ISqlPredicate? ConvertPredicate(IBuildContext? context, Expression expression, ProjectFlags flags)
		{
			ISqlPredicate? CheckExpression(Expression expr)
			{
				if (expr is SqlPlaceholderExpression placheolder && placheolder.Sql is SqlSearchCondition sc)
					return sc;

				return null;
			}

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

					var left  = MakeExpression(context, e.Left, flags.ExpandFlag());
					var right = MakeExpression(context, e.Right, flags.ExpandFlag());

					var newExpr = e.Update(left, e.Conversion, right);

					if (!ReferenceEquals(e, newExpr))
					{
						return ConvertPredicate(context, newExpr, flags);
					}

					return CheckExpression(ConvertCompareExpression(context, expression.NodeType, left, right, flags));
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expression;

					ISqlPredicate? predicate = null;

					if (e.Method.Name == "Equals" && e.Object != null && e.Arguments.Count == 1)
						return CheckExpression(ConvertCompareExpression(context, ExpressionType.Equal, e.Object, e.Arguments[0], flags));

					if (e.Method.DeclaringType == typeof(string))
					{
						switch (e.Method.Name)
						{
							case "Contains"   : predicate = CreateStringPredicate(context, e, SqlPredicate.SearchString.SearchKind.Contains,   IsCaseSensitive(e), flags); break;
							case "StartsWith" : predicate = CreateStringPredicate(context, e, SqlPredicate.SearchString.SearchKind.StartsWith, IsCaseSensitive(e), flags); break;
							case "EndsWith"   : predicate = CreateStringPredicate(context, e, SqlPredicate.SearchString.SearchKind.EndsWith,   IsCaseSensitive(e), flags); break;
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
					else if (e.Method.Name == "ContainsKey" &&
						(typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!) ||
						 typeof(IReadOnlyDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!)))
					{
						var type = typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!) ? typeof(IDictionary<,>) : typeof(IReadOnlyDictionary<,>);
						var args = e.Method.DeclaringType!.GetGenericArguments(type)!;
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
					else if (e.Method == ReflectionHelper.Functions.String.Like11) predicate = ConvertLikePredicate(context!, e, flags);
					else if (e.Method == ReflectionHelper.Functions.String.Like12) predicate = ConvertLikePredicate(context!, e, flags);
#endif
					else if (e.Method == ReflectionHelper.Functions.String.Like21) predicate = ConvertLikePredicate(context!, e, flags);
					else if (e.Method == ReflectionHelper.Functions.String.Like22) predicate = ConvertLikePredicate(context!, e, flags);

					if (predicate != null)
						return predicate;

					var attr = e.Method.GetExpressionAttribute(MappingSchema);

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
					var contextRef = GetRootContext(context, e.Expression, false);

					if (contextRef != null && SequenceHelper.GetTableContext(contextRef.BuildContext) != null)
						return MakeIsPredicate(contextRef.BuildContext, e, flags);

					break;
				}

				case ExpressionType.Convert:
				{
					var e = (UnaryExpression)expression;

					if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
						return ConvertPredicate(context, e.Operand, flags);

					break;
				}
			}

			if (!TryConvertToSql(context, flags, expression, null, out var ex, out _))
				return null;

			if (SqlExpression.NeedsEqual(ex))
			{
				var descriptor = QueryHelper.GetColumnDescriptor(ex);
				var trueValue  = ConvertToSql(context, ExpressionInstances.True, columnDescriptor: descriptor);
				var falseValue = ConvertToSql(context, ExpressionInstances.False, columnDescriptor: descriptor);

				return new SqlPredicate.IsTrue(ex, trueValue, falseValue, Configuration.Linq.CompareNullsAsValues ? false : null, false);
			}

			return new SqlPredicate.Expr(ex);
		}

		#region ConvertCompare

		static LambdaExpression BuildMemberPathLambda(Expression path)
		{
			var memberPath = new List<MemberInfo>();

			var current = path;
			do
			{
				if (current is MemberExpression me)
				{
					current = me.Expression;
					memberPath.Add(me.Member);
				}
				else 
					break;

			} while (true);

			var        param = Expression.Parameter(current.Type, "o");
			Expression body  = param;
			for (int i = memberPath.Count - 1; i >= 0; i--)
			{
				body = Expression.MakeMemberAccess(body, memberPath[i]);
			}

			return Expression.Lambda(body, param);
		}

		public SqlSearchCondition GenerateComparison(
			IBuildContext? context, 
			Expression     left,
			Expression     right)
		{
			var expr = ConvertCompareExpression(context, ExpressionType.Equal, left, right, ProjectFlags.SQL);
			if (expr is SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
				return sc;
			if (expr is SqlErrorExpression error)
				throw error.CreateError();

			throw CreateSqlError(context, Expression.Equal(left, right)).CreateError();
		}

		Expression ConvertCompareExpression(IBuildContext? context, ExpressionType nodeType, Expression left, Expression right, ProjectFlags flags)
		{
			Expression GetOriginalExpression()
			{
				var rightExpr = right;
				if (rightExpr.Type != left.Type)
					rightExpr = Expression.Convert(rightExpr, left.Type);
				return Expression.MakeBinary(nodeType, left, rightExpr);
			}

			Expression GenerateNullComparison(List<SqlPlaceholderExpression> placeholders, bool isNot)
			{
				if (placeholders.Count == 0)
					return GetOriginalExpression();

				var nullability = new NullabilityContext(context.SelectQuery);

				var notNull = placeholders.Where(p => p.Sql.CanBeNullable(nullability)).ToList();
				if (notNull.Count == 0)
					notNull = placeholders;

				var searchCondition = new SqlSearchCondition();
				foreach (var placeholder in notNull)
				{
					var sql = placeholder.Sql;
					if (!sql.CanBeNullable(nullability))
						sql = new SqlNullabilityExpression(sql);
					searchCondition.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(sql, isNot), isNot));
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}

			Expression GenerateObjectComparison(SqlGenericConstructorExpression genericConstructor, Expression paramExpr)
			{
				var searchCondition = new SqlSearchCondition();

				Expression? errorExpression = null;
				if (!GenerateObjectComparisonRecursive(searchCondition, genericConstructor, paramExpr, ref errorExpression))
				{
					return errorExpression ?? throw new InvalidOperationException();
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}

			bool GenerateObjectComparisonRecursive(SqlSearchCondition searchCondition, SqlGenericConstructorExpression genericConstructor, Expression paramExpr, ref Expression? errorExpression)
			{
				foreach (var assignment in genericConstructor.Assignments)
				{
					var accessExpression = Expression.MakeMemberAccess(paramExpr, assignment.MemberInfo);

					if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
					{
						if (!GenerateObjectComparisonRecursive(searchCondition, subGeneric, accessExpression,
							    ref errorExpression))
							return false;
					}
					else if (assignment.Expression is SqlPlaceholderExpression placeholder)
					{
						var paramSql = ConvertToSql(context, accessExpression,
							columnDescriptor: QueryHelper.GetColumnDescriptor(placeholder.Sql));

						var predicate =
							new SqlPredicate.ExprExpr(
								placeholder.Sql,
								nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
								paramSql, Configuration.Linq.CompareNullsAsValues ? true : null);

						searchCondition.Conditions.Add(new SqlCondition(false, predicate, nodeType == ExpressionType.NotEqual));
					}
					else
					{
						errorExpression = accessExpression;
						return false;
						/*throw new InvalidOperationException(
							$"Expression '{assignment.Expression}' cannot be used for comparison.");*/
					}

				}

				return true;
			}

			Expression GenerateConstructorComparison(SqlGenericConstructorExpression leftConstructor, SqlGenericConstructorExpression rightConstructor)
			{
				var strict = leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.Full ||
				             rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.Full;

				var searchCondition = new SqlSearchCondition();
				var usedMembers     = new HashSet<MemberInfo>(MemberInfoEqualityComparer.Default);

				foreach (var leftAssignment in leftConstructor.Assignments)
				{
					var found = rightConstructor.Assignments.FirstOrDefault(a =>
						MemberInfoEqualityComparer.Default.Equals(a.MemberInfo, leftAssignment.MemberInfo));

					if (found == null && strict)
					{
						// fail fast and prepare correct error expression
						return CreateSqlError(context, Expression.MakeMemberAccess(right, leftAssignment.MemberInfo));
					}

					var rightExpression = found?.Expression;
					if (rightExpression == null)
					{
						rightExpression = Expression.Default(leftAssignment.Expression.Type);
					}
					else
					{
						usedMembers.Add(found!.MemberInfo);
					}

					var predicateExpr = ConvertCompareExpression(context, nodeType, leftAssignment.Expression, rightExpression, flags);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
						return predicateExpr;
					searchCondition.Conditions.Add(new SqlCondition(false, sc, nodeType == ExpressionType.NotEqual));
				}

				foreach (var rightAssignment in rightConstructor.Assignments)
				{
					if (usedMembers.Contains(rightAssignment.MemberInfo))
						continue;

					if (strict)
					{
						// fail fast and prepare correct error expression
						return CreateSqlError(context, Expression.MakeMemberAccess(left, rightAssignment.MemberInfo));
					}

					var leftExpression = Expression.Default(rightAssignment.Expression.Type);

					var predicateExpr = ConvertCompareExpression(context, nodeType, leftExpression, rightAssignment.Expression, flags);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
						return predicateExpr;
					searchCondition.Conditions.Add(new SqlCondition(false, sc, nodeType == ExpressionType.NotEqual));
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}


			if (!RestoreCompare(ref left, ref right))
				RestoreCompare(ref right, ref left);

			if (context == null)
				throw new InvalidOperationException();
			  
			ISqlExpression? l = null;
			ISqlExpression? r = null;

			var columnDescriptor = SuggestColumnDescriptor(context, left, right, flags);
			var leftExpr         = ConvertToSqlExpr(context, left,  flags | ProjectFlags.Keys, columnDescriptor: columnDescriptor);
			var rightExpr        = ConvertToSqlExpr(context, right, flags | ProjectFlags.Keys, columnDescriptor: columnDescriptor);

			if (leftExpr is SqlErrorExpression leftError)
				return leftError.WithType(typeof(bool));

			if (rightExpr is SqlErrorExpression rightError)
				return rightError.WithType(typeof(bool));

			if (leftExpr is not SqlPlaceholderExpression && leftExpr is not SqlGenericConstructorExpression)
				return GetOriginalExpression();

			if (rightExpr is not SqlPlaceholderExpression && rightExpr is not SqlGenericConstructorExpression)
				return GetOriginalExpression();

			if (leftExpr is SqlPlaceholderExpression placeholderLeft)
			{
				l = placeholderLeft.Sql;
			}

			if (rightExpr is SqlPlaceholderExpression placeholderRight)
			{
				r = placeholderRight.Sql;
			}

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					var isNot = nodeType == ExpressionType.NotEqual;

					if (l != null && r != null)
						break;

					if (leftExpr is SqlGenericConstructorExpression leftGenericConstructor &&
					    rightExpr is SqlGenericConstructorExpression rightGenericConstructor)
					{
						return GenerateConstructorComparison(leftGenericConstructor, rightGenericConstructor);
					}

					if (l is SqlValue lv && lv.Value == null)
					{
						if (rightExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SqlSearchCondition rightSearchCond } } && rightSearchCond.Conditions.Count == 1)
						{
							var condition  = rightSearchCond.Conditions[0];
							var localIsNot = isNot;
							if (condition.IsNot)
								localIsNot = !localIsNot;

							if (condition.Predicate is SqlPredicate.IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, isnull)), GetOriginalExpression());

								return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, (ISqlPredicate)isnull.Invert())), GetOriginalExpression());
							}
						}

						var rightPlaceholders = CollectDistinctPlaceholders(rightExpr);
						return GenerateNullComparison(rightPlaceholders, isNot);
					}

					if (r is SqlValue rv && rv.Value == null)
					{
						if (leftExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SqlSearchCondition leftSearchCond } } && leftSearchCond.Conditions.Count == 1)
						{
							var condition  = leftSearchCond.Conditions[0];
							var localIsNot = isNot;
							if (condition.IsNot)
								localIsNot = !localIsNot;

							if (condition.Predicate is SqlPredicate.IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, isnull)), GetOriginalExpression());

								return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, (ISqlPredicate)isnull.Invert())), GetOriginalExpression());
							}
						}

						var leftPlaceholders = CollectDistinctPlaceholders(leftExpr);
						return GenerateNullComparison(leftPlaceholders, isNot);
					}

					if (rightExpr is SqlGenericConstructorExpression rightGeneric && l is SqlParameter lParam)
					{
						return GenerateObjectComparison(rightGeneric , left);
					}

					if (leftExpr is SqlGenericConstructorExpression leftGeneric && r is SqlParameter rParam)
					{
						return GenerateObjectComparison(leftGeneric, right);
					}

					return null;
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
					return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, p)), GetOriginalExpression());
			}

			l ??= ConvertToSql(context, left,  flags, unwrap: false, columnDescriptor: columnDescriptor);
			r ??= ConvertToSql(context, right, flags, unwrap: true,  columnDescriptor: columnDescriptor);

			var lOriginal = l;
			var rOriginal = r;

            l = QueryHelper.UnwrapExpression(l, checkNullability: true);
            r = QueryHelper.UnwrapExpression(r, checkNullability: true);

			if (l is SqlValue lValue)
				lValue.ValueType = GetDataType(r, lValue.ValueType);

			if (r is SqlValue rValue)
				rValue.ValueType = GetDataType(l, rValue.ValueType);

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					if (!context!.SelectQuery.IsParameterDependent &&
						(l is SqlParameter && lOriginal.CanBeNull || r is SqlParameter && r.CanBeNull))
					{
						context.SelectQuery.IsParameterDependent = true;
					}

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
					CanBeNull     = false,
					DoNotOptimize = true
				};
				lOriginal = l;
			}

			if (r is SqlSearchCondition)
			{
				r = new SqlFunction(typeof(bool), "CASE", r, new SqlValue(true), new SqlValue(false))
				{
					CanBeNull     = false,
					DoNotOptimize = true
				};
				rOriginal = r;
			}

			ISqlPredicate? predicate = null;
			if (op == SqlPredicate.Operator.Equal || op == SqlPredicate.Operator.NotEqual)
			{
				bool?           value      = null;
				ISqlExpression? expression = null;
				var             isNullable = false;
				if (IsBooleanConstant(left, out value))
				{
					isNullable = typeof(bool?) == left.Type || rOriginal.CanBeNull;
					expression = rOriginal;
				}
				else if (IsBooleanConstant(right, out value))
				{
					isNullable = typeof(bool?) == right.Type || lOriginal.CanBeNull;
					expression = lOriginal;
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
					var trueValue  = ConvertToSql(context, ExpressionInstances.True,  unwrap: false, columnDescriptor: descriptor);
					var falseValue = ConvertToSql(context, ExpressionInstances.False, unwrap: false, columnDescriptor: descriptor);

					var withNullValue = CompareNullsAsValues &&
										(isNullable || NeedNullCheck(expression))
						? withNull
						: (bool?)null;
					predicate = new SqlPredicate.IsTrue(expression, trueValue, falseValue, withNullValue, isNot);
				}
			}

			var compareNullsAsValues = CompareNullsAsValues;

			predicate ??= new SqlPredicate.ExprExpr(lOriginal, op, rOriginal,
				compareNullsAsValues
					? true
					: null);

			return CreatePlaceholder(context, new SqlSearchCondition(new SqlCondition(false, predicate)), GetOriginalExpression());
		}

		public static List<SqlPlaceholderExpression> CollectPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					list.Add(placeholder);
				}
			});

			return result;
		}

		public static IEnumerable<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> CollectPlaceholders2(
			Expression expression, List<MemberInfo> currentPath)
		{
			IEnumerable<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> Collect(Expression expr, Stack<MemberInfo> current)
			{
				if (expr is SqlPlaceholderExpression placeholder)
					yield return (placeholder, current.ToArray());

				if (expr is SqlGenericConstructorExpression generic)
				{
					foreach (var assignment in generic.Assignments)
					{
						current.Push(assignment.MemberInfo);
						foreach (var found in Collect(assignment.Expression, current))
							yield return found;
						current.Pop();
					}

					foreach (var parameter in generic.Parameters)
					{
						if (parameter.MemberInfo == null)
							throw new LinqException("Parameters which are not mapped to field are not supported.");

						current.Push(parameter.MemberInfo);
						foreach (var found in Collect(parameter.Expression, current))
							yield return found;
						current.Pop();
					}
				}
			}

			foreach (var found in Collect(expression, new (currentPath)))
				yield return found;
		}

		public static List<SqlPlaceholderExpression> CollectDistinctPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (!list.Contains(placeholder))
						list.Add(placeholder);
				}
			});

			return result;
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

					var cd = SuggestColumnDescriptor(context, operand, value, ProjectFlags.SQL);

					var l = ConvertToSql(context, operand, columnDescriptor: cd);
					var r = ConvertToSql(context, value, columnDescriptor: cd);

					return new SqlPredicate.ExprExpr(l, op, r, true);
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

		private sealed class GetDataTypeContext
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
						if (e is ISqlExpression expr)
						{
							var type = expr.GetExpressionType();
							context.DataType  = type.DataType;
							context.DbType    = type.DbType;
							context.Length    = type.Length;
							context.Precision = type.Precision;
							context.Scale     = type.Scale;
							return true;
						}
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

			if (expr == null)
			{
				var sql = BuildSqlExpression(context, arg, ProjectFlags.SQL | ProjectFlags.Keys, null);

				var placeholders = CollectDistinctPlaceholders(sql);

				if (placeholders.Count == 1)
					expr = placeholders[0].Sql;
				else
				{
					var objParam = Expression.Parameter(typeof(object));

					var getters = new SqlGetValue[placeholders.Count];
					for (int i = 0; i < getters.Length; i++)
					{
						var placeholder = placeholders[i];

						var cd = QueryHelper.GetColumnDescriptor(placeholder.Sql);

						if (cd != null)
						{
							getters[i] = new SqlGetValue(placeholder.Sql, placeholder.Type, cd, null);
						}
						else
						{
							var body = placeholder.Path.Replace(arg, Expression.Convert(objParam, arg.Type));
							body = Expression.Convert(body, typeof(object));

							var lambda = Expression.Lambda<Func<object, object>>(body, objParam);

							getters[i] = new SqlGetValue(placeholder.Sql, placeholder.Type, null, lambda.Compile());
						}
					}

					expr = new SqlObjectExpression(MappingSchema, getters);
				}
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

					if (CanBeCompiled(arr, false))
					{
						var p = ParametersContext.BuildParameter(arr, columnDescriptor, false, buildParameterType: ParametersContext.BuildParameterType.InPredicate).SqlParameter;
						p.IsQueryParameter = false;
						return new SqlPredicate.InList(expr, Configuration.Linq.CompareNullsAsValues ? false : null, false, p);
					}

					break;
			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		#endregion

		#region ColumnDescriptor Helpers

		public ColumnDescriptor? SuggestColumnDescriptor(IBuildContext context, Expression expr, ProjectFlags flags)
		{
			expr = expr.Unwrap();
			if (TryConvertToSql(context, flags | ProjectFlags.Test, expr, null, out var sqlExpr, out _))
			{
				var descriptor = QueryHelper.GetColumnDescriptor(sqlExpr);
				if (descriptor != null)
				{
					return descriptor;
				}
			}

			return null;
		}

		public ColumnDescriptor? SuggestColumnDescriptor(IBuildContext context, Expression expr1, Expression expr2,
			ProjectFlags flags)
		{
			return SuggestColumnDescriptor(context, expr1, flags) ?? SuggestColumnDescriptor(context, expr2, flags);
		}

		public ColumnDescriptor? SuggestColumnDescriptor(IBuildContext context, ReadOnlyCollection<Expression> expressions, ProjectFlags flags)
		{
			foreach (var expr in expressions)
			{
				var descriptor = SuggestColumnDescriptor(context, expr, flags);
				if (descriptor != null)
					return descriptor;
			}

			return null;
		}

		#endregion

		#region LIKE predicate

		ISqlPredicate CreateStringPredicate(IBuildContext? context, MethodCallExpression expression, SqlPredicate.SearchString.SearchKind kind, ISqlExpression caseSensitive, ProjectFlags flags)
		{
			var e = expression;

			var descriptor = SuggestColumnDescriptor(context, e.Object, e.Arguments[0], flags);

			var o = ConvertToSql(context, e.Object,       unwrap: false, columnDescriptor: descriptor);
			var a = ConvertToSql(context, e.Arguments[0], unwrap: false, columnDescriptor: descriptor);

			return new SqlPredicate.SearchString(o, false, a, kind, caseSensitive);
		}

		ISqlPredicate ConvertLikePredicate(IBuildContext context, MethodCallExpression expression, ProjectFlags flags)
		{
			var e  = expression;

			var descriptor = SuggestColumnDescriptor(context, e.Arguments, flags);

			var a1 = ConvertToSql(context, e.Arguments[0], unwrap: false, columnDescriptor: descriptor);
			var a2 = ConvertToSql(context, e.Arguments[1], unwrap: false, columnDescriptor: descriptor);

			ISqlExpression? a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2], unwrap: false, columnDescriptor: descriptor);

			return new SqlPredicate.Like(a1, false, a2, a3);
		}

		#endregion

		#region MakeIsPredicate

		public ISqlPredicate MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
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

		public ISqlPredicate MakeIsPredicate<TContext>(
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
				{
					var discriminatorSql = getSql(getSqlContext, mapping[0].DiscriminatorName);
					var sqlValue = MappingSchema.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code);

					if (QueryHelper.IsNullValue(sqlValue))
					{
						if (!discriminatorSql.CanBeNull)
							discriminatorSql = new SqlNullabilityExpression(discriminatorSql);
						return new SqlPredicate.IsNull(discriminatorSql, false);
					}

					return new SqlPredicate.ExprExpr(
						discriminatorSql,
						SqlPredicate.Operator.Equal,
						sqlValue,
						Configuration.Linq.CompareNullsAsValues ? true : null);
				}
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

		ISqlPredicate MakeIsPredicate(IBuildContext context, TypeBinaryExpression expression, ProjectFlags flags)
		{
			var predicateExpr = MakeIsPredicateExpression(context, expression);

			return ConvertPredicate(context, predicateExpr, flags);
		}

		Expression MakeIsPredicateExpression(IBuildContext context, TypeBinaryExpression expression)
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
					return Expression.Constant(true);
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

				var memberInfo = ttype.GetMemberEx(field.ColumnDescriptor.MemberInfo) ?? throw new InvalidOperationException();

				var left = Expression.MakeMemberAccess(obj, memberInfo);
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

			return expr!;
		}

		#endregion

		#endregion

		#region Search Condition Builder

		public bool BuildSearchCondition(IBuildContext? context, Expression expression, ProjectFlags flags, List<SqlCondition> conditions)
		{
			//expression = GetRemoveNullPropagationTransformer(true).Transform(expression);

			switch (expression.NodeType)
			{
				case ExpressionType.And     :
				case ExpressionType.AndAlso :
					{
						var e = (BinaryExpression)expression;

						BuildSearchCondition(context, e.Left, flags,  conditions);
						BuildSearchCondition(context, e.Right, flags, conditions);

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

						BuildSearchCondition(context, e.Left, flags,  orCondition.Conditions);
						orCondition.Conditions[orCondition.Conditions.Count - 1].IsOr = true;
						BuildSearchCondition(context, e.Right, flags, orCondition.Conditions);

						conditions.Add(new SqlCondition(false, orCondition));

						break;
					}

				case ExpressionType.Not    :
					{
						var e            = (UnaryExpression)expression;
						var notCondition = new SqlSearchCondition();

						BuildSearchCondition(context, e.Operand, flags, notCondition.Conditions);

						conditions.Add(new SqlCondition(true, notCondition));

						break;
					}

				default                    :
					var predicate = ConvertPredicate(context, expression, flags);

					if (predicate == null)
						return false;

					conditions.Add(new SqlCondition(false, predicate));

					break;
			}

			return true;
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

		private sealed class CanBeTranslatedToSqlContext
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

		#endregion

		#region Helpers

		public IBuildContext? GetContext(IBuildContext? current, Expression? expression)
		{
			throw new NotImplementedException();
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
			if (CanBeCompiled(expr, false))
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
							assignments.Add((ma, dic.TryGetValue(ma.Member.Name, out var idx) ? idx : 1000000));

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
			/*if (query.Select.Columns.Count == 0)
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
			}*/
			}


		#endregion

		#region CTE

		List<Tuple<Expression, CteContext>>? _ctes;
		Dictionary<IQueryable, Expression>?  _ctesObjectMapping;

		public CteContext RegisterCte(IQueryable? queryable, Expression? cteExpression, Func<CteClause> buildFunc)
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
				var cteClause = buildFunc();

				value = new CteContext(this, null, cteClause, cteExpression);

				_ctes ??= new();
				_ctes.Add(Tuple.Create(cteExpression, value));
			}

			return value;
		}

		CteContext? FindRegisteredCteByExpression(Expression cteExpression, out int? idx)
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


		/*
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
		*/

		/*
		public IBuildContext? GetCteContext(Expression cteExpression)
		{
			return FindRegisteredCteByExpression(cteExpression, out _)?.Item2;
		}
		*/

		#endregion

		#region Eager Loading

		public static readonly ParameterExpression PreambleParam =
			Expression.Parameter(typeof(object[]), "preamble");

		public int RegisterPreamble<T>(
			object? data,
			Func<object?, IDataContext, Expression, object?[]?, T> func,
			Func<object?, IDataContext, Expression, object?[]?, CancellationToken, Task<T>> funcAsync
			)
		{
			throw new NotImplementedException();

			/*_preambles ??= new();
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
			return _preambles.Count - 1;*/
		}

		#endregion

		#region Query Filter

		private Stack<Type[]>? _disabledFilters;

		public void PushDisabledQueryFilters(Type[] disabledFilters)
		{
			_disabledFilters ??= new Stack<Type[]>();
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

		public void PopDisabledFilter()
		{
			if (_disabledFilters == null)
				throw new InvalidOperationException();

			_ = _disabledFilters.Pop();
		}

		#endregion

		#region Query Hint Stack

		List<SqlQueryExtension>? _sqlQueryExtensionStack;

		public void PushSqlQueryExtension(SqlQueryExtension extension)
		{
			(_sqlQueryExtensionStack ??= new()).Add(extension);
		}

		public void PopSqlQueryExtension(SqlQueryExtension extension)
		{
			if (_sqlQueryExtensionStack == null || _sqlQueryExtensionStack.Count > 0)
				throw new InvalidOperationException();
			_sqlQueryExtensionStack.RemoveAt(_sqlQueryExtensionStack.Count - 1);
		}

		#endregion

		#region Grouping Guard

		public bool IsGroupingGuardDisabled { get; set; }

		#endregion

		#region Projection

		public Expression Project(IBuildContext context, Expression? path, List<Expression>? nextPath, int nextIndex, ProjectFlags flags, Expression body, bool strict)
		{
			MemberInfo? member = null;
			Expression? next   = null;

			if (path is MemberExpression memberExpression)
			{
				nextPath ??= new();
				nextPath.Add(memberExpression);

				if (memberExpression.Expression is MemberExpression me)
				{
					// going deeper
					return Project(context, me, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				if (memberExpression.Expression.NodeType == ExpressionType.Convert)
				{
					// going deeper
					return Project(context, ((UnaryExpression)memberExpression.Expression).Operand, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path is SqlGenericParamAccessExpression accessExpression)
			{
				nextPath ??= new();
				nextPath.Add(accessExpression);

				if (accessExpression.Constructor is SqlGenericParamAccessExpression ae)
				{
					// going deeper
					return Project(context, ae, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path == null)
			{
				if (nextPath == null || nextIndex < 0)
				{
					if (body == null)
						throw new InvalidOperationException();

					return body;
				}

				next = nextPath[nextIndex];

				if (next is MemberExpression me)
				{
					member = me.Member;
				}
				else if (next is SqlGenericParamAccessExpression)
				{
					// nothing to do right now
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (flags.HasFlag(ProjectFlags.SQL))
			{
				body = RemoveNullPropagation(body, flags.HasFlag(ProjectFlags.Keys));
			}

			switch (body.NodeType)
			{
				case ExpressionType.Extension:
				{
					if (body is SqlPlaceholderExpression placeholder)
					{
						return placeholder;
					}

					if (member != null)
					{
						if (body is ContextRefExpression contextRef)
						{
							var objExpression   = body;
							var memberCorrected = contextRef.Type.GetMemberEx(member);
							if (memberCorrected  is null)
							{
								// inheritance handling
								if (member.DeclaringType != null &&
								    contextRef.Type.IsSameOrParentOf(member.DeclaringType))
								{
									memberCorrected = member;
									objExpression   = Expression.Convert(objExpression, member.DeclaringType);
								}
								else
								{
									throw new InvalidOperationException(
										$"Member '{member}' not found in type '{contextRef.Type}'.");
								}
							}

							var ma      = Expression.MakeMemberAccess(objExpression, memberCorrected);
							var newPath = nextPath![0].Replace(next!, ma);

							return newPath;
						}

						if (body is SqlGenericConstructorExpression genericConstructor)
						{
							Expression? bodyExpresion = null;
							for (int i = 0; i < genericConstructor.Assignments.Count; i++)
							{
								var assignment = genericConstructor.Assignments[i];
								if (MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, member))
								{
									bodyExpresion = assignment.Expression;
									break;
								}
							}

							if (bodyExpresion == null)
							{
								for (int i = 0; i < genericConstructor.Parameters.Count; i++)
								{
									var parameter = genericConstructor.Parameters[i];
									if (MemberInfoEqualityComparer.Default.Equals(parameter.MemberInfo, member))
									{
										bodyExpresion = parameter.Expression;
										break;
									}
								}
							}

							if (bodyExpresion == null)
							{
								// search in base class
								for (int i = 0; i < genericConstructor.Assignments.Count; i++)
								{
									var assignment = genericConstructor.Assignments[i];
									if (assignment.MemberInfo.ReflectedType != member.ReflectedType && assignment.MemberInfo.Name == member.Name)
									{
										var mi = assignment.MemberInfo.ReflectedType.GetMemberEx(member);
										if (mi != null && MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, mi))
										{
											bodyExpresion = assignment.Expression;
											break;
										}
									}
								}
							}

							if (bodyExpresion is not null)
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, bodyExpresion, strict);
							}

							if (strict)
								return CreateSqlError(null, nextPath![0]);

							return new DefaultValueExpression(null, nextPath![0].Type);
						}
					}

					if (next is SqlGenericParamAccessExpression paramAccessExpression)
					{

						/*
						var projected = Project(context, path, nextPath, nextIndex - 1, flags,
							paramAccessExpression);

						return projected;
						*/

						if (body is SqlGenericConstructorExpression constructorExpression)
						{
							var projected = Project(context, path, nextPath, nextIndex - 1, flags,
								constructorExpression.Parameters[paramAccessExpression.ParamIndex].Expression, strict);
							return projected;
						}

						//throw new InvalidOperationException();
					}

					return body;
				}

				case ExpressionType.MemberAccess:
				{
					var ma = (MemberExpression)body;
					if (member != null)
					{

						var nextMember = nextPath[nextIndex] as MemberExpression;
						if (nextMember != null && nextMember.Expression.Type.IsSameOrParentOf(body.Type))
						{
							var newMember = nextMember.Update(body);

							return Project(context, null, nextPath, nextIndex - 1, flags, newMember, strict);
						}
					}

					break;
				}

				case ExpressionType.New:
				{
					var ne = (NewExpression)body;

					if (ne.Members != null)
					{
						if (member == null)
						{
							break;
						}

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}
					else
					{
						var parameters = ne.Constructor.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType, parameter);

							if (memberByParam != null &&
							    MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					if (member == null)
						return ne;

					if (strict)
						return CreateSqlError(null, nextPath![0]);

					return Expression.Default(nextPath![0].Type);
				}

				case ExpressionType.MemberInit:
				{
					var mi = (MemberInitExpression)body;
					var ne = mi.NewExpression;

					if (member == null)
					{
						throw new NotImplementedException();
					}

					if (ne.Members != null)
					{

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					var memberInType = body.Type.GetMemberEx(member);
					if (memberInType != null)
					{
						for (int index = 0; index < mi.Bindings.Count; index++)
						{
							var binding = mi.Bindings[index];
							switch (binding.BindingType)
							{
								case MemberBindingType.Assignment:
								{
									var assignment = (MemberAssignment)binding;
									if (MemberInfoEqualityComparer.Default.Equals(assignment.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											assignment.Expression, strict);
									}

									break;
								}
								case MemberBindingType.MemberBinding:
								{
									var memberMemberBinding = (MemberMemberBinding)binding;
									if (MemberInfoEqualityComparer.Default.Equals(memberMemberBinding.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											new SqlGenericConstructorExpression(
												memberMemberBinding.Member.GetMemberType(),
												memberMemberBinding.Bindings), strict);
									}

									break;
								}
								case MemberBindingType.ListBinding:
									throw new NotImplementedException();
								default:
									throw new NotImplementedException();
							}
						}
					}

					if (strict)
						return CreateSqlError(null, nextPath![0]);

					return Expression.Default(nextPath![0].Type);

				}
				case ExpressionType.Conditional:
				{
					var cond      = (ConditionalExpression)body;
					var trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, strict);
					var falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, strict);

					var trueHasError = trueExpr is SqlErrorExpression;
					var falseHasError = falseExpr is SqlErrorExpression;

					if (strict && (trueHasError || falseHasError))
					{
						if (trueHasError == falseHasError)
						{
							return trueExpr;
						}

						trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, false);
						falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, false);
					}

					if (trueExpr is SqlErrorExpression || falseExpr is SqlErrorExpression)
					{
						break;
					}

					var newExpr = (Expression)Expression.Condition(cond.Test, trueExpr, falseExpr);

					return newExpr;
				}

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						/*
						if (expr.Type.IsValueType)
						{
							var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);
							return placeholder;
						}
						*/

						return Expression.Default(expr.Type);
					}

					break;

				}
				case ExpressionType.Default:
				{
					var expr = (path ?? next)!;

					/*
					if (expr.Type.IsValueType)
					{
						var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);
						return placeholder;
					}
					*/

					return Expression.Default(expr.Type);
				}

				/*
				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);

						return placeholder;
					}

					return body;
				}

				case ExpressionType.Default:
				{
					var placeholder = CreatePlaceholder(context, new SqlValue(body.Type, null), body);
					return placeholder;
				}
				*/

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)body;

					if (mc.Method.IsStatic)
					{
						var parameters = mc.Method.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(mc.Method.ReturnType, parameter);

							if (memberByParam != null &&
							    MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, mc.Arguments[i], strict);
							}
						}
					}

					if (member != null)
					{
						var ma = Expression.MakeMemberAccess(mc, member);
						return Project(context, path, nextPath, nextIndex - 1, flags, ma, strict);
					}

					return mc;
				}

				case ExpressionType.TypeAs:
				{
					var unary = (UnaryExpression)body;

					var truePath = Project(context, path, nextPath, nextIndex, flags, unary.Operand, strict);

					var isPredicate = MakeIsPredicateExpression(context, Expression.TypeIs(unary.Operand, unary.Type));

					if (isPredicate is ConstantExpression constExpr)
					{
						if (constExpr.Value is true)
							return truePath;
						return Expression.Default(truePath.Type);
					}

					var falsePath = Expression.Default(truePath.Type);

					var conditional = Expression.Condition(isPredicate, truePath, falsePath);

					return conditional;
				}
			}

			return CreateSqlError(context, next);
		}


		private Dictionary<SqlCacheKey, Expression> _expressionCache = new(SqlCacheKey.SqlCacheKeyComparer);
		private Dictionary<ColumnCacheKey, SqlPlaceholderExpression> _columnCache = new(ColumnCacheKey.ColumnCacheKeyComparer);

		/// <summary>
		/// Caches expressions generated by context
		/// </summary>
		/// <param name="currentContext"></param>
		/// <param name="path"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public Expression MakeExpression(IBuildContext? currentContext, Expression path, ProjectFlags flags)
		{
			static Expression ExecuteMake(IBuildContext context, Expression expression, ProjectFlags projectFlags)
			{
				var result = context.MakeExpression(expression, projectFlags);
				return result;
			}

			static void DebugCacheHit(IBuildContext context, Expression original, Expression cached, ProjectFlags projectFlags)
			{
				if (!projectFlags.IsTest() && (projectFlags.IsExpression() || projectFlags.IsSql()))
				{
					var i = 1;
				}
			}

			// nothing to project here
			if (path.NodeType   == ExpressionType.Parameter 
				|| path.NodeType == ExpressionType.Lambda
				|| path.NodeType == ExpressionType.New
				|| path.NodeType == ExpressionType.MemberInit
			    || path.NodeType == ExpressionType.Extension && path is SqlPlaceholderExpression or SqlGenericConstructorExpression)
			{
				return path;
			}

			if ((flags & (ProjectFlags.Root | ProjectFlags.AggregationRoot | ProjectFlags.AssociationRoot | ProjectFlags.Expand | ProjectFlags.Table)) == 0)
			{
				// try to find already converted to SQL
				var sqlKey = new SqlCacheKey(path, null, null, null, flags.SqlFlag());
				if (_cachedSql.TryGetValue(sqlKey, out var cachedSql) && cachedSql is SqlPlaceholderExpression)
				{
					return cachedSql;
				}
			}

			var key = new SqlCacheKey(path, null, null, null, flags);

			if (_expressionCache.TryGetValue(key, out var expression) && expression.Type == path.Type)
			{
#if DEBUG
				DebugCacheHit(currentContext, path, expression, flags);
#endif
				return expression;
			}

			var doNotProcess = false;
			expression = null;

			ContextRefExpression? rootContext = null;

			if (path is MemberExpression memberExpression && memberExpression.Expression != null)
			{
				if (memberExpression.Member.IsNullableValueMember())
				{
					var corrected = MakeExpression(currentContext, memberExpression.Expression, flags);
					if (corrected.Type != path.Type)
					{
						corrected = Expression.Convert(corrected, path.Type);
					}
					return MakeExpression(currentContext, corrected, flags);
				}

				if (memberExpression.Expression.NodeType == ExpressionType.Convert)
				{
					var unary = (UnaryExpression)memberExpression.Expression;
					if (unary.Operand is ContextRefExpression contextRef)
					{
						memberExpression = memberExpression.Update(contextRef.WithType(memberExpression.Expression.Type));
						return MakeExpression(currentContext, memberExpression, flags);
					}
				}

				//TODO: why i cannot do that without GetLevelExpression ???
				var rootLevelExpression = memberExpression.GetLevelExpression(MappingSchema, 0);

				// do not process if root is parameter
				if (rootLevelExpression.NodeType == ExpressionType.Parameter)
					return path;

				rootContext = rootLevelExpression as ContextRefExpression;
				if (rootContext != null)
				{
					// SetOperationContext can know how to process such path without preparing

					var corrected = ExecuteMake(rootContext.BuildContext, path, flags);

					if (!ExpressionEqualityComparer.Instance.Equals(corrected, path) && corrected is not DefaultValueExpression && corrected is not SqlErrorExpression)
					{
						corrected = MakeExpression(rootContext.BuildContext, corrected, flags);

						var pathStr = path.ToString();

						if (corrected is SqlPlaceholderExpression placeholder)
						{
							corrected = placeholder.WithTrackingPath(path);
						}

						return corrected;
					}
				}

				var root = MakeExpression(currentContext, memberExpression.Expression, flags.RootFlag());

				var newPath = memberExpression.Update(root);

				path = newPath;

				if (IsAssociation(newPath))
				{
					if (root is ContextRefExpression contextRef)
					{
						expression = TryCreateAssociation(newPath, contextRef, flags);
					}
				}

				rootContext = root as ContextRefExpression;
				if (rootContext == null)
				{
					//TODO: why i cannot do that without GetLevelExpression ???
					rootContext = root.GetLevelExpression(MappingSchema, 0) as ContextRefExpression;
				}
			}
			else if (path is MethodCallExpression mc)
			{
				if (mc.Method.IsSqlPropertyMethodEx())
				{
					var arguments = mc.Arguments.ToArray();
					var rootArg = MakeExpression(currentContext, arguments[0], flags.RootFlag());
					if (rootArg is ContextRefExpression contextRef)
						rootContext = contextRef;

					arguments[0] = rootArg;
					expression   = mc.Update(null, arguments);
					if (ExpressionEqualityComparer.Instance.Equals(expression, path))
					{
						expression = null;
					}
				} 
				else if (IsAssociation(mc))
				{
					if (!mc.Method.IsStatic)
						throw new NotImplementedException();

					var arguments = mc.Arguments;
					if (arguments.Count == 0)
						throw new InvalidOperationException("Association methods should have at least one parameter");

					var rootArgument = MakeExpression(currentContext, arguments[0], flags.RootFlag());
					if (!ReferenceEquals(rootArgument, arguments[0]))
					{
						var argumentsArray = arguments.ToArray();
						argumentsArray[0] = rootArgument;

						mc = mc.Update(mc.Object, argumentsArray);
					}

					if (mc.Arguments[0] is ContextRefExpression contextRef)
					{
						expression  = TryCreateAssociation(mc, contextRef, flags);
						rootContext = expression as ContextRefExpression;
					}
				}
			}
			else if (path is ContextRefExpression contextRef)
			{
				rootContext = contextRef;
			}
			else if (path is SqlGenericParamAccessExpression paramAccessExpression)
			{
				var root = paramAccessExpression.Constructor;
				while (root is SqlGenericParamAccessExpression pa)
				{
					root = pa.Constructor;
				}

				if (root is ContextRefExpression contextRefExpression)
				{
					rootContext = contextRefExpression;
				}
			}
			else if (path.NodeType == ExpressionType.Convert)
			{
				var unary      = (UnaryExpression)path;

				expression   = MakeExpression(currentContext, unary.Operand, flags);
				if (expression.Type != path.Type)
					expression = Expression.Convert(expression, path.Type);
				doNotProcess = true;
			}
			else if (path.NodeType == ExpressionType.TypeAs && currentContext != null)
			{
				var unary     = (UnaryExpression)path;
				var testExpr  = MakeIsPredicateExpression(currentContext, Expression.TypeIs(unary.Operand, unary.Type));
				var trueCase  = Expression.Convert(unary.Operand, unary.Type);
				var falseCase = Expression.Default(unary.Type);

				if (testExpr is ConstantExpression constExpr)
				{
					if (constExpr.Value is true)
						expression  = trueCase;
					else 
						expression = falseCase;
				}
				else
				{
					doNotProcess = true;
					expression   = Expression.Condition(testExpr, trueCase, falseCase);
				}
			}
			else if (path.NodeType == ExpressionType.TypeIs && currentContext != null)
			{
				var typeBinary = (TypeBinaryExpression)path;
				expression = MakeIsPredicateExpression(currentContext, typeBinary);
				doNotProcess = true;
			}

			if (expression == null)
			{
				if (rootContext != null)
				{
					currentContext = rootContext.BuildContext;
					expression     = ExecuteMake(currentContext, path, flags);
					if (expression is SqlEagerLoadExpression eager && rootContext.BuildContext != eager.ContextRef.BuildContext)
					{
						expression = new SqlEagerLoadExpression(rootContext, path, GetSequenceExpression(rootContext.BuildContext));
					}
				}	
				else
					expression = path;
			}

			if (!doNotProcess)
			{
				if (!ExpressionEqualityComparer.Instance.Equals(expression, path))
				{
					// Do recursive again
					expression = MakeExpression(currentContext, expression, flags);
				}
				else
				{
					var handled = false;

					if (!handled)
					{
						var exposed = ExposeExpression(path);
						if (!ReferenceEquals(exposed, path))
						{
							expression = MakeExpression(currentContext, exposed, flags);
							handled    = true;
						}
					}

					if (!handled)
					{
						var applyConvert = true;
						if (currentContext != null && flags.HasFlag(ProjectFlags.Expression))
						{
							if (CanBeCompiled(path, true) || CanBeConstant(path))
							{
								applyConvert = false;
							}

							if (applyConvert && path is MethodCallExpression mc)
							{
								var converted = ConvertSingleExpression(path);
								if (!ReferenceEquals(converted, path))
								{
									// apply only if it is convertible
									//
									var newMc = mc.Update(
										mc.Object == null
											? null
											: BuildSqlExpression(currentContext, mc.Object, flags),
										mc.Arguments.Select(a =>
											BuildSqlExpression(currentContext, a, flags)));

									if (!ExpressionEqualityComparer.Instance.Equals(mc, newMc))
									{
										expression   = MakeExpression(currentContext, newMc, flags);
										handled      = true;
										applyConvert = false;
									}
								}
							}
						}

						if (applyConvert)
						{
							var converted = ConvertSingleExpression(path);
							if (!ReferenceEquals(converted, path))
							{
								expression = MakeExpression(currentContext, converted, flags);
								handled    = true;
							}
						}
					}

					if (!handled && flags.HasFlag(ProjectFlags.Expression) && CanBeCompiled(path, true))
					{
						expression = path;
						handled    = true;
					}

					if (flags.HasFlag(ProjectFlags.Expression) && path.NodeType == ExpressionType.NewArrayInit)
					{
						expression = path;
						handled    = true;
					}

					if (!handled && (flags.HasFlag(ProjectFlags.SQL) || flags.HasFlag(ProjectFlags.Expression)))
					{
						// Handling subqueries
						//

						var ctx = rootContext?.BuildContext ?? currentContext;
						if (ctx != null)
						{
							var subqueryExpression =
								TryGetSubQueryExpression(ctx, path, null, flags.HasFlag(ProjectFlags.Test));
							if (subqueryExpression != null)
							{
								expression = MakeExpression(ctx, subqueryExpression, flags);
								if (expression.Type != path.Type)
								{
									expression = new SqlAdjustTypeExpression(expression, path.Type, MappingSchema);
								}
							}
						}
					}
				}
			}

			if (expression is SqlPlaceholderExpression placeholderExpression)
			{
				expression = placeholderExpression.WithTrackingPath(path);
			}

			_expressionCache[key] = expression;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				if ((flags.HasFlag(ProjectFlags.SQL) ||
				     flags.HasFlag(ProjectFlags.Keys)) && expression is SqlPlaceholderExpression)
				{
					var anotherKey = new SqlCacheKey(path, null, null, null, ProjectFlags.Expression);
					_expressionCache[anotherKey] = expression;

					if (flags.HasFlag(ProjectFlags.Keys))
					{
						anotherKey = new SqlCacheKey(path, null, null, null, ProjectFlags.Expression | ProjectFlags.Keys);
						_expressionCache[anotherKey] = expression;
					}
				}
			}

			return expression;
		}

		public SqlPlaceholderExpression MakeColumn(SelectQuery? parentQuery, SqlPlaceholderExpression sqlPlaceholder, bool asNew = false)
		{
			if (parentQuery == sqlPlaceholder.SelectQuery)
				throw new InvalidOperationException();

			var placeholderType = sqlPlaceholder.Type;
			if (placeholderType.IsNullable())
				placeholderType = placeholderType.UnwrapNullableType();

			var key = new ColumnCacheKey(sqlPlaceholder.Path, placeholderType, sqlPlaceholder.SelectQuery, parentQuery);

			if (!asNew && _columnCache.TryGetValue(key, out var placeholder))
			{
				return placeholder.WithType(sqlPlaceholder.Type);
			}

			var alias = sqlPlaceholder.Alias;

			if (sqlPlaceholder.TrackingPath is MemberExpression tme)
				alias = tme.Member.Name;
			else if (sqlPlaceholder.Path is MemberExpression me)
				alias = me.Member.Name;


			var sql = sqlPlaceholder.Sql;
			/*// Right now, handling nullability in that way
			//
			var sql = sqlPlaceholder.Sql;
			if (sqlPlaceholder.SelectQuery.IsNullable && !sql.CanBeNull)
			{
				sql = new SqlNullabilityExpression(sql);
			}*/

			//TODO: correct
			var idx = asNew
				? sqlPlaceholder.SelectQuery.Select.AddNew(sql)
				: sqlPlaceholder.SelectQuery.Select.AddNew(sql);

			var column = sqlPlaceholder.SelectQuery.Select.Columns[idx];

			if (!string.IsNullOrEmpty(alias))
			{
				column.RawAlias = alias;
			}

			placeholder = CreatePlaceholder(parentQuery, column, sqlPlaceholder.Path, sqlPlaceholder.ConvertType, alias, idx, trackingPath: sqlPlaceholder.TrackingPath);

			_columnCache[key] = placeholder;

			/*if (parentQuery != null)
			{
				var preciseCacheKey = new SqlCacheKey(sqlPlaceholder.Path, null, null, parentQuery, ProjectFlags.SQL);
				_preciseCachedSql[preciseCacheKey] = placeholder;

				var cacheKey = new SqlCacheKey(sqlPlaceholder.Path, null, null, null, ProjectFlags.SQL);
				_cachedSql[cacheKey] =  placeholder;
			}*/

			return placeholder;
		}

		#endregion
	}
}
