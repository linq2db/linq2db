using System;
using System.Collections.Generic;
using System.Diagnostics;
using LinqToDB.CodeModel;
using LinqToDB.Common;
using LinqToDB.DataModel;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Provides attribute-based implementation of data model metadata generator.
	/// General rule used for generation here is that we skip property setters generation when they will set
	/// attribute property to it's default value when default value is static.
	/// Which means this rule is not applied to names that depend on mapped member name, which could be changed due to rename.
	/// E.g. table name derived from class name, or column name derived from property name.
	/// </summary>
	internal sealed class FluentMetadataBuilder : IMetadataBuilder
	{
		private const string BUILDER_VAR_NAME = "builder";

		private readonly CodeVariable                                                                                                  _builderVar;
		private readonly List<ICodeStatement>                                                                                          _builderCalls = new();
		// [T, (Attr, [Member, Attr])]
		private readonly Dictionary<CodeClass, (ICodeExpression? tableAttribute, Dictionary<CodeReference, ICodeExpression?> members)> _entities     = new();

		public FluentMetadataBuilder(CodeBuilder builder)
		{
			_builderVar = builder.Variable(builder.Name(BUILDER_VAR_NAME), WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder, true);
		}

		void IMetadataBuilder.BuildEntityMetadata(IDataModelGenerationContext context, EntityModel entity)
		{
			var metadata      = entity.Metadata;
			var entityBuilder = context.GetEntityBuilder(entity);

			ICodeExpression[] parameters;
			if (metadata.Name != null)
			{
				// always generate table name when it is provided, even if it match class name
				// otherwise generated code will be refactoring-unfriendly (will break on class rename)
				// note that rename could happen not only as user's action in generated code, but also during code
				// generation to resolve naming conflicts with other members/types
				parameters = new ICodeExpression[] { context.AST.Constant(metadata.Name.Value.Name, true) };
			}
			else
				parameters = [];

			List<CodeAssignmentStatement>? initializers = null;

			if (metadata.Name != null)
			{
				if (metadata.Name.Value.Schema   != null) (initializers   = new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Schema  , context.AST.Constant(metadata.Name.Value.Schema  , true)));
				if (metadata.Name.Value.Database != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Database, context.AST.Constant(metadata.Name.Value.Database, true)));
				if (metadata.Name.Value.Server   != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Server  , context.AST.Constant(metadata.Name.Value.Server  , true)));
			}

			if (metadata.IsView                              ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsView                   , context.AST.Constant(true                  , true)));
			if (metadata.Configuration != null               ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration          , context.AST.Constant(metadata.Configuration, true)));
			if (!metadata.IsColumnAttributeRequired          ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsColumnAttributeRequired, context.AST.Constant(false                 , true)));
			if (metadata.IsTemporary                         ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsTemporary              , context.AST.Constant(true                  , true)));
			if (metadata.TableOptions  != TableOptions.NotSet) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.TableAttribute_TableOptions             , context.AST.Constant(metadata.TableOptions , true)));

			var attr = context.AST.New(WellKnownTypes.LinqToDB.Mapping.TableAttribute, parameters, initializers?.ToArray() ?? []);

			_entities.Add(entityBuilder.Type, (attr, new Dictionary<CodeReference, ICodeExpression?>()));
		}

		void IMetadataBuilder.BuildColumnMetadata(IDataModelGenerationContext context, CodeClass entityClass, ColumnMetadata metadata, PropertyBuilder propertyBuilder)
		{
			if (!_entities.TryGetValue(entityClass, out var entity))
			{
				_entities.Add(entityClass, entity = (null, new Dictionary<CodeReference, ICodeExpression?>()));
			}

			var members = entity.members;

			//.IsNotColumn()
			if (!metadata.IsColumn)
			{
				members.Add(propertyBuilder.Property.Reference, null);
				return;
			}

			ICodeExpression[] parameters;
			if (metadata.Name != null)
			{
				// always generate column name when it is provided, even if it match property name
				// otherwise generated code will be refactoring-unfriendly (will break on property rename)
				// note that rename could happen not only as user's action in generated code, but also during code
				// generation to resolve naming conflicts with other members/types
				parameters = new ICodeExpression[] { context.AST.Constant(metadata.Name, true) };
			}
			else
				parameters = [];

			List<CodeAssignmentStatement>? initializers = null;

			// generate only "CanBeNull = false" only for non-default cases (where linq2db already infer nullability from type):
			// - for reference type is is true by default
			// - for value type it is false
			// - for nullable value type it is true
			if ((!propertyBuilder.Property.Type.Type.IsValueType && !metadata.CanBeNull)
				|| (propertyBuilder.Property.Type.Type.IsValueType && metadata.CanBeNull != propertyBuilder.Property.Type.Type.IsNullable))
				(initializers = new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_CanBeNull, context.AST.Constant(metadata.CanBeNull, true)));

			if (metadata.Configuration != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration, context.AST.Constant(metadata.Configuration , true)));
			if (metadata.DataType      != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_DataType      , context.AST.Constant(metadata.DataType.Value, true)));

			// generate database type attributes
			if (metadata.DbType?.Name      != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_DbType   , context.AST.Constant(metadata.DbType.Name           , true)));
			if (metadata.DbType?.Length    != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Length   , context.AST.Constant(metadata.DbType.Length.Value   , true)));
			if (metadata.DbType?.Precision != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Precision, context.AST.Constant(metadata.DbType.Precision.Value, true)));
			if (metadata.DbType?.Scale     != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Scale    , context.AST.Constant(metadata.DbType.Scale.Value    , true)));

			if (metadata.IsPrimaryKey)
			{
				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsPrimaryKey, context.AST.Constant(true, true)));
				if (metadata.PrimaryKeyOrder != null)
					(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_PrimaryKeyOrder, context.AST.Constant(metadata.PrimaryKeyOrder.Value, true)));
			}

			if (metadata.IsIdentity          ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsIdentity       , context.AST.Constant(true                 , true)));
			if (metadata.SkipOnInsert        ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnInsert     , context.AST.Constant(true                 , true)));
			if (metadata.SkipOnUpdate        ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnUpdate     , context.AST.Constant(true                 , true)));
			if (metadata.SkipOnEntityFetch   ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnEntityFetch, context.AST.Constant(true                 , true)));
			if (metadata.MemberName   != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_MemberName       , context.AST.Constant(metadata.MemberName  , true)));
			if (metadata.Storage      != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Storage          , context.AST.Constant(metadata.Storage     , true)));
			if (metadata.CreateFormat != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_CreateFormat     , context.AST.Constant(metadata.CreateFormat, true)));
			if (metadata.IsDiscriminator     ) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsDiscriminator  , context.AST.Constant(true                 , true)));
			if (metadata.Order        != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Order            , context.AST.Constant(metadata.Order.Value , true)));

			var attr = context.AST.New(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute, parameters, initializers?.ToArray() ?? []);

			_entities[entityClass].members.Add(propertyBuilder.Property.Reference, attr);
		}

		void IMetadataBuilder.BuildAssociationMetadata(IDataModelGenerationContext context, CodeClass entityClass, AssociationMetadata metadata, PropertyBuilder propertyBuilder)
		{
			_entities[entityClass].members.Add(propertyBuilder.Property.Reference, BuildAssociationAttribute(context, metadata));
		}

		void IMetadataBuilder.BuildAssociationMetadata(IDataModelGenerationContext context, CodeClass entityClass, AssociationMetadata metadata, MethodBuilder methodBuilder)
		{
			// for extension method generate:
			// builder.HasAttribute<T>(T e => ExtensionsType.Method(e), attr);
			var entityType  = methodBuilder.Method.Parameters[0].Type.Type;
			var lambdaParam = context.AST.LambdaParameter(context.AST.Name("e"), entityType);
			var lambda      = context.AST
				.Lambda(WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(methodBuilder.Method.ReturnType!.Type, entityType)), true)
				.Parameter(lambdaParam);
			lambda
				.Body()
				.Append(
					context.AST.Return(
						context.AST.Call(
							new CodeTypeReference(entityClass.Type),
							methodBuilder.Method.Name,
							methodBuilder.Method.ReturnType!.Type,
							lambdaParam.Reference,
							// default(IDataContext)
							context.AST.Default(methodBuilder.Method.Parameters[1].Type.Type, false))));

			_builderCalls.Add(
				context.AST.Call(
					_builderVar.Reference,
					WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder_HasAttribute,
					new IType[] { entityType },
					false,
					lambda.Method,
					BuildAssociationAttribute(context, metadata)));
		}

		void IMetadataBuilder.BuildFunctionMetadata(IDataModelGenerationContext context, FunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			ICodeExpression[] parameters;
			if (metadata.Name != null)
				parameters = new ICodeExpression[] { context.AST.Constant(context.MakeFullyQualifiedRoutineName(metadata.Name.Value), true) };
			else
				parameters = [];

			List<CodeAssignmentStatement>? initializers = null;

			if (metadata.Configuration    != null) (initializers   = new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration  , context.AST.Constant(metadata.Configuration         , true)));
			if (metadata.ServerSideOnly   != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_ServerSideOnly  , context.AST.Constant(metadata.ServerSideOnly.Value  , true)));
			if (metadata.PreferServerSide != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_PreferServerSide, context.AST.Constant(metadata.PreferServerSide.Value, true)));
			if (metadata.InlineParameters != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_InlineParameters, context.AST.Constant(metadata.InlineParameters.Value, true)));
			if (metadata.IsPredicate      != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsPredicate     , context.AST.Constant(metadata.IsPredicate.Value     , true)));
			if (metadata.IsAggregate      != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsAggregate     , context.AST.Constant(metadata.IsAggregate.Value     , true)));
			if (metadata.IsWindowFunction != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsWindowFunction, context.AST.Constant(metadata.IsWindowFunction.Value, true)));
			if (metadata.IsPure           != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsPure          , context.AST.Constant(metadata.IsPure.Value          , true)));
			if (metadata.CanBeNull        != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_CanBeNull       , context.AST.Constant(metadata.CanBeNull.Value       , true)));
			if (metadata.IsNullable       != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsNullable      , context.AST.Constant(metadata.IsNullable.Value      , true)));

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_ArgIndices, context.AST.Array(WellKnownTypes.System.Int32, true, true, BuildArgIndices(context, metadata.ArgIndices))));

			// Sql.FunctionAttribute.Precedence not generated, as we currenty don't allow expressions for function name in generator

			var attr = context.AST.New(WellKnownTypes.LinqToDB.SqlFunctionAttribute, parameters, initializers?.ToArray() ?? []);

			// generate:
			// builder.HasAttribute(() => Type.Method(defaults), attr);
			var lambda = context.AST
				.Lambda(
					WellKnownTypes.System.Linq.Expressions.Expression(
						methodBuilder.Method.ReturnType != null
							? WellKnownTypes.System.Func(methodBuilder.Method.ReturnType.Type)
							: WellKnownTypes.System.Action),
					true);

			var fakeParameters = BuildDefaultArgs(context, methodBuilder.Method, metadata.IsAggregate == true);

			var typeParams = metadata.IsAggregate == true ? new  IType[] { WellKnownTypes.System.Object } : [];

			if (methodBuilder.Method.ReturnType != null)
				lambda.Body().Append(context.AST.Return(context.AST.Call(new CodeTypeReference(context.NonTableFunctionsClass.Type), methodBuilder.Method.Name, methodBuilder.Method.ReturnType.Type, typeParams, false, fakeParameters)));
			else
				lambda.Body().Append(context.AST.Call(new CodeTypeReference(context.NonTableFunctionsClass.Type), methodBuilder.Method.Name, typeParams, false, fakeParameters));

			_builderCalls.Add(
				context.AST.Call(
					_builderVar.Reference,
					WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder_HasAttribute,
					lambda.Method,
					attr));
		}

		void IMetadataBuilder.BuildTableFunctionMetadata(IDataModelGenerationContext context, TableFunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			ICodeExpression[] parameters;
			if (metadata.Name != null)
			{
				// compared to Sql.FunctionAttribute, Sql.TableFunctionAttribute provides proper FQN mapping attributes
				parameters = new ICodeExpression[] { context.AST.Constant(metadata.Name.Value.Name, true) };
			}
			else
				parameters = [];

			List<CodeAssignmentStatement>? initializers = null;

			if (metadata.Name != null)
			{
				// compared to Sql.FunctionAttribute, Sql.TableFunctionAttribute provides proper FQN mapping attributes
				if (metadata.Name.Value.Package  != null) (initializers   = new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Package , context.AST.Constant(metadata.Name.Value.Package , true)));
				if (metadata.Name.Value.Schema   != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Schema  , context.AST.Constant(metadata.Name.Value.Schema  , true)));
				if (metadata.Name.Value.Database != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Database, context.AST.Constant(metadata.Name.Value.Database, true)));
				if (metadata.Name.Value.Server   != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Server  , context.AST.Constant(metadata.Name.Value.Server  , true)));
			}

			if (metadata.Configuration != null)
				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration, context.AST.Constant(metadata.Configuration, true)));

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_ArgIndices, context.AST.Array(WellKnownTypes.System.Int32, true, true, BuildArgIndices(context, metadata.ArgIndices))));

			var attr = context.AST.New(WellKnownTypes.LinqToDB.SqlTableFunctionAttribute, parameters, initializers?.ToArray() ?? []);

			// generate:
			// builder.HasAttribute<TContext>(ctx => ctx.Method(defaults), attr);
			var lambdaParam = context.AST.LambdaParameter(context.AST.Name("ctx"), context.TableFunctionsClass.Type);
			var lambda      = context.AST
				.Lambda(WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(methodBuilder.Method.ReturnType!.Type, context.TableFunctionsClass.Type)), true)
				.Parameter(lambdaParam);
			lambda
				.Body()
				.Append(
					context.AST.Return(
						context.AST.Call(
							lambdaParam.Reference,
							methodBuilder.Method.Name,
							methodBuilder.Method.ReturnType!.Type,
							BuildDefaultArgs(context, methodBuilder.Method, false))));

			_builderCalls.Add(
				context.AST.Call(
					_builderVar.Reference,
					WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder_HasAttribute,
					new IType[] { context.TableFunctionsClass.Type },
					false,
					lambda.Method,
					attr));
		}

		/// <summary>
		/// Generates array values for <see cref="Sql.TableFunctionAttribute.ArgIndices"/>
		/// or <see cref="Sql.ExpressionAttribute.ArgIndices"/> setter.
		/// </summary>
		/// <param name="argIndices">Array values.</param>
		/// <returns>AST nodes for array values.</returns>
		private static ICodeExpression[] BuildArgIndices(IDataModelGenerationContext context, int[] argIndices)
		{
			var values = new ICodeExpression[argIndices.Length];

			for (var i = 0; i < argIndices.Length; i++)
				values[i] = context.AST.Constant(argIndices[i], true);

			return values;
		}

		/// <summary>
		/// Generates list of default(T) parameters for method.
		/// </summary>
		private static ICodeExpression[] BuildDefaultArgs(IDataModelGenerationContext context, CodeMethod method, bool replaceTArg)
		{
			if (method.Parameters.Count == 0)
				return [];

			var parameters = new ICodeExpression[method.Parameters.Count];
			for (var i = 0; i < parameters.Length; i++)
				// no target-typing to avoid overloads conflict
				parameters[i] = context.AST.Default(replaceTArg ? ReplaceTArg(method.Parameters[i].Type.Type) : method.Parameters[i].Type.Type, false);

			return parameters;
		}

		private static IType ReplaceTArg(IType type)
		{
			switch (type.Kind)
			{
				case TypeKind.Dynamic:
				case TypeKind.Regular:
				case TypeKind.OpenGeneric:
					return type;
				case TypeKind.Array:
				{
					var elemType = ReplaceTArg(type.ArrayElementType!);

					if (elemType == type.ArrayElementType)
						return type;

					return new ArrayType(elemType, type.ArraySizes!, type.IsNullable);
				}
				case TypeKind.Generic:
				{
					IType[]? typeArguments = null;
					for (var i = 0; i < type.TypeArguments!.Count; i++)
					{
						var argType = ReplaceTArg(type.TypeArguments[i]);
						if (argType != type.TypeArguments[i] || typeArguments != null)
						{
							if (typeArguments == null)
							{
								typeArguments = new IType[type.TypeArguments!.Count];
								for (var j = 0; j < i; j++)
									typeArguments[j] = type.TypeArguments[j];
							}

							typeArguments[i] = argType;
						}
					}

					if (typeArguments == null)
						return type;

					if (type.Parent != null)
						return new GenericType(type.Parent, type.Name!, type.IsValueType, type.IsNullable, typeArguments, type.External);
					return new GenericType(type.Namespace, type.Name!, type.IsValueType, type.IsNullable, typeArguments, type.External);
				}
				case TypeKind.TypeArgument:
					return WellKnownTypes.System.Object;
				default:
					throw new NotImplementedException($"Type {type.Kind} support not implemented in {nameof(ReplaceTArg)}()");
			}
		}

		/// <summary>
		/// Generates <see cref="AssociationAttribute"/> on association property or method.
		/// </summary>
		/// <param name="metadata">Association metadata descriptor.</param>
		/// <returns>Attribute creation expression.</returns>
		private static ICodeExpression BuildAssociationAttribute(IDataModelGenerationContext context, AssociationMetadata metadata)
		{
			List<CodeAssignmentStatement>? initializers = null;

			if (!metadata.CanBeNull)
				(initializers = new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_CanBeNull, context.AST.Constant(false, true)));

			// track association is configured to avoid generation of multiple conflicting configurations
			// as assocation could be configured in several ways
			var associationConfigured = false;
			if (metadata.ExpressionPredicate != null)
			{
				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ExpressionPredicate, context.AST.Constant(metadata.ExpressionPredicate, true)));
				associationConfigured = true;
			}

			if (metadata.QueryExpressionMethod != null)
			{
				if (associationConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_QueryExpressionMethod, context.AST.Constant(metadata.QueryExpressionMethod, true)));
				associationConfigured = true;
			}

			// track setup status of by-column assocation mapping
			var thisConfigured  = false;
			var otherConfigured = false;

			if (metadata.ThisKeyExpression != null)
			{
				if (associationConfigured || metadata.ThisKey != null)
					throw new InvalidOperationException("Association contains multiple relation setups");

				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ThisKey, metadata.ThisKeyExpression));

				thisConfigured = true;
			}

			if (metadata.OtherKeyExpression != null)
			{
				if (associationConfigured || metadata.OtherKey != null)
					throw new InvalidOperationException("Association contains multiple relation setups");

				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_OtherKey, metadata.OtherKeyExpression));

				otherConfigured = true;
			}

			if (metadata.ThisKey != null)
			{
				if (associationConfigured || thisConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ThisKey, context.AST.Constant(metadata.ThisKey, true)));
				thisConfigured = true;
			}

			if (metadata.OtherKey != null)
			{
				if (associationConfigured || otherConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				(initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_OtherKey, context.AST.Constant(metadata.OtherKey, true)));
				otherConfigured = true;
			}

			if (!associationConfigured && !(thisConfigured && otherConfigured))
				throw new InvalidOperationException("Association is missing relation setup");

			if (metadata.Configuration != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration, context.AST.Constant(metadata.Configuration, true)));
			if (metadata.Alias         != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_AliasName, context.AST.Constant(metadata.Alias        , true)));
			if (metadata.Storage       != null) (initializers ??= new()).Add(context.AST.Assign(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_Storage  , context.AST.Constant(metadata.Storage      , true)));

			return context.AST.New(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute, Array.Empty<ICodeExpression>(), initializers?.ToArray() ?? []);
		}

		void IMetadataBuilder.Complete(IDataModelGenerationContext context)
		{
			// generate fluent builder setup in context static constructor

			// var fluentBuilder = new FluentMappingBuilder(ContextSchema);
			context.StaticInitializer.Append(context.AST.Assign(_builderVar, context.AST.New(WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder, context.ContextMappingSchema)).NewLine());

			// add entity attributes (with properties)
			foreach (var kvp in _entities)
			{
				var isStatement       = kvp.Value.members.Count == 0;
				var entityType        = kvp.Key.Type;
				var entityBuilderType = WellKnownTypes.LinqToDB.Mapping.EntityMappingBuilderWithType(entityType);

				if (isStatement && kvp.Value.tableAttribute == null)
					continue;

				// builder.Entity<T>()
				var expression = context.AST.Call(
					_builderVar.Reference,
					WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder_Entity,
					entityBuilderType,
					new IType[] { entityType },
					false).Wrap(1);

				// builder.Entity<T>().HasAttribute(new TableAttribute("table") { ... })
				if (isStatement)
				{
					context.StaticInitializer.Append(
						// .HasAttribute(new TableAttribute("table") { ... })
						context.AST.Call(
							expression,
							WellKnownTypes.LinqToDB.Mapping.EntityMappingBuilder_HasAttribute,
							kvp.Value.tableAttribute!)
						.Wrap(2)
						.NewLine());

					continue;
				}

				if (kvp.Value.tableAttribute != null)
				{
					// .HasAttribute(new TableAttribute("table") { ... })
					expression = context.AST.Call(
						expression,
						WellKnownTypes.LinqToDB.Mapping.EntityMappingBuilder_HasAttribute,
						entityBuilderType,
						kvp.Value.tableAttribute).Wrap(2);
				}

				var cnt = 0;
				foreach (var members in kvp.Value.members)
				{
					var member    = members.Key;
					var attribute = members.Value;
					cnt++;
					var isLast = cnt == kvp.Value.members.Count;

					// builder.Member(e => e.Prop)
					var lambdaParam = context.AST.LambdaParameter(context.AST.Name("e"), entityType);
					var lambda = context.AST
						.Lambda(WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(member.Referenced.Type.Type, entityType)), true)
						.Parameter(lambdaParam);
					lambda.Body().Append(context.AST.Return(context.AST.Member(lambdaParam.Reference, member)));

					expression = context.AST.Call(
						expression,
						WellKnownTypes.LinqToDB.Mapping.EntityMappingBuilder_Member,
						entityBuilderType,
						lambda.Method).Wrap(2);

					if (attribute == null)
					{
						// .IsNotColumn()
						if (isLast)
						{
							context.StaticInitializer.Append(context.AST.Call(expression, WellKnownTypes.LinqToDB.Mapping.PropertyMappingBuilder_IsNotColumn).Wrap(3).NewLine());
							break;
						}
						else
							expression = context.AST.Call(expression, WellKnownTypes.LinqToDB.Mapping.PropertyMappingBuilder_IsNotColumn, entityBuilderType).Wrap(3);
					}
					else
					{
						// .HasAttribute()
						if (isLast)
						{
							context.StaticInitializer.Append(context.AST.Call(expression, WellKnownTypes.LinqToDB.Mapping.EntityMappingBuilder_HasAttribute, attribute).Wrap(3).NewLine());
							break;
						}
						else
							expression = context.AST.Call(expression, WellKnownTypes.LinqToDB.Mapping.EntityMappingBuilder_HasAttribute, entityBuilderType, attribute).Wrap(3);
					}
				}
			}

			// add non-entity attributes (e.g. extension methods)
			if (_builderCalls.Count > 0)
			{
				foreach (var line in _builderCalls)
					context.StaticInitializer.Append(line.NewLine());
			}

			// flientBuilder.Build();
			context.StaticInitializer.Append(context.AST.Call(_builderVar.Reference, WellKnownTypes.LinqToDB.Mapping.FluentMappingBuilder_Build));
		}
	}
}
