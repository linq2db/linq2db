﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		readonly HashSet<Expression>                    _skippedExpressions   = new HashSet<Expression>();
		readonly Dictionary<Expression,UnaryExpression> _convertedExpressions = new Dictionary<Expression,UnaryExpression>();

		public void UpdateConvertedExpression(Expression oldExpression, Expression newExpression)
		{
			if (_convertedExpressions.TryGetValue(oldExpression, out var conversion)
				&& !_convertedExpressions.ContainsKey(newExpression))
			{
				UnaryExpression newConversion;
				if (conversion.NodeType == ExpressionType.Convert)
				{
					newConversion = Expression.Convert(newExpression, conversion.Type);
				}
				else
				{
					newConversion = Expression.ConvertChecked(newExpression, conversion.Type);
				}

				_convertedExpressions.Add(newExpression, newConversion);
			}
		}

		public void RemoveConvertedExpression(Expression ex)
		{
			_convertedExpressions.Remove(ex);
		}

		public Expression BuildExpression(IBuildContext context, Expression expression, bool enforceServerSide)
		{
			var newExpr = expression.Transform(expr => TransformExpression(context, expr, enforceServerSide, null));
			return newExpr;
		}

		TransformInfo TransformExpression(IBuildContext context, Expression expr, bool enforceServerSide, string alias)
		{
			if (_skippedExpressions.Contains(expr))
				return new TransformInfo(expr, true);

			if (expr.Find(IsNoneSqlMember) != null)
				return new TransformInfo(expr);

			switch (expr.NodeType)
			{
				case ExpressionType.Convert       :
				case ExpressionType.ConvertChecked:
					{
						if (expr.Type == typeof(object))
							break;

						var cex = (UnaryExpression)expr;

						_convertedExpressions.Add(cex.Operand, cex);

						var nex = BuildExpression(context, cex.Operand, enforceServerSide);

						if (nex.Type != cex.Type)
							nex = cex.Update(nex);

						var ret = new TransformInfo(nex, true);

						RemoveConvertedExpression(cex.Operand);

						return ret;
					}

				case ExpressionType.MemberAccess:
					{
						if (IsServerSideOnly(expr) || PreferServerSide(expr, enforceServerSide))
							return new TransformInfo(BuildSql(context, expr, alias));

						var ma = (MemberExpression)expr;

						var l  = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);
						if (l != null)
						{
							// In Grouping KeyContext we have to perform calculation on server side
							if (Contexts.Any(c => c is GroupByBuilder.KeyContext))
								return new TransformInfo(BuildSql(context, expr, alias));
							break;
						}

						if (ma.Member.IsNullableValueMember())
							break;

						if (ma.Member.IsNullableHasValueMember())
						{
							Expression e = Expression.NotEqual(
								ma.Expression, Expression.Constant(null, ma.Expression.Type));

							return new TransformInfo(
								BuildExpression(
									context,
									ma.Expression.Type.IsPrimitiveEx() ?
										Expression.Call(
											MemberHelper.MethodOf(() => Sql.AsSql(true)),
											e) :
										e, enforceServerSide),
								true);
						}

						var ctx = GetContext(context, ma);

						if (ctx != null)
						{
							if (ma.Type.IsGenericTypeEx() && typeof(IEnumerable<>).IsSameOrParentOf(ma.Type))
							{
								var res = ctx.IsExpression(ma, 0, RequestFor.Association);

								if (res.Result)
								{
									var table = (TableBuilder.AssociatedTableContext)res.Context;
									if (table.IsList)
									{
										var mexpr = GetMultipleQueryExpression(context, MappingSchema, ma, new HashSet<ParameterExpression>());
										return new TransformInfo(BuildExpression(context, mexpr, enforceServerSide));
									}
								}
							}

							var prevCount  = ctx.SelectQuery.Select.Columns.Count;
							var expression = ctx.BuildExpression(ma, 0, enforceServerSide);
							if (!alias.IsNullOrEmpty() && (ctx.SelectQuery.Select.Columns.Count - prevCount) == 1)
							{
								ctx.SelectQuery.Select.Columns[ctx.SelectQuery.Select.Columns.Count - 1].Alias = alias;
							}
							return new TransformInfo(expression);
						}

						var ex = ma.Expression;

						while (ex is MemberExpression)
							ex = ((MemberExpression)ex).Expression;

						if (ex is MethodCallExpression ce)
						{
							if (IsSubQuery(context, ce))
							{
								if (!IsMultipleQuery(ce))
								{
									var info = GetSubQueryContext(context, ce);
									if (alias != null)
										info.Context.SetAlias(alias);
									var par  = Expression.Parameter(ex.Type);
									var bex  = info.Context.BuildExpression(ma.Transform(e => e == ex ? par : e), 0, enforceServerSide);

									if (bex != null)
										return new TransformInfo(bex);
								}
							}
						}

						ex = ma.Expression;

						if (ex != null && ex.NodeType == ExpressionType.Constant)
						{
							// field = localVariable
							//
							var c = _expressionAccessors[ex];
							return new TransformInfo(Expression.MakeMemberAccess(Expression.Convert(c, ex.Type), ma.Member));
						}

						break;
					}

				case ExpressionType.Parameter:
					{
						if (expr == ParametersParam)
							break;

						var ctx = GetContext(context, expr);

						if (ctx != null)
						{
							var buildExpr = ctx.BuildExpression(expr, 0, enforceServerSide);
							if (buildExpr.Type != expr.Type)
							{
								buildExpr = Expression.Convert(buildExpr, expr.Type);
							}
							return new TransformInfo(buildExpr);
						}

						break;
					}

				case ExpressionType.Constant:
					{
						if (expr.Type.IsConstantable())
							break;

						if (_expressionAccessors.TryGetValue(expr, out var accessor))
							return new TransformInfo(Expression.Convert(accessor, expr.Type));

						break;
					}

				case ExpressionType.Coalesce:

					if (expr.Type == typeof(string) && MappingSchema.GetDefaultValue(typeof(string)) != null)
						return new TransformInfo(BuildSql(context, expr, alias));

					if (CanBeTranslatedToSql(context, ConvertExpression(expr), true))
						return new TransformInfo(BuildSql(context, expr, alias));

					break;

				case ExpressionType.Conditional:
					var cond    = (ConditionalExpression)expr;
					var newTest = CorrectEquality(context, cond.Test);
					if (newTest != cond.Test)
					{
						cond = cond.Update(newTest, cond.IfTrue, cond.IfFalse);
						return new TransformInfo(BuildExpression(context, cond, enforceServerSide));
					}

					if (CanBeTranslatedToSql(context, ConvertExpression(expr), true))
						return new TransformInfo(BuildSql(context, expr, alias));
					break;

				case ExpressionType.Call:
					{
						var ce = (MethodCallExpression)expr;

						if (IsGroupJoinSource(context, ce))
						{
							foreach (var arg in ce.Arguments.Skip(1))
								if (!_skippedExpressions.Contains(arg))
									_skippedExpressions.Add(arg);

							if (IsSubQuery(context, ce))
							{
								if (ce.IsQueryable())
								//if (!typeof(IEnumerable).IsSameOrParentOf(expr.Type) || expr.Type == typeof(string) || expr.Type.IsArray)
								{
									var ctx = GetContext(context, expr);

									if (ctx != null)
										return new TransformInfo(ctx.BuildExpression(expr, 0, enforceServerSide));
								}
							}

							break;
						}

						if (ce.IsAssociation(MappingSchema))
						{
							var ctx = GetContext(context, ce);
							if (ctx == null)
								throw new InvalidOperationException();

							return new TransformInfo(ctx.BuildExpression(ce, 0, enforceServerSide));
						}

						if ((_buildMultipleQueryExpressions == null || !_buildMultipleQueryExpressions.Contains(ce)) && IsSubQuery(context, ce))
						{
							if (IsMultipleQuery(ce))
								return new TransformInfo(BuildMultipleQuery(context, ce, enforceServerSide));

							return new TransformInfo(GetSubQueryExpression(context, ce, enforceServerSide, alias));
						}

						if (IsServerSideOnly(expr) || PreferServerSide(expr, enforceServerSide) || ce.Method.IsSqlPropertyMethodEx())
							return new TransformInfo(BuildSql(context, expr, alias));
					}

					break;

				case ExpressionType.New:
					{
						var ne = (NewExpression)expr;

						List<Expression> arguments = new List<Expression>();
						for (var i = 0; i < ne.Arguments.Count; i++)
						{
							var a = ne.Arguments[i];
							var memberAlias = ne.Members?[i].Name;
							var newArgument =
								a.Transform(ae => TransformExpression(context, ae, enforceServerSide, memberAlias));

							// Update nullability
							if (newArgument.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)newArgument;
								var attr = MappingSchema.GetAttribute<Sql.ExpressionAttribute>(mc.Type, mc.Method);

								if (attr != null 
								    && attr.IsNullable == Sql.IsNullableType.IfAnyParameterNullable 
								    && mc.Arguments.Count == 1 
									&& attr.Expression == "{0}" 
								    && mc.Method.ReturnParameter?.ParameterType.IsNullable() == true
								)
								{
									var parameter = mc.Method.GetParameters()[0];
									if (mc.Method.ReturnParameter?.ParameterType != parameter.ParameterType 
										&& parameter.ParameterType.IsValueTypeEx()
										&& mc.Arguments[0] is ConvertFromDataReaderExpression readerExpression)
									{
										newArgument = readerExpression.MakeNullable();
									}
								}
							}
							else if (newArgument.NodeType == ExpressionType.Convert || newArgument.NodeType == ExpressionType.ConvertChecked)
							{
								var conv = (UnaryExpression)newArgument;
								if (ne.Members?[i].GetMemberType().IsNullable() == true
								    && conv.Operand is ConvertFromDataReaderExpression readerExpression
								    && !readerExpression.Type.IsNullable())
								{
									newArgument = readerExpression.MakeNullable();
								}
							}
							else if (newArgument.NodeType == ExpressionType.Extension &&
							         newArgument is ConvertFromDataReaderExpression readerExpression)
							{
								if (ne.Members?[i].GetMemberType().IsNullable() == true &&
								    !readerExpression.Type.IsNullable())
								{
									newArgument = readerExpression.MakeNullable();
								}
							}

							a = newArgument;
							arguments.Add(a);
						}

						if (arguments.Count > 0)
						{
							ne = ne.Update(arguments);
						}

						return new TransformInfo(ne, true);
					}
			}

			if (EnforceServerSide(context))
			{
				switch (expr.NodeType)
				{
					case ExpressionType.MemberInit :
					case ExpressionType.Convert    :
						break;

					default                        :
						if (CanBeCompiled(expr))
							break;
						return new TransformInfo(BuildSql(context, expr, alias));
				}
			}

			return new TransformInfo(expr);
		}

		Expression CorrectEquality(IBuildContext context, Expression expr)
		{
			var result = expr.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Equal || e.NodeType == ExpressionType.NotEqual)
				{
					var b = (BinaryExpression)e;

					Expression cnt = null;
					Expression obj = null;

					if      (IsNullConstant(b.Left))  { cnt = b.Left;  obj = b.Right; }
					else if (IsNullConstant(b.Right)) { cnt = b.Right; obj = b.Left;  }

					if (cnt != null)
					{
						var objContext = GetContext(context, obj);
						if (objContext != null && objContext.IsExpression(obj, 0, RequestFor.Object).Result)
						{
							var sql = objContext.ConvertToSql(obj, 0, ConvertFlags.Key);
							if (sql.Length == 0)
								sql = objContext.ConvertToSql(obj, 0, ConvertFlags.All);

							if (sql.Length > 0)
							{
								Expression predicate = null;
								foreach (var f in sql)
								{
									if (f.Sql is SqlField field && field.Table.All == field)
										continue;

									var valueType = f.Sql.SystemType;

									if (!valueType.IsNullable() && valueType.IsValueTypeEx())
										valueType = typeof(Nullable<>).MakeGenericType(valueType);

									var reader     = BuildSql(context, f.Sql, valueType, null);
									var comparison = Expression.MakeBinary(e.NodeType,
										Expression.Default(valueType), reader);

									predicate = predicate == null
										? comparison
										: Expression.MakeBinary(
											e.NodeType == ExpressionType.Equal
												? ExpressionType.AndAlso
												: ExpressionType.OrElse, predicate, comparison);
								}

								if (predicate != null)
									return predicate;
							}
						}
					}
				}

				return e;
			});
			return result;
		}

		static bool IsMultipleQuery(MethodCallExpression ce)
		{
			return typeof(IEnumerable).IsSameOrParentOf(ce.Type) && ce.Type != typeof(string) && !ce.Type.IsArray;
		}

		class SubQueryContextInfo
		{
			public MethodCallExpression Method;
			public IBuildContext        Context;
			public Expression           Expression;
		}

		readonly Dictionary<IBuildContext,List<SubQueryContextInfo>> _buildContextCache = new Dictionary<IBuildContext,List<SubQueryContextInfo>>();

		SubQueryContextInfo GetSubQueryContext(IBuildContext context, MethodCallExpression expr)
		{
			if (!_buildContextCache.TryGetValue(context, out var sbi))
				_buildContextCache[context] = sbi = new List<SubQueryContextInfo>();

			foreach (var item in sbi)
			{
				if (expr.EqualsTo(item.Method, new Dictionary<Expression,QueryableAccessor>()))
					return item;
			}

			var ctx = GetSubQuery(context, expr);

			var info = new SubQueryContextInfo { Method = expr, Context = ctx };

			sbi.Add(info);

			return info;
		}

		public Expression GetSubQueryExpression(IBuildContext context, MethodCallExpression expr, bool enforceServerSide, string alias)
		{
			var info = GetSubQueryContext(context, expr);
			if (info.Expression == null)
				info.Expression = info.Context.BuildExpression(null, 0, enforceServerSide);

			if (!alias.IsNullOrEmpty())
				info.Context.SetAlias(alias);
			return info.Expression;
		}

		static bool EnforceServerSide(IBuildContext context)
		{
			return context.SelectQuery.Select.IsDistinct;
		}

		#endregion

		#region BuildSql

		Expression BuildSql(IBuildContext context, Expression expression, string alias)
		{
			var sqlex = ConvertToSqlExpression(context, expression);
			var idx   = context.SelectQuery.Select.Add(sqlex);

			if (alias != null)
				context.SelectQuery.Select.Columns[idx].RawAlias = alias;

			idx = context.ConvertToParentIndex(idx, context);

			var field = BuildSql(expression, idx);

			return field;
		}

		Expression BuildSql(IBuildContext context, ISqlExpression sqlExpression, Type overrideType, string alias)
		{
			var idx   = context.SelectQuery.Select.Add(sqlExpression);

			if (alias != null)
				context.SelectQuery.Select.Columns[idx].RawAlias = alias;

			idx = context.ConvertToParentIndex(idx, context);

			var field = BuildSql(overrideType ?? sqlExpression.SystemType, idx);

			return field;
		}

		public Expression BuildSql(Expression expression, int idx)
		{
			var type = expression.Type;

			if (_convertedExpressions.TryGetValue(expression, out var cex))
			{
				if (cex.Type.IsNullable() && !type.IsNullable() && type.IsSameOrParentOf(cex.Type.ToNullableUnderlying()))
					type = cex.Type;
			}

			return BuildSql(type, idx);
		}

		public Expression BuildSql(Type type, int idx)
		{
			return new ConvertFromDataReaderExpression(type, idx, DataReaderLocal, DataContext);
		}

		#endregion

		#region IsNonSqlMember

		bool IsNoneSqlMember(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var me = (MemberExpression)expr;

						var om = (
							from c in Contexts.OfType<TableBuilder.TableContext>()
							where c.ObjectType == me.Member.DeclaringType
							select c.EntityDescriptor
						).FirstOrDefault();

						return om != null && om.Associations.All(a => !a.MemberInfo.EqualsTo(me.Member)) && om[me.Member.Name] == null;
					}
			}

			return false;
		}

		#endregion

		#region PreferServerSide

		bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var pi = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Expression?.Type, pi.Member);

						if (l != null)
						{
							var info = l.Body.Unwrap();

							if (l.Parameters.Count == 1 && pi.Expression != null)
								info = info.Transform(wpi => wpi == l.Parameters[0] ? pi.Expression : wpi);

							return info.Find(e => PreferServerSide(e, enforceServerSide)) != null;
						}

						var attr = GetExpressionAttribute(pi.Member);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return l.Body.Unwrap().Find(e => PreferServerSide(e, enforceServerSide)) != null;

						var attr = GetExpressionAttribute(pi.Method);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}
			}

			return false;
		}

		#endregion

		#region Build Mapper

		public Expression BuildBlock(Expression expression)
		{
			if (IsBlockDisable || BlockExpressions.Count == 0)
				return expression;

			BlockExpressions.Add(expression);

			var blockExpression = Expression.Block(BlockVariables, BlockExpressions);

			while (BlockVariables.  Count > 1) BlockVariables.  RemoveAt(BlockVariables.  Count - 1);
			while (BlockExpressions.Count > 1) BlockExpressions.RemoveAt(BlockExpressions.Count - 1);

			return blockExpression;
		}

		public ParameterExpression BuildVariable(Expression expr, string name = null)
		{
			if (name == null)
				name = expr.Type.Name + Interlocked.Increment(ref VarIndex);

			var variable = Expression.Variable(
				expr.Type,
				name.IndexOf('<') >= 0 ? null : name);

			BlockVariables.  Add(variable);
			BlockExpressions.Add(Expression.Assign(variable, expr));

			return variable;
		}

		public Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],T>> BuildMapper<T>(Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			var mapper = Expression.Lambda<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],T>>(
				BuildBlock(expr), new[]
				{
					QueryRunnerParam,
					DataContextParam,
					DataReaderParam,
					ExpressionParam,
					ParametersParam
				});

			return mapper;
		}

		#endregion

		#region BuildMultipleQuery

		interface IMultipleQueryHelper
		{
			Expression GetSubquery(
				ExpressionBuilder       builder,
				Expression              expression,
				ParameterExpression     paramArray,
				IEnumerable<Expression> parameters);
		}

		class MultipleQueryHelper<TRet> : IMultipleQueryHelper
		{
			public Expression GetSubquery(
				ExpressionBuilder       builder,
				Expression              expression,
				ParameterExpression     paramArray,
				IEnumerable<Expression> parameters)
			{
				var lambda      = Expression.Lambda<Func<IDataContext,object[],TRet>>(
					expression,
					Expression.Parameter(typeof(IDataContext), "ctx"),
					paramArray);
				var queryReader = CompiledQuery.Compile(lambda);

				return Expression.Call(
					null,
					MemberHelper.MethodOf(() => ExecuteSubQuery(null, null, null)),
						DataContextParam,
						Expression.NewArrayInit(typeof(object), parameters),
						Expression.Constant(queryReader)
					);
			}

			static TRet ExecuteSubQuery(
				IDataContext                     dataContext,
				object[]                         parameters,
				Func<IDataContext,object[],TRet> queryReader)
			{
				var db = dataContext.Clone(true);

				db.CloseAfterUse = true;

				return queryReader(db, parameters);
			}
		}

		static readonly MethodInfo _whereMethodInfo =
			MemberHelper.MethodOf(() => LinqExtensions.Where<int,int,object>(null,null)).GetGenericMethodDefinition();

		static readonly MethodInfo _queryableMethodInfo =
			MemberHelper.MethodOf<IQueryable<bool>>(n => n.Where(a => a)).GetGenericMethodDefinition();

		static Expression GetMultipleQueryExpression(IBuildContext context, MappingSchema mappingSchema, Expression expression, HashSet<ParameterExpression> parameters)
		{
			if (!Common.Configuration.Linq.AllowMultipleQuery)
				throw new LinqException("Multiple queries are not allowed. Set the 'LinqToDB.Common.Configuration.Linq.AllowMultipleQuery' flag to 'true' to allow multiple queries.");

			expression.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foreach (var p in ((LambdaExpression)e).Parameters)
						parameters.Add(p);
			});

			// Convert associations.
			//
			return expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var root = e.GetRootObject(mappingSchema);

							if (root != null &&
								root.NodeType == ExpressionType.Parameter &&
								!parameters.Contains((ParameterExpression)root))
							{
								var res = context.IsExpression(e, 0, RequestFor.Association);

								if (res.Result)
								{
									var table = (TableBuilder.AssociatedTableContext)res.Context;

									if (table.IsList)
									{
										var me = (MemberExpression)e;
										Expression expr;

										var parentType = me.Expression.Type;
										var childType  = table.ObjectType;

										var queryMethod = table.Association.GetQueryMethod(parentType, childType);
										if (queryMethod != null)
										{
											//TODO: MARS
											var dcConst = Expression.Constant(context.Builder.DataContext.Clone(true));

											expr = queryMethod.GetBody(me.Expression, dcConst);
										}
										else
										{
											var ttype  = typeof(Table<>).MakeGenericType(childType);
											var tbl    = Activator.CreateInstance(ttype, context.Builder.DataContext);
											var method = e == expression ?
												MemberHelper.MethodOf<IEnumerable<bool>>(n => n.Where(a => a)).GetGenericMethodDefinition().MakeGenericMethod(childType) :
												_whereMethodInfo.MakeGenericMethod(e.Type, childType, ttype);

											var op = Expression.Parameter(childType, "t");

											parameters.Add(op);

											Expression ex = null;

											for (var i = 0; i < table.Association.ThisKey.Length; i++)
											{
												var field1 = table.ParentAssociation.SqlTable.Fields[table.Association.ThisKey [i]];
												var field2 = table.                  SqlTable.Fields[table.Association.OtherKey[i]];

												var ma1 = Expression.MakeMemberAccess(op,            field2.ColumnDescriptor.MemberInfo);
												var ma2 = Expression.MakeMemberAccess(me.Expression, field1.ColumnDescriptor.MemberInfo);

												var ee = Equal(mappingSchema, ma1, ma2);

												ex = ex == null ? ee : Expression.AndAlso(ex, ee);
											}

											var predicate = table.Association.GetPredicate(parentType, childType);
											if (predicate != null)
											{
												var body = predicate.GetBody(me.Expression, op);
												ex = ex == null ? body : Expression.AndAlso(ex, body);
											}

											if (ex == null)
												throw new LinqToDBException($"Invalid association configuration for {table.Association.MemberInfo.DeclaringType}.{table.Association.MemberInfo.Name}");

											expr = Expression.Call(null, method, Expression.Constant(tbl), Expression.Lambda(ex, op));
										}

										if (e == expression)
										{
											expr = Expression.Call(
												MemberHelper.MethodOf<IEnumerable<int>>(n => n.ToList()).GetGenericMethodDefinition().MakeGenericMethod(childType),
												expr);
										}

										return expr;

									}
								}
							}

							break;
						}
				}

				return e;
			});
		}

		HashSet<Expression> _buildMultipleQueryExpressions;

		public Expression BuildMultipleQuery(IBuildContext context, Expression expression, bool enforceServerSide)
		{
			var parameters = new HashSet<ParameterExpression>();

			expression = GetMultipleQueryExpression(context, MappingSchema, expression, parameters);

			var paramex = Expression.Parameter(typeof(object[]), "ps");
			var parms   = new List<Expression>();

			// Convert parameters.
			//
			expression = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
				{
					foreach (var param in ((LambdaExpression)e).Parameters)
					{
						parameters.Add(param);
					}
				}

				var root = e.GetRootObject(MappingSchema);

				if (root != null &&
					root.NodeType == ExpressionType.Parameter &&
					!parameters.Contains((ParameterExpression)root))
				{
					if (_buildMultipleQueryExpressions == null)
						_buildMultipleQueryExpressions = new HashSet<Expression>();

					_buildMultipleQueryExpressions.Add(e);

					var ex = Expression.Convert(BuildExpression(context, e, enforceServerSide), typeof(object));

					_buildMultipleQueryExpressions.Remove(e);

					parms.Add(ex);

					return Expression.Convert(
						Expression.ArrayIndex(paramex, Expression.Constant(parms.Count - 1)),
						e.Type);
				}

				return e;
			});

			var sqtype = typeof(MultipleQueryHelper<>).MakeGenericType(expression.Type);
			var helper = (IMultipleQueryHelper)Activator.CreateInstance(sqtype);

			return helper.GetSubquery(this, expression, paramex, parms);
		}

		#endregion
	}
}
