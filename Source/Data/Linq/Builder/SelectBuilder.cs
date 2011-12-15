using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	using Reflection;

	class SelectBuilder : MethodCallBuilder
	{
		#region SelectBuilder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.IsQueryable("Select"))
			{
				switch (((LambdaExpression)methodCall.Arguments[1].Unwrap()).Parameters.Count)
				{
					case 1 :
					case 2 : return true;
					default: break;
				}
			}

			return false;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			sequence.SetAlias(selector.Parameters[0].Name);

			var body = selector.Body.Unwrap();

			switch (body.NodeType)
			{
				case ExpressionType.Parameter : break;
				default                       :
					sequence = CheckSubQueryForSelect(sequence);
					break;
			}

			var context = selector.Parameters.Count == 1 ?
				new SelectContext (buildInfo.Parent, selector, sequence) :
				new SelectContext2(buildInfo.Parent, selector, sequence);

#if DEBUG
			context.MethodCall = methodCall;
#endif

			return context;
		}

		static IBuildContext CheckSubQueryForSelect(IBuildContext context)
		{
			return context.SqlQuery.Select.IsDistinct ? new SubQueryContext(context) : context;
		}

		#endregion

		#region SelectContext2

		class SelectContext2 : SelectContext
		{
			public SelectContext2(IBuildContext parent, LambdaExpression lambda, IBuildContext sequence)
				: base(parent, lambda, sequence)
			{
			}

			static readonly ParameterExpression _counterParam = Expression.Parameter(typeof(int), "counter");

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0);

			if (expr.Type != typeof(T))
				expr = Expression.Convert(expr, typeof(T));

				var mapper = Expression.Lambda<Func<int,QueryContext,IDataContext,IDataReader,Expression,object[],T>>(
					Builder.BuildBlock(expr), new []
					{
						_counterParam,
						ExpressionBuilder.ContextParam,
						ExpressionBuilder.DataContextParam,
						ExpressionBuilder.DataReaderParam,
						ExpressionBuilder.ExpressionParam,
						ExpressionBuilder.ParametersParam,
					});

				var func    = mapper.Compile();
				var counter = 0;

				Func<QueryContext,IDataContext,IDataReader,Expression,object[],T> map = (ctx,db,rd,e,ps) => func(counter++, ctx, db, rd, e, ps);

				query.SetQuery(map);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				switch (requestFlag)
				{
					case RequestFor.Expression :
					case RequestFor.Root       :
						if (expression == Lambda.Parameters[1])
							return IsExpressionResult.True;
						break;
				}

				return base.IsExpression(expression, level, requestFlag);
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (expression == Lambda.Parameters[1])
					return _counterParam;

				return base.BuildExpression(expression, level);
			}
		}

		#endregion

		#region Convert

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression originalMethodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			var methodCall = originalMethodCall;
			var selector   = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var info       = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), selector.Parameters[0]);

			if (info != null)
			{
				methodCall = (MethodCallExpression)methodCall.Transform(
					ex => ConvertMethod(methodCall, 0, info, selector.Parameters[0], ex));
				selector   = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			}

			if (param != builder.SequenceParameter)
			{
				var list =
					(
						from path in GetExpressions(selector.Parameters[0], param, 0, selector.Body.Unwrap())
						orderby path.Level descending
						select path
					).ToList();

				if (list.Count > 0)
				{
					var plist = list.Where(e => e.Expr == selector.Parameters[0]).ToList();

					if (plist.Count > 1)
						list = list.Except(plist.Skip(1)).ToList();

					var p = plist.FirstOrDefault();

					if (p == null)
					{
						var types  = methodCall.Method.GetGenericArguments();
						var mgen   = methodCall.Method.GetGenericMethodDefinition();
						var btype  = typeof(ExpressionHoder<,>).MakeGenericType(types[0], selector.Body.Type);
						var fields = btype.GetFields();
						var pold   = selector.Parameters[0];
						var psel   = Expression.Parameter(types[0], selector.Parameters[0].Name);

						methodCall = Expression.Call(
							methodCall.Object,
							mgen.MakeGenericMethod(types[0], btype),
							methodCall.Arguments[0],
							Expression.Lambda(
								Expression.MemberInit(
									Expression.New(btype),
									Expression.Bind(fields[0], psel),
									Expression.Bind(fields[1], selector.Body)),
								psel));

						selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
						param    = Expression.Parameter(selector.Body.Type, param.Name);

						list.Add(new SequenceConvertPath { Path = param, Expr = Expression.MakeMemberAccess(param, fields[1]), Level = 1 });

						var expr = Expression.MakeMemberAccess(param, fields[0]);

						foreach (var t in list)
							t.Expr = t.Expr.Transform(ex => ex == pold ? expr : ex);

						return new SequenceConvertInfo
						{
							Parameter            = param,
							Expression           = methodCall,
							ExpressionsToReplace = list
						};
					}

					if (info != null)
					{
						if (info.ExpressionsToReplace != null)
						{
							foreach (var path in info.ExpressionsToReplace)
							{
								path.Path = path.Path.Transform(e => e == info.Parameter ? p.Path : e);
								path.Expr = path.Expr.Transform(e => e == info.Parameter ? p.Path : e);
								path.Level += p.Level;

								list.Add(path);
							}

							list = list.OrderByDescending(path => path.Level).ToList();
						}
					}

					if (list.Count > 1)
					{
						return new SequenceConvertInfo
						{
							Parameter            = param,
							Expression           = methodCall,
							ExpressionsToReplace = list
								.Where (e => e != p)
								.Select(ei =>
								{
									ei.Expr = ei.Expr.Transform(e => e == p.Expr ? p.Path : e);
									return ei;
								})
								.ToList()
						};
					}
				}
			}

			if (methodCall != originalMethodCall)
				return new SequenceConvertInfo
				{
					Parameter  = param,
					Expression = methodCall,
				};

			return null;
		}

		static IEnumerable<SequenceConvertPath> GetExpressions(ParameterExpression param, Expression path, int level, Expression expression)
		{
			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

						if (expr.Members != null) for (var i = 0; i < expr.Members.Count; i++)
						{
							var q = GetExpressions(param, Expression.MakeMemberAccess(path, expr.Members[i]), level + 1, expr.Arguments[i]);
							foreach (var e in q)
								yield return e;
						}

						break;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo.Name, _ => _.i);

						foreach (var binding in expr.Bindings.Cast<MemberAssignment>().OrderBy(b => dic[b.Member.Name]))
						{
							var q = GetExpressions(param, Expression.MakeMemberAccess(path, binding.Member), level + 1, binding.Expression);
							foreach (var e in q)
								yield return e;
						}

						break;
					}

				// parameter
				//
				case ExpressionType.Parameter  :
					if (expression == param)
						yield return new SequenceConvertPath { Path = path, Expr = expression, Level = level };
					break;

				case ExpressionType.TypeAs     :
					yield return new SequenceConvertPath { Path = path, Expr = expression, Level = level };
					break;

				// Queriable method.
				//
				case ExpressionType.Call       :
					{
						var call = (MethodCallExpression)expression;

						if (call.IsQueryable())
							if (typeof(IEnumerable).IsSameOrParentOf(call.Type) ||
							    typeof(IQueryable). IsSameOrParentOf(call.Type))
								yield return new SequenceConvertPath { Path = path, Expr = expression, Level = level };

						break;
					}
			}
		}

		#endregion
	}
}
