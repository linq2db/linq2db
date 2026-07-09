using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Metrics;
using LinqToDB.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	internal abstract class EntityConstructorBase
	{
		public MappingSchema MappingSchema { get; private set; } = default!;
		public IDataContext DataContext { get; private set; } = default!;

		public DataOptions DataOptions => DataContext.Options;

		#region Entity Construction

		public virtual LoadWithEntity? GetTableLoadWith(Expression path)
		{
			return null;
		}

		protected SqlGenericConstructorExpression BuildGenericFromMembers(
			IReadOnlyCollection<ColumnDescriptor> columns, ProjectFlags flags, Expression currentPath, Expression constructionRoot, int level,
			FullEntityPurpose purpose)
		{
			var members          = new List<SqlGenericConstructorExpression.Assignment>();
			var entityDescriptor = MappingSchema.GetEntityDescriptor(currentPath.Type);
			var buildCalculated  = ShouldBuildCalculatedColumns(constructionRoot, flags, purpose);

			// A calculated column (ExpressionMethodAttribute.IsColumn=true) may also be mapped as a physical
			// column — e.g. fluent .Property(e => e.X).HasAttribute(new ExpressionMethodAttribute(...) { IsColumn = true }),
			// where .Property() forces IsColumn. Such a member's value comes from BuildCalculatedColumns (the
			// expanded substitution body), not a physical column read, so exclude it from the physical-column
			// assignments below — otherwise the entity emits both a bogus column read and the calculated
			// expression for the same member (see #5540: the removed blanket ConvertExpressionTree pass used to
			// rewrite that stray read into the same substitution).
			// For an inheritance root, InitInheritanceMapping merges derived types' Columns into this
			// descriptor but NOT their CalculatedMembers, so a calculated member declared on a subtype would
			// otherwise leak through as a stray column read — union the derived types' calculated members too.
			HashSet<MemberInfo>? calculatedMembers = null;
			if (buildCalculated)
			{
				if (entityDescriptor.HasCalculatedMembers)
					calculatedMembers = entityDescriptor.CalculatedMembers!.Select(m => m.MemberInfo).ToHashSet(MemberInfoComparer.Instance);

				foreach (var inheritance in entityDescriptor.InheritanceMapping)
				{
					var derivedDescriptor = MappingSchema.GetEntityDescriptor(inheritance.Type);
					if (derivedDescriptor.HasCalculatedMembers)
						(calculatedMembers ??= new HashSet<MemberInfo>(MemberInfoComparer.Instance))
							.UnionWith(derivedDescriptor.CalculatedMembers!.Select(m => m.MemberInfo));
				}
			}

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
				// A physical column shared by several inheritance siblings (distinct members mapped to the
				// same column) must be projected only once per read-shape. Cache the value expression built
				// for each distinct read-shape (member type + value converter) of a physical column and reuse
				// it for later siblings that read the column identically, so they collapse onto a single SELECT
				// column. Each assignment keeps its own member as the bind target, so the per-type
				// discriminator switch still assigns each subtype's member. A list per physical column is kept
				// (not just the first shape) so that two later siblings sharing a shape that differs from the
				// first-seen one still collapse. Columns with distinct physical names stay separate.
				var sharedColumns = new Dictionary<string, List<(ColumnDescriptor Column, Expression Expression)>>(StringComparer.Ordinal);

				foreach (var column in columns)
				{
					if (column.SkipOnEntityFetch)
						continue;

					if (calculatedMembers?.Contains(column.MemberInfo) == true)
						continue;

					if (column.MemberName.Contains('.', StringComparison.Ordinal) && !column.MemberInfo.Name.Contains('.', StringComparison.Ordinal))
					{
						hasNested = true;
					}
					else
					{
						var declaringType = column.MemberInfo.DeclaringType!;

						// Target ReflectedType to DeclaringType for better caching
						//
						var memberInfo = declaringType.GetMemberEx(column.MemberInfo) ??
						                 throw new InvalidOperationException();

						Expression? me = null;
						if (sharedColumns.TryGetValue(column.ColumnName, out var shapes))
						{
							foreach (var shape in shapes)
							{
								if (shape.Column.MemberType == column.MemberType
									&& ReferenceEquals(shape.Column.ValueConverter, column.ValueConverter))
								{
									me = shape.Expression;
									break;
								}
							}
						}

						if (me == null)
						{
							var objExpression = SequenceHelper.EnsureType(currentPath, declaringType);
							me = MakeAssignExpression(objExpression, memberInfo, column);
							if (shapes == null)
								sharedColumns[column.ColumnName] = shapes = [];
							shapes.Add((column, me));
						}

						members.Add(new SqlGenericConstructorExpression.Assignment(memberInfo, me,
							column.MemberAccessor.HasSetter, false));
					}

				}
			}

			if (level > 0 || hasNested)
			{
				var processed = new HashSet<string>(StringComparer.Ordinal);
				foreach (var column in columns)
				{
					if (column.SkipOnEntityFetch)
						continue;

					if (calculatedMembers?.Contains(column.MemberInfo) == true)
						continue;

					if (!column.MemberName.Contains('.', StringComparison.Ordinal))
						continue;

					// explicit interface implementation
					//
					if (column.MemberInfo.Name.Contains('.', StringComparison.Ordinal))
						continue;

					var names = column.MemberName.Split('.');

					if (level >= names.Length)
						continue;

					var currentMemberName = names[level];
					MemberInfo? memberInfo;
					Expression assignExpression;

					if (names.Length - 1 > level)
					{
						var propPath = string.JoinStrings('.', names.Take(level + 1));
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

						var prefix     = $"{propPath}.";
						var newColumns = columns.Where(c => c.MemberName.StartsWith(prefix, StringComparison.Ordinal)).ToList();
						var newPath    = MakeAssignExpression(currentPath, memberInfo, column);

						assignExpression = BuildGenericFromMembers(newColumns, flags, newPath, constructionRoot, level + 1, purpose);
					}
					else
					{
						memberInfo       = column.MemberInfo;
						assignExpression = MakeAssignExpression(currentPath, memberInfo, column);
					}

					members.Add(new SqlGenericConstructorExpression.Assignment(memberInfo, assignExpression, column.MemberAccessor.HasSetter, false));
				}
			}

			if (buildCalculated)
			{
				// currentPath may have been converted to an inheritance subtype while resolving a flattened
				// (dotted-MemberName) column above, so re-resolve the descriptor here instead of reusing the
				// entry-type one — BuildCalculatedColumns must expand the calculated members of the actual
				// constructed type, not the base.
				var constructedDescriptor = MappingSchema.GetEntityDescriptor(currentPath.Type);
				BuildCalculatedColumns(currentPath, constructedDescriptor, constructedDescriptor.ObjectType, members);
			}

			if (!flags.IsKeys() && level == 0 && purpose == FullEntityPurpose.Default)
			{
				var loadWith = GetTableLoadWith(currentPath);

				if (loadWith?.MembersToLoad?.Count > 0)
				{
					var assignedMembers = new HashSet<MemberInfo>(MemberInfoComparer.Instance);

					foreach (var info in loadWith.MembersToLoad)
					{
						if (!info.ShouldLoad)
							continue;

						var memberInfo = info.MemberInfo;

						if (!assignedMembers.Add(memberInfo))
							continue;

						var currentMember = currentPath.Type.GetMemberEx(memberInfo);

						if (currentMember == null)
							continue;

						Expression expression = Expression.MakeMemberAccess(currentPath, currentMember);

						if (DataOptions.LinqOptions.ImplicitCollectionLoading == ImplicitCollectionLoading.Throw)
						{
							// When implicit collection loading is set to Throw, tag explicit (LoadWith/ThenLoad) collection loads
							// so ImplicitEagerLoadGuardVisitor lets them through; unmarked (implicit) projections throw.
							expression = new MarkerExpression(expression, MarkerType.ExplicitEagerLoad);
						}

						members.Add(
							new SqlGenericConstructorExpression.Assignment(currentMember, expression, true, true));
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
			if (!memberExpr.Type.IsNullableOrReferenceType)
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

			// Include sibling columns so each concrete TPH type can reference its own physical column (same C# member, different DB column names).
			var columns = entityDescriptor.InheritanceSiblingColumns.Count == 0
				? entityDescriptor.Columns
				: (IReadOnlyCollection<ColumnDescriptor>)entityDescriptor.Columns.Concat(entityDescriptor.InheritanceSiblingColumns).ToList();

			var generic = BuildGenericFromMembers(columns, flags, refExpression, refExpression, 0, purpose);

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

		void BuildCalculatedColumns(Expression root, EntityDescriptor entityDescriptor, Type objectType, List<SqlGenericConstructorExpression.Assignment> assignments)
		{
			HashSet<MemberInfo>? seen = null;

			AddCalculatedColumns(root, entityDescriptor, objectType, assignments, ref seen);

			// Calculated members declared on inheritance subtypes are not merged into the root descriptor
			// (InitInheritanceMapping merges Columns only — see BuildGenericFromMembers), so expand them
			// explicitly against each subtype, casting the root to that subtype. Single-table inheritance
			// keeps all columns in one table, so the substitution's referenced columns exist for every row.
			foreach (var inheritance in entityDescriptor.InheritanceMapping)
			{
				var derived = MappingSchema.GetEntityDescriptor(inheritance.Type);
				AddCalculatedColumns(root, derived, derived.ObjectType, assignments, ref seen);
			}
		}

		void AddCalculatedColumns(Expression root, EntityDescriptor entityDescriptor, Type objectType, List<SqlGenericConstructorExpression.Assignment> assignments, ref HashSet<MemberInfo>? seen)
		{
			if (!entityDescriptor.HasCalculatedMembers)
				return;

			if (root.Type != objectType)
				root = Expression.Convert(root, objectType);

			foreach (var member in entityDescriptor.CalculatedMembers!)
			{
				// A member may appear on both the base and a derived descriptor (override) — expand once.
				if (!(seen ??= new HashSet<MemberInfo>(MemberInfoComparer.Instance)).Add(member.MemberInfo))
					continue;

				// Calculated columns are exactly the ExpressionMethodAttribute.IsColumn=true members.
				// Expand their substitution body here, where the IsColumn distinction is known, instead
				// of relying on a blanket ConvertExpressionTree pass over the whole entity afterwards
				// (which also rewrites IsColumn=false column reads at materialization — see issue #5540).
				var memberAccess = ExposeCalculatedColumn(Expression.MakeMemberAccess(root, member.MemberInfo));

				var assignment = new SqlGenericConstructorExpression.Assignment(member.MemberInfo,
					memberAccess, true, false);

				assignments.Add(assignment);
			}
		}

		/// <summary>
		/// Whether calculated columns (<see cref="ExpressionMethodAttribute"/> with <c>IsColumn = true</c>) should be
		/// expanded for the current construction. Defaults to <see langword="false"/>, so non-query construction paths (e.g.
		/// reader-based materialization from raw SQL in <c>RecordReaderBuilder</c>) never process calculated columns.
		/// The query-building constructor overrides this to opt in only for table-backed full-entity materialization.
		/// </summary>
		protected virtual bool ShouldBuildCalculatedColumns(Expression constructionRoot, ProjectFlags flags, FullEntityPurpose purpose) => false;

		/// <summary>
		/// Expands a calculated column's <see cref="ExpressionMethodAttribute"/> substitution body. Only invoked when
		/// <see cref="ShouldBuildCalculatedColumns"/> returns <see langword="true"/> (i.e. from the query-building constructor,
		/// which overrides this to run <c>ConvertExpressionTree</c> on the member access). The base implementation is
		/// an inert fallback that returns the access unchanged.
		/// </summary>
		protected virtual Expression ExposeCalculatedColumn(Expression memberAccess) => memberAccess;

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

			found = FindIndex(
				members,
				x =>
					x.MemberInfo.GetMemberType() == parameter.ParameterType &&
					string.Equals(x.MemberInfo.Name, parameter.Name, StringComparison.Ordinal)
			);

			if (found < 0)
			{
				found = FindIndex(members, x =>
					x.MemberInfo.GetMemberType() == parameter.ParameterType &&
					x.MemberInfo.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
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
					if (assignment.MemberInfo.IsDynamicColumnProperty)
					{
						dynamicProperties ??= new List<SqlGenericConstructorExpression.Assignment>();
						dynamicProperties.Add(assignment);
					}
					else
					{
						var memberAccessor = typeAccessor.GetOrCreateMemberAccessor(assignment.MemberInfo.Name);

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

			if (additionalSteps != null || (dynamicProperties?.Count > 0 && ed.DynamicColumnSetter != null))
			{
				var generator   = new ExpressionGenerator();
				var objVariable = generator.AssignToVariable(result, "obj");

				if (dynamicProperties != null)
				{
					if (ed.DynamicColumnStorageInitializer != null)
						generator.AddExpression(ed.DynamicColumnStorageInitializer.GetBody(objVariable));

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
				var inheritanceMappings   = entityDescriptor.InheritanceMapping;
				var perSubtypeConstructor = false;

				// An abstract intermediate type (e.g. OfType<TIntermediate>()) carries no inheritance
				// mappings of its own — they live on the hierarchy root. Resolve the root's mappings, keep the
				// concrete subtypes assignable to this type, and build a dedicated constructor per subtype (the
				// intermediate's own constructor would lack the subtype-specific columns).
				if (inheritanceMappings.Count == 0 && entityType.IsAbstract)
				{
					for (var baseType = entityType.BaseType; baseType != null; baseType = baseType.BaseType)
					{
						var rootMappings = MappingSchema.GetEntityDescriptor(baseType).InheritanceMapping;
						if (rootMappings.Count > 0)
						{
							inheritanceMappings   = rootMappings.Where(m => entityType.IsAssignableFrom(m.Type)).ToList();
							perSubtypeConstructor = true;
							break;
						}
					}
				}

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

						Expression tableExpr;
						if (perSubtypeConstructor)
						{
							// Build the concrete subtype against the hierarchy root so it gets its own columns.
							var subConstructor = BuildFullEntityExpressionInternal(rootReference, inheritance.Type, flags, FullEntityPurpose.Default);
							tableExpr = Expression.Convert(ConstructFullEntity(subConstructor, flags, false), current.Type);
						}
						else
						{
							var fullEntity = TryConstructFullEntity(constructorExpression, inheritance.Type, flags, false, out error);
							if (fullEntity == null)
								return null;
							tableExpr = Expression.Convert(fullEntity, current.Type);
						}

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
			return objectType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) switch
			{
				[] => null,

				[{ } c] => c,

				var constructors
					when constructors.FirstOrDefault(c => c.GetParameters().Length == 0) is { } noParams =>
					noParams,

				var constructors
					when constructors.Where(c => c.IsPublic).Take(2).ToList() is [{ } pc] =>
					pc,

				_ => throw new InvalidOperationException($"Type '{objectType.Name}' has ambiguous constructors."),
			};
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

			return constructorExpression.ConstructType switch
			{
				SqlGenericConstructorExpression.CreateType.Full =>
					TryConstructFullEntity(constructorExpression, constructorExpression.ObjectType, flags, true, out error),

				SqlGenericConstructorExpression.CreateType.MemberInit or
				SqlGenericConstructorExpression.CreateType.Auto or
				SqlGenericConstructorExpression.CreateType.Keys or
				SqlGenericConstructorExpression.CreateType.New or
				SqlGenericConstructorExpression.CreateType.MethodCall =>
					TryConstructObject(constructorExpression, constructorExpression.ObjectType, out error),

				_ =>
					throw new NotSupportedException(),
			};
		}

		#endregion
	}
}
