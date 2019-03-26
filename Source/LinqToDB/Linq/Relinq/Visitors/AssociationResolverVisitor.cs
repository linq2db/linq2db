using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Parser;
using LinqToDB.Linq.Relinq.Clauses;
using LinqToDB.Mapping;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace LinqToDB.Linq.Relinq.Visitors
{

	class AssociationResolverVisitor : ExtendedQueryModelVisitorBase
	{
		public ParsingContext ParsingContext { get; }

		[JetBrains.Annotations.NotNull]
		public IDataContext DataContext { get; }

		// used for registering associations mapping
		
		public ExpressionMapper Mapper { get; } = new ExpressionMapper();

		public MappingSchema MappingSchema => DataContext.MappingSchema;

		public int NestingLevel { get; private set; }

		public AssociationResolverVisitor([JetBrains.Annotations.NotNull] ParsingContext parsingContext,
			[JetBrains.Annotations.NotNull] IDataContext dataContext,
			[JetBrains.Annotations.NotNull] ExpressionMapper mapper)
		{
			ParsingContext = parsingContext ?? throw new ArgumentNullException(nameof(parsingContext));
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
//			Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
		}

		static AssociationAttribute GetAssociationAttribute(MappingSchema mappingSchema, MemberInfo member)
		{
			return mappingSchema.GetAttribute<AssociationAttribute>(member.ReflectedTypeEx(), member, a => a.Configuration);
		}

		private void GenerateFromAssociation(
			MappingSchema mappingSchema,
			MainFromClause fromClause, 
			Expression objectExpression,
			MemberInfo associationMemberInfo, 
			AssociationAttribute associationAttribute, 
			QueryModel queryModel,
			string itemName,
			int index)
		{
			var generatedClause = CreateAssociationClause(true, queryModel, mappingSchema, objectExpression, associationAttribute,  associationMemberInfo, itemName, false);

			var innerMapper = new ExpressionMapper();

			if (generatedClause is IQuerySource qs)
			{
				var fromReference = new QuerySourceReferenceExpression(fromClause);
				var reference = new QuerySourceReferenceExpression(qs);
				innerMapper.RegisterMapping(fromReference, reference );
			}

			innerMapper.Resolve(queryModel);

			if (objectExpression is MemberExpression)
			{
				var enforceInnerJoin = false;
				if (generatedClause is ExtendedJoinClause joinClause)
					enforceInnerJoin = joinClause.JoinType == SqlJoinType.Inner;

				EnsureAssociations((IBodyClause)generatedClause, queryModel, index, enforceInnerJoin);
			}
		}

		private void GenerateAssociation(
			MappingSchema mappingSchema,
			AdditionalFromClause fromClause, 
			Expression objectExpression,
			MemberInfo associationMemberInfo, 
			AssociationAttribute associationAttribute, 
			QueryModel queryModel,
			string itemName,
			int index)
		{
			var generatedClause = CreateAssociationClause(false, queryModel, mappingSchema, objectExpression, associationAttribute,  associationMemberInfo, itemName, true);

			var innerMapper = new ExpressionMapper();

			if (generatedClause is IQuerySource qs)
			{
				var fromReference = new QuerySourceReferenceExpression(fromClause);
				var reference = new QuerySourceReferenceExpression(qs);
				innerMapper.RegisterMapping(fromReference, reference );
			}

			//TODO: result operator
			queryModel.BodyClauses[index] = (IBodyClause)generatedClause;

			innerMapper.Resolve(queryModel);

			if (objectExpression is MemberExpression)
			{
				var enforceInnerJoin = false;
				if (generatedClause is ExtendedJoinClause joinClause)
					enforceInnerJoin = joinClause.JoinType == SqlJoinType.Left;

				EnsureAssociations((IBodyClause)generatedClause, queryModel, index, enforceInnerJoin);
			}
		}

		private static IQuerySource GenerateInnerAssociation(MappingSchema mappingSchema, Expression objectExpression, 
			MemberInfo associationMemberInfo, 
			AssociationAttribute associationAttribute, 
			QueryModel queryModel,
			int index,
			bool enforceInnerJoin)
		{

			var itemName = associationAttribute.AliasName ??
			                (Common.Configuration.Sql.AssociationAlias.IsNullOrEmpty()
				                ? "t"
				                : string.Format(Common.Configuration.Sql.AssociationAlias,
					                associationMemberInfo.Name));

			var generatedClause = CreateAssociationClause(false, queryModel, mappingSchema, objectExpression, associationAttribute,  associationMemberInfo, itemName, enforceInnerJoin);

			//TODO: result operator
			queryModel.BodyClauses.Insert(index < 0 ? queryModel.BodyClauses.Count : index, (IBodyClause)generatedClause);

			return generatedClause;
		}

		private static IQuerySource CreateAssociationClause(bool isMain, QueryModel queryModel, MappingSchema mappingSchema, Expression objectExpression,
			AssociationAttribute associationAttribute,
			MemberInfo associationMemberInfo, string itemName, bool enforceInnerJoin)
		{
			var descriptor = new AssociationDescriptor(
				objectExpression.Type,
				associationMemberInfo,
				associationAttribute.GetThisKeys(),
				associationAttribute.GetOtherKeys(),
				associationAttribute.ExpressionPredicate,
				associationAttribute.Predicate,
				associationAttribute.QueryExpressionMethod,
				associationAttribute.QueryExpression,
				associationAttribute.Storage,
				associationAttribute.CanBeNull,
				associationAttribute.AliasName
			);

			var parentType = objectExpression.Type;

			var childType = associationMemberInfo.GetMemberType();
			if (typeof(IEnumerable<>).IsSameOrParentOf(childType))
				childType = childType.GetGenericArgumentsEx()[0];

			var queryMethod = descriptor.GetQueryMethod(parentType, childType);
			if (queryMethod != null)
				throw new NotImplementedException();

			Expression predicate = null;

			var table = Expression.Call(null, ReflectionMethods.GetTable.MakeGenericMethod(childType),
				ExpressionPredefines.DataContextParam);

			var mainFromClause = new MainFromClause(itemName, childType, table);
			var childSourceReference =
				new QuerySourceReferenceExpression(mainFromClause);

			if (descriptor.ThisKey.Length > 0)
			{
				predicate = descriptor.ThisKey.Select(k => Expression.PropertyOrField(objectExpression, k))
					.Zip(
						descriptor.OtherKey.Select(k => Expression.PropertyOrField(childSourceReference, k)),
						(m, c) => ExpressionGeneratorHelper.Equal(mappingSchema, m, c)
					).Aggregate(Expression.AndAlso);
			}

			var customPredicateLambda = descriptor.GetPredicate(parentType, childType);
			if (customPredicateLambda != null)
			{
				var customPredicate = customPredicateLambda.GetBody(objectExpression, childSourceReference);
				predicate = predicate == null ? customPredicate : Expression.AndAlso(predicate, customPredicate);
			}

			if (predicate == null)
				throw new InvalidOperationException(
					$"Association {parentType.Name}.{associationMemberInfo.Name} improperly defined");

			if (isMain)
			{
				queryModel.MainFromClause = mainFromClause;
				queryModel.BodyClauses.Add(new WhereClause(predicate));
				return mainFromClause;
			}
			
			var joinClause = new ExtendedJoinClause(itemName, childType, table, predicate,
				 descriptor.CanBeNull && !enforceInnerJoin ? SqlJoinType.Left : SqlJoinType.Inner);

			var joinReferenceExpression = new QuerySourceReferenceExpression(joinClause);
			var internalMapper = new ExpressionMapper();
			internalMapper.RegisterMapping(childSourceReference, joinReferenceExpression);
			internalMapper.Resolve(joinClause);

			return joinClause;
		}

		public override void VisitQueryModel(QueryModel queryModel)
		{
			Mapper.Resolve(queryModel);

			base.VisitQueryModel(queryModel);
		}

		public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
		{
			switch (fromClause.FromExpression.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var memberAccess = (MemberExpression)fromClause.FromExpression;
						var attr = GetAssociationAttribute(MappingSchema, memberAccess.Member);
						if (attr == null)
							throw new LinqToDBException($"Expression '{fromClause.FromExpression}' can not be converted to SQL");

						// It generates association only for last member access, 
						// from p in db.Parent
						// from c1 in p.Children         - usual situation
						// from c2 in c1.Parent.Children - it will be replaced with 
						//
						// from p in db.Parent
						// from c1 in p.Children         - ignored in sample     
						// join c2 in db.Children on (c1.Parent.Id == a_Children.ParentId)
						//
						// so we have to process search conditions separately
						GenerateFromAssociation(MappingSchema, fromClause, memberAccess.Expression, memberAccess.Member, attr, queryModel, fromClause.ItemName, -1);

						// revisit model
						VisitQueryModel(queryModel);

						return;
					}
			}
		}

		public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
		{
			switch (fromClause.FromExpression.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var memberAccess = (MemberExpression)fromClause.FromExpression;
						var attr = GetAssociationAttribute(MappingSchema, memberAccess.Member);
						if (attr == null)
							throw new LinqToDBException($"Expression '{fromClause.FromExpression}' can not be converted to SQL");

						// It generates association only for last member access, 
						// from p in db.Parent
						// from c1 in p.Children         - usual situation
						// from c2 in c1.Parent.Children - it will be replaced with 
						//
						// from p in db.Parent
						// from c1 in p.Children         - ignored in sample     
						// join c2 in db.Children on (c1.Parent.Id == a_Children.ParentId)
						//
						// so we have to process search conditions separately
						GenerateAssociation(MappingSchema, fromClause, memberAccess.Expression, memberAccess.Member, attr, queryModel, fromClause.ItemName, index);

						// revisit model
						VisitQueryModel(queryModel);

						return;
					}
			}

			base.VisitAdditionalFromClause(fromClause, queryModel, index);
		}


		private Expression ResolveAssociations(QueryModel queryModel, MappingSchema mappingSchema,
			Expression expression, int index, bool enforceInnerJoin)
		{
			var result = expression.Transform(e =>
			{
				if (e is MemberExpression memberAccess)
				{
					if (Mapper.HasRegistration(memberAccess, out var transformedTo))
						return transformedTo;

					var attr = GetAssociationAttribute(mappingSchema, memberAccess.Member);
					if (attr != null)
					{
						if (memberAccess.Expression.NodeType == ExpressionType.MemberInit)
							throw new LinqToDBException($"Source '{memberAccess.Expression}' can not be used for associations");

						var associatedTo = GenerateInnerAssociation(mappingSchema, memberAccess.Expression, memberAccess.Member, attr,
							queryModel, index, enforceInnerJoin);
						e = new QuerySourceReferenceExpression(associatedTo);
						Mapper.RegisterMapping(memberAccess, e);
					}
				}

				if (e is SubQueryExpression subQuery)
				{
					++NestingLevel;
					VisitQueryModel(subQuery.QueryModel);
					--NestingLevel;
				}

				return e;
			});

			return result;
		}

		public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
		{
			if (NestingLevel == 0) 
				EnsureAssociations(selectClause.Selector, queryModel, MappingSchema);

			Mapper.Resolve(selectClause);

			base.VisitSelectClause(selectClause, queryModel);
		}

		public override void VisitJoinClause(ExtendedJoinClause joinClause, QueryModel queryModel, int index)
		{
			EnsureAssociations(joinClause, queryModel, index);
			base.VisitJoinClause(joinClause, queryModel, index);
		}

		public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
		{
			EnsureAssociations(whereClause, queryModel, index);
			base.VisitWhereClause(whereClause, queryModel, index);
		}

		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
		{
			EnsureAssociations(joinClause, queryModel, index);
			base.VisitJoinClause(joinClause, queryModel, index);
		}

		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause)
		{
			EnsureAssociations(joinClause, queryModel, queryModel.BodyClauses.IndexOf(groupJoinClause));
			base.VisitJoinClause(joinClause, queryModel, groupJoinClause);
		}

		protected override void VisitOrderings(ObservableCollection<Ordering> orderings, QueryModel queryModel, OrderByClause orderByClause)
		{
			EnsureAssociations(orderByClause, queryModel, queryModel.BodyClauses.IndexOf(orderByClause));
			base.VisitOrderings(orderings, queryModel, orderByClause);
		}

		private void EnsureAssociations(IBodyClause bodyClause, QueryModel queryModel, int index, bool enforceInnerJoin = false)
		{
			bodyClause.TransformExpressions(e => ResolveAssociations(queryModel, MappingSchema, e, index, enforceInnerJoin));
		}

		public Expression EnsureAssociations(Expression expression, QueryModel queryModel, MappingSchema mappingSchema, bool enforceInnerJoin = false)
		{
			var result = expression.Transform(e => ResolveAssociations(queryModel, mappingSchema, e, -1, enforceInnerJoin));
			return result;
		}
	}
}
