using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{

	using Common;
	using Common.Internal;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using System.Diagnostics.CodeAnalysis;

	internal class EagerLoading
	{
		static readonly MethodInfo EnlistEagerLoadingFunctionalityMethodInfo = MemberHelper.MethodOfGeneric(() =>
			EnlistEagerLoadingFunctionality<int, int, int>(null!, null!, null!, null!, null!));

		static readonly MethodInfo EnlistEagerLoadingFunctionalityDetachedMethodInfo = MemberHelper.MethodOfGeneric(() =>
			EnlistEagerLoadingFunctionalityDetached<int>(null!, null!));

		class EagerLoadingContext<T, TKey>
			where TKey : notnull
		{
			private Dictionary<TKey, List<T>>? _items;
			private TKey                       _prevKey = default!;
			private List<T>?                   _prevList;

			public void Add(TKey key, T item)
			{
				List<T>? list;

				if (_prevList != null && _prevKey!.Equals(key))
				{
					list = _prevList;
				}
				else
				{
					if (_items == null)
					{
						_items = new Dictionary<TKey, List<T>>();
						list = new List<T>();
						_items.Add(key, list);
					}
					else if (!_items.TryGetValue(key, out list))
					{
						list = new List<T>();
						_items.Add(key, list);
					}

					_prevKey  = key;
					_prevList = list;
				}

				list.Add(item);
			}

			public List<T> GetList(TKey key)
			{
				if (_items == null || !_items.TryGetValue(key, out var list))
					return new List<T>();
				return list;
			}
		}

		private static Expression GenerateKeyExpression(Expression[] members, int startIndex)
		{
			var count = members.Length - startIndex;
			if (count == 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (count == 1)
				return members[startIndex];

			Expression[] arguments;

			if (count > MutableTuple.MaxMemberCount)
			{
				count = MutableTuple.MaxMemberCount;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var type         = MutableTuple.MTypes[count - 1];
			var concreteType = type.MakeGenericType(arguments.Select(a => a.Type).ToArray());
			var constructor  = concreteType.GetConstructor(Array<Type>.Empty) ??
							   throw new LinqToDBException($"Can not retrieve default constructor for '{type.Name}'");

			var newExpression  = Expression.New(constructor);
			var initExpression = Expression.MemberInit(newExpression,
				arguments.Select((a, i) => Expression.Bind(concreteType.GetProperty("Item" + (i + 1)), a)));
			return initExpression;
		}

		static bool IsDetailType(Type type)
		{
			var isEnumerable = type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type);

			if (!isEnumerable && type.IsClass && type.IsGenericType && type.Name.StartsWith("<>"))
			{
				isEnumerable = type.GenericTypeArguments.Any(t => IsDetailType(t));
			}

			return isEnumerable;
		}

		public static bool IsDetailsMember(IBuildContext context, Expression expression)
		{
			if (IsDetailType(expression.Type))
			{
				var buildInfo = new BuildInfo(context, expression, new SelectQuery());
				if (context.Builder.IsSequence(buildInfo))
					return true;
			}

			return false;
		}

		public static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!IsEnumerableType(type, mappingSchema))
				return type;
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}

		public static bool IsEnumerableType(Type type, MappingSchema mappingSchema)
		{
			if (mappingSchema.IsScalarType(type))
				return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
		}

		static Expression? ConstructMemberPath(IEnumerable<AccessorMember> memberPath, Expression ob, bool throwOnError)
		{
			Expression result = ob;
			foreach (var memberInfo in memberPath)
			{
				if (result.Type != memberInfo.MemberInfo.DeclaringType)
				{
					if (throwOnError)
						throw new LinqToDBException($"Type {result.Type.Name} does not have member {memberInfo.MemberInfo.Name}.");
					return null;
				}
				if (memberInfo.MemberInfo.IsMethodEx())
				{
					var methodInfo = (MethodInfo)memberInfo.MemberInfo;
					if (methodInfo.IsStatic)
						result = Expression.Call(methodInfo, memberInfo.Arguments.ToArray());
					else
						result = Expression.Call(result, methodInfo, memberInfo.Arguments.ToArray());
				}
				else
					result = Expression.MakeMemberAccess(result, memberInfo.MemberInfo);
			}

			if (result == ob)
				return null;

			return result;
		}

		static Expression? ConstructMemberPath(IEnumerable<MemberInfo> memberPath, Expression ob, bool throwOnError)
		{
			Expression result = ob;
			foreach (var memberInfo in memberPath)
			{
				if (!memberInfo.DeclaringType!.IsSameOrParentOf(result.Type))
				{
					if (throwOnError)
						throw new LinqToDBException($"Type {result.Type.Name} does not have member {memberInfo.Name}.");
					return null;
				}
				result = Expression.MakeMemberAccess(result, memberInfo);
			}

			if (result == ob)
				return null;

			return result;
		}

		static IEnumerable<Expression> ExtractArguments(Expression expression)
		{
			if (expression is MemberInitExpression mi)
			{
				foreach (var binding in mi.Bindings)
				{
					var assignment = (MemberAssignment)binding;
					foreach (var subArgument in ExtractArguments(assignment.Expression))
					{
						yield return subArgument;
					}
				}
			}
			else
			{
				yield return expression;
			}
		}

		static IEnumerable<Tuple<Expression, Expression>> ExtractTupleValues(Expression expression, Expression obj)
		{
			if (expression is MemberInitExpression mi)
			{
				foreach (var binding in mi.Bindings)
				{
					var assignment = (MemberAssignment)binding;
					var memberAccess = Expression.MakeMemberAccess(obj, assignment.Member);
					foreach (var subExpr in ExtractTupleValues(assignment.Expression, memberAccess))
					{
						yield return subExpr;
					}
				}
			}
			else
			{
				if (obj.NodeType == ExpressionType.MemberAccess)
				{
					yield return Tuple.Create(expression, obj);
				}
			}
		}

		static bool IsQueryableMethod(Expression expression, string methodName, [NotNullWhen(true)] out MethodCallExpression? queryableMethod)
		{
			expression = expression.Unwrap();
			if (expression.NodeType == ExpressionType.Call)
			{
				queryableMethod = (MethodCallExpression)expression;
				return queryableMethod.IsQueryable(methodName);
			}

			queryableMethod = null;
			return false;
		}

		public static Expression EnsureEnumerable(Expression expression, MappingSchema mappingSchema)
		{
			var enumerable = typeof(IEnumerable<>).MakeGenericType(GetEnumerableElementType(expression.Type, mappingSchema));
			if (expression.Type != enumerable)
				expression = Expression.Convert(expression, enumerable);
			return expression;
		}

		static Expression ValidateMainQuery(Expression mainQuery)
		{
			if (typeof(IQueryable<>).IsSameOrParentOf(mainQuery.Type))
				return mainQuery;

			if (mainQuery.NodeType != ExpressionType.Call)
				throw new LinqException($"Expected Method call but found '{mainQuery.NodeType}'");
			var mc = (MethodCallExpression) mainQuery;
			if (!mc.IsQueryable() || !FirstSingleMethods.Contains(mc.Method.Name))
				throw new LinqException($"Unsupported Method call '{mc.Method.Name}'");

			var newExpr = TypeHelper.MakeMethodCall(Methods.Queryable.Take, mc.Arguments[0], Expression.Constant(1));
			return newExpr;
		}

		private static readonly HashSet<string> FirstSingleMethods        = new () { "First", "FirstOrDefault", "Single", "SingleOrDefault" };
		private static readonly HashSet<string> NotSupportedDetailMethods = new ()
		{
			"Any",
			"Sum",
			"Min",
			"Max",
			"Count",
			"Average",
			"Distinct",
			"Skip",
			"Take",
			"First",
			"FirstOrDefault",
			"Last",
			"LastOrDefault",
			"Single",
			"SingleOrDefault",
			"ToArray",
			"ToList",
			"ToDictionary",
			"AsEnumerable",
			"GroupBy"
		};

		static bool IsChainContainsNotSupported(Expression expression)
		{
			var current = expression;
			while (current?.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)current;
				if (!mc.IsQueryable())
					return false;
				if (NotSupportedDetailMethods.Contains(mc.Method.Name))
					return true;
				current = mc.Arguments[0];
			}

			return false;
		}

		class ExtractNotSupportedPartContext
		{
			public ExtractNotSupportedPartContext(MappingSchema mappingSchema)
			{
				MappingSchema = mappingSchema;
			}

			public readonly MappingSchema MappingSchema;

			public Expression?          NewQueryable;
			public ParameterExpression? NewParam;
		}

		static void ExtractNotSupportedPart(
			MappingSchema mappingSchema,
			Expression detailExpression,
			Type desiredType,
			out Expression queryableExpression,
			out Expression finalExpression,
			out ParameterExpression? replaceParam)
		{
			queryableExpression = detailExpression;
			finalExpression = null!;
			replaceParam = null;

			var ctx = new ExtractNotSupportedPartContext(mappingSchema);

			finalExpression = detailExpression.Transform(ctx, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Call:
					{
						if (context.NewQueryable != null)
							return new TransformInfo(e, true); ;

						var mc = (MethodCallExpression)e;
						if (mc.IsQueryable(LoadWithBuilder.MethodNames))
							return new TransformInfo(mc, true);

						if (mc.IsQueryable(true))
						{
							var isSupported = !NotSupportedDetailMethods.Contains(mc.Method.Name);
							var isChainContainsNotSupported = IsChainContainsNotSupported(mc.Arguments[0]);
							if (isSupported && !isChainContainsNotSupported)
							{
								context.NewQueryable = mc;
								var subElementType   = GetEnumerableElementType(context.NewQueryable.Type, context.MappingSchema);
								context.NewParam     = Expression.Parameter(typeof(List<>).MakeGenericType(subElementType), "replacement");
								var replaceExpr      = (Expression)context.NewParam;
								if (mc.IsQueryable(false))
								{
									replaceExpr = Expression.Call(
										Methods.Enumerable.AsQueryable.MakeGenericMethod(subElementType), replaceExpr);
								}
								return new TransformInfo(replaceExpr, true);
							}

							if (!isSupported && !isChainContainsNotSupported)
							{
								context.NewQueryable = mc.Arguments[0];
								var subElementType   = GetEnumerableElementType(context.NewQueryable.Type, context.MappingSchema);
								context.NewParam     = Expression.Parameter(typeof(List<>).MakeGenericType(subElementType), "replacement");
								var replaceExpr      = (Expression)context.NewParam;
								if (typeof(IQueryable<>).IsSameOrParentOf(mc.Method.GetParameters()[0].ParameterType))
								{
									replaceExpr = Expression.Call(
										Methods.Enumerable.AsQueryable.MakeGenericMethod(subElementType), replaceExpr);
								}
								var newMethod       = mc.Update(mc.Object, new[]{replaceExpr}.Concat(mc.Arguments.Skip(1)));
								return new TransformInfo(newMethod, true);
							}
						}

						break;
					}
				}

				return new TransformInfo(e);
			});

			if (ctx.NewQueryable != null)
			{
				queryableExpression = ctx.NewQueryable;
				replaceParam        = ctx.NewParam;

				// remove not needed AsQueryable() call
				//
				while (finalExpression is MethodCallExpression mc && mc.IsQueryable("AsQueryable"))
				{
					finalExpression = mc.Arguments[0];
				}

				if (!IsEnumerableType(desiredType, mappingSchema))
				{
					if (desiredType != finalExpression.Type)
					{
						finalExpression =
							Expression.Call(Methods.Enumerable.FirstOrDefault.MakeGenericMethod(desiredType),
								finalExpression);
					}

					return;
				}
			}

			var elementType = GetEnumerableElementType(desiredType, mappingSchema);
			if (replaceParam == null)
			{
				replaceParam    = Expression.Parameter(typeof(List<>).MakeGenericType(elementType), "replacement");
				finalExpression = replaceParam;
			}

			finalExpression = AdjustType(finalExpression, desiredType, mappingSchema);

		}

		public static Expression AdjustType(Expression expression, Type desiredType, MappingSchema mappingSchema)
		{
			if (desiredType.IsSameOrParentOf(expression.Type))
				return expression;

			var elementType = GetEnumerableElementType(desiredType, mappingSchema);

			var result = (Expression?)null;

			if (desiredType.IsArray)
			{
				var method = typeof(IQueryable<>).IsSameOrParentOf(expression.Type)
					? Methods.Queryable.ToArray
					: Methods.Enumerable.ToArray;

				result = Expression.Call(method.MakeGenericMethod(elementType),
					expression);
			}
			else if (typeof(IOrderedEnumerable<>).IsSameOrParentOf(desiredType))
			{
				result = expression;
			}
			else if (!typeof(IQueryable<>).IsSameOrParentOf(desiredType) && !desiredType.IsArray)
			{
				var convertExpr = mappingSchema.GetConvertExpression(
					typeof(IEnumerable<>).MakeGenericType(elementType), desiredType);
				if (convertExpr != null)
					result = Expression.Invoke(convertExpr, expression);
			}

			if (result == null)
			{
				result = expression;
				if (!typeof(IQueryable<>).IsSameOrParentOf(result.Type))
				{
					result = Expression.Call(Methods.Enumerable.AsQueryable.MakeGenericMethod(elementType),
						expression);
				}

				if (typeof(ITable<>).IsSameOrParentOf(desiredType))
				{
					var tableType   = typeof(PersistentTable<>).MakeGenericType(elementType);
					result = Expression.New(tableType.GetConstructor(new[] { result.Type }),
						result);
				}
			}

			return result;
		}

		class KeyInfo
		{
			public Expression Original       = null!;
			public Expression ForSelect      = null!;
			public Expression ForCompilation = null!;
		}

		static IEnumerable<KeyInfo> ExtractKeys(IBuildContext context, Expression expr)
		{
			foreach (var arg in ExtractArguments(expr))
			{
				var ctx = context.Builder.GetContext(context, arg);
				if (ctx == null)
					continue;

				foreach (var keyInfo in ConvertToKeyInfos(ctx, arg, arg))
					yield return keyInfo;
			}
		}

		static IEnumerable<KeyInfo> ExtractKeysFromContext(IBuildContext context, IEnumerable<Expression> keys)
		{
			foreach (var key in keys)
			{
				foreach (var extracted in ConvertToKeyInfos2(context, key))
				{
					yield return extracted;
				}
			}
		}

		static IEnumerable<KeyInfo> ConvertToKeyInfos2(IBuildContext ctx, Expression forExpr)
		{
			var mappingSchema = ctx.Builder.MappingSchema;

			forExpr = forExpr.Unwrap();
			var exprCtx = ctx.Builder.GetContext(ctx, forExpr) ?? ctx;

			var level      = forExpr.GetLevel(mappingSchema);
			var flags = ctx.IsExpression(forExpr, level, RequestFor.Field).Result ||
						ctx.IsExpression(forExpr, 0, RequestFor.Field).Result
				? ConvertFlags.Field
				: ConvertFlags.Key;
			var noIndexSql = exprCtx.ConvertToSql(forExpr, 0, flags);

			// filter out keys which are queries
			//
			if (noIndexSql.Any(s => s.Sql.ElementType == QueryElementType.SqlQuery))
			{
				yield break;
			}

			var sql = exprCtx.ConvertToIndex(forExpr, 0, flags);

			if (sql.Length == 1)
			{
				var sqlInfo = sql[0];
				var memberChain = sqlInfo.MemberChain.Length > 0
					? sqlInfo.MemberChain
					: noIndexSql.FirstOrDefault(s => sqlInfo.Sql.Equals(s.Sql) && s.MemberChain.Length > 0)
						?.MemberChain ?? Array<MemberInfo>.Empty;

				if (memberChain.Length == 0 && sqlInfo.Sql.SystemType == forExpr.Type)
				{
					var parentIdx      = exprCtx.ConvertToParentIndex(sqlInfo.Index, exprCtx);
					var forCompilation = exprCtx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx, sqlInfo.Sql);
					yield return new KeyInfo
					{
						Original = forExpr,
						ForSelect = forExpr,
						ForCompilation = forCompilation
					};
					yield break;
				}
			}

			foreach (var sqlInfo in sql)
			{
				var memberChain = sqlInfo.MemberChain.Length > 0
					? sqlInfo.MemberChain
					: noIndexSql.FirstOrDefault(s => sqlInfo.Sql.Equals(s.Sql) && s.MemberChain.Length > 0)
						?.MemberChain ?? Array<MemberInfo>.Empty;

				if (sqlInfo.Sql.ElementType == QueryElementType.SqlQuery)
					continue;

				if (sqlInfo.Sql.SystemType == null || !mappingSchema.IsScalarType(sqlInfo.Sql.SystemType))
					continue;

				var forSelect = ConstructMemberPath(memberChain, forExpr, false);

				if (forSelect == null)
				{
					// TODO: We need more support from sequences to do that correctly
					var members = memberChain.ToList();
					while (members.Count > 1)
					{
						members.RemoveAt(0);
						forSelect = ConstructMemberPath(members, forExpr, false);
						if (forSelect != null)
							break;
					}
				}

				if (forSelect == null && forExpr is MemberExpression forExprMember)
				{
					if (memberChain.Last() == forExprMember.Member)
						forSelect = forExpr;
				}

				if (forSelect != null)
				{
					var parentIdx      = exprCtx.ConvertToParentIndex(sqlInfo.Index, exprCtx);
					var forCompilation = exprCtx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx, sqlInfo.Sql);

					yield return new KeyInfo
					{
						Original = forExpr,
						ForSelect = forSelect,
						ForCompilation = forCompilation
					};
				}
			}
		}

		static IEnumerable<KeyInfo> ExtractKeys(IBuildContext context, ParameterExpression param)
		{
			return ConvertToKeyInfos(context, null, param);
		}

		static IEnumerable<KeyInfo> ConvertToKeyInfos(IBuildContext ctx, Expression? forExpr,
			Expression obj)
		{
			var flags = forExpr == null || forExpr.NodeType == ExpressionType.Parameter ? ConvertFlags.Key : ConvertFlags.Field;
			var sql   = ctx.ConvertToIndex(forExpr, 0, flags);

			if (sql.Length == 1)
			{
				var sqlInfo = sql[0];
				if (sqlInfo.MemberChain.Length == 0 && sqlInfo.Sql.SystemType == obj.Type)
				{
					var parentIdx      = ctx.ConvertToParentIndex(sqlInfo.Index, ctx);
					var forCompilation = ctx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx, sqlInfo.Sql);
					yield return new KeyInfo
					{
						Original = forExpr ?? obj,
						ForSelect = obj,
						ForCompilation = forCompilation
					};
					yield break;
				}
			}

			foreach (var sqlInfo in sql)
			{
				var forSelect = ConstructMemberPath(sqlInfo.MemberChain, obj, false);
				if (forSelect == null && forExpr?.NodeType == ExpressionType.MemberAccess)
					forSelect = forExpr;
				if (forSelect != null)
				{
					var parentIdx = ctx.ConvertToParentIndex(sqlInfo.Index, ctx);
					var forCompilation = ctx.Builder.BuildSql(sqlInfo.Sql.SystemType!, parentIdx, sqlInfo.Sql);

					yield return new KeyInfo
					{
						Original = forExpr ?? obj,
						ForSelect = forSelect,
						ForCompilation = forCompilation
					};
				}
			}
		}

		static Expression RemoveProjection(Expression expression)
		{
			while (expression is MethodCallExpression mc)
			{
				if (mc.IsQueryable("Select"))
					expression = mc.Arguments[0];
				else
					break;
			}

			return expression;
		}

		public static Expression? GenerateAssociationExpression(ExpressionBuilder builder, IBuildContext context, Expression expression, AssociationDescriptor association)
		{
			var initialMainQuery = ValidateMainQuery(builder.Expression);
			var mainQuery        = RemoveProjection(initialMainQuery);
			var mappingSchema    = builder.MappingSchema;

			// that means we processing association from TableContext. First parameter is master
			//
			Expression? detailQuery;

			var mainQueryElementType  = GetEnumerableElementType(mainQuery.Type, builder.MappingSchema);
			var alias = "lw_" + (association.MemberInfo.DeclaringType?.Name ?? "master");

			var masterParam           = Expression.Parameter(mainQueryElementType, alias);


			var reversedAssociationPath = builder.AssociationPath == null
				? new List<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>()
				: new List<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>(builder.AssociationPath);

			reversedAssociationPath.Reverse();

			var projectionVariant = false;

			if (reversedAssociationPath.Count == 0)
			{
				reversedAssociationPath.Add(new Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>(
					new AccessorMember(expression),
					context,
					null
				));

				projectionVariant = true;
			}

			var associationPath = new List<AccessorMember>(reversedAssociationPath.Select(a => a.Item1));


			var extractContext        = reversedAssociationPath[0].Item2;
			var associationParentType = associationPath[0].MemberInfo.DeclaringType!;
			var loadWithItems         = reversedAssociationPath[reversedAssociationPath.Count - 1].Item3;

			if (!associationParentType.IsSameOrParentOf(mainQueryElementType) && !typeof(KeyDetailEnvelope<,>).IsSameOrParentOf(mainQueryElementType))
			{
				var parentExpr = builder.AssociationRoot;

				if (parentExpr == null)
				{
					throw new NotImplementedException();
				}

				if (projectionVariant)
					detailQuery = parentExpr;
				else
					detailQuery = ConstructMemberPath(associationPath, parentExpr, true)!;

				var result = GenerateDetailsExpression(context.Parent!, builder.MappingSchema, detailQuery);

				return result;
			}

			Expression           resultExpression;
			Expression           finalExpression;
			ParameterExpression? replaceParam;

			// recursive processing
			if (typeof(KeyDetailEnvelope<,>).IsSameOrParentOf(mainQueryElementType))
			{
				if (!IsQueryableMethod(mainQuery, "SelectMany", out var mainSelectManyMethod))
					throw new InvalidOperationException("Unexpected Main Query");

				var detailProp   = ExpressionHelper.Property(masterParam, nameof(KeyDetailEnvelope<object, object>.Detail));

				if (!detailProp.Type.IsSameOrParentOf(associationParentType))
				{
					var parentExpr = builder.AssociationRoot;

					if (parentExpr == null)
					{
						throw new NotImplementedException();
					}

					if (projectionVariant)
						detailQuery = parentExpr;
					else
						detailQuery = ConstructMemberPath(associationPath, parentExpr, true)!;

					var result = GenerateDetailsExpression(context.Parent!, builder.MappingSchema, detailQuery);

					return result;
				}

				var envelopeCreateLambda = (LambdaExpression)mainSelectManyMethod.Arguments[2].Unwrap();
				var envelopeCreateMethod = (MemberInitExpression)envelopeCreateLambda.Body;

				var keyExpression    = ((MemberAssignment)envelopeCreateMethod.Bindings[0]).Expression;
				var detailExpression = ((MemberAssignment)envelopeCreateMethod.Bindings[1]).Expression;

				var prevKeys       = ExtractKeys(context, keyExpression).ToArray();
				var subMasterKeys  = ExtractKeys(context, detailExpression).ToArray();

				var prevKeysByParameter = ExtractTupleValues(keyExpression, ExpressionHelper.Property(masterParam, nameof(KeyDetailEnvelope<object, object>.Key)));

				var correctLookup = prevKeysByParameter.ToLookup(tv => tv.Item1, tv => tv.Item2, ExpressionEqualityComparer.Instance);
				foreach (var key in prevKeys)
				{
					if (correctLookup.Contains(key.ForSelect))
						key.ForSelect = correctLookup[key.ForSelect].First();
				}

				var subMasterObj = detailExpression;
				foreach (var key in subMasterKeys)
				{
					key.ForSelect = key.ForSelect.Replace(subMasterObj, detailProp);
				}

				var associationMember = associationPath[associationPath.Count - 1];
				associationPath.RemoveAt(associationPath.Count - 1);
				if (associationPath.Count == 0)
					detailQuery = detailProp;
				else
					detailQuery = ConstructMemberPath(associationPath, detailProp, true)!;

				var parentType = association.GetParentElementType();
				var objectType = association.GetElementType(builder.MappingSchema);
				var associationLambda = AssociationHelper.CreateAssociationQueryLambda(builder, associationMember, association, parentType, parentType,
					objectType, false, false, loadWithItems, out _);

				detailQuery = associationLambda.GetBody(detailQuery);

				ExtractNotSupportedPart(mappingSchema, detailQuery, associationMember.MemberInfo.GetMemberType(), out detailQuery, out finalExpression, out replaceParam);

				var masterKeys   = prevKeys.Concat(subMasterKeys).ToArray();
				resultExpression = GeneratePreambleExpression(masterKeys, masterParam, detailQuery, mainQuery, builder);
			}
			else
			{
				if (!associationParentType.IsSameOrParentOf(mainQueryElementType))
				{
					var parentExpr = builder.AssociationRoot;

					if (parentExpr == null)
					{
						throw new NotImplementedException();
					}

					detailQuery = ConstructMemberPath(associationPath, parentExpr, true)!;

					var result = GenerateDetailsExpression(context.Parent!, builder.MappingSchema, detailQuery);

					return result;
				}

				var associationMember = associationPath[associationPath.Count - 1];
				associationPath.RemoveAt(associationPath.Count - 1);
				if (associationPath.Count == 0)
					detailQuery = masterParam;
				else
					detailQuery = ConstructMemberPath(associationPath, masterParam, true)!;

				var parentType = association.GetParentElementType();
				var objectType = association.GetElementType(builder.MappingSchema);
				var associationLambda = AssociationHelper.CreateAssociationQueryLambda(builder, associationMember, association, parentType, parentType,
					objectType, false, false, loadWithItems, out _);

				var dependencyAnchor = detailQuery;
				detailQuery = associationLambda.GetBody(dependencyAnchor);

				var dependencies = new List<Expression>();
				CollectDependenciesByParameter(mappingSchema, detailQuery, dependencyAnchor, dependencies);

				var masterKeys  = ExtractKeysFromContext(extractContext, dependencies).ToList();

				var allCollected = dependencies.Count > 0 && dependencies.All(d => masterKeys.Any(ki => ki.Original == d));
				if (!allCollected)
				{
					masterKeys.AddRange(ExtractKeys(extractContext, masterParam));
				}

				ExtractNotSupportedPart(mappingSchema, detailQuery, associationMember.MemberInfo.GetMemberType(), out detailQuery, out finalExpression, out replaceParam);

				resultExpression = GeneratePreambleExpression(masterKeys, masterParam, detailQuery, mainQuery, builder);
			}

			if (replaceParam != null)
			{
				resultExpression = finalExpression.Replace(replaceParam, resultExpression);
			}

			return resultExpression;
		}

		public static LambdaExpression? FindContainingLambda(Expression expr, Expression toFind)
		{
			var ctx = new WritableContext<LambdaExpression?, Expression>(toFind);

			expr.Visit(ctx, static (context, e) =>
			{
				if (context.WriteableValue == null && e.NodeType == ExpressionType.Lambda)
				{
					var lambda = (LambdaExpression)e;
					var found = lambda.Body.Find(context.StaticValue);
					if (found != null)
					{
						context.WriteableValue = lambda;
					}
				}
			});

			return ctx.WriteableValue;
		}

		public static MethodCallExpression? FindContainingMethod(Expression expr, Expression toFind)
		{
			var ctx = new WritableContext<MethodCallExpression?, Expression>(toFind);

			expr.Find(ctx, static (context, e) =>
			{
				if (context.WriteableValue != null)
					return true;

				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;
					foreach (var argument in mc.Arguments)
					{
						var method = FindContainingMethod(argument, context.StaticValue);
						if (method != null)
						{
							context.WriteableValue = method;
							break;
						}
					}

					if (context.WriteableValue == null)
					{
						foreach (var argument in mc.Arguments)
						{
							var lambda = FindContainingLambda(argument, context.StaticValue);
							if (lambda != null)
							{
								context.WriteableValue = mc;
								break;
							}
						}
					}
				}

				return context.WriteableValue != null;
			});

			return ctx.WriteableValue;
		}

		private static void CollectDependenciesByParameter(MappingSchema mappingSchema, Expression forExpr, Expression byParameter, List<Expression> dependencies)
		{
			var ignore  = new HashSet<Expression>();
			ignore.Add(forExpr);

			// child first
			forExpr.Visit(new { ignore, mappingSchema, byParameter, dependencies }, static (context, e) =>
			{
				if (context.ignore.Contains(e))
					return true;

				if (e.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression)e;

					if (IsEnumerableType(ma.Type, context.mappingSchema))
						return true;

					var root = InternalExtensions.GetRootObject(ma, context.mappingSchema);
					if (root == context.byParameter || ma.Expression == context.byParameter)
					{
						context.dependencies.Add(e);
						while (ma.Expression.Unwrap()?.NodeType == ExpressionType.MemberAccess)
						{
							context.ignore.Add(ma.Expression);
							ma = (MemberExpression) ma.Expression.Unwrap();
						}
					}
				}

				return true;
			});
		}

		private static void CollectDependencies(MappingSchema mappingSchema, Expression forExpr, List<Expression> dependencies, List<ParameterExpression> dependencyParameters)
		{
			var ignore  = new HashSet<Expression>();
			ignore.Add(forExpr);

			// parent first
			forExpr.Visit(ignore, static (ignore, e) =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					ignore.AddRange(((LambdaExpression)e).Parameters);
			});

			// child first
			forExpr.Visit(new { ignore, mappingSchema, dependencies, dependencyParameters }, static (context, e) =>
			{
				if (context.ignore.Contains(e))
					return true;

				if (e.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression)e;

					if (IsEnumerableType(ma.Type, context.mappingSchema))
						return true;

					var root = InternalExtensions.GetRootObject(ma, context.mappingSchema);
					if (root.NodeType == ExpressionType.Parameter && !context.ignore.Contains(root))
					{
						context.dependencies.Add(e);
						if (ma.Expression?.NodeType == ExpressionType.Parameter)
						{
							context.ignore.Add(ma.Expression);
							context.dependencyParameters.Add((ParameterExpression)ma.Expression);
						}
						else
							while (ma.Expression.Unwrap()?.NodeType == ExpressionType.MemberAccess)
							{
								ma = (MemberExpression)ma.Expression.Unwrap()!;
								context.ignore.Add(ma);
							}
					}
				}
				else if (e.NodeType == ExpressionType.Parameter)
				{
					context.dependencies.Add(e);
					context.dependencyParameters.Add((ParameterExpression)e);
					context.ignore.Add(e);
				}

				return true;
			});
		}

		private static IEnumerable<Expression> GenerateEquals(MappingSchema mappingSchema, Expression expr1,
			Expression expr2)
		{
			switch (expr1.NodeType)
			{
				case ExpressionType.New:
					{
						var ne1 = (NewExpression)expr1;
						var ne2 = (NewExpression)expr2;
						for (var index = 0; index < ne1.Arguments.Count; index++)
						{
							var a1 = ne1.Arguments[index];
							var a2 = ne2.Arguments[index];

							foreach (var equal in GenerateEquals(mappingSchema, a1, a2))
							{
								yield return equal;
							}
						}

						break;
					}
				case ExpressionType.MemberInit:
					{
						var mi1 = (MemberInitExpression)expr1;
						var mi2 = (MemberInitExpression)expr2;

						var b1 = mi1.Bindings.OfType<MemberAssignment>();
						var b2 = mi2.Bindings.OfType<MemberAssignment>();

						var found = b1.Join(b2, _ => _.Member, _ => _.Member, Tuple.Create);

						foreach (var tuple in found)
						{
							foreach (var equal in GenerateEquals(mappingSchema, tuple.Item1.Expression, tuple.Item2.Expression))
							{
								yield return equal;
							}
						}

						break;
					}
				default:
					yield return ExpressionBuilder.Equal(mappingSchema, expr1, expr2);
					break;
			}

		}

		private static Expression InjectQuery(Expression destination, Expression query, MappingSchema mappingSchema)
		{
			var allowed = new HashSet<ParameterExpression>();
			var result = destination.Transform(new { allowed, mappingSchema, query }, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Lambda:
						{
							context.allowed.AddRange(((LambdaExpression)e).Parameters);
							break;
						}
					case ExpressionType.MemberAccess:
						{
							var ma = (MemberExpression)e;
							if (typeof(IEnumerable<>).IsSameOrParentOf(ma.Type) &&
								GetEnumerableElementType(ma.Type, context.mappingSchema) ==
								GetEnumerableElementType(context.query.Type, context.mappingSchema))
							{
								var root = InternalExtensions.GetRootObject(ma, context.mappingSchema);
								if (root.NodeType == ExpressionType.Parameter &&
									!context.allowed.Contains((ParameterExpression)root))
								{
									return context.query;
								}
							}

							break;
						}
					case ExpressionType.Parameter:
						{
							var prm = (ParameterExpression)e;
							if (!context.allowed.Contains(prm))
							{
								if (typeof(IEnumerable<>).IsSameOrParentOf(prm.Type) &&
									GetEnumerableElementType(prm.Type, context.mappingSchema) ==
									GetEnumerableElementType(context.query.Type, context.mappingSchema))
								{
									return context.query;
								}
							}

							break;
						}
				}

				return e;
			});
			return result;
		}

		public static Expression? GenerateDetailsExpression(IBuildContext context, MappingSchema mappingSchema, Expression expression)
		{
			expression                  = expression.Unwrap();

			var builder                 = context.Builder;
			var initialMainQuery        = ValidateMainQuery(builder.Expression.Unwrap());
			var unchangedDetailQuery    = expression;
			var hasConnectionWithMaster = true;

			ExtractNotSupportedPart(builder.MappingSchema, unchangedDetailQuery,
				unchangedDetailQuery.Type, out var queryableDetail, out var finalExpression,
				out var replaceParam);

			Expression resultExpression;
			LambdaExpression? detailLambda;

			var dependencies         = new List<Expression>();
			var dependencyParameters = new List<ParameterExpression>();

			//GroupJoin case, we do not need keys from itself
			if (queryableDetail.NodeType == ExpressionType.Parameter)
			{
				dependencyParameters.Remove((ParameterExpression)queryableDetail);
				dependencies.Remove(queryableDetail);
			}

			//TODO: we have to create sophisticated grouping handling
			var root = builder.GetRootObject(queryableDetail);
			if (typeof(IGrouping<,>).IsSameOrParentOf(root.Type))
				return null;

			var contextForKeys = context;

			if (context is JoinBuilder.GroupJoinContext groupJoin)
			{
				// GroupJoin contains keys in second and third parameters. We will create query based on these keys

				var groupJoinMethod   = FindContainingMethod(initialMainQuery, groupJoin.OuterKeyLambda.Body)!;
				detailLambda          = (LambdaExpression)groupJoinMethod.Arguments[4].Unwrap();
				contextForKeys        = groupJoin.Sequence[0];
				var masterKeySelector = (LambdaExpression)groupJoinMethod.Arguments[2].Unwrap();

				CollectDependencies(mappingSchema, masterKeySelector.GetBody(detailLambda.Parameters[0]), dependencies, dependencyParameters);

				// creating detail query
				var detailParam  = groupJoin.InnerKeyLambda.Parameters[0];
				var param_d      = Expression.Parameter(detailParam.Type, "gjd_" + detailParam.Name);

				var detailQuery = groupJoinMethod.Arguments[1].Unwrap();

				var equalityBody = GenerateEquals(mappingSchema,
						groupJoin.InnerKeyLambda.GetBody(param_d).Unwrap(),
						groupJoin.OuterKeyLambda.GetBody(detailLambda.Parameters[0]).Unwrap())
					.Aggregate((Expression?)null, (a, e) => a == null ? e : Expression.AndAlso(a, e));

				var methodInfo  = Methods.Queryable.Where.MakeGenericMethod(param_d.Type);
				var filteredQueryableDetail = Expression.Call(methodInfo, detailQuery,
					Expression.Quote(Expression.Lambda(equalityBody, param_d)));

				queryableDetail = InjectQuery(queryableDetail, filteredQueryableDetail, mappingSchema);
			}
			else
			{
				var searchExpression = queryableDetail;

				// handling case with association
				for (;;)
				{
					detailLambda = FindContainingLambda(initialMainQuery, searchExpression);
					if (detailLambda != null)
						break;

					if (searchExpression.NodeType == ExpressionType.MemberAccess)
						searchExpression = ((MemberExpression)searchExpression).Expression;
					else
						throw new NotImplementedException();
				}

				CollectDependencies(mappingSchema, queryableDetail, dependencies, dependencyParameters);

				if (dependencies.Count == 0 && dependencyParameters.Count == 0)
					hasConnectionWithMaster = false;
				else
				{
					// append lambda parameters
					dependencyParameters.AddRange(detailLambda.Parameters.Except(dependencyParameters));
				}
			}


			if (!hasConnectionWithMaster)
			{
				// detail query has no dependency with master, so load all

				var enlistMethod =
					EnlistEagerLoadingFunctionalityDetachedMethodInfo.MakeGenericMethod(GetEnumerableElementType(unchangedDetailQuery.Type, builder.MappingSchema));

				resultExpression = (Expression)enlistMethod.Invoke(null,
					new object[]
						{ builder, unchangedDetailQuery })!;
			}
			else
			{
				var replaceInfo = new ReplaceInfo(mappingSchema) { TargetLambda = detailLambda };

				var keysInfo     = ExtractKeysFromContext(contextForKeys, dependencies).ToList();
				var allCollected = dependencies.Count > 0 && dependencies.All(d => keysInfo.Any(ki => ki.Original == d));

				if (!allCollected)
				{
					var keysInfoByParams = ExtractKeysFromContext(contextForKeys, dependencyParameters).ToList();
					foreach (var info in keysInfoByParams)
					{
						if (!keysInfo.Any(_ =>
							_.ForSelect.EqualsTo(info.ForSelect, builder.GetSimpleEqualsToContext(false))))
							keysInfo.Add(info);
					}
				}

				// replaceInfo.Keys.AddRange(keysInfo.Select(k => k.ForSelect));
				replaceInfo.Keys.AddRange(dependencyParameters);

				var mainQueryWithCollectedKey = ApplyReMapping(initialMainQuery, replaceInfo, true);

				// something happened and we failed to enrich query
				if (mainQueryWithCollectedKey == initialMainQuery)
					return null;

				var resultParameterType = GetEnumerableElementType(mainQueryWithCollectedKey.Type, builder.MappingSchema);
				var resultParameter     = Expression.Parameter(resultParameterType, "key_data_result");

				var dataType = resultParameterType.GetGenericArguments()[1];

				// Collecting keys from previous iteration
				if (typeof(KeyDetailEnvelope<,>).IsSameOrParentOf(dataType))
				{
					if (!IsQueryableMethod(initialMainQuery, "SelectMany", out var mainSelectManyMethod))
						throw new InvalidOperationException("Unexpected Main Query");

					var envelopeCreateLambda = (LambdaExpression)mainSelectManyMethod.Arguments[2].Unwrap();
					var envelopeCreateMethod = (MemberInitExpression)envelopeCreateLambda.Body;

					var keyExpression = ((MemberAssignment)envelopeCreateMethod.Bindings[0]).Expression;
					var prevKeys      = ExtractKeys(context, keyExpression).ToArray();

					var keysPath      = ExpressionHelper.PropertyOrField(ExpressionHelper.PropertyOrField(resultParameter, "Data"), "Key");
					var prevPairs     = ExtractTupleValues(keyExpression, keysPath)
						.ToLookup(p => p.Item1);

					foreach (var prevKey in prevKeys)
					{
						prevKey.ForSelect = prevKey.ForSelect.Transform(prevPairs, static (prevPairs, e) => prevPairs.Contains(e) ? prevPairs[e].First().Item2 : e);
					}

					keysInfo.AddRange(prevKeys);
				}

				if (keysInfo.Count == 0)
					throw new LinqException("Could not retrieve information about unique keys for generating detail query.");

				var generateKeyExpression   = GenerateKeyExpression(keysInfo.Select(k => k.ForCompilation).ToArray(), 0);
				var keySelectExpression     = GenerateKeyExpression(keysInfo.Select(k => k.ForSelect).ToArray(), 0);
				var keyParametersExpression = GenerateKeyExpression(dependencyParameters.Cast<Expression>().ToArray(), 0);

				var keyField = ExpressionHelper.PropertyOrField(resultParameter, "Key");
				var pairs    = ExtractTupleValues(keyParametersExpression, keyField)
					.ToArray();

				var keySelectExpressionCorrected = keySelectExpression.Transform(new { dependencyParameters, pairs }, static (context, e) =>
				{
					if (e.NodeType == ExpressionType.Parameter)
					{
						var idx = context.dependencyParameters.IndexOf((ParameterExpression)e);
						if (idx >= 0)
						{
							return context.pairs[idx].Item2;
						}
					}

					return e;
				});

				var queryableDetailCorrected = queryableDetail.Transform(new { dependencyParameters, pairs }, static (context, e) =>
				{
					if (e.NodeType == ExpressionType.Parameter)
					{
						var idx = context.dependencyParameters.IndexOf((ParameterExpression)e);
						if (idx >= 0)
						{
							return context.pairs[idx].Item2;
						}
					}

					return e;
				});

				queryableDetailCorrected = MakeExpressionCopy(queryableDetailCorrected);
				queryableDetailCorrected = EnsureEnumerable(queryableDetailCorrected, builder.MappingSchema);

				var queryableDetailLambda = Expression.Lambda(queryableDetailCorrected, resultParameter);
				var keySelectLambda       = Expression.Lambda(keySelectExpressionCorrected, resultParameter);

				// mark query as distinct
				//
				mainQueryWithCollectedKey = TypeHelper.MakeMethodCall(Methods.LinqToDB.SelectDistinct, mainQueryWithCollectedKey);

				var enlistMethodFinal =
					EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
						resultParameterType,
						GetEnumerableElementType(queryableDetail.Type, builder.MappingSchema),
						generateKeyExpression.Type);

				resultExpression = (Expression)enlistMethodFinal.Invoke(null,
					new object[]
						{ builder, mainQueryWithCollectedKey, queryableDetailLambda, generateKeyExpression, keySelectLambda })!;
			}

			if (replaceParam != null)
			{
				resultExpression = finalExpression.Replace(replaceParam, resultExpression);
			}

			return resultExpression;
		}

		private static bool IsEqualPath(Expression? exp1, Expression? exp2)
		{
			if (ReferenceEquals(exp1, exp2))
				return true;

			if (exp1 == null || exp2 == null)
				return false;

			if (exp1.NodeType != exp2.NodeType)
				return false;

			if (exp1.NodeType == ExpressionType.Parameter)
				return exp1 == exp2;

			if (exp1.NodeType == ExpressionType.MemberAccess)
			{
				var ma1 = (MemberExpression)exp1;
				var ma2 = (MemberExpression)exp2;
				return ma1.Member == ma2.Member && IsEqualPath(ma1.Expression, ma2.Expression);
			}

			return false;
		}

		private static Expression GeneratePreambleExpression(IList<KeyInfo> preparedKeys, 
			ParameterExpression masterParam, Expression detailQuery, Expression masterQuery, ExpressionBuilder builder)
		{
			// mark query as distinct
			//
			masterQuery = TypeHelper.MakeMethodCall(Methods.LinqToDB.SelectDistinct, masterQuery);

			var keyCompiledExpression = GenerateKeyExpression(preparedKeys.Select(k => k.ForCompilation).ToArray(), 0);
			var keySelectExpression   = GenerateKeyExpression(preparedKeys.Select(k => k.ForSelect).ToArray(), 0);

			var keySelectLambda    = Expression.Lambda(keySelectExpression, masterParam);
			var detailsQueryLambda = Expression.Lambda(EnsureEnumerable(detailQuery, builder.MappingSchema), masterParam);

			var enlistMethod =
				EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
					GetEnumerableElementType(masterQuery.Type, builder.MappingSchema),
					GetEnumerableElementType(detailQuery.Type, builder.MappingSchema),
					keyCompiledExpression.Type);

			var resultExpression = (Expression)enlistMethod.Invoke(null,
				new object[]
					{ builder, masterQuery, detailsQueryLambda, keyCompiledExpression, keySelectLambda })!;
			return resultExpression;
		}

		static Expression EnlistEagerLoadingFunctionalityDetached<TD>(
			ExpressionBuilder builder,
			Expression detailQueryExpression)
		{
			var detailQuery = Internals.CreateExpressionQueryInstance<TD>(builder.DataContext, detailQueryExpression);

			//TODO: currently we run in separate query

			var idx = RegisterPreamblesDetached(builder, detailQuery);

			var resultExpression = Expression.Convert(
				Expression.ArrayIndex(ExpressionBuilder.PreambleParam, Expression.Constant(idx)),
				typeof(List<TD>));

			return resultExpression;
		}


		internal readonly struct KeyDetailEnvelope<TKey, TDetail>
		{
			public KeyDetailEnvelope(TKey key, TDetail detail)
			{
				Key    = key;
				Detail = detail;
			}
			public readonly TKey    Key;
			public readonly TDetail Detail;
		}

		static Expression EnlistEagerLoadingFunctionality<T, TD, TKey>(
			ExpressionBuilder builder,
			Expression mainQueryExpr, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryLambda,
			Expression compiledKeyExpression,
			Expression<Func<T, TKey>> selectKeyExpression)
			where TKey : notnull
		{
			var mainQuery   = Internals.CreateExpressionQueryInstance<T>(builder.DataContext, mainQueryExpr);
			var detailQuery = mainQuery
				.RemoveOrderBy()
				.SelectMany(detailQueryLambda,
						// don't replace with CompileExpression extension point
						// Compile will be replaced with expression embedding
					(main, detail) => new KeyDetailEnvelope<TKey, TD>(selectKeyExpression.Compile()(main), detail));

			//TODO: currently we run in separate query

			var idx = RegisterPreambles(builder, detailQuery);

			var getListMethod = MemberHelper.MethodOf((EagerLoadingContext<TD, TKey> c) => c.GetList(default!));

			var resultExpression =
				Expression.Call(
					Expression.Convert(Expression.ArrayIndex(ExpressionBuilder.PreambleParam, Expression.Constant(idx)),
						typeof(EagerLoadingContext<TD, TKey>)), getListMethod, compiledKeyExpression);

			return resultExpression;
		}

		private static void PrepareParameters(Expression expr, ExpressionBuilder builder, out ParameterContainer container,
			out Expression correctedExpression)
		{
			container           = new ParameterContainer();
			var indexes         = new Dictionary<ParameterAccessor, Expression>();
			var knownParameters = builder._parameters;
			var containerLocal  = container;

			correctedExpression = expr.Transform(new { knownParameters, builder, indexes, containerLocal }, static (context, e) =>
			{
				if (!context.knownParameters.TryGetValue(e, out var registered))
				{
					if (e.NodeType == ExpressionType.MemberAccess || e.NodeType == ExpressionType.Call)
					{
						if (e is MethodCallExpression mc)
						{
							if (mc.IsQueryable())
								return e;
						}

						// registering missed parameters
						registered = context.builder.RegisterParameter(e);
					}
				}

				if (registered != null)
				{
					if (!context.indexes.TryGetValue(registered, out var accessExpression))
					{
						var idx = context.containerLocal.RegisterAccessor(registered);

						accessExpression = Expression.Call(Expression.Constant(context.containerLocal),
							ParameterContainer.GetValueMethodInfo.MakeGenericMethod(e.Type),
							Expression.Constant(idx));

						context.indexes.Add(registered, accessExpression);
					}

					return accessExpression;
				}

				return e;
			});
		}

		private static int RegisterPreamblesDetached<TD>(ExpressionBuilder builder, IQueryable<TD> detailQuery)
		{
			var idx = builder.RegisterPreamble((dc, expr, ps) =>
				{
					//TODO: needed more performant way
					PrepareParameters(detailQuery.Expression, builder, out var container, out var detailExpression);

					container.DataContext         = dc;
					container.CompiledParameters  = ps;
					container.ParameterExpression = expr;

					var queryable = new ExpressionQueryImpl<TD>(dc, detailExpression);
					var details = queryable.ToList();
					return details;
				},
				async (dc, expr, ps, ct) =>
				{
					//TODO: needed more performant way
					PrepareParameters(detailQuery.Expression, builder, out var container, out var detailExpression);

					container.DataContext         = dc;
					container.CompiledParameters  = ps;
					container.ParameterExpression = expr;

					var queryable = new ExpressionQueryImpl<TD>(dc, detailExpression);
					var details = await queryable.ToListAsync(ct).ConfigureAwait(Configuration.ContinueOnCapturedContext);
					return details;
				}
			);
			return idx;
		}

		private static int RegisterPreambles<TD, TKey>(ExpressionBuilder builder, IQueryable<KeyDetailEnvelope<TKey, TD>> detailQuery)
			where TKey : notnull
		{
			// Finalize keys for recursive processing
			var expression = detailQuery.Expression;
			expression     = builder.ExposeExpression(expression);
			expression     = FinalizeExpressionKeys(new HashSet<Expression>(), expression);

			// Filler code is duplicated for the future usage with IAsyncEnumerable
			var idx = builder.RegisterPreamble((dc, expr, ps) =>
				{
					//TODO: needed more performant way
					PrepareParameters(expression, builder, out var container, out var detailExpression);

					container.DataContext         = dc;
					container.CompiledParameters  = ps;
					container.ParameterExpression = expr;

					var queryable           = new ExpressionQueryImpl<KeyDetailEnvelope<TKey, TD>>(dc, detailExpression);
					var detailsWithKey      = queryable.ToList();
					var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

					foreach (var d in detailsWithKey)
					{
						eagerLoadingContext.Add(d.Key, d.Detail);
					}

					return eagerLoadingContext;
				},
				async (dc, expr, ps, ct) =>
				{
					//TODO: needed more performant way
					PrepareParameters(expression, builder, out var container, out var detailExpression);

					container.DataContext         = dc;
					container.CompiledParameters  = ps;
					container.ParameterExpression = expr;

					var queryable           = new ExpressionQueryImpl<KeyDetailEnvelope<TKey, TD>>(dc, detailExpression);
					var detailsWithKey      = await queryable.ToListAsync(ct).ConfigureAwait(Configuration.ContinueOnCapturedContext);
					var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

					foreach (var d in detailsWithKey)
					{
						eagerLoadingContext.Add(d.Key, d.Detail);
					}

					return eagerLoadingContext;
				}
			);
			return idx;
		}

		public static LambdaExpression CorrectLambdaType(LambdaExpression before, LambdaExpression after, MappingSchema mappingSchema)
		{
			if (IsEnumerableType(before.ReturnType, mappingSchema) && before.ReturnType.IsGenericType)
			{
				var generic     = before.ReturnType.GetGenericTypeDefinition();
				var elementType = GetEnumerableElementType(after.ReturnType, mappingSchema);
				var desiredType = generic.MakeGenericType(elementType);
				if (after.ReturnType != desiredType && IsEnumerableType(after.ReturnType, mappingSchema))
				{
					after = Expression.Lambda(Expression.Convert(after.Body, desiredType), after.Parameters);
				}
			}

			return after;
		}

		internal class ReplaceInfo
		{
			public ReplaceInfo(MappingSchema mappingSchema)
			{
				MappingSchema = mappingSchema;
			}

			public MappingSchema                        MappingSchema { get;      }
			public LambdaExpression                     TargetLambda  { get; set; } = null!;
			public List<Expression>                     Keys          { get;      } = new List<Expression>();
			public Dictionary<MemberInfo, MemberInfo[]> MemberMapping { get;      } = new Dictionary<MemberInfo, MemberInfo[]>();
		}

		internal static Expression CreateKDH(Expression key, Expression data)
		{
			var genericType   = typeof(KDH<,>).MakeGenericType(key.Type, data.Type);
			var constructor   = genericType.GetConstructor(Array<Type>.Empty);
			var newExpression = Expression.New(constructor);

			var memberInit    = Expression.MemberInit(newExpression, 
				Expression.Bind(genericType.GetProperty("Key"), key),
				Expression.Bind(genericType.GetProperty("Data"), data));

			return memberInit;
		}

		internal static Expression MakeExpressionCopy(Expression expression)
		{
			var result = _makeExpressionCopyTransformer.Transform(expression);

			return result;
		}

		private static readonly TransformVisitor<object?> _makeExpressionCopyTransformer = TransformVisitor<object?>.Create(MakeExpressionCopyTransformer);
		private static Expression MakeExpressionCopyTransformer(Expression e)
		{
			if (e.NodeType == ExpressionType.Lambda)
			{
				var lambda        = (LambdaExpression)e;
				var newParameters = lambda.Parameters
						.Select(p => Expression.Parameter(p.Type, "_" + p.Name))
						.ToList();

				var newBody = lambda.Body.Transform(new { lambda, newParameters}, static (context, b) =>
					{
						if (b.NodeType == ExpressionType.Parameter)
						{
							var prm = (ParameterExpression)b;
							var idx = context.lambda.Parameters.IndexOf(prm);
							if (idx >= 0)
								return context.newParameters[idx];
						}

						return b;
					});

				newBody = MakeExpressionCopy(newBody);

				return Expression.Lambda(newBody, newParameters);
			}

			return e;
		}

		internal static Type FinalizeType(Type type)
		{
			if (!type.IsGenericType)
				return type;

			var arguments = type.GenericTypeArguments.Select(FinalizeType).ToArray();

			var newType = type;

			if (typeof(KDH<,>).IsSameOrParentOf(type))
				newType = typeof(FKDH<,>).MakeGenericType(arguments);
			else
				newType = type.GetGenericTypeDefinition().MakeGenericType(arguments);

			return newType;
		}

		internal static MemberInfo? GetMemberForType(Type type, MemberInfo memberInfo)
		{
			if (type == memberInfo.DeclaringType)
				return memberInfo;
			return type.GetMemberEx(memberInfo);
		}


		[return: NotNullIfNotNull("body")]
		internal static Expression? ReplaceParametersWithChangedType(Expression? body, IList<ParameterExpression> before, IList<ParameterExpression> after)
		{
			if (body == null)
				return null;

			var newBody = body.Transform(new { before, after }, static (context, b) =>
			{
				if (b.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression)b;
					if (ma.Expression?.NodeType == ExpressionType.Parameter)
					{
						var idx = context.before.IndexOf((ParameterExpression)ma.Expression);
						if (idx >= 0)
						{
							var prm = context.after[idx];
							if (prm != ma.Expression)
								return Expression.MakeMemberAccess(prm, GetMemberForType(prm.Type, ma.Member));
						}
					}
				}
				else if (b.NodeType == ExpressionType.Invoke)
				{
					var inv = (InvocationExpression)b;
					var newExpression = ReplaceParametersWithChangedType(inv.Expression, context.before, context.after);
					var newArguments  = inv.Arguments.Select(a => ReplaceParametersWithChangedType(a, context.before, context.after));
					return Expression.Invoke(newExpression, newArguments);
				}

				return b;
			});

			newBody = newBody.Transform(new { before, after }, static (context, b) =>
			{
				if (b.NodeType == ExpressionType.Parameter)
				{
					var idx = context.before.IndexOf((ParameterExpression)b);
					if (idx >= 0)
					{
						return context.after[idx];
					}
				}

				return b;
			});

			return newBody;
		}

		[return: NotNullIfNotNull("expr")]
		internal static Expression? FinalizeExpressionKeys(HashSet<Expression> stable, Expression? expr)
		{
			if (expr == null)
				return null;

			if (stable.Contains(expr))
				return expr;

			var result = expr.Transform(stable, static (stable, e) =>
			{
				if (stable.Contains(e))
					return e;

				switch (e.NodeType)
				{
					case ExpressionType.MemberInit:
						{
							var newType = FinalizeType(e.Type);
							if (newType != e.Type)
							{
								var mi = (MemberInitExpression)e;
								var newAssignments = mi.Bindings.Cast<MemberAssignment>().Select(a =>
									{
										var finalized = FinalizeExpressionKeys(stable, a.Expression);
										return Expression.Bind(GetMemberForType(newType, a.Member), finalized);
									})
									.ToArray();

								var newMemberInit = Expression.MemberInit(
									Expression.New(newType.GetConstructor(Array<Type>.Empty) ??
												   throw new InvalidOperationException($"Default constructor not found for type {newType}")), newAssignments);
								return newMemberInit;
							}

							break;
						}
					case ExpressionType.Convert:
						{
							var unary      = (UnaryExpression)e;
							var newType    = FinalizeType(unary.Type);
							var newOperand = FinalizeExpressionKeys(stable, unary.Operand);
							if (newType != unary.Type || newOperand != unary.Operand)
								return Expression.Convert(newOperand, newType);
							break;
						}
					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)e;

							var changed = false;
							var newArguments = mc.Arguments.Select(a =>
							{
								var n = FinalizeExpressionKeys(stable, a);
								changed = changed || n != a;
								return n;
							}).ToArray();

							var method = mc.Method;
							if (mc.Method.IsGenericMethod)
							{
								var newGenericArguments = mc.Method.GetGenericArguments().Select(t =>
									{
										var nt = FinalizeType(t);
										changed = changed || nt != t;
										return nt;
									})
									.ToArray();

								if (changed)
									method = mc.Method.GetGenericMethodDefinition()
										.MakeGenericMethod(newGenericArguments);

							}

							if (changed)
								return Expression.Call(method, newArguments);

							break;
						}
					case ExpressionType.MemberAccess:
						{
							var ma     = (MemberExpression)e;
							var newObj = FinalizeExpressionKeys(stable, ma.Expression);
							if (newObj != ma.Expression)
							{
								return Expression.MakeMemberAccess(newObj, GetMemberForType(newObj.Type, ma.Member));
							}
							break;
						}
					case ExpressionType.Invoke:
						{
							break;
						}
					case ExpressionType.Lambda:
						{
							var lambda  = (LambdaExpression)e;
							var changed = false;
							var newParameters = lambda.Parameters.Select(p =>
								{
									var nt = FinalizeType(p.Type);
									if (nt != p.Type)
									{
										p = Expression.Parameter(nt, p.Name);
										changed = true;
									}

									return p;
								})
								.ToArray();

							var newBody = FinalizeExpressionKeys(stable, lambda.Body);
							if (changed || newBody != lambda.Body)
							{
								newBody = ReplaceParametersWithChangedType(newBody, lambda.Parameters, newParameters);
								return Expression.Lambda(newBody, newParameters);
							}

							break;
						}
				}

				return e;
			});

			if (ReferenceEquals(expr, result))
			{
				stable.Add(expr);
			}

			return result;
		}

		private static readonly MethodInfo[] JoinMethods = new []
		{
			Methods.Enumerable.GroupJoin, Methods.Queryable.GroupJoin,
			Methods.Enumerable.Join,      Methods.Queryable.Join
		};

		internal static bool IsTransientParam(MethodCallExpression mc, int paramIndex)
		{
			if (mc.IsSameGenericMethod(JoinMethods))
				return paramIndex == 2 || paramIndex == 3;

			return false;
		}

		internal static Expression ApplyReMapping(Expression expr, ReplaceInfo replaceInfo, bool isQueryable)
		{
			var newExpr = expr;
			switch (expr.NodeType)
			{
				case ExpressionType.Lambda:
					{
						var lambdaExpression = (LambdaExpression)expr;

						if (expr == replaceInfo.TargetLambda)
						{
							if (isQueryable)
							{
								// replacing body
								var neededKey     = GenerateKeyExpression(replaceInfo.Keys.ToArray(), 0);
								var itemType      = GetEnumerableElementType(lambdaExpression.Body.Type, replaceInfo.MappingSchema);
								var parameter     = Expression.Parameter(itemType, "t");
								var selectBody    = CreateKDH(neededKey, parameter);

								var selectMethod  = Methods.Queryable.Select.MakeGenericMethod(itemType, selectBody.Type);
								var newBody       = Expression.Call(selectMethod, lambdaExpression.Body, Expression.Lambda(selectBody, parameter));
								newExpr           = Expression.Lambda(newBody, lambdaExpression.Parameters);

								newExpr = CorrectLambdaType(lambdaExpression, (LambdaExpression)newExpr, replaceInfo.MappingSchema);
							}
							else
							{
								// replacing body
								var neededKey = GenerateKeyExpression(replaceInfo.Keys.ToArray(), 0);

								var newBody   = CreateKDH(neededKey, lambdaExpression.Body);
								newExpr       = Expression.Lambda(newBody, lambdaExpression.Parameters);
							}

						}
						else
						{
							var current = lambdaExpression.Body.Unwrap();
							var newBody = ApplyReMapping(current, replaceInfo, isQueryable);
							if (newBody != current)
							{
								newExpr = Expression.Lambda(newBody, lambdaExpression.Parameters);
								if (isQueryable)
									newExpr = CorrectLambdaType(lambdaExpression, (LambdaExpression)newExpr, replaceInfo.MappingSchema);
							}

						}
						break;
					}
				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expr;

						if (mc.IsQueryable())
						{
							var methodGenericArguments  = mc.Method.GetGenericArguments();
							var methodGenericDefinition = mc.Method.GetGenericMethodDefinition();
							var genericArguments        = methodGenericDefinition.GetGenericArguments();
							var genericParameters       = methodGenericDefinition.GetParameters();
							var typesMapping            = new Dictionary<Type, Type>();
							var newArguments            = mc.Arguments.ToArray();
							var methodNeedsUpdate       = false;

							for (int i = 0; i < mc.Arguments.Count; i++)
							{
								var arg = mc.Arguments[i];
								if (!methodNeedsUpdate)
								{
									var genericParameter = genericParameters[i];
									var expectQueryable = typeof(IEnumerable<>).IsSameOrParentOf(genericParameter.ParameterType);
									if (!expectQueryable)
									{
										var lambdaType = typeof(Expression<>).IsSameOrParentOf(genericParameter.ParameterType)
											? genericParameter.ParameterType.GetGenericArguments()[0]
											: genericParameter.ParameterType;

										if (lambdaType.IsGenericType)
										{
											var lambdaGenericParams = lambdaType.GetGenericArguments();
											var resultType          = lambdaGenericParams[lambdaGenericParams.Length - 1];
											expectQueryable         = typeof(IEnumerable<>).IsSameOrParentOf(resultType);
										}
									}

									if (!expectQueryable)
									{
										var unwrapped = arg.Unwrap();
										if (unwrapped is LambdaExpression lambda)
										{
											if (typeof(IQueryable<>).IsSameOrParentOf(lambda.Body.Type))
											{
												expectQueryable = false;
											}
										}
									}

									var newArg = ApplyReMapping(arg, replaceInfo, expectQueryable);

									if (arg != newArg)
									{
										if (typeof(Expression<>).IsSameOrParentOf(genericParameters[i].ParameterType) 
										    && newArg.Unwrap().NodeType == ExpressionType.Lambda)
										{
											newArg = Expression.Quote(CorrectLambdaType((LambdaExpression)arg.Unwrap(),
												(LambdaExpression)newArg.Unwrap(), replaceInfo.MappingSchema));
										}


										methodNeedsUpdate = true;
										TypeHelper.RegisterTypeRemapping(genericParameters[i].ParameterType, newArg.Type,
											genericArguments, typesMapping);

										newArguments[i] = newArg;
									}
								}
								else
								{
									arg = arg.Unwrap();
									if (arg.NodeType == ExpressionType.Lambda && typesMapping.Count > 0)
									{
										var currentLambdaTemplateParams = genericParameters[i];
										var templateLambdaType = currentLambdaTemplateParams.ParameterType;
										var lambdaGenericArguments =
											typeof(Expression<>).IsSameOrParentOf(templateLambdaType)
												? templateLambdaType.GetGenericArguments()[0]
													.GetGenericArguments()
												: templateLambdaType.GetGenericArguments();

										
										var argLambda     = (LambdaExpression)arg;
										var newParameters = argLambda.Parameters.ToArray();
										var newBody       = argLambda.Body;
										var needsUpdate   = false;
										ParameterExpression? transientParam = null;
										for (int j = 0; j < argLambda.Parameters.Count; j++)
										{
											var prm = argLambda.Parameters[j];

											var genericType = lambdaGenericArguments[j];
											if (typesMapping.TryGetValue(genericType, out var replacedType))
											{
												if (replacedType != prm.Type)
												{
													needsUpdate = true;

													if (typeof(KDH<,>).IsSameOrParentOf(replacedType))
													{
														var newParam     = Expression.Parameter(replacedType, prm.Name);
														transientParam   = newParam;
														newParameters[j] = newParam;

														var accessExpr =
															ExpressionHelper.PropertyOrField(newParam, "Data");
														newBody = newBody.Replace(prm, accessExpr);
													}
													else if (typeof(IGrouping<,>).IsSameOrParentOf(replacedType))
													{
														//We do not support grouping yet
														return expr;
													}
												}
											}
										}

										if (needsUpdate)
										{
											var resultTemplateParam =
												lambdaGenericArguments[lambdaGenericArguments.Length - 1];

											if (typesMapping.TryGetValue(resultTemplateParam, out var replacedType))
											{
												throw new NotImplementedException();
											}
											else
											{
												if (transientParam != null)
												{
													// replacing body
													if (typeof(IEnumerable<>).IsSameOrParentOf(resultTemplateParam))
													{
														// not SelectMany second param
														if (i == mc.Arguments.Count - 1)
														{
															var neededKey  = ExpressionHelper.PropertyOrField(transientParam, "Key");
															var itemType   = GetEnumerableElementType(newBody.Type, replaceInfo.MappingSchema);
															var parameter  = Expression.Parameter(itemType, "t");
															var selectBody = CreateKDH(neededKey, parameter);

															var selectMethod =
																(typeof(IQueryable<>).IsSameOrParentOf(
																	resultTemplateParam)
																	? Methods.Queryable.Select
																	: Methods.Enumerable.Select)
																.MakeGenericMethod(itemType, selectBody.Type);
															newBody = Expression.Call(selectMethod, newBody,
																Expression.Lambda(selectBody, parameter));
														}
													}
													else
													{
														// Where, OrderBy, ThenBy, etc
														var isTransient =
															GetEnumerableElementType(genericParameters[0].ParameterType,
																replaceInfo.MappingSchema) ==
															GetEnumerableElementType(methodGenericDefinition.ReturnType,
																replaceInfo.MappingSchema);
														if (!isTransient && !IsTransientParam(mc, i))
														{
															var neededKey =
																ExpressionHelper.PropertyOrField(transientParam, "Key");
															newBody = CreateKDH(neededKey, newBody);
														}
													}

												}
											}

											var newArgLambda = Expression.Lambda(newBody, newParameters);
											    newArgLambda = CorrectLambdaType(argLambda, newArgLambda, replaceInfo.MappingSchema);

											var forRegister = typeof(Expression<>).IsSameOrParentOf(templateLambdaType)
												? templateLambdaType.GetGenericArguments()[0]
												: templateLambdaType;

											TypeHelper.RegisterTypeRemapping(forRegister,
												newArgLambda.Type, genericArguments, typesMapping);

											newArguments[i] = newArgLambda;
										}
									}
								}
							}

							if (methodNeedsUpdate)
							{
								var newGenericArguments = genericArguments.Select((t, i) =>
								{
									if (typesMapping.TryGetValue(t, out var replaced))
										return replaced;
									return methodGenericArguments[i];
								}).ToArray();

								var newMethodInfo = methodGenericDefinition.MakeGenericMethod(newGenericArguments);
								newExpr = Expression.Call(newMethodInfo, newArguments);
							}
						}

						break;
					}
				case ExpressionType.MemberInit:
					{
						var mi = (MemberInitExpression)expr;
						Expression? updated = null;
						for (int i = 0; i < mi.Bindings.Count; i++)
						{
							if (mi.Bindings[i] is MemberAssignment arg)
							{
								var newArg = ApplyReMapping(arg.Expression, replaceInfo, false);
								if (newArg != arg.Expression)
								{
									updated = newArg;
									break;
								}
							}
						}

						if (updated != null)
						{
							if (typeof(KDH<,>).IsSameOrParentOf(updated.Type))
							{
								updated = updated.NodeType == ExpressionType.MemberInit
									? ((MemberAssignment)((MemberInitExpression)updated).Bindings[0]).Expression
									: ExpressionHelper.PropertyOrField(updated, "Key");
							}	
							newExpr = CreateKDH(updated, mi);
						}

						break;
					}
				case ExpressionType.New:
					{
						var ne = (NewExpression)expr;
						Expression? updated = null;
						for (int i = 0; i < ne.Arguments.Count; i++)
						{
							var arg    = ne.Arguments[i];
							var newArg = ApplyReMapping(arg, replaceInfo, false);
							if (arg != newArg)
							{
								updated = newArg;
								break;
							}
						}

						if (updated != null)
						{
							if (typeof(KDH<,>).IsSameOrParentOf(updated.Type))
							{
								updated = updated.NodeType == ExpressionType.MemberInit
									? ((MemberAssignment)((MemberInitExpression)updated).Bindings[0]).Expression
									: ExpressionHelper.PropertyOrField(updated, "Key");
							}
							else if (IsEnumerableType(updated.Type, replaceInfo.MappingSchema))
							{
								var elementType = GetEnumerableElementType(updated.Type, replaceInfo.MappingSchema);
								if (typeof(KDH<,>).IsSameOrParentOf(elementType))
								{
									var p          = Expression.Parameter(elementType, "t");
									var body       = ExpressionHelper.PropertyOrField(p, "Key");
									var methodInfo = (typeof(IQueryable<>).IsSameOrParentOf(
										updated.Type)
										? Methods.Queryable.Select
										: Methods.Enumerable.Select).MakeGenericMethod(p.Type, body.Type);
									updated = Expression.Call(methodInfo, updated, Expression.Lambda(body, p));
								}
								else
									throw new NotImplementedException();
							};
							newExpr = CreateKDH(updated, ne);
						}

						break;
					}
				default:
					{
						if (expr is UnaryExpression unary)
						{
							var newArg = ApplyReMapping(unary.Operand, replaceInfo, isQueryable);
							if (newArg != unary.Operand && newArg.NodeType == ExpressionType.Lambda)
							{
								newArg = CorrectLambdaType((LambdaExpression)unary.Operand, (LambdaExpression)newArg, replaceInfo.MappingSchema);
							}

							newExpr = unary.Update(newArg);
						}
						
						break;
					}
			}

			return newExpr;
		}

	}
}
