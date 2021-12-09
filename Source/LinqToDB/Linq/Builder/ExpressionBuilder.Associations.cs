using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	partial class ExpressionBuilder
	{
		Dictionary<AccessorMember, Tuple<IBuildContext, bool>>? _associationContexts;
		Dictionary<AccessorMember, IBuildContext>?              _collectionAssociationContexts;

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
					accessorMember.MemberInfo.DeclaringType!, accessorMember.MemberInfo,
					static a => a.Configuration);

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

		public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext)
		{
			if (!IsAssociation(expression))
				return expression;

			_associations ??= new Dictionary<SqlCacheKey, Expression>(SqlCacheKey.SqlCacheKeyComparer);

			var key = new SqlCacheKey(expression, rootContext.BuildContext, null, ProjectFlags.Root);

			if (_associations.TryGetValue(key, out var associationExpression))
				return associationExpression;

			AccessorMember? memberInfo;
			var associationDescriptor = GetAssociationDescriptor(expression, out memberInfo);
			if (associationDescriptor == null || memberInfo == null)
				return expression;

			if (associationDescriptor.IsList)
			{
				var collectionAssociation = CreateCollectionAssociation(expression, rootContext);

				_associations[key] = collectionAssociation;

				// such associations are processed by ContextRefBuilder
				return collectionAssociation;
			}

			var buildInfo = new BuildInfo(rootContext.BuildContext, expression, rootContext.BuildContext.SelectQuery);
			var isOuter   = associationDescriptor.CanBeNull;

			var association = AssociationHelper.BuildAssociationInline(this, buildInfo, rootContext.BuildContext, memberInfo,
				associationDescriptor, true, ref isOuter);

			associationExpression = new ContextRefExpression(expression.Type, association);

			_associations[key] = associationExpression;

			return associationExpression;
		}

		public Expression CreateCollectionAssociation(Expression expression, ContextRefExpression rootContext)
		{
			if (!IsAssociation(expression))
				return expression;

			AccessorMember? memberInfo;

			var associationDescriptor = GetAssociationDescriptor(expression, out memberInfo);
			if (associationDescriptor == null || memberInfo == null)
				return expression;

			if (!associationDescriptor.IsList)
			{
				throw new InvalidOperationException();
			}

			var elementType     = associationDescriptor.GetElementType(MappingSchema);
			var parentExactType = associationDescriptor.GetParentElementType();

			var queryMethod = AssociationHelper.CreateAssociationQueryLambda(
				this, memberInfo, associationDescriptor, elementType/*OriginalType*/, parentExactType, elementType,
				false, false, null/*GetLoadWith()*/, out _);
			;
			var expr = queryMethod.GetBody(rootContext);

			return expr;

			//return associationExpression;
		}

	}
}
