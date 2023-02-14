using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using Reflection;
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
						var objExpression = currentPath;
						if (declaringType != objExpression.Type)
							objExpression = Expression.Convert(objExpression, declaringType);

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
					MemberInfo memberInfo;
					Expression assignExpression;

					if (names.Length - 1 > level)
					{
						var propPath = string.Join(".", names.Take(level + 1));
						if (!processed.Add(propPath))
							continue;

						memberInfo = currentPath.Type.GetMember(currentMemberName).Single();

						var newColumns = columns.Where(c => c.MemberName.StartsWith(propPath)).ToList();
						var newPath    = Expression.MakeMemberAccess(currentPath, memberInfo);

						assignExpression = BuildGenericFromMembers(null, newColumns, flags, newPath, level + 1, purpose);
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

			if (level == 0 && context != null && purpose == FullEntityPurpose.Default)
			{
				var loadWith = GetLoadWith(context);

				if (loadWith != null)
				{
					var assignedMembers = new HashSet<MemberInfo>(MemberInfoComparer.Instance);

					foreach (var info in loadWith)
					{
						var memberInfo = info[0].MemberInfo;

						if (!assignedMembers.Add(memberInfo))
							continue;

						var expression = Expression.MakeMemberAccess(currentPath, memberInfo);
						var ad         = GetAssociationDescriptor(expression, out var accessorMember);
						if (ad != null)
						{
							if (!string.IsNullOrEmpty(ad.Storage))
							{
								memberInfo = memberInfo.ReflectedType!.GetMember(ad.Storage!,
									BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy |
									BindingFlags.NonPublic).SingleOrDefault();
							}
						}

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

		#endregion Entity Construction

		#region Generic Entity Construction

		public enum FullEntityPurpose
		{
			Default,
			Insert,
			Update
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(IBuildContext context, Type entityType, ProjectFlags flags)
		{
			entityType = GetTypeForInstantiation(entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			var objectType    = entityDescriptor.ObjectType;
			var refExpression = new ContextRefExpression(objectType, context);

			var generic = BuildGenericFromMembers(context, entityDescriptor.Columns, flags, refExpression, 0, FullEntityPurpose.Default);

			return generic;
		}

		public SqlGenericConstructorExpression BuildFullEntityExpression(Expression root, Type entityType, ProjectFlags flags, FullEntityPurpose purpose)
		{
			entityType = GetTypeForInstantiation(entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			var generic = BuildGenericFromMembers(null, entityDescriptor.Columns, flags, root, 0, purpose);

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
			ConstructorInfo                                   constructorInfo,
			SqlGenericConstructorExpression                   constructorExpression, 
			List<SqlGenericConstructorExpression.Assignment>? missed)
		{
			NewExpression newExpression;

			var loadedColumns = new HashSet<int>();
			var parameters    = constructorInfo.GetParameters();

			if (parameters.Length <= 0)
			{
				newExpression = Expression.New(constructorInfo);
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
				newExpression = Expression.New(constructorInfo, parameterValues);
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

			for (int i = 0; i < constructorExpression.Assignments.Count; i++)
			{
				if (loadedColumns.Contains(i))
					continue;

				var assignment     = constructorExpression.Assignments[i];

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

			if (loadedColumns.Count + bindings.Count + ignored + (dynamicProperties?.Count ?? 0) != constructorExpression.Assignments.Count)
				return null;

			Expression result = Expression.MemberInit(newExpression, bindings);

			//TODO: we can make it in MemberInit
			if (dynamicProperties?.Count > 0 && ed.DynamicColumnSetter != null)
			{
				var generator   = new ExpressionGenerator();
				var objVariable = generator.AssignToVariable(result, "obj");

				foreach (var d in dynamicProperties)
				{
					generator.AddExpression(
						ed.DynamicColumnSetter.GetBody(objVariable, Expression.Constant(d.MemberInfo.Name), d.Expression));
				}

				generator.AddExpression(objVariable);

				result = generator.Build();
			}

			return result;
		}

		public Expression ConstructFullEntity(IBuildContext context,
			SqlGenericConstructorExpression constructorExpression, ProjectFlags flags, bool checkInheritance = true)
		{
			var constructed = TryConstructFullEntity(context, constructorExpression, flags, checkInheritance);

			if (constructed == null)
				throw new InvalidOperationException(
					$"Cannot construct full object '{constructorExpression.ObjectType}'. No suitable constructors found.");

			return constructed;
		}

		public Expression? TryConstructFullEntity(IBuildContext context, SqlGenericConstructorExpression constructorExpression, ProjectFlags flags, bool checkInheritance = true)
		{
			var entityType           = constructorExpression.ObjectType;
			var entityDescriptor     = MappingSchema.GetEntityDescriptor(entityType);
			var contextRefExpression = new ContextRefExpression(entityType, context);

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
							var subConstructor = BuildFullEntityExpression(context, defaultDescriptor.Type, flags);
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
							throw new LinqToDBException("Could not get discriminator ReflectedType.");
						}

						var generator    = new ExpressionGenerator();

						Expression<Func<object, Type, Exception>> throwExpr = (code, et) =>
							new LinqException(
								"Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.",
								code, et);

						var access = Expression.MakeMemberAccess(contextRefExpression.WithType(onType), firstMapping.Discriminator.MemberInfo);

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

						var contextRef = contextRefExpression.WithType(onType);
						var member     = contextRef.Type.GetMemberEx(discriminatorMemberInfo);
						member = discriminatorMemberInfo;
						if (false)
						{
							//TODO: strange behaviour, Member of inheritance has no Discriminator column

							var dynamicPropCall = Expression.Call(Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(discriminatorMemberInfo.GetMemberType()),
								contextRef, Expression.Constant(discriminatorMemberInfo.Name));

							var dynamicSql = ConvertToSqlPlaceholder(context, dynamicPropCall, columnDescriptor: inheritance.Discriminator);

							test = new SqlReaderIsNullExpression(dynamicSql, false);

							// throw new InvalidOperationException(
							// 	$"Type '{contextRef.Type.Name}' has no member '{inheritance.Discriminator.MemberInfo.Name}'");
						}
						else
						{
							var memberAccess = Expression.MakeMemberAccess(contextRef, member);

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

						var subConstructor = BuildFullEntityExpression(context, inheritance.Type, flags);

						var fullEntity = TryConstructFullEntity(context, subConstructor, flags, false);
						if (fullEntity == null)
							return null;
						var tableExpr = Expression.Convert(fullEntity, current.Type);

						current = Expression.Condition(test, tableExpr, current);
					}

					return current;
				}
			}

			return TryConstructObject(MappingSchema, constructorExpression);
		}

		public Expression? TryConstructObject(MappingSchema mappingSchema, SqlGenericConstructorExpression constructorExpression)
		{
			if (constructorExpression.ObjectType.IsAbstract)
				return null;

			var typeAccessor = TypeAccessor.GetAccessor(constructorExpression.ObjectType);

			if (constructorExpression.Constructor != null)
			{
				var instantiation = TryWithConstructor(mappingSchema, typeAccessor, constructorExpression.Constructor, constructorExpression, null);
				if (instantiation != null)
					return instantiation;
			}

			var constructors = constructorExpression.ObjectType
				.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.OrderBy(c => c.GetParameters().Length);

			foreach (var constructor in constructors)
			{
				var instantiation = TryWithConstructor(mappingSchema, typeAccessor, constructor, constructorExpression, null);
				if (instantiation != null)
					return instantiation;
			}

			return null;
			//throw new NotImplementedException("ConstructObject all code paths");

			/*
			for (int i = 0; i < constructors.Length; i++)
			{
				var constructor = constructors[i];

				var missed = new List<SqlGenericConstructorExpression.Assignment>();

				var instantiation = TryWithConstructor(typeAccessor, constructor, constructorExpression, missed);
				if (instantiation != null)
					return instantiation;
			}
		*/
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
					return TryConstructFullEntity(context, constructorExpression, flags);
				}
				case SqlGenericConstructorExpression.CreateType.MemberInit:
				case SqlGenericConstructorExpression.CreateType.Auto:
				case SqlGenericConstructorExpression.CreateType.Keys:
				case SqlGenericConstructorExpression.CreateType.New:
				{
					return TryConstructObject(mappingSchema, constructorExpression);
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
