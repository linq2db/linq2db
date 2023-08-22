using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using Reflection;
	using Interceptors;
	using SqlQuery;
	using LinqToDB.Expressions;

	internal partial class ExpressionBuilder
	{
		#region Entity Construction

		public static Type GetTypeForInstantiation(Type entityType)
		{
			// choosing type that can be instantiated
			if ((entityType.IsInterface || entityType.IsAbstract) && !(entityType.IsInterface || entityType.IsAbstract))
			{
				throw new NotImplementedException();
			}
			return entityType;
		}

		SqlGenericConstructorExpression BuildGenericFromMembers(IBuildContext? context,
			MappingSchema mappingSchema,
			IReadOnlyCollection<ColumnDescriptor> columns, ProjectFlags flags, Expression currentPath, int level,
			FullEntityPurpose purpose)
		{
			var members = new List<SqlGenericConstructorExpression.Assignment>();

			var checkForKey = flags.HasFlag(ProjectFlags.Keys) && columns.Any(c => c.IsPrimaryKey);

			if (checkForKey || purpose != FullEntityPurpose.Default)
			{
				columns = columns.Where(c =>
				{
					var valid = true;
					if (checkForKey)
						valid = c.IsPrimaryKey;
					if (valid)
					{
						if (purpose == FullEntityPurpose.Insert)
							valid = !c.SkipOnInsert;
						else if (purpose == FullEntityPurpose.Update)
							valid = !c.SkipOnUpdate;
					}
					return valid;
				}).ToList();
			}

			var hasNested   = false;

			if (level == 0)
			{
				foreach (var column in columns)
				{
					Expression me;
					if (column.MemberName.Contains('.'))
					{
						hasNested = true;
					}
					else
					{
						var declaringType = column.MemberInfo.DeclaringType!;
						var objExpression = SequenceHelper.EnsureType(currentPath, declaringType);

						// Target ReflectedType to DeclaringType for better caching
						//
						var memberInfo = declaringType.GetMemberEx(column.MemberInfo) ??
						                 throw new InvalidOperationException();
						me = Expression.MakeMemberAccess(objExpression, memberInfo);

						members.Add(new SqlGenericConstructorExpression.Assignment(memberInfo, me,
							column.MemberAccessor.HasSetter, false));
					}

				}
			}

			if (level > 0 || hasNested)
			{
				var processed = new HashSet<string>();
				foreach (var column in columns)
				{
					if (!column.MemberName.Contains('.'))
					{
						continue;
					}

					var names = column.MemberName.Split('.');

					if (level >= names.Length)
						continue;

					var currentMemberName = names[level];
					MemberInfo? memberInfo;
					Expression assignExpression;

					if (names.Length - 1 > level)
					{
						var propPath = string.Join(".", names.Take(level + 1));
						if (!processed.Add(propPath))
							continue;

						memberInfo = currentPath.Type.GetMember(currentMemberName).FirstOrDefault();

						if (memberInfo == null)
						{
							var ed = mappingSchema.GetEntityDescriptor(currentPath.Type);

							foreach (var inheritance in ed.InheritanceMapping)
							{
								memberInfo = inheritance.Type.GetMember(currentMemberName).FirstOrDefault();
								if (memberInfo != null)
								{
									currentPath = Expression.Convert(currentPath, inheritance.Type);
									break;
								}
							}

							if (memberInfo == null)
								throw new InvalidOperationException($"No suitable member '[currentMemberName]' found for type '{currentPath.Type}'");
						}

						var newColumns = columns.Where(c => c.MemberName.StartsWith(propPath)).ToList();
						var newPath    = Expression.MakeMemberAccess(currentPath, memberInfo);

						assignExpression = BuildGenericFromMembers(null, mappingSchema, newColumns, flags, newPath, level + 1, purpose);
					}
					else
					{
						memberInfo       = column.MemberInfo;
						assignExpression = Expression.MakeMemberAccess(currentPath, memberInfo);
					}

					members.Add(new SqlGenericConstructorExpression.Assignment(memberInfo, assignExpression, column.MemberAccessor.HasSetter, false));
				}
			}

			if (context != null && !flags.HasFlag(ProjectFlags.Keys) && purpose == FullEntityPurpose.Default)
			{
				var entityDescriptor = MappingSchema.GetEntityDescriptor(currentPath.Type);
				BuildCalculatedColumns(context, entityDescriptor, entityDescriptor.ObjectType, members);
			}

			if (!flags.IsKeys() && level == 0 && context != null && purpose == FullEntityPurpose.Default && context is ITableContext table)
			{
				var ed = MappingSchema.GetEntityDescriptor(table.ObjectType,
					DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				var loadWith = GetTableLoadWith(table);

				if (loadWith.Count > 0)
				{
					var assignedMembers = new HashSet<MemberInfo>(MemberInfoComparer.Instance);

					foreach (var info in loadWith)
					{
						if (!info.ShouldLoad)
							continue;

						var memberInfo = info.MemberInfo;

						if (memberInfo == null || !assignedMembers.Add(memberInfo))
							continue;

						var expression = Expression.MakeMemberAccess(currentPath, memberInfo);

						members.Add(
							new SqlGenericConstructorExpression.Assignment(memberInfo, expression, false, true));
					}
				}
			}

			var generic = new SqlGenericConstructorExpression(
				purpose == FullEntityPurpose.Default
					? checkForKey
						? SqlGenericConstructorExpression.CreateType.Keys
						: SqlGenericConstructorExpression.CreateType.Full
					: SqlGenericConstructorExpression.CreateType.Auto,
				currentPath.Type,
				null, 
				new ReadOnlyCollection<SqlGenericConstructorExpression.Assignment>(members),
				context);

			return generic;
		}

		AssociationDescriptor? GetFieldOrPropAssociationDescriptor(MemberInfo memberInfo, EntityDescriptor entityDescriptor)
		{
			if (entityDescriptor.FindAssociationDescriptor(memberInfo) is AssociationDescriptor associationDescriptor)
				return associationDescriptor;

			foreach (var m in entityDescriptor.InheritanceMapping)
			{
				var ed = MappingSchema.GetEntityDescriptor(m.Type, DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				if (ed.FindAssociationDescriptor(memberInfo) is AssociationDescriptor inheritedAssociationDescriptor)
					return inheritedAssociationDescriptor;
			}	

			return null;
		}


		#endregion Entity Construction

		#region Generic Entity Construction

		public enum FullEntityPurpose
		{
			Default,
			Insert,
			Update
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(IBuildContext context, Expression refExpression, Type entityType, ProjectFlags flags)
		{
			entityType = GetTypeForInstantiation(entityType);

			refExpression = SequenceHelper.EnsureType(refExpression, entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			var generic = BuildGenericFromMembers(context, MappingSchema, entityDescriptor.Columns, flags, refExpression, 0, FullEntityPurpose.Default);

			return generic;
		}

		public SqlGenericConstructorExpression BuildEntityExpression(IBuildContext context, Expression refExpression, Type entityType, IReadOnlyCollection<MemberInfo> members)
		{
			entityType = GetTypeForInstantiation(entityType);

			refExpression = SequenceHelper.EnsureType(refExpression, entityType);

			var assignments = new List<SqlGenericConstructorExpression.Assignment>(members.Count);

			foreach (var member in members)
			{
				assignments.Add(new SqlGenericConstructorExpression.Assignment(member, Expression.MakeMemberAccess(refExpression, member), false, false));
			}

			var generic = new SqlGenericConstructorExpression(SqlGenericConstructorExpression.CreateType.Auto, entityType, null, assignments.AsReadOnly());

			return generic;
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(Expression root, Type entityType, ProjectFlags flags, FullEntityPurpose purpose)
		{
			entityType = GetTypeForInstantiation(entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			var generic = BuildGenericFromMembers(null, MappingSchema, entityDescriptor.Columns, flags, root, 0, purpose);

			return generic;
		}

		void BuildCalculatedColumns(IBuildContext context, EntityDescriptor entityDescriptor, Type objectType, List<SqlGenericConstructorExpression.Assignment> assignments)
		{
			if (!entityDescriptor.HasCalculatedMembers)
				return;

			var contextRef = new ContextRefExpression(objectType, context);

			foreach (var member in entityDescriptor.CalculatedMembers!)
			{
				var assignment = new SqlGenericConstructorExpression.Assignment(member.MemberInfo,
					Expression.MakeMemberAccess(contextRef, member.MemberInfo), true, false);

				assignments.Add(assignment);
			}
		}

		public static int FindIndex<T>(ReadOnlyCollection<T> collection, Func<T, bool> predicate)
		{
			for (int i = 0; i < collection.Count; i++)
			{
				if (predicate(collection[i]))
					return i;
			}

			return -1;
		}

		static int MatchParameter(ParameterInfo parameter, ReadOnlyCollection<SqlGenericConstructorExpression.Assignment> members)
		{
			var found = FindIndex(members, x =>
				x.MemberInfo.GetMemberType() == parameter.ParameterType &&
				x.MemberInfo.Name            == parameter.Name);

			if (found < 0)
			{
				found = FindIndex(members, x =>
					x.MemberInfo.GetMemberType() == parameter.ParameterType &&
					x.MemberInfo.Name.Equals(parameter.Name,
						StringComparison.InvariantCultureIgnoreCase));
			}

			return found;
		}


		Expression? TryWithConstructor(
			MappingSchema                                     mappingSchema,
			TypeAccessor                                      typeAccessor,
			ConstructorInfo?                                  constructorInfo,
			SqlGenericConstructorExpression                   constructorExpression, 
			List<SqlGenericConstructorExpression.Assignment>? missed)
		{
			NewExpression newExpression;

			var loadedColumns = new HashSet<int>();
			var parameters    = constructorInfo?.GetParameters();

			if (parameters == null || parameters.Length <= 0)
			{
				newExpression = parameters == null
					? Expression.New(typeAccessor.Type)
					: Expression.New(constructorInfo!);
			}
			else
			{
				var parameterValues = new List<Expression>();

				if (constructorExpression.Parameters.Count == parameters.Length)
				{
					for (int i = 0; i < parameters.Length; i++)
					{
						var parameterInfo = parameters[i];
						var param         = constructorExpression.Parameters[i];
						parameterValues.Add(param.Expression);

						var idx = MatchParameter(parameterInfo, constructorExpression.Assignments);
						if (idx >= 0)
							loadedColumns.Add(i);
					}
				}
				else
				{
					foreach (var parameterInfo in parameters)
					{
						var idx = MatchParameter(parameterInfo, constructorExpression.Assignments);

						if (idx >= 0)
						{
							var ai = constructorExpression.Assignments[idx];
							parameterValues.Add(ai.Expression);

							loadedColumns.Add(idx);
						}
						else
						{
							parameterValues.Add(Expression.Constant(
								MappingSchema.GetDefaultValue(parameterInfo.ParameterType),
								parameterInfo.ParameterType));
						}
					}
				}
				newExpression = Expression.New(constructorInfo!, parameterValues);
			}


			if (constructorExpression.Assignments.Count == 0 || loadedColumns.Count == constructorExpression.Assignments.Count)
			{
				// Everything is fit into parameters
				return newExpression;
			}

			var bindings = new List<MemberBinding>(Math.Max(0, constructorExpression.Assignments.Count - loadedColumns.Count));
			var ignored  = 0;

			var ed = mappingSchema.GetEntityDescriptor(typeAccessor.Type);

			List<SqlGenericConstructorExpression.Assignment>? dynamicProperties = null;
			List<LambdaExpression>? additionalSteps = null;

			for (int i = 0; i < constructorExpression.Assignments.Count; i++)
			{
				if (loadedColumns.Contains(i))
					continue;

				var assignment = constructorExpression.Assignments[i];

				// handling inheritance
				if (assignment.MemberInfo.DeclaringType?.IsAssignableFrom(typeAccessor.Type) == true)
				{
					if (assignment.MemberInfo.IsDynamicColumnPropertyEx())
					{
						dynamicProperties ??= new List<SqlGenericConstructorExpression.Assignment>();
						dynamicProperties.Add(assignment);
					}
					else
					{
						var memberAccessor = typeAccessor[assignment.MemberInfo.Name];

						var memberInfo = assignment.MemberInfo;
						var descriptor = GetFieldOrPropAssociationDescriptor(memberInfo, ed);
						if (descriptor != null)
						{
							var expr = descriptor.GetAssociationAssignmentLambda(assignment.Expression.Unwrap(), memberInfo);
							if (expr != null)
							{
								additionalSteps ??= new();
								additionalSteps.Add(expr);

								continue;
							}
						}
						
						if (!memberAccessor.HasSetter)
						{
							if (assignment.IsMandatory)
								missed?.Add(assignment);
							else
								++ignored;
						}
						else
						{
							bindings.Add(Expression.Bind(assignment.MemberInfo, assignment.Expression));
						}
					}
				}
				else
				{
					++ignored;
				}
			}

			if (loadedColumns.Count + bindings.Count + ignored + (dynamicProperties?.Count ?? 0) + (additionalSteps?.Count ?? 0) 
			    != constructorExpression.Assignments.Count)
			{
				return null;
			}

			Expression result = Expression.MemberInit(newExpression, bindings);

			if (additionalSteps != null || dynamicProperties?.Count > 0 && ed.DynamicColumnSetter != null)
			{
				var generator   = new ExpressionGenerator();
				var objVariable = generator.AssignToVariable(result, "obj");

				if (dynamicProperties != null)
				{
					//TODO: we can make it in MemberInit
					foreach (var d in dynamicProperties)
					{
						generator.AddExpression(ed.DynamicColumnSetter!.GetBody(objVariable, Expression.Constant(d.MemberInfo.Name), d.Expression));
					}
				}

				if (additionalSteps != null)
				{
					foreach(var lambda in additionalSteps)
					{
						generator.AddExpression(lambda.GetBody(objVariable));
					}
				}

				generator.AddExpression(objVariable);

				result = generator.Build();
			}

			return result;
		}

		public Expression ConstructFullEntity(IBuildContext context,
			SqlGenericConstructorExpression constructorExpression, ProjectFlags flags, bool checkInheritance = true)
		{
			var constructed = TryConstructFullEntity(context, constructorExpression, constructorExpression.ObjectType, flags, checkInheritance);

			if (constructed == null)
				throw new InvalidOperationException(
					$"Cannot construct full object '{constructorExpression.ObjectType}'. No suitable constructors found.");

			return constructed;
		}

		static Expression GetMemberExpression(SqlGenericConstructorExpression constructorExpression, MemberInfo memberInfo)
		{
			var me = constructorExpression.Assignments.FirstOrDefault(a =>
				MemberInfoComparer.Instance.Equals(memberInfo, a.MemberInfo));

			if (me == null)
				throw new InvalidOperationException();
			return me.Expression;
		}

		public Expression? TryConstructFullEntity(IBuildContext context, SqlGenericConstructorExpression constructorExpression, Type constructType, ProjectFlags flags, bool checkInheritance = true)
		{
			var entityType           = constructorExpression.ObjectType;
			var entityDescriptor     = MappingSchema.GetEntityDescriptor(entityType);
			var rootReference        = constructorExpression.ConstructionRoot ?? new ContextRefExpression(entityType, context);

			rootReference = SequenceHelper.EnsureType(rootReference, entityType);

			if (checkInheritance && flags.HasFlag(ProjectFlags.Expression))
			{
				var inheritanceMappings = entityDescriptor.InheritanceMapping;
				if (inheritanceMappings.Count > 0)
				{
					var defaultDescriptor = inheritanceMappings.FirstOrDefault(x => x.IsDefault);

					Expression defaultExpression;
					if (defaultDescriptor != null)
					{
						if (defaultDescriptor.Type != constructorExpression.Type)
						{
							var subConstructor = BuildFullEntityExpression(context, rootReference, defaultDescriptor.Type, flags);
							defaultExpression = ConstructFullEntity(context, subConstructor, flags, false);
							defaultExpression = Expression.Convert(defaultExpression, constructorExpression.Type);
						}
						else
						{
							defaultExpression = ConstructFullEntity(context, constructorExpression, flags, false);
						}
					}
					else
					{
						var firstMapping = inheritanceMappings[0];

						var onType = firstMapping.Discriminator.MemberInfo.DeclaringType;
						if (onType == null)
						{
							throw new LinqToDBException("Could not get discriminator DeclaringType.");
						}

						var generator    = new ExpressionGenerator();

						Expression<Func<object, Type, Exception>> throwExpr = (code, et) =>
							new LinqException(
								"Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.",
								code, et);

						//var access = Expression.MakeMemberAccess(EnsureType(rootReference, onType), firstMapping.Discriminator.MemberInfo);
						var access = GetMemberExpression(constructorExpression, firstMapping.Discriminator.MemberInfo);

						var codeExpr = Expression.Convert(access, typeof(object));

						generator.Throw(throwExpr.GetBody(codeExpr, Expression.Constant(onType, typeof(Type))));
						generator.AddExpression(new DefaultValueExpression(MappingSchema, entityType));

						defaultExpression = generator.Build();
					}

					var current = defaultExpression;

					for (int i = 0; i < inheritanceMappings.Count; i++)
					{
						var inheritance = inheritanceMappings[i];
						if (inheritance.IsDefault)
							continue;

						if (inheritance.Type.IsAbstract)
							continue;

						Expression test;

						var discriminatorMemberInfo = inheritance.Discriminator.MemberInfo;

						var onType = discriminatorMemberInfo.DeclaringType ?? inheritance.Type;

						var currentRef = SequenceHelper.EnsureType(rootReference, onType);
						var member     = currentRef.Type.GetMemberEx(discriminatorMemberInfo);
						member = discriminatorMemberInfo;
						if (false)
						{
							//TODO: strange behaviour, Member of inheritance has no Discriminator column

							var dynamicPropCall = Expression.Call(Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(discriminatorMemberInfo.GetMemberType()),
								currentRef, Expression.Constant(discriminatorMemberInfo.Name));

							var dynamicSql = ConvertToSqlPlaceholder(context, dynamicPropCall, columnDescriptor: inheritance.Discriminator);

							test = new SqlReaderIsNullExpression(dynamicSql, false);

							// throw new InvalidOperationException(
							// 	$"Type '{contextRef.Type.Name}' has no member '{inheritance.Discriminator.MemberInfo.Name}'");
						}
						else
						{
							//var memberAccess = Expression.MakeMemberAccess(currentRef, member);
							var memberAccess = GetMemberExpression(constructorExpression, member);

							if (inheritance.Code == null)
							{
								var discriminatorSql = ConvertToSqlPlaceholder(context, memberAccess, columnDescriptor: inheritance.Discriminator);
								test = new SqlReaderIsNullExpression(discriminatorSql, false);
							}
							else
							{
								test = Equal(
									MappingSchema,
									memberAccess,
									Expression.Constant(inheritance.Code));
							}
						}

						var fullEntity = TryConstructFullEntity(context, constructorExpression, inheritance.Type, flags, false);
						if (fullEntity == null)
							return null;
						var tableExpr = Expression.Convert(fullEntity, current.Type);

						current = Expression.Condition(test, tableExpr, current);
					}

					return current;
				}
			}

			var constructed = TryConstructObject(MappingSchema, constructorExpression, constructType);

			if (constructed == null)
				return null;

			if (constructorExpression.BuildContext != null)
			{
				var tableContext = SequenceHelper.GetTableContext(constructorExpression.BuildContext);
				if (tableContext != null)
					constructed = NotifyEntityCreated(constructed, tableContext.SqlTable);
			}

			return constructed;
		}

		static object OnEntityCreated(IDataContext context, object entity, TableOptions tableOptions, string? tableName, string? schemaName, string? databaseName, string? serverName)
		{
			return context is IInterceptable<IEntityServiceInterceptor> entityService ?
				entityService.Interceptor?.EntityCreated(new(context, tableOptions, tableName, schemaName, databaseName, serverName), entity) ?? entity :
				entity;
		}

		static readonly MethodInfo _onEntityCreatedMethodInfo = MemberHelper.MethodOf(() =>
			OnEntityCreated(null!, null!, TableOptions.NotSet, null, null, null, null));

		Expression NotifyEntityCreated(Expression expr, SqlTable sqlTable)
		{
			if (DataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: {} })
			{
				expr = Expression.Convert(
					Expression.Call(
						_onEntityCreatedMethodInfo,
						ExpressionConstants.DataContextParam,
						expr,
						Expression.Constant(sqlTable.TableOptions),
						Expression.Constant(sqlTable.TableName.Name,     typeof(string)),
						Expression.Constant(sqlTable.TableName.Schema,   typeof(string)),
						Expression.Constant(sqlTable.TableName.Database, typeof(string)),
						Expression.Constant(sqlTable.TableName.Server,   typeof(string))
					),
					expr.Type);
			}

			return expr;
		}


		ConstructorInfo? SelectParameterizedConstructor(Type objectType)
		{
			var constructors = objectType.GetConstructors();

			if (constructors.Length == 0)
			{
				constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
			}

			if (constructors.Length > 1)
			{
				var noParams = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
				if (noParams != null)
					return noParams;

				throw new InvalidOperationException($"Type '{objectType.Name}' has ambiguous constructors.");
			}

			return constructors.Length > 0 ? constructors[0] : null;
		}

		public Expression? TryConstructObject(MappingSchema mappingSchema,
			SqlGenericConstructorExpression constructorExpression, Type constructType)
		{
			if (constructType.IsAbstract)
				return null;

			var typeAccessor = TypeAccessor.GetAccessor(constructType);

			if (constructorExpression.Constructor != null)
			{
				var instantiation = TryWithConstructor(mappingSchema, typeAccessor, constructorExpression.Constructor, constructorExpression, null);
				if (instantiation != null)
					return instantiation;
			}

			var constructor = SelectParameterizedConstructor(constructType);
			if (constructor != null)
			{
				var instantiation = TryWithConstructor(mappingSchema, typeAccessor, constructor,
					constructorExpression, null);
				if (instantiation != null)
					return instantiation;
			}

			if (constructType.IsValueType)
			{
				return TryWithConstructor(mappingSchema, typeAccessor, null, constructorExpression, null);
			}

			return null;
		}

		public Expression Construct(MappingSchema mappingSchema, SqlGenericConstructorExpression constructorExpression,
			IBuildContext                         context,       ProjectFlags                    flags)
		{
			var constructed = TryConstruct(mappingSchema, constructorExpression, context, flags);
			if (constructed == null)
			{
				throw new InvalidOperationException(
					$"Cannot construct object '{constructorExpression.ObjectType}'. No suitable constructors found.");
			}

			return constructed;
		}

		public Expression? TryConstruct(MappingSchema mappingSchema, SqlGenericConstructorExpression constructorExpression, IBuildContext context,  ProjectFlags flags)
		{
			switch (constructorExpression.ConstructType)
			{
				case SqlGenericConstructorExpression.CreateType.Full:
				{
					return TryConstructFullEntity(context, constructorExpression, constructorExpression.ObjectType, flags);
				}
				case SqlGenericConstructorExpression.CreateType.MemberInit:
				case SqlGenericConstructorExpression.CreateType.Auto:
				case SqlGenericConstructorExpression.CreateType.Keys:
				case SqlGenericConstructorExpression.CreateType.New:
				{
					return TryConstructObject(mappingSchema, constructorExpression, constructorExpression.ObjectType);
				}
				default:
					throw new NotImplementedException();
			}
		}
		

		#endregion


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
				constructorExpression.ObjectType, null, newAssignments.AsReadOnly());
		}
	}
}
