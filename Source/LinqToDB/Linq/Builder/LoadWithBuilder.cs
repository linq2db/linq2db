using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Reflection;
	using Mapping;

	[BuildsMethodCall("LoadWith", "ThenLoad", "LoadWithAsTable", "LoadWithInternal")]
	sealed class LoadWithBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

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

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult.BuildContext == null)
				return buildResult;
			var sequence = buildResult.BuildContext;

			ILoadWithContext? table = null;

			LoadWithInfo lastLoadWith;

			if (methodCall.Method.Name == "LoadWithInternal")
			{
				table = SequenceHelper.GetTableOrCteContext(sequence);

				if (table == null)
					return BuildSequenceResult.Error(methodCall);

				var loadWith     = methodCall.Arguments[1].EvaluateExpression<LoadWithInfo>();
				var loadWithPath = methodCall.Arguments[2].EvaluateExpression<MemberInfo[]>();

				table.LoadWithRoot = loadWith!;
				table.LoadWithPath = loadWithPath;
				lastLoadWith       = loadWith!;
			}
			else
			{
				var selector = methodCall.Arguments[1].UnwrapLambda();

				// reset LoadWith sequence
				if (methodCall.IsQueryable("LoadWith"))
				{
					while (sequence is LoadWithContext lw)
						sequence = lw.Context;
				}
				else
				{
					if (sequence is LoadWithContext lw)
						table = lw.RegisterContext as ILoadWithContext;
				}

				var path = SequenceHelper.PrepareBody(selector, sequence);

				var extractResult = ExtractAssociations(builder, table, path, null);

				if (extractResult == null)
					throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{selector}'");

				var associations = extractResult.Value.info.Length <= 1
					? extractResult.Value.info
					: extractResult.Value.info
						.Reverse()
						.ToArray();

				if (associations.Length == 0)
					throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{path}'");

				table = extractResult.Value.context ?? throw new LinqToDBException("Unable to find table for LoadWith association.");

				var tableLoadWith = table.LoadWithRoot;

				if (methodCall.Method.Name == "ThenLoad")
				{
					var prevSequence = (LoadWithContext)sequence;
					if (prevSequence.LastLoadWithInfo == null)
						throw new InvalidOperationException();

					// append to the last member chain
					var lastPath = CorrectLastPath(prevSequence.LastLoadWithInfo, table.LoadWithPath);
					if (lastPath == null)
						throw new LinqToDBException($"ThenLoad function should be followed after LoadWith. Cannot find previous property for '{path}'.");

					lastLoadWith = MergeLoadWith(lastPath, associations);

					if (methodCall.Arguments.Count == 3)
					{
						var lastElement = associations[associations.Length - 1];
						lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
						if (lastElement.MemberInfo != null)
							CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, sequence.MappingSchema);
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
							CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, sequence.MappingSchema);
					}

					lastLoadWith = MergeLoadWith(lastPath, associations);
				}
				else
					throw new InvalidOperationException();
			}

			var loadWithSequence = sequence as LoadWithContext ?? new LoadWithContext(sequence, table);
			loadWithSequence.LastLoadWithInfo = lastLoadWith;

			return BuildSequenceResult.FromContext(loadWithSequence);
		}

		static (ILoadWithContext? context, LoadWithInfo[] info)? ExtractAssociations(ExpressionBuilder builder, ILoadWithContext? parentContext, Expression expression, Expression? stopExpression)
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

			var (context, members) = GetAssociations(builder, parentContext, currentExpression, stopExpression);
			if (context == null)
				return default;

			var loadWithInfos = members
				.Select((m, i) => new LoadWithInfo(m, true) { MemberFilter = i == 0 ? filterExpression : null })
				.ToArray();

			return (context, loadWithInfos);
		}

		static (ILoadWithContext? context, List<MemberInfo> members) GetAssociations(ExpressionBuilder builder, ILoadWithContext? parentContext, Expression expression, Expression? stopExpression)
		{
			ILoadWithContext? context    = parentContext;
			MemberInfo?       lastMember = null;

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
							var memberInfo   = MemberHelper.GetMemberInfo(cexpr);
							var memberAccess = Expression.MakeMemberAccess(cexpr.Arguments[0], memberInfo);
							expression = memberAccess;

							continue;
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
						expression = builder.BuildTraverseExpression(expression).UnwrapConvert();

						if (expression.NodeType != ExpressionType.MemberAccess)
							break;

						var mexpr         = (MemberExpression)expression;
						var member        = lastMember = mexpr.Member;
						var isAssociation = builder.IsAssociation(expression, out _);

						if (!isAssociation)
						{
							var projected = builder.BuildTraverseExpression(expression);
							if (ExpressionEqualityComparer.Instance.Equals(projected, expression))
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
							var newExpression = builder.BuildTableExpression(expression);
							if (!ReferenceEquals(newExpression, expression))
							{
								expression = newExpression;
							}
							else
							{
								stop    = true;
								context = contextRef.BuildContext as ILoadWithContext;
							}

							break;
						}

						goto default;
					}

					case ExpressionType.Convert       :
					case ExpressionType.ConvertChecked:
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

			return (context ?? parentContext, members);
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
						found.FilterFunc   = d.FilterFunc;
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

		internal sealed class LoadWithContext : PassThroughContext
		{
			public IBuildContext RegisterContext { get; }
			public LoadWithInfo? LastLoadWithInfo { get; set; }

			public LoadWithContext(IBuildContext context, IBuildContext registerContext) : base(context)
			{
				RegisterContext = registerContext;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					if (flags.IsRoot())
						return path;

					if (flags.IsAssociationRoot())
						return new ContextRefExpression(path.Type, RegisterContext);
				}
				return base.MakeExpression(path, flags);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new LoadWithContext(context.CloneContext(Context), context.CloneContext(RegisterContext));
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return RegisterContext.GetContext(expression, buildInfo);
			}
		}
	}
}
