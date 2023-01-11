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
		HashSet<Expression>?                 _isOuterAssociations;

		public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext, ProjectFlags flags)
		{
			if (!IsAssociation(expression))
				return expression;

			var associationRoot = (ContextRefExpression)MakeExpression(rootContext.BuildContext, rootContext, flags.AssociationRootFlag());

			_associations ??= new Dictionary<SqlCacheKey, Expression>(SqlCacheKey.SqlCacheKeyComparer);

			var key = new SqlCacheKey(expression, associationRoot.BuildContext, null, null, flags.RootFlag());

			if (_associations.TryGetValue(key, out var associationExpression))
				return associationExpression;

			AccessorMember? memberInfo;
			var associationDescriptor = GetAssociationDescriptor(expression, out memberInfo);
			if (associationDescriptor == null || memberInfo == null)
				return expression;

			var loadWith = GetLoadWith(rootContext.BuildContext);

			bool isOuter = flags.HasFlag(ProjectFlags.ForceOuterAssociation);

			if (associationDescriptor.IsList)
			{
				/*if (_isOuterAssociations?.Contains(rootContext) == true)
					isOuter = true;*/
			}
			else
			{
				isOuter = isOuter || associationDescriptor.CanBeNull || _isOuterAssociations?.Contains(rootContext) == true;
			}

			var association = AssociationHelper.BuildAssociationQuery(this, rootContext, memberInfo, associationDescriptor, !associationDescriptor.IsList, loadWith, ref isOuter);

			associationExpression = association;

			if (!associationDescriptor.IsList)
			{
				// IsAssociation will force to create OuterApply instead of subquery. Handled in FirstSingleContext
				//
				var buildInfo = new BuildInfo(rootContext.BuildContext, association, new SelectQuery())
				{
					IsTest = flags.IsTest(),
					IsAssociation = true
				};

				var sequence = BuildSequence(buildInfo);

				sequence.SetAlias(associationDescriptor.GenerateAlias());

				associationExpression = new ContextRefExpression(association.Type, sequence);

				if (!flags.IsTest() && isOuter)
				{
					var root = MakeExpression(rootContext.BuildContext, associationExpression, flags.AssociationRootFlag());
					_isOuterAssociations ??= new HashSet<Expression>(ExpressionEqualityComparer.Instance);
					_isOuterAssociations.Add(root);
				}
			}
			else
			{
				if (associationExpression.Type != expression.Type)
					associationExpression = new SqlAdjustTypeExpression(associationExpression, expression.Type, MappingSchema);
			}

			_associations[key] = associationExpression;

			return associationExpression;
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
