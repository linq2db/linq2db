using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace LinqToDB.Linq.Builder
{
	using Common;

	using LinqToDB.Expressions;
	using Extensions;

	using Mapping;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		readonly HashSet<Expression>                    _skippedExpressions   = new HashSet<Expression>();
		readonly Dictionary<Expression,UnaryExpression> _convertedExpressions = new Dictionary<Expression,UnaryExpression>();

		public Expression BuildExpression(IBuildContext context, Expression expression)
		{
			var newExpr = expression.Transform(expr =>
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

							var nex = BuildExpression(context, cex.Operand);

							if (nex.Type != cex.Type)
								nex = cex.Update(nex);

							var ret = new TransformInfo(nex, true);

							_convertedExpressions.Remove(cex.Operand);

							return ret;
						}

					case ExpressionType.MemberAccess:
						{
							if (IsServerSideOnly(expr) || PreferServerSide(expr))
								return new TransformInfo(BuildSql(context, expr));

							var ma = (MemberExpression)expr;

							var l  = Expressions.ConvertMember(MappingSchema, ma.Expression == null ? null : ma.Expression.Type, ma.Member);
							if (l != null)
							{
								// In Grouping KeyContext whe have to perform calculation on server side
								if (Contexts.Any(c => c is GroupByBuilder.KeyContext))
									return new TransformInfo(BuildSql(context, expr));
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
											e),
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
											return new TransformInfo(BuildExpression(context, mexpr));
										}
									}
								}

								return new TransformInfo(ctx.BuildExpression(ma, 0));
							}

							var ex = ma.Expression;

							while (ex is MemberExpression)
								ex = ((MemberExpression)ex).Expression;

							if (ex is MethodCallExpression)
							{
								var ce = (MethodCallExpression)ex;

								if (IsSubQuery(context, ce))
								{
									if (!IsMultipleQuery(ce))
									{
										var info = GetSubQueryContext(context, ce);
										var par  = Expression.Parameter(ex.Type);
										var bex  = info.Context.BuildExpression(ma.Transform(e => e == ex ? par : e), 0);

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
								return new TransformInfo(ctx.BuildExpression(expr, 0));

							break;
						}

					case ExpressionType.Constant:
						{
							if (expr.Type.IsConstantable())
								break;

							if (_expressionAccessors.ContainsKey(expr))
								return new TransformInfo(Expression.Convert(_expressionAccessors[expr], expr.Type));

							break;
						}

					case ExpressionType.Coalesce:

						if (expr.Type == typeof(string) && MappingSchema.GetDefaultValue(typeof(string)) != null)
							return new TransformInfo(BuildSql(context, expr));

						if (CanBeTranslatedToSql(context, ConvertExpression(expr), true))
							return new TransformInfo(BuildSql(context, expr));

						break;

					case ExpressionType.Conditional:

						if (CanBeTranslatedToSql(context, ConvertExpression(expr), true))
							return new TransformInfo(BuildSql(context, expr));
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
											return new TransformInfo(ctx.BuildExpression(expr, 0));
									}
								}

								break;
							}

							if ((_buildMultipleQueryExpressions == null || !_buildMultipleQueryExpressions.Contains(ce)) && IsSubQuery(context, ce))
							{
								if (IsMultipleQuery(ce))
									return new TransformInfo(BuildMultipleQuery(context, ce));

								return new TransformInfo(GetSubQueryExpression(context, ce));
							}

							if (IsServerSideOnly(expr) || PreferServerSide(expr))
								return new TransformInfo(BuildSql(context, expr));
						}

						break;
				}

				if (EnforceServerSide(context))
				{
					switch (expr.NodeType)
					{
						case ExpressionType.MemberInit :
						case ExpressionType.New        :
						case ExpressionType.Convert    :
							break;

						default                        :
							if (CanBeCompiled(expr))
								break;
							return new TransformInfo(BuildSql(context, expr));
					}
				}

				return new TransformInfo(expr);
			});

			return newExpr;
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
			List<SubQueryContextInfo> sbi;

			if (!_buildContextCache.TryGetValue(context, out sbi))
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

		public Expression GetSubQueryExpression(IBuildContext context, MethodCallExpression expr)
		{
			var info = GetSubQueryContext(context, expr);
			return info.Expression ?? (info.Expression = info.Context.BuildExpression(null, 0));
		}

		static bool EnforceServerSide(IBuildContext context)
		{
			return context.SelectQuery.Select.IsDistinct;
		}

		#endregion

		#region BuildSql

		Expression BuildSql(IBuildContext context, Expression expression)
		{
			var sqlex = ConvertToSqlExpression(context, expression);
			var idx   = context.SelectQuery.Select.AddNew(sqlex);

			idx = context.ConvertToParentIndex(idx, context);

			var field = BuildSql(expression, idx);

			return field;
		}

		public Expression BuildSql(Expression expression, int idx)
		{
			UnaryExpression cex;

			var type = expression.Type;

			if (_convertedExpressions.TryGetValue(expression, out cex))
			{
				if (cex.Type.IsNullable() && !type.IsNullable() && type.IsSameOrParentOf(cex.Type.ToNullableUnderlying()))
					type = cex.Type;
			}

			return BuildSql(type, idx);
		}

		public Expression BuildSql(Type type, int idx)
		{
			return new ConvertFromDataReaderExpression(type, idx, DataReaderLocal, DataContextInfo.DataContext);
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

		bool PreferServerSide(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var pi = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Expression == null ? null : pi.Expression.Type, pi.Member);

						if (l != null)
						{
							var info = l.Body.Unwrap();

							if (l.Parameters.Count == 1 && pi.Expression != null)
								info = info.Transform(wpi => wpi == l.Parameters[0] ? pi.Expression : wpi);

							return info.Find(PreferServerSide) != null;
						}

						var attr = GetExpressionAttribute(pi.Member);
						return attr != null && attr.PreferServerSide && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var e  = pi;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object == null ? null : pi.Object.Type, e.Method);

						if (l != null)
							return l.Body.Unwrap().Find(PreferServerSide) != null;

						var attr = GetExpressionAttribute(e.Method);
						return attr != null && attr.PreferServerSide && !CanBeCompiled(expr);
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

			expression = Expression.Block(BlockVariables, BlockExpressions);

			while (BlockVariables.  Count > 1) BlockVariables.  RemoveAt(BlockVariables.  Count - 1);
			while (BlockExpressions.Count > 1) BlockExpressions.RemoveAt(BlockExpressions.Count - 1);

			return expression;
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

		public Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> BuildMapper<T>(Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			var mapper = Expression.Lambda<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>>(
				BuildBlock(expr), new []
				{
					ContextParam,
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
						ContextParam,
						Expression.NewArrayInit(typeof(object), parameters),
						Expression.Constant(queryReader)
					);
			}

			static TRet ExecuteSubQuery(
				QueryContext                     queryContext,
				object[]                         parameters,
				Func<IDataContext,object[],TRet> queryReader)
			{
				var db = queryContext.GetDataContext();

				try
				{
					return queryReader(db.DataContextInfo.DataContext, parameters);
				}
				finally
				{
					queryContext.ReleaseDataContext(db);
				}
			}
		}

		static readonly MethodInfo _whereMethodInfo =
			MemberHelper.MethodOf(() => LinqExtensions.Where<int,int,object>(null,null)).GetGenericMethodDefinition();

		static Expression GetMultipleQueryExpression(IBuildContext context, MappingSchema mappringSchema, Expression expression, HashSet<ParameterExpression> parameters)
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
							var root = e.GetRootObject();

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
										var ttype  = typeof(Table<>).MakeGenericType(table.ObjectType);
										var tbl    = Activator.CreateInstance(ttype);
										var method = e == expression ?
											MemberHelper.MethodOf<IEnumerable<bool>>(n => n.Where(a => a)).GetGenericMethodDefinition().MakeGenericMethod(table.ObjectType) :
											_whereMethodInfo.MakeGenericMethod(e.Type, table.ObjectType, ttype);

										var me = (MemberExpression)e;
										var op = Expression.Parameter(table.ObjectType, "t");

										parameters.Add(op);

										Expression ex = null;

										for (var i = 0; i < table.Association.ThisKey.Length; i++)
										{
											var field1 = table.ParentAssociation.SqlTable.Fields[table.Association.ThisKey [i]];
											var field2 = table.                  SqlTable.Fields[table.Association.OtherKey[i]];

											var ma1 = Expression.MakeMemberAccess(op,            field2.ColumnDescriptor.MemberInfo);
											var ma2 = Expression.MakeMemberAccess(me.Expression, field1.ColumnDescriptor.MemberInfo);

											var ee = Equal(mappringSchema, ma1, ma2);

											ex = ex == null ? ee : Expression.AndAlso(ex, ee);
										}

										var expr = Expression.Call(null, method, Expression.Constant(tbl), Expression.Lambda(ex, op));

										if (e == expression)
										{
											expr = Expression.Call(
												MemberHelper.MethodOf<IEnumerable<int>>(n => n.ToList()).GetGenericMethodDefinition().MakeGenericMethod(table.ObjectType),
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

		public Expression BuildMultipleQuery(IBuildContext context, Expression expression)
		{
			var parameters = new HashSet<ParameterExpression>();

			expression = GetMultipleQueryExpression(context, MappingSchema, expression, parameters);

			var paramex = Expression.Parameter(typeof(object[]), "ps");
			var parms   = new List<Expression>();

			// Convert parameters.
			//
			expression = expression.Transform(e =>
			{
				var root = e.GetRootObject();

				if (root != null &&
					root.NodeType == ExpressionType.Parameter &&
					!parameters.Contains((ParameterExpression)root))
				{
					if (_buildMultipleQueryExpressions == null)
						_buildMultipleQueryExpressions = new HashSet<Expression>();

					_buildMultipleQueryExpressions.Add(e);

					var ex = Expression.Convert(BuildExpression(context, e), typeof(object));

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
