using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Builder
{
	partial class ExpressionBuilder
	{
		Dictionary<AccessorMember, Tuple<IBuildContext, bool>>? _associationContexts;
		Dictionary<AccessorMember, IBuildContext>?              _collectionAssociationContexts;

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
	
	}
}
