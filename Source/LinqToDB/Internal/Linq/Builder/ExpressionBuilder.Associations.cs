﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		public Expression AssociationToRealization(Expression associationExpression)
		{
			// TODO: in case of https://github.com/linq2db/linq2db/issues/4139 implemented, we probably will need
			// to update this logic with additional tests for https://github.com/linq2db/linq2db/issues/4790
			if (associationExpression is MemberExpression
				{
					Expression: ContextRefExpression
					{
						Type.IsInterface: true,
						BuildContext.ElementType: var elementType,
					} contextRef,
					Member: var member
				}
				&& elementType != contextRef.Type)
			{
				var newMember = elementType.GetMemberEx(member);
				if (newMember != null)
				{
					if (InternalExtensions.IsAssociation(newMember, MappingSchema))
					{
						return Expression.MakeMemberAccess(
							contextRef.WithType(elementType),
							newMember);
					}
				}
			}

			return associationExpression;
		}

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

			if (expression != null && member.ReflectedType != expression.Type)
			{
				var newMember = expression.Type.GetMemberEx(member);
				if (newMember != null)
					return IsAssociationInRealization(null, newMember, out associationMember);
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

		public AssociationDescriptor? GetAssociationDescriptor(Expression expression, out AccessorMember? memberInfo, bool onlyCurrent = true)
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

			var memberInfo = accessorMember.MemberInfo;

			if (memberInfo is { MemberType: MemberTypes.Method, DeclaringType: not null })
			{
				var attribute = MappingSchema.GetAttribute<AssociationAttribute>(memberInfo.DeclaringType!, memberInfo);

				if (attribute == null && memberInfo.DeclaringType?.IsInterface == true && !entityDescriptor.ObjectType.IsInterface)
				{
					var newInfo = entityDescriptor.ObjectType.GetImplementation(memberInfo);
					if (newInfo != null)
					{
						attribute  = MappingSchema.GetAttribute<AssociationAttribute>(newInfo.DeclaringType!, newInfo);
						memberInfo = newInfo;
					}
				}

				if (attribute != null)
					descriptor = new AssociationDescriptor
					(
						MappingSchema,
						entityDescriptor.ObjectType,
						memberInfo,
						attribute.GetThisKeys(),
						attribute.GetOtherKeys(),
						attribute.ExpressionPredicate,
						attribute.Predicate,
						attribute.QueryExpressionMethod,
						attribute.QueryExpression,
						attribute.Storage,
						attribute.AssociationSetterExpressionMethod,
						attribute.AssociationSetterExpression,
						attribute.ConfiguredCanBeNull,
						attribute.AliasName
					);
			}
			else if (memberInfo.MemberType == MemberTypes.Property || memberInfo.MemberType == MemberTypes.Field)
			{
				foreach (var ed in entityDescriptor.Associations)
					if (ed.MemberInfo.EqualsTo(memberInfo))
						return ed;

				foreach (var m in entityDescriptor.InheritanceMapping)
					foreach (var ed in MappingSchema.GetEntityDescriptor(m.Type).Associations)
						if (ed.MemberInfo.EqualsTo(memberInfo))
							return ed;
			}

			return descriptor;
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
	}
}
