using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		readonly HashSet<Expression> _skippedExpressions = new HashSet<Expression>();

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
					case ExpressionType.MemberAccess:
						{
							if (IsServerSideOnly(expr) || PreferServerSide(expr))
								return new TransformInfo(BuildSql(context, expr));

							var ma = (MemberExpression)expr;

							if (Expressions.ConvertMember(MappingSchema.NewSchema, ma.Member) != null)
								break;

							var ctx = GetContext(context, expr);

							if (ctx != null)
								return new TransformInfo(ctx.BuildExpression(expr, 0));

							var ex = ma.Expression;

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

						if (expr.Type == typeof(string) && MappingSchema.NewSchema.GetDefaultValue(typeof(string)) != null)
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

								break;
							}

							if (IsSubQuery(context, ce))
							{
									if (typeof(IEnumerable).IsSameOrParentOf(expr.Type) && expr.Type != typeof(string) && !expr.Type.IsArray)
									return new TransformInfo(BuildMultipleQuery(context, expr));

								return new TransformInfo(GetSubQuery(context, ce).BuildExpression(null, 0));
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

		static bool EnforceServerSide(IBuildContext context)
		{
			return context.SqlQuery.Select.IsDistinct;
		}

		#endregion

		#region BuildSql

		Expression BuildSql(IBuildContext context, Expression expression)
		{
			var sqlex = ConvertToSqlExpression(context, expression);
			var idx   = context.SqlQuery.Select.Add(sqlex);

			idx = context.ConvertToParentIndex(idx, context);

			var field = BuildSql(expression.Type, idx);

			return field;
		}

		public Expression BuildSql(Type type, int idx)
		{
			return new ConvertFromDataReaderExpression(type, idx, DataReaderLocal, DataContextInfo);
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
							select c.ObjectMapper
						).FirstOrDefault();

						return om != null && om.Associations.All(a => !a.MemberInfo.EqualsTo(me.Member)) && om[me.Member.Name, true] == null;
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
						var l  = Expressions.ConvertMember(MappingSchema.NewSchema, pi.Member);

						if (l != null)
						{
							var info = l.Body.Unwrap();

							if (l.Parameters.Count == 1 && pi.Expression != null)
								info = info.Transform(wpi => wpi == l.Parameters[0] ? pi.Expression : wpi);

							return info.Find(PreferServerSide) != null;
						}

						var attr = GetFunctionAttribute(pi.Member);
						return attr != null && attr.PreferServerSide && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var e  = pi;
						var l  = Expressions.ConvertMember(MappingSchema.NewSchema, e.Method);

						if (l != null)
							return l.Body.Unwrap().Find(PreferServerSide) != null;

						var attr = GetFunctionAttribute(e.Method);
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

//			BlockVariables.  Clear();
//			BlockExpressions.Clear();
			while (BlockVariables.  Count > 1) BlockVariables.  RemoveAt(BlockVariables.  Count - 1);
			while (BlockExpressions.Count > 1) BlockExpressions.RemoveAt(BlockExpressions.Count - 1);

			return expression;
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
					ParametersParam,
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

		public Expression BuildMultipleQuery(IBuildContext context, Expression expression)
		{
			if (!Common.Configuration.Linq.AllowMultipleQuery)
				throw new LinqException("Multiple queries are not allowed. Set the 'LinqToDB.Common.Configuration.Linq.AllowMultipleQuery' flag to 'true' to allow multiple queries.");

			var parameters = new HashSet<ParameterExpression>();

			expression.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foreach (var p in ((LambdaExpression)e).Parameters)
						parameters.Add(p);
			});

			// Convert associations.
			//
			expression = expression.Transform(e =>
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
										var method = typeof(LinqExtensions)
											.GetMethod("Where", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
											.MakeGenericMethod(e.Type, table.ObjectType, ttype);

										var me = (MemberExpression)e;
										var op = Expression.Parameter(table.ObjectType, "t");

										parameters.Add(op);

										Expression ex = null;

										for (var i = 0; i < table.Association.ThisKey.Length; i++)
										{
											var field1 = table.ParentAssociation.SqlTable.Fields[table.Association.ThisKey [i]];
											var field2 = table.                  SqlTable.Fields[table.Association.OtherKey[i]];

											var ee = Expression.Equal(
												Expression.MakeMemberAccess(op,            field2.MemberMapper.MemberAccessor.MemberInfo),
												Expression.MakeMemberAccess(me.Expression, field1.MemberMapper.MemberAccessor.MemberInfo));

											ex = ex == null ? ee : Expression.AndAlso(ex, ee);
										}

										return Expression.Call(null, method, Expression.Constant(tbl), Expression.Lambda(ex, op));
									}
								}
							}

							break;
						}
				}

				return e;
			});

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
					var ex = Expression.Convert(BuildExpression(context, e), typeof(object));

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
