using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	partial class ExpressionBuilder
	{
		public bool IsAssociation(Expression expression)
		{
			if (expression is MemberExpression memberExpression)
			{
				return memberExpression.IsAssociation(MappingSchema);
			}

			if (expression is MethodCallExpression methodCall)
			{
				return methodCall.IsAssociation(MappingSchema);
			}

			return false;
		}

		AssociationDescriptor? GetAssociationDescriptor(Expression expression, out AccessorMember? memberInfo, bool onlyCurrent = true)
		{
			memberInfo = null;

			Type objectType;
			if (expression is MemberExpression memberExpression)
			{
				if (!memberExpression.IsAssociation(MappingSchema))
					return null;

				var type = memberExpression.Member.ReflectedType ?? memberExpression.Member.DeclaringType;
				if (type == null)
					return null;
				objectType = type;
			}
			else if (expression is MethodCallExpression methodCall)
			{
				if (!methodCall.IsAssociation(MappingSchema))
					return null;

				var type = methodCall.Method.IsStatic ? methodCall.Arguments[0].Type : methodCall.Method.DeclaringType;
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
		HashSet<Expression>?                 _isOuterAssociations;

		public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext, IBuildContext? forContext, ProjectFlags flags)
		{
			if (!IsAssociation(expression))
				return expression;

			var associationDescriptor = GetAssociationDescriptor(expression, out var memberInfo);

			if (associationDescriptor == null || memberInfo == null)
				return expression;

			var associationRoot = (ContextRefExpression)MakeExpression(rootContext.BuildContext, rootContext, flags.AssociationRootFlag());

			_associations ??= new Dictionary<SqlCacheKey, Expression>(SqlCacheKey.SqlCacheKeyComparer);

			var key = new SqlCacheKey(expression, associationRoot.BuildContext, null, null, flags.RootFlag() & ~ProjectFlags.Subquery);

			if (_associations.TryGetValue(key, out var associationExpression))
				return associationExpression;

			LoadWithInfo? loadWith     = null;
			MemberInfo[]? loadWithPath = null;

			if (rootContext.BuildContext is ITableContext table)
			{
				loadWith     = table.LoadWithRoot;
				loadWithPath = table.LoadWithPath;
			}

			bool? isOuter = flags.HasFlag(ProjectFlags.ForceOuterAssociation) ? true : null;

			var prevIsOuter = _isOuterAssociations?.Contains(rootContext) == true;

			if (associationDescriptor.IsList)
			{
				/*if (_isOuterAssociations?.Contains(rootContext) == true)
					isOuter = true;*/
			}
			else
			{
				isOuter = isOuter == true || associationDescriptor.CanBeNull || prevIsOuter;
			}

			if (forContext != null)
			{
				rootContext = (ContextRefExpression)SequenceHelper.MoveToScopedContext(rootContext, forContext);
			}

			Expression? notNullCheck = null;
			if (associationDescriptor.IsList && (prevIsOuter || flags.IsSubquery()))
			{
				var keys = MakeExpression(forContext, rootContext, flags.KeyFlag());
				notNullCheck = ExtractNotNullCheck(keys);
			}

			var association = AssociationHelper.BuildAssociationQuery(this, rootContext, memberInfo,
				associationDescriptor, notNullCheck, !associationDescriptor.IsList, loadWith, loadWithPath, ref isOuter);

			associationExpression = association;

			if (!associationDescriptor.IsList && !flags.IsExpand() && !flags.IsSubquery())
			{
				// IsAssociation will force to create OuterApply instead of subquery. Handled in FirstSingleContext
				//
				var buildInfo = new BuildInfo(forContext, association, new SelectQuery())
				{
					IsTest = flags.IsTest(),
					IsAssociation = true
				};

				var sequence = BuildSequence(buildInfo);

				sequence.SetAlias(associationDescriptor.GenerateAlias());

				associationExpression = new ContextRefExpression(association.Type, sequence);

				if (!flags.IsTest() && isOuter == true)
				{
					var root = MakeExpression(rootContext.BuildContext, associationExpression, flags.AssociationRootFlag());
					_isOuterAssociations ??= new HashSet<Expression>(ExpressionEqualityComparer.Instance);
					_isOuterAssociations.Add(root);
				}
			}
			else
			{
				if (associationExpression.Type != expression.Type)
					associationExpression =
						new SqlAdjustTypeExpression(associationExpression, expression.Type, MappingSchema);
			}

			if (!flags.IsExpand())
				_associations[key] = associationExpression;

			return associationExpression;
		}

		Expression? ExtractNotNullCheck(Expression expr)
		{
			SqlPlaceholderExpression? notNull = null;

			if (expr is SqlPlaceholderExpression placeholder)
			{
				notNull = placeholder.MakeNullable();
			}

			if (notNull == null)
			{
				var placeholders = CollectDistinctPlaceholders(expr);

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

			var elementType = GetEnumerableElementType(desiredType);

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
				if (!typeof(IQueryable<>).IsSameOrParentOf(result.Type))
				{
					result = Expression.Call(Methods.Enumerable.AsQueryable.MakeGenericMethod(elementType),
						expression);
				}

				if (typeof(ITable<>).IsSameOrParentOf(desiredType))
				{
					var tableType = typeof(PersistentTable<>).MakeGenericType(elementType);
					result = Expression.New(tableType.GetConstructor(new[] { result.Type })!,
						result);
				}
			}

			return result;
		}

	}
}
