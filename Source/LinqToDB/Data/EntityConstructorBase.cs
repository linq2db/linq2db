using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	using Expressions;
	using Extensions;
	using Interceptors.Internal;
	using Interceptors;
	using Linq.Builder;
	using Linq;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using Tools;

	internal abstract class EntityConstructorBase
	{
		public MappingSchema MappingSchema { get; private set; } = default!;
		public IDataContext DataContext { get; private set; } = default!;

		public DataOptions DataOptions => DataContext.Options;

		#region Entity Construction

		public virtual List<LoadWithInfo>? GetTableLoadWith(Expression path)
		{
			return null;
		}

		protected SqlGenericConstructorExpression BuildGenericFromMembers(
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
					if (column.SkipOnEntityFetch)
						continue;

					Expression me;
					if (column.MemberName.Contains('.') && !column.MemberInfo.Name.Contains("."))
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

						me = MakeAssignExpression(objExpression, memberInfo, column);

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
					if (column.SkipOnEntityFetch)
						continue;

					if (!column.MemberName.Contains('.'))
						continue;

					// explicit interface implementation
					//
					if (column.MemberInfo.Name.Contains("."))
						continue;

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
							var ed = MappingSchema.GetEntityDescriptor(currentPath.Type);

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
								throw new InvalidOperationException($"No suitable member '[{currentMemberName}]' found for type '{currentPath.Type}'");
						}

						var newColumns = columns.Where(c => c.MemberName.StartsWith(propPath)).ToList();
						var newPath    = MakeAssignExpression(currentPath, memberInfo, column);

						assignExpression = BuildGenericFromMembers(newColumns, flags, newPath, level + 1, purpose);
					}
					else
					{
						memberInfo       = column.MemberInfo;
						assignExpression = MakeAssignExpression(currentPath, memberInfo, column);
					}

					members.Add(new SqlGenericConstructorExpression.Assignment(memberInfo, assignExpression, column.MemberAccessor.HasSetter, false));
				}
			}

			if (!flags.HasFlag(ProjectFlags.Keys) && purpose == FullEntityPurpose.Default)
			{
				var entityDescriptor = MappingSchema.GetEntityDescriptor(currentPath.Type);
				BuildCalculatedColumns(currentPath, entityDescriptor, entityDescriptor.ObjectType, members);
			}

			if (!flags.IsKeys() && level == 0 && purpose == FullEntityPurpose.Default)
			{
				var loadWith = GetTableLoadWith(currentPath);

				if (loadWith?.Count > 0)
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
							new SqlGenericConstructorExpression.Assignment(memberInfo, expression, true, true));
					}
				}
			}

			var generic = new SqlGenericConstructorExpression(
				(purpose, checkForKey) switch
				{
					(FullEntityPurpose.Default, true) => SqlGenericConstructorExpression.CreateType.Keys,
					(FullEntityPurpose.Default, _)    => SqlGenericConstructorExpression.CreateType.Full,
					_                                 => SqlGenericConstructorExpression.CreateType.Auto,
				},
				currentPath.Type,
				null,
				new ReadOnlyCollection<SqlGenericConstructorExpression.Assignment>(members), MappingSchema,
				currentPath);

			return generic;
		}

		protected virtual Expression MakeAssignExpression(Expression objExpression, MemberInfo memberInfo, ColumnDescriptor column)
		{
			var me = Expression.MakeMemberAccess(objExpression, memberInfo);
			return me;
		}

		protected virtual Expression MakeIsNullExpression(Expression objExpression, MemberInfo memberInfo, ColumnDescriptor column)
		{
			var memberExpr = GetMemberExpression((SqlGenericConstructorExpression)objExpression, memberInfo);
			if (memberExpr.Type.IsValueType && !memberExpr.Type.IsNullable())
			{
				memberExpr = Expression.Convert(memberExpr, memberExpr.Type.AsNullable());
			}

			var test = Expression.Equal(memberExpr, new DefaultValueExpression(MappingSchema, memberExpr.Type));
			return test;
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
			Update,
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(IDataContext dataContext, MappingSchema mappingSchema, Expression refExpression, Type entityType, ProjectFlags flags, FullEntityPurpose purpose)
		{
			DataContext   = dataContext;
			MappingSchema = mappingSchema;

			var generic = BuildFullEntityExpressionInternal(refExpression, entityType, flags, purpose);

			return generic;
		}

		SqlGenericConstructorExpression BuildFullEntityExpressionInternal(Expression refExpression, Type entityType, ProjectFlags flags, FullEntityPurpose purpose)
		{
			refExpression = SequenceHelper.EnsureType(refExpression, entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			var generic = BuildGenericFromMembers(entityDescriptor.Columns, flags, refExpression, 0, purpose);

			return generic;
		}

		public SqlGenericConstructorExpression BuildEntityExpression(IDataContext dataContext, MappingSchema mappingSchema, Expression refExpression, Type entityType, IReadOnlyCollection<MemberInfo> members)
		{
			DataContext   = dataContext;
			MappingSchema = mappingSchema;

			refExpression = SequenceHelper.EnsureType(refExpression, entityType);

			var assignments = new List<SqlGenericConstructorExpression.Assignment>(members.Count);

			foreach (var member in members)
			{
				assignments.Add(new SqlGenericConstructorExpression.Assignment(member, Expression.MakeMemberAccess(refExpression, member), false, false));
			}

			var generic = new SqlGenericConstructorExpression(SqlGenericConstructorExpression.CreateType.Auto, entityType, null, assignments.AsReadOnly(), MappingSchema, refExpression);

			return generic;
		}

		static void BuildCalculatedColumns(Expression root, EntityDescriptor entityDescriptor, Type objectType, List<SqlGenericConstructorExpression.Assignment> assignments)
		{
			if (!entityDescriptor.HasCalculatedMembers)
				return;

			if (root.Type != objectType)
				root = Expression.Convert(root, objectType);

			foreach (var member in entityDescriptor.CalculatedMembers!)
			{
				var assignment = new SqlGenericConstructorExpression.Assignment(member.MemberInfo,
					Expression.MakeMemberAccess(root, member.MemberInfo), true, false);

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
			var found = -1;

			found = FindIndex(members, x =>
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
					for (int i = 0; i < parameters.Length; i++)
					{
						var parameterInfo = parameters[i];

						var idx = MatchParameter(parameterInfo, constructorExpression.Assignments);

						if (idx >= 0)
						{
							var ai = constructorExpression.Assignments[idx];

							var assignment = ai.Expression;
							if (parameterInfo.ParameterType != assignment.Type)
								assignment = Expression.Convert(assignment, parameterInfo.ParameterType);

							parameterValues.Add(assignment);

							loadedColumns.Add(idx);
						}
						else
						{
							if (constructorExpression.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
								return null;

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

			var ed = MappingSchema.GetEntityDescriptor(typeAccessor.Type);

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

		public Expression ConstructFullEntity(
			SqlGenericConstructorExpression constructorExpression, ProjectFlags flags, bool checkInheritance = true)
		{
			var constructed = TryConstructFullEntity(constructorExpression, constructorExpression.ObjectType, flags, checkInheritance, out var error);

			if (constructed == null)
			{
				throw new InvalidOperationException(
					$"Cannot construct full object '{constructorExpression.ObjectType}'. {error ?? "No suitable constructors found."}");
			}

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

		static MethodInfo _throwErrorMethodInf = MemberHelper.MethodOfGeneric(() => ThrowError<int>(null, typeof(object)));

		static T ThrowError<T>(object? code, Type onType)
		{
			throw new LinqToDBException($"Inheritance mapping is not defined for discriminator value '{code}' in the '{onType}' hierarchy.");
		}

		public virtual Expression? TryConstructFullEntity(SqlGenericConstructorExpression constructorExpression, Type constructType, ProjectFlags flags, bool checkInheritance, out string? error)
		{
			error = null;

			var entityType       = constructorExpression.ObjectType;
			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);
			var rootReference    = constructorExpression.ConstructionRoot;

			if (rootReference == null)
				return null;

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
							var subConstructor = BuildFullEntityExpressionInternal(rootReference, defaultDescriptor.Type, flags, FullEntityPurpose.Default);
							defaultExpression = ConstructFullEntity(subConstructor, flags, false);
							defaultExpression = Expression.Convert(defaultExpression, constructorExpression.Type);
						}
						else
						{
							defaultExpression = ConstructFullEntity(constructorExpression, flags, false);
						}
					}
					else
					{
						var firstMapping = inheritanceMappings[0];

						var onType = firstMapping.Discriminator.MemberInfo.DeclaringType;
						if (onType == null)
						{
							throw new LinqToDBException("Could not get discriminator's DeclaringType.");
						}

						var access   = GetMemberExpression(constructorExpression, firstMapping.Discriminator.MemberInfo);
						var codeExpr = Expression.Convert(access, typeof(object));

						var generator    = new ExpressionGenerator();
						generator.AddExpression(Expression.Call(_throwErrorMethodInf.MakeGenericMethod(entityType), codeExpr, Expression.Constant(onType, typeof(Type))));
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

						var memberAccess = GetMemberExpression(constructorExpression, member);

						if (inheritance.Code == null)
						{
							test = MakeIsNullExpression(constructorExpression, member, inheritance.Discriminator);
						}
						else
						{
							test = ExpressionBuilder.Equal(
								MappingSchema,
								memberAccess,
								Expression.Constant(inheritance.Code));
						}

						// Tell Builder to prefer client side
						test = MarkerExpression.PreferClientSide(test);

						var fullEntity = TryConstructFullEntity(constructorExpression, inheritance.Type, flags, false, out error);
						if (fullEntity == null)
							return null;
						var tableExpr = Expression.Convert(fullEntity, current.Type);

						current = Expression.Condition(test, tableExpr, current);
					}

					return current;
				}
			}

			List<SqlGenericConstructorExpression.Assignment>? newAssignments = null;

			// Handle storage redefining
			for (var index = 0; index < constructorExpression.Assignments.Count; index++)
			{
				var a  = constructorExpression.Assignments[index];
				var cd = entityDescriptor.FindColumnDescriptor(a.MemberInfo);
				if (cd != null)
				{
					if (cd.StorageInfo != a.MemberInfo)
					{
						if (newAssignments == null)
						{
							newAssignments = new(constructorExpression.Assignments.Count);
							newAssignments.AddRange(constructorExpression.Assignments.Take(index));
						}

						var expression = a.Expression;
						var memberType = cd.StorageInfo.GetMemberType();

						if (expression.Type != memberType)
						{
							if (expression is SqlPlaceholderExpression placeholder)
								expression = placeholder.WithType(memberType);
							else
								expression = Expression.Convert(expression, memberType);
						}

						var newAssignment = new SqlGenericConstructorExpression.Assignment(cd.StorageInfo, expression, a.IsMandatory, a.IsLoaded);
						newAssignments.Add(newAssignment);
					}
				}

				newAssignments?.Add(a);
			}

			if (newAssignments != null)
				constructorExpression = constructorExpression.ReplaceAssignments(newAssignments.AsReadOnly());

			var constructed = TryConstructObject(constructorExpression, constructType, out error);

			if (constructed == null)
				return null;

			return constructed;
		}

		static object OnEntityCreated(IDataContext context, object entity, TableOptions tableOptions, string? tableName, string? schemaName, string? databaseName, string? serverName)
		{
			if (context is not IInterceptable<IEntityServiceInterceptor> { Interceptor: { } interceptor })
				return entity;

			using (ActivityService.Start(ActivityID.EntityServiceInterceptorEntityCreated))
				return interceptor.EntityCreated(
					new(context, tableOptions, tableName, schemaName, databaseName, serverName), entity);
		}

		static readonly MethodInfo _onEntityCreatedMethodInfo = MemberHelper.MethodOf(() =>
			OnEntityCreated(null!, null!, TableOptions.NotSet, null, null, null, null));

		protected Expression NotifyEntityCreated(Expression expr, SqlTable sqlTable)
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

		static ConstructorInfo? SelectParameterizedConstructor(Type objectType)
		{
			var constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (constructors.Length == 0)
			{
				return null;
			}

			if (constructors.Length > 1)
			{
				var noParams = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
				if (noParams != null)
					return noParams;

				var publicConstructors = constructors.Where(c => c.IsPublic).ToList();

				if (publicConstructors.Count == 1)
					return publicConstructors[0];

				throw new InvalidOperationException($"Type '{objectType.Name}' has ambiguous constructors.");
			}

			return constructors.Length > 0 ? constructors[0] : null;
		}

		public Expression? TryConstructObject(
			SqlGenericConstructorExpression constructorExpression, Type constructType, out string? failureReason)
		{
			failureReason = null;

			if (constructorExpression.ConstructorMethod != null)
			{
				var parameterInfos = constructorExpression.ConstructorMethod.GetParameters();
				if (parameterInfos.Length != constructorExpression.Parameters.Count)
					return null;

				var constructedByMethod = Expression.Call(constructorExpression.ConstructorMethod,
					constructorExpression.Parameters.Select(p => p.Expression));

				return constructedByMethod;
			}

			if (constructType.IsAbstract)
				return null;

			var typeAccessor = TypeAccessor.GetAccessor(constructType);

			if (constructorExpression.Constructor != null)
			{
				var instantiation = TryWithConstructor(typeAccessor, constructorExpression.Constructor, constructorExpression, null);
				if (instantiation != null)
					return instantiation;
			}

			var constructor = SelectParameterizedConstructor(constructType);
			if (constructor != null)
			{
				var unset = constructorExpression.Assignments.Any(a => a.IsMandatory)
					? new List<SqlGenericConstructorExpression.Assignment>(constructorExpression.Assignments.Count)
					: null;

				var instantiation = TryWithConstructor(typeAccessor, constructor,
					constructorExpression, unset);
				if (instantiation != null)
					return instantiation;

				if (unset?.Count > 0)
				{
					failureReason = $"Following members are not assignable: {string.Join(", ", unset.Select(a => a.MemberInfo.Name))}.";
				}
			}

			if (constructType.IsValueType)
			{
				failureReason = null;
				return TryWithConstructor(typeAccessor, null, constructorExpression, null);
			}

			return null;
		}

		public Expression Construct(
			IDataContext                    dataContext,
			MappingSchema                   mappingSchema,
			SqlGenericConstructorExpression constructorExpression,
			ProjectFlags                    flags)
		{
			if (DataContext is IInterceptable<IEntityBindingInterceptor> expressionServices)
				constructorExpression = expressionServices.Interceptor?.ConvertConstructorExpression(constructorExpression) ?? constructorExpression;

			var constructed = TryConstruct(dataContext, mappingSchema, constructorExpression, flags, out var error);
			if (constructed == null)
			{
				throw new InvalidOperationException(
					$"Cannot construct object '{constructorExpression.ObjectType}'. {error ?? "No suitable constructors found."}");
			}

			return constructed;
		}

		public Expression? TryConstruct(IDataContext dataContext, MappingSchema mappingSchema, SqlGenericConstructorExpression constructorExpression,  ProjectFlags flags, out string? error)
		{
			DataContext   = dataContext;
			MappingSchema = mappingSchema;

			switch (constructorExpression.ConstructType)
			{
				case SqlGenericConstructorExpression.CreateType.Full:
				{
					return TryConstructFullEntity(constructorExpression, constructorExpression.ObjectType, flags, true, out error);
				}
				case SqlGenericConstructorExpression.CreateType.MemberInit:
				case SqlGenericConstructorExpression.CreateType.Auto:
				case SqlGenericConstructorExpression.CreateType.Keys:
				case SqlGenericConstructorExpression.CreateType.New:
				case SqlGenericConstructorExpression.CreateType.MethodCall:
				{
					return TryConstructObject(constructorExpression, constructorExpression.ObjectType, out error);
				}
				default:
					throw new NotImplementedException();
			}
		}

		#endregion
	}
}
