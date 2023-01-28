using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using Common;
	using LinqToDB.Expressions;
	
	sealed class LoadWithBuilder : MethodCallBuilder
	{
		public static readonly string[] MethodNames = { "LoadWith", "ThenLoad", "LoadWithAsTable", "LoadWithInternal" };

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

		static LoadWithInfo CorrectLastPath(LoadWithInfo lastPath, MemberInfo[]? loadWithPath)
		{
			if (loadWithPath?.Length > 0)
			{
				var current = lastPath;
				foreach (var memberInfo in loadWithPath)
				{
					var found = current.NextInfos?.FirstOrDefault(li =>
						MemberInfoEqualityComparer.Default.Equals(li.MemberInfo, memberInfo));
					if (found == null)
						throw new InvalidOperationException();
					current = found;
				}

				lastPath = current;
			}

			return lastPath;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			TableBuilder.TableContext table;

			LoadWithInfo lastLoadWith;

			if (methodCall.Method.Name == "LoadWithInternal")
			{
				table = GetTableContext(sequence, methodCall.Arguments[0], out _);

				var loadWith     = methodCall.Arguments[1].EvaluateExpression<LoadWithInfo>();
				var loadWithPath = methodCall.Arguments[2].EvaluateExpression<MemberInfo[]>();

				table.LoadWith     = loadWith;
				table.LoadWithPath = loadWithPath;
				lastLoadWith       = loadWith!;
			}
			else
			{
				var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();

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

				var path  = selector.Body.Unwrap();
				table = GetTableContext(sequence, path, out var level);

				var associations = ExtractAssociations(builder, path, level)
					.Reverse()
					.ToArray();

				if (associations.Length == 0)
					throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{path}'");

				var tableLoadWith = table.EnsureLoadWith();

				if (methodCall.Method.Name == "ThenLoad")
				{
					var prevSequence = (LoadWithContext)sequence;
					if (prevSequence.LastLoadWithInfo == null)
						throw new InvalidOperationException();

					// append to the last member chain
					var lastPath = CorrectLastPath(prevSequence.LastLoadWithInfo, table.LoadWithPath);
					if (lastPath == null)
						throw new LinqToDBException($"ThenLoad function should be followed after LoadWith. Can not find previous property for '{path}'.");

					lastLoadWith = MergeLoadWith(lastPath, associations);

					if (methodCall.Arguments.Count == 3)
					{
						var lastElement = associations[associations.Length - 1];
						lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
						if (lastElement.MemberInfo != null)
							CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, builder.MappingSchema);
					}
				}
				else if (methodCall.Method.Name == "LoadWith" || methodCall.Method.Name == "LoadWithAsTable")
				{
					var lastPath = CorrectLastPath(tableLoadWith, table.LoadWithPath);

					if (tableLoadWith == null)
						throw new InvalidOperationException();

					if (methodCall.Arguments.Count == 3)
					{
						var lastElement = associations[associations.Length - 1];
						lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
						if (lastElement.MemberInfo != null)
							CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, builder.MappingSchema);
					}

					lastLoadWith = MergeLoadWith(lastPath, associations);
				}
				else
					throw new InvalidOperationException();
			}

			var loadWithSequence = sequence as LoadWithContext ?? new LoadWithContext(sequence, table);
			loadWithSequence.LastLoadWithInfo = lastLoadWith;

			return loadWithSequence;
		}

		static LoadWithInfo MergeLoadWith(LoadWithInfo loadWith, LoadWithInfo[] defined)
		{
			var current = loadWith;

			for (var index = 0; index < defined.Length; index++)
			{
				current.NextInfos ??= new List<LoadWithInfo>();

				var d = defined[index];
				var found = current.NextInfos.FirstOrDefault(lw =>
					MemberInfoEqualityComparer.Default.Equals(lw.MemberInfo, d.MemberInfo));

				if (found != null)
				{
					found.ShouldLoad = true;

					if (index == defined.Length - 1)
					{
						// reassign everything
						found.FilterFunc = d.FilterFunc;
						found.MemberFilter = d.MemberFilter;
					}

					current = found;
				}
				else
				{
					current.NextInfos.Add(d);
					current = d;
				}
			}

			return current;
		}

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

		static IEnumerable<LoadWithInfo> ExtractAssociations(ExpressionBuilder builder, Expression expression, Expression? stopExpression)
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

			foreach (var member in GetAssociations(builder, currentExpression, stopExpression))
			{
				yield return new LoadWithInfo(member, true) { MemberFilter = filterExpression };
				filterExpression = null;
			}
		}

		static IEnumerable<MemberInfo> GetAssociations(ExpressionBuilder builder, Expression expression, Expression? stopExpression)
		{
			MemberInfo? lastMember = null;

			for (;;)
			{
				if (stopExpression == expression)
				{
					yield break;
				}

				switch (expression.NodeType)
				{
					case ExpressionType.Parameter :
						if (lastMember == null)
							goto default;
						yield break;

					case ExpressionType.Call      :
						{
							var cexpr = (MethodCallExpression)expression;

							if (cexpr.Method.IsSqlPropertyMethodEx())
							{
								foreach (var assoc in GetAssociations(builder, builder.ConvertExpression(expression), stopExpression))
									yield return assoc;

								yield break;
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
							var mexpr         = (MemberExpression)expression;
							var member        = lastMember = mexpr.Member;
							var isAssociation = builder.MappingSchema.HasAttribute<AssociationAttribute>(member.ReflectedType!, member);

							if (!isAssociation)
							{
								member        = mexpr.Expression!.Type.GetMemberEx(member)!;
								isAssociation = builder.MappingSchema.HasAttribute<AssociationAttribute>(mexpr.Expression.Type, member);
							}

							if (!isAssociation)
								throw new LinqToDBException($"Member '{expression}' is not an association.");

							yield return member;

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
		}

		internal sealed class LoadWithContext : PassThroughContext
		{
			private readonly TableBuilder.TableContext _tableContext;

			public LoadWithInfo?             LastLoadWithInfo { get; set; }
			public TableBuilder.TableContext TableContext     => _tableContext;

			public LoadWithContext(IBuildContext context, TableBuilder.TableContext tableContext) : base(context)
			{
				_tableContext = tableContext;
			}
		}
	}
}
