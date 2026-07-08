using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

using static LinqToDB.Internal.Linq.Builder.EntityConstructorBase;

namespace LinqToDB.Internal.Linq.Builder
{
	internal partial class ExpressionBuilder
	{
		EntityConstructor? _entityConstructor;

		internal sealed class EntityConstructor : EntityConstructorBase
		{
			public ExpressionBuilder Builder { get; }

			public EntityConstructor(ExpressionBuilder builder)
			{
				Builder = builder;
			}

			public override LoadWithEntity? GetTableLoadWith(Expression path)
			{
				var unwrapped = path.UnwrapConvert();
				var table     = SequenceHelper.GetTableOrCteContext(Builder, unwrapped);

				return table?.LoadWithRoot;
			}

			protected override Expression ExposeCalculatedColumn(Expression memberAccess)
				=> Builder.ConvertExpressionTree(memberAccess);

			protected override bool ShouldBuildCalculatedColumns(Expression constructionRoot, ProjectFlags flags, FullEntityPurpose purpose)
			{
				// Calculated columns are expanded only for table-backed full-entity materialization, matching the
				// NotifyEntityCreated boundary in TryConstructFullEntity. Keys projections and non-Default purposes
				// (Insert/Update) don't materialize calculated columns.
				if (purpose != FullEntityPurpose.Default)
					return false;

				if (flags.HasFlag(ProjectFlags.Keys))
					return false;

				return SequenceHelper.GetTableContext(Builder, constructionRoot) != null;
			}

			public override Expression? TryConstructFullEntity(SqlGenericConstructorExpression constructorExpression, Type constructType, ProjectFlags flags, bool checkInheritance, out string? error)
			{
				var constructed = base.TryConstructFullEntity(constructorExpression, constructType, flags, checkInheritance, out error);

				if (constructed != null)
				{
					if (constructorExpression.ConstructionRoot != null)
					{
						var tableContext = SequenceHelper.GetTableContext(Builder, constructorExpression.ConstructionRoot); 
						if (tableContext != null)
							constructed = NotifyEntityCreated(constructed, tableContext.SqlTable);
					}
				}

				return constructed;
			}
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(MappingSchema mappingSchema, Expression refExpression, Type entityType, ProjectFlags flags, FullEntityPurpose purpose = FullEntityPurpose.Default)
		{
			_entityConstructor ??= new EntityConstructor(this);

			var generic = _entityConstructor.BuildFullEntityExpression(DataContext, mappingSchema, refExpression, entityType, flags, purpose);

			return generic;
		}

		public SqlGenericConstructorExpression BuildEntityExpression(MappingSchema mappingSchema,
			Expression refExpression, Type entityType, IReadOnlyCollection<MemberInfo> members)
		{
			_entityConstructor ??= new EntityConstructor(this);

			var generic = _entityConstructor.BuildEntityExpression(DataContext, mappingSchema, refExpression, entityType, members);

			return generic;
		}

		public Expression? TryConstruct(MappingSchema mappingSchema, SqlGenericConstructorExpression constructorExpression, ProjectFlags flags)
		{
			mappingSchema      =   constructorExpression.MappingSchema ?? mappingSchema;
			_entityConstructor ??= new EntityConstructor(this);

			var result = _entityConstructor.TryConstruct(DataContext, mappingSchema, constructorExpression, flags, out var error);

			return result;
		}

		public Expression Construct(MappingSchema mappingSchema, SqlGenericConstructorExpression constructorExpression, ProjectFlags flags)
		{
			mappingSchema =   constructorExpression.MappingSchema ?? mappingSchema;
			_entityConstructor ??= new EntityConstructor(this);

			var constructed = _entityConstructor.Construct(DataContext, mappingSchema, constructorExpression, flags);

			return constructed;
		}

		public SqlGenericConstructorExpression RemapToNewPath(Expression prefixPath, SqlGenericConstructorExpression constructorExpression, Expression currentPath)
		{
			//TODO: only assignments
			var newAssignments = new List<SqlGenericConstructorExpression.Assignment>();

			foreach (var assignment in constructorExpression.Assignments)
			{
				Expression newAssignmentExpression;

				var memberAccess = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);

				if (assignment.Expression is SqlGenericConstructorExpression generic)
				{
					newAssignmentExpression = RemapToNewPath(prefixPath, generic, memberAccess);
				}
				else
				{
					newAssignmentExpression = memberAccess;
				}

				newAssignments.Add(new SqlGenericConstructorExpression.Assignment(assignment.MemberInfo,
					newAssignmentExpression, assignment.IsMandatory, assignment.IsLoaded));
			}

			return new SqlGenericConstructorExpression(SqlGenericConstructorExpression.CreateType.Auto,
				constructorExpression.ObjectType, null, newAssignments.AsReadOnly(), constructorExpression.MappingSchema, currentPath);
		}
	}
}
