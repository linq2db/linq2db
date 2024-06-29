using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	partial class ExpressionBuilder
	{
		bool IsAssociationInRealization(Expression? expression, MemberInfo member, [NotNullWhen(true)] out MemberInfo? associationMember)
		{
			if (InternalExtensions.IsAssociation(member, MappingSchema))
			{
				associationMember = member;
				return true;
			}

			if (expression?.Type.IsInterface == true)
			{
				if (expression is ContextRefExpression contextRef && contextRef.BuildContext.ElementType != expression.Type)
				{
					var newMember = contextRef.BuildContext.ElementType.GetMemberEx(member);
					if (newMember != null)
					{
						if (InternalExtensions.IsAssociation(newMember, MappingSchema))
						{
							associationMember = newMember;
							return true;
						}
					}
				}
			}

			associationMember = null;
			return false;
		}

		public bool IsAssociation(Expression expression, [NotNullWhen(true)] out MemberInfo? associationMember)
		{
			switch (expression)
			{
				case MemberExpression memberExpression:
					return IsAssociationInRealization(memberExpression.Expression, memberExpression.Member, out associationMember);

				case MethodCallExpression methodCall:
					return IsAssociationInRealization(methodCall.Object, methodCall.Method, out associationMember);

				default:
					associationMember = null;
					return false;
			}
		}

		AssociationDescriptor? GetAssociationDescriptor(Expression expression, out AccessorMember? memberInfo, bool onlyCurrent = true)
		{
			memberInfo = null;

			Type objectType;
			if (expression is MemberExpression memberExpression)
			{
				if (!IsAssociationInRealization(memberExpression.Expression, memberExpression.Member,
					    out var associationMember))
					return null;

				var type = associationMember.ReflectedType ?? associationMember.DeclaringType;
				if (type == null)
					return null;
				objectType = type;
			}
			else if (expression is MethodCallExpression methodCall)
			{
				if (!IsAssociationInRealization(methodCall.Object, methodCall.Method, out var associationMember))
					return null;

				var type = methodCall.Method.IsStatic ? methodCall.Arguments[0].Type : associationMember.DeclaringType;
				if (type == null)
					return null;
				objectType = type;
			}
			else
				return null;

			if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
				memberInfo = new AccessorMember(expression);

			if (memberInfo == null)
				return null;

			var entityDescriptor = MappingSchema.GetEntityDescriptor(objectType);

			var descriptor = GetAssociationDescriptor(memberInfo, entityDescriptor);
			if (descriptor == null && !onlyCurrent && memberInfo.MemberInfo.DeclaringType != entityDescriptor.ObjectType)
				descriptor = GetAssociationDescriptor(memberInfo, MappingSchema.GetEntityDescriptor(memberInfo.MemberInfo.DeclaringType!));

			return descriptor;
		}

		AssociationDescriptor? GetAssociationDescriptor(AccessorMember accessorMember, EntityDescriptor entityDescriptor)
		{
			AssociationDescriptor? descriptor = null;

			if (accessorMember.MemberInfo.MemberType == MemberTypes.Method)
			{
				var attribute = MappingSchema.GetAttribute<AssociationAttribute>(
					accessorMember.MemberInfo.DeclaringType!, accessorMember.MemberInfo);

				if (attribute != null)
					descriptor = new AssociationDescriptor
					(
						entityDescriptor.ObjectType,
						accessorMember.MemberInfo,
						attribute.GetThisKeys(),
						attribute.GetOtherKeys(),
						attribute.ExpressionPredicate,
						attribute.Predicate,
						attribute.QueryExpressionMethod,
						attribute.QueryExpression,
						attribute.Storage,
						attribute.AssociationSetterExpressionMethod,
						attribute.AssociationSetterExpression,
						attribute.CanBeNull,
						attribute.AliasName
					);
			}
			else if (accessorMember.MemberInfo.MemberType == MemberTypes.Property || accessorMember.MemberInfo.MemberType == MemberTypes.Field)
			{
				foreach (var ed in entityDescriptor.Associations)
					if (ed.MemberInfo.EqualsTo(accessorMember.MemberInfo))
						return ed;

				foreach (var m in entityDescriptor.InheritanceMapping)
					foreach (var ed in MappingSchema.GetEntityDescriptor(m.Type).Associations)
						if (ed.MemberInfo.EqualsTo(accessorMember.MemberInfo))
							return ed;
			}

			return descriptor;
		}

		Dictionary<SqlCacheKey, Expression>? _associations;

		public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext, IBuildContext? forContext, ProjectFlags flags)
		{
			var associationDescriptor = GetAssociationDescriptor(expression, out var memberInfo);

			if (associationDescriptor == null || memberInfo == null)
				return expression;

			var associationRoot = (ContextRefExpression)MakeExpression(rootContext.BuildContext, rootContext, flags.AssociationRootFlag());

			_associations ??= new Dictionary<SqlCacheKey, Expression>(SqlCacheKey.SqlCacheKeyComparer);

			var cacheFlags = flags.RootFlag() & ~(ProjectFlags.Subquery | ProjectFlags.ExtractProjection | ProjectFlags.ForceOuterAssociation);
			var key        = new SqlCacheKey(expression, associationRoot.BuildContext, null, null, cacheFlags);

			if (_associations.TryGetValue(key, out var associationExpression))
				return associationExpression;

			LoadWithInfo? loadWith     = null;
			MemberInfo[]? loadWithPath = null;

			var   prevIsOuter = flags.HasFlag(ProjectFlags.ForceOuterAssociation);
			bool? isOptional  = prevIsOuter ? true : null;

			if (rootContext.BuildContext.IsOptional)
				isOptional = true;

			var table = SequenceHelper.GetTableOrCteContext(rootContext.BuildContext);

			if (table != null)
			{
				loadWith     = table.LoadWithRoot;
				loadWithPath = table.LoadWithPath;
				if (table.IsOptional)
					isOptional = true;
			}

			if (forContext?.IsOptional == true)
				isOptional = true;

			if (associationDescriptor.IsList)
			{
				/*if (_isOuterAssociations?.Contains(rootContext) == true)
					isOuter = true;*/
			}
			else
			{
				isOptional = isOptional == true || associationDescriptor.CanBeNull;
			}

			Expression? notNullCheck = null;
			if (associationDescriptor.IsList && (prevIsOuter || flags.IsSubquery()) && !flags.IsExtractProjection())
			{
				var keys = MakeExpression(forContext, rootContext, flags.SqlFlag().KeyFlag());
				if (forContext != null)
				{
					notNullCheck = ExtractNotNullCheck(forContext, keys, flags.SqlFlag());
				}
			}

			var association = AssociationHelper.BuildAssociationQuery(this, rootContext, memberInfo,
				associationDescriptor, notNullCheck, !associationDescriptor.IsList, loadWith, loadWithPath, ref isOptional);

			associationExpression = association;

			if (!associationDescriptor.IsList && !flags.IsSubquery() && !flags.IsExtractProjection())
			{
				// IsAssociation will force to create OuterApply instead of subquery. Handled in FirstSingleContext
				//
				var buildInfo = new BuildInfo(forContext, association, new SelectQuery())
				{
					IsTest = flags.IsTest(),
					SourceCardinality = isOptional == true ? SourceCardinality.ZeroOrOne : SourceCardinality.One,
					IsAssociation = true
				};

				var sequence = BuildSequence(buildInfo);

				if (!flags.IsTest())
				{
					if (!IsSupportedSubquery(rootContext.BuildContext, sequence, out var errorMessage))
						return new SqlErrorExpression(null, expression, errorMessage, expression.Type, true);
				}

				sequence.SetAlias(associationDescriptor.GenerateAlias());

				if (forContext != null)
					sequence = new ScopeContext(sequence, forContext);

				associationExpression = new ContextRefExpression(association.Type, sequence);
			}
			else
			{
				associationExpression = SqlAdjustTypeExpression.AdjustType(associationExpression, expression.Type, MappingSchema);
			}

			if (!flags.IsExtractProjection())
				_associations[key] = associationExpression;

			return associationExpression;
		}

		Expression? ExtractNotNullCheck(IBuildContext context, Expression expr, ProjectFlags flags)
		{
			SqlPlaceholderExpression? notNull = null;

			if (expr is SqlPlaceholderExpression placeholder)
			{
				notNull = placeholder.MakeNullable();
			}

			if (notNull == null)
			{
				List<Expression> expressions = new();
				if (!CollectNullCompareExpressions(context, expr, expressions) || expressions.Count == 0)
					return null;

				List<SqlPlaceholderExpression> placeholders = new(expressions.Count);

				foreach (var expression in expressions)
				{
					var predicateExpr = ConvertToSqlExpr(context, expression, flags.SqlFlag());
					if (predicateExpr is SqlPlaceholderExpression current)
					{
						placeholders.Add(current);
					}
				}

				notNull = placeholders
					.FirstOrDefault(pl => !pl.Sql.CanBeNullable(NullabilityContext.NonQuery));
			}

			if (notNull == null)
			{
				return null;
			}

			var notNullPath = notNull.Path;

			if (notNullPath.Type.IsValueType && !notNullPath.Type.IsNullable())
			{
				notNullPath = Expression.Convert(notNullPath, typeof(Nullable<>).MakeGenericType(notNullPath.Type));
			}

			var notNullExpression = Expression.NotEqual(notNullPath, Expression.Constant(null, notNullPath.Type));

			return notNullExpression;

		}

		public static Expression AdjustType(Expression expression, Type desiredType, MappingSchema mappingSchema)
		{
			if (desiredType.IsSameOrParentOf(expression.Type))
				return expression;

			if (typeof(IGrouping<,>).IsSameOrParentOf(desiredType))
			{
				if (typeof(IEnumerable<>).IsSameOrParentOf(expression.Type))
					return expression;
			}

			var elementType = TypeHelper.GetEnumerableElementType(desiredType);

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
					result = convertExpr.GetBody(expression);
			}

			if (result == null)
			{
				result = expression;
				if (result.Type == typeof(object))
				{
					result = Expression.Convert(result, typeof(IEnumerable<>).MakeGenericType(elementType));
				}

				if (!typeof(IQueryable<>).IsSameOrParentOf(result.Type))
				{
					result = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType),
						result);
				}

				if (typeof(ITable<>).IsSameOrParentOf(desiredType))
				{
					var tableType = typeof(PersistentTable<>).MakeGenericType(elementType);
					result = Expression.New(tableType.GetConstructor(new[] { result.Type })!,
						result);
				}

				if (result.Type != desiredType)
					result = Expression.Convert(result, desiredType);

			}

			return result;
		}

		public bool GetAssociationTransformation(IBuildContext buildContext, [NotNullWhen(true)] out Expression? transformation)
		{
			transformation = null;
			if (_associations == null)
				return false;

			foreach (var pair in _associations)
			{
				if (pair.Value is ContextRefExpression contextRef && contextRef.BuildContext == buildContext)
				{
					transformation = pair.Key.Expression!;
					return true;
				}
			}

			return false;
		}
	}
}
