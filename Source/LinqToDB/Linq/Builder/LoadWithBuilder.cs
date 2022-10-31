using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Common;

	class LoadWithBuilder : MethodCallBuilder
	{
		public static readonly string[] MethodNames = { "LoadWith", "ThenLoad", "LoadWithAsTable" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		static void CheckFilterFunc(Type expectedType, Type filterType, MappingSchema mappingSchema)
		{
			var propType = expectedType;
			if (EagerLoading.IsEnumerableType(expectedType, mappingSchema))
				propType = EagerLoading.GetEnumerableElementType(expectedType, mappingSchema);
			var itemType = typeof(Expression<>).IsSameOrParentOf(filterType) ?
				filterType.GetGenericArguments()[0].GetGenericArguments()[0].GetGenericArguments()[0] :
				filterType.GetGenericArguments()[0].GetGenericArguments()[0];
			if (propType != itemType)
				throw new LinqException("Invalid filter function usage.");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var selector = methodCall.Arguments[1].UnwrapLambda();

			// reset LoadWith sequence
			if (methodCall.IsQueryable("LoadWith"))
			{
				for(;;)
				{
					if (sequence is LoadWithContext lw)
						sequence = lw.Context;
					else
						break;
				}
			}

			var path = SequenceHelper.PrepareBody(selector, sequence);

			var extractResult = ExtractAssociations(builder, path, null);

			if (extractResult == null)
				throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{selector}'");

			var associations = extractResult.Value.info.Length <= 1
				? extractResult.Value.info
				: extractResult.Value.info
					.Reverse()
					.ToArray();

			if (associations.Length == 0)
				throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{path}'");


			if (methodCall.Arguments.Count == 3)
			{
				var lastElement = associations[associations.Length - 1];
				lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
				CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, builder.MappingSchema);
			}

			var registerContext = extractResult.Value.context;

			if (registerContext is LoadWithContext lwContext)
				registerContext = lwContext.RegisterContext;

			builder.RegisterLoadWith(registerContext, associations, methodCall.Method.Name == "ThenLoad");

			var loadWithSequence = sequence as LoadWithContext ?? new LoadWithContext(sequence, registerContext);

			return loadWithSequence;
		}

		/*
		TableBuilder.TableContext GetTableContext(IBuildContext ctx, Expression path, out Expression? stopExpression)
		{
			stopExpression = null;

			var table = ctx as TableBuilder.TableContext;

			if (table != null)
				return table;

			if (ctx is LoadWithContext lwCtx)
				return lwCtx.TableContext;

			var isTableResult = ctx.IsExpression(null, 0, RequestFor.Table);
			if (isTableResult.Result)
			{
				table = isTableResult.Context as TableBuilder.TableContext;
				if (table != null)
					return table;
			}

			var maxLevel = path.GetLevel(ctx.Builder.MappingSchema);
			var level    = 1;
			while (level <= maxLevel)
			{
				var levelExpression = path.GetLevelExpression(ctx.Builder.MappingSchema, level);
				isTableResult       = ctx.IsExpression(levelExpression, 1, RequestFor.Table);
				if (isTableResult.Result)
				{
					table = isTableResult.Context switch
					{
						TableBuilder.TableContext t => t,
						AssociationContext a => a.TableContext as TableBuilder.TableContext,
						_ => null
					};

					if (table != null)
					{
						stopExpression = levelExpression;
						return table;
					}
				}

				++level;
			}

			var expr = path.GetLevelExpression(ctx.Builder.MappingSchema, 0);

			throw new LinqToDBException(
				$"Unable to find table information for LoadWith. Consider moving LoadWith closer to GetTable<{expr.Type.Name}>() method.");
		}
		*/

		static (IBuildContext context, LoadWithInfo[] info)? ExtractAssociations(ExpressionBuilder builder, Expression expression, Expression? stopExpression)
		{
			var currentExpression = expression;

			while (currentExpression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)currentExpression;
				if (mc.IsQueryable())
					currentExpression = mc.Arguments[0];
				else
					break;
			}

			LambdaExpression? filterExpression = null;
			if (currentExpression != expression)
			{
				var parameter  = Expression.Parameter(currentExpression.Type, "e");

				var body   = expression.Replace(currentExpression, parameter);
				var lambda = Expression.Lambda(body, parameter);

				filterExpression = lambda;
			}

			var extractResult = GetAssociations(builder, currentExpression, stopExpression);
			if (extractResult.context == null)
				return default;

			var loadWithInfos = extractResult.members
				.Select((m, i) => new LoadWithInfo(m) { MemberFilter = i == 0 ? filterExpression : null })
				.ToArray();

			return (extractResult.context, loadWithInfos);
		}

		static (IBuildContext? context, List<MemberInfo> members) GetAssociations(ExpressionBuilder builder, Expression expression, Expression? stopExpression)
		{
			IBuildContext? context    = null;
			MemberInfo?    lastMember = null;

			var members = new List<MemberInfo>();
			var stop    = false;

			for (;;)
			{
				if (stopExpression == expression || stop)
				{
					break;
				}

				switch (expression.NodeType)
				{
					case ExpressionType.Parameter :
						{
							if (lastMember == null)
								goto default;
							stop = true;
							break;
						}

					case ExpressionType.Call      :
						{
							var cexpr = (MethodCallExpression)expression;

							if (cexpr.Method.IsSqlPropertyMethodEx())
							{
								throw new NotImplementedException();
								
								/*foreach (var assoc in GetAssociations(builder, builder.ConvertExpression(expression), stopExpression))
									yield return assoc;

								yield break;*/
							}

							if (lastMember == null)
								goto default;

							var expr  = cexpr.Object;

							if (expr == null)
							{
								if (cexpr.Arguments.Count == 0)
									goto default;

								expr = cexpr.Arguments[0];
							}

							if (expr.NodeType != ExpressionType.MemberAccess)
								goto default;

							var member = ((MemberExpression)expr).Member;
							var mtype  = member.GetMemberType();

							if (lastMember.ReflectedType != mtype.GetItemType())
								goto default;

							expression = expr;

							break;
						}

					case ExpressionType.MemberAccess :
						{
							var mexpr  = (MemberExpression)expression;
							var member = lastMember = mexpr.Member;
							var attr   = builder.MappingSchema.GetAttribute<AssociationAttribute>(member.ReflectedType!, member);
							if (attr == null)
							{
								member = mexpr.Expression!.Type.GetMemberEx(member)!;
								attr = builder.MappingSchema.GetAttribute<AssociationAttribute>(mexpr.Expression.Type, member);
							}

							if (attr == null)
							{
								var projected = builder.MakeExpression(context, expression, ProjectFlags.Expand);
								if (projected == expression)
									throw new LinqToDBException($"Member '{expression}' is not an association.");
								expression = projected;
								break;
							}

							members.Add(member);

							expression = mexpr.Expression!;

							break;
						}

					case ExpressionType.ArrayIndex   :
						{
							expression = ((BinaryExpression)expression).Left;
							break;
						}

					case ExpressionType.Extension    :
						{
							if (expression is GetItemExpression getItemExpression)
							{
								expression = getItemExpression.Expression;
								break;
							}

							if (expression is ContextRefExpression contextRef)
							{
								var newExpression = builder.MakeExpression(context, expression, ProjectFlags.AssociationRoot);
								if (!ReferenceEquals(newExpression, expression))
								{
									expression = newExpression;
								}
								else
								{
									stop    = true;
									context = contextRef.BuildContext;
								}

								break;
							}

							goto default;
					}

					case ExpressionType.Convert      :
						{
							expression = ((UnaryExpression)expression).Operand;
							break;
						}

					default :
						{
							throw new LinqToDBException($"Expression '{expression}' is not an association.");
						}
				}
			}

			return (context, members);
		}

		internal class LoadWithContext : PassThroughContext
		{
			public IBuildContext RegisterContext { get; }

			public LoadWithContext(IBuildContext context, IBuildContext registerContext) : base(context)
			{
				RegisterContext = registerContext;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if(SequenceHelper.IsSameContext(path, this))
				{
					if (flags.HasFlag(ProjectFlags.Root))
						return path;

					if (flags.HasFlag(ProjectFlags.AssociationRoot))
						return new ContextRefExpression(path.Type, RegisterContext);
				}
				return base.MakeExpression(path, flags);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new LoadWithContext(context.CloneContext(Context), context.CloneContext(RegisterContext));
			}

		}
	}
}
