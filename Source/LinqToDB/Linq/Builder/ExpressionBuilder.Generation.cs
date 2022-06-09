using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using LinqToDB.Expressions;
	using LinqToDB.Reflection;

	partial class ExpressionBuilder
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

		class AssignmentInfo
		{
			public AssignmentInfo(ColumnDescriptor column, Expression expression)
			{
				Column     = column;
				Member     = column.MemberInfo;
				Expression = expression;
			}

			public AssignmentInfo(MemberInfo member, Expression expression)
			{
				Member     = member;
				Expression = expression;
			}

			public ColumnDescriptor? Column     { get; }
			public MemberInfo        Member     { get; }
			public Expression        Expression { get; }
		}

		public Expression BuildEntityExpression(IBuildContext context, Type entityType, ProjectFlags flags, bool checkInheritance = true)
		{
			entityType = GetTypeForInstantiation(entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			if (checkInheritance && flags.HasFlag(ProjectFlags.Expression))
			{
				var inheritanceMappings = entityDescriptor.InheritanceMapping;
				if (inheritanceMappings.Count > 0)
				{
					var defaultDescriptor = inheritanceMappings.FirstOrDefault(x => x.IsDefault);

					Expression defaultExpression;
					if (defaultDescriptor != null)
					{
						defaultExpression = BuildEntityExpression(context, defaultDescriptor.Type, flags, false);
					}
					else
					{
						var firstMapping = inheritanceMappings[0];

						var onType = firstMapping.Discriminator.MemberInfo.ReflectedType;
						if (onType == null)
						{
							throw new LinqToDBException("Could not get discriminator ReflectedType.");
						}

						var generator    = new ExpressionGenerator();

						Expression<Func<object, Type, Exception>> throwExpr = (code, et) =>
							new LinqException(
								"Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.",
								code, et);

						var access = Expression.MakeMemberAccess(
							new ContextRefExpression(onType, context), firstMapping.Discriminator.MemberInfo);

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

						var contextRef = new ContextRefExpression(inheritance.Type, context);

						var test = Equal(
							MappingSchema,
							Expression.MakeMemberAccess(contextRef, inheritance.Discriminator.MemberInfo),
							Expression.Constant(inheritance.Code));

						var tableExpr = Expression.Convert(BuildEntityExpression(context, inheritance.Type, flags, false), current.Type);

						current = Expression.Condition(test, tableExpr, current);
					}

					return current;
				}
			}

			var members = BuildMembers(context, entityDescriptor, flags);

			if (flags.HasFlag(ProjectFlags.SQL) || flags.HasFlag(ProjectFlags.Test))
			{
				var assignments = members
					.Select(x => new SqlGenericConstructorExpression.Assignment(x.Member, x.Expression, x.Column.MemberAccessor.HasSetter))
					.ToList();

				return new SqlGenericConstructorExpression(true, entityType, null, new ReadOnlyCollection<SqlGenericConstructorExpression.Assignment>(assignments));
			}

			//if (flags.HasFlag(ProjectFlags.Expression))
			{
				List<LambdaExpression>? postProcess = null;

				var expr =
					IsRecord(MappingSchema.GetAttributes<Attribute>(entityType), out var _)
						?
						BuildRecordConstructor(entityDescriptor, members)
						: IsAnonymous(entityType)
							? BuildRecordConstructor(entityDescriptor, members)
							:
							BuildDefaultConstructor(entityDescriptor, members, ref postProcess);

				if (flags.HasFlag(ProjectFlags.Expression))
				{
					BuildCalculatedColumns(context, entityDescriptor, expr.Type, ref postProcess);

					//TODO:
					/*
					expr = ProcessExpression(expr);
					expr = NotifyEntityCreated(expr);
					*/
				}

				return new ContextConstructionExpression(context, expr, postProcess);
			}

			throw new NotImplementedException();
		}

		void BuildCalculatedColumns(IBuildContext context, EntityDescriptor entityDescriptor, Type objectType, ref List<LambdaExpression>? postProcess)
		{
			if (!entityDescriptor.HasCalculatedMembers)
				return;

			var contextRef = new ContextRefExpression(objectType, context);

			var param    = Expression.Parameter(objectType, "e");

			postProcess ??= new List<LambdaExpression>();

			foreach (var member in entityDescriptor.CalculatedMembers!)
			{
				var assign = Expression.Assign(Expression.MakeMemberAccess(param, member.MemberInfo),
					Expression.MakeMemberAccess(contextRef, member.MemberInfo));

				var assignLambda = Expression.Lambda(assign, param);

				postProcess.Add(assignLambda);
			}
		}

		List<AssignmentInfo> BuildMembers(IBuildContext context,
			EntityDescriptor entityDescriptor, ProjectFlags flags)
		{
			var members       = new List<AssignmentInfo>();
			var objectType    = entityDescriptor.ObjectType;
			var refExpression = new ContextRefExpression(objectType, context);

			var checkForKey = flags.HasFlag(ProjectFlags.Keys) && entityDescriptor.Columns.Any(c => c.IsPrimaryKey);

			foreach (var column in entityDescriptor.Columns)
			{
				if (checkForKey && !column.IsPrimaryKey)
					continue;

				Expression me;
				if (column.MemberName.Contains('.'))
				{
					var memberNames = column.MemberName.Split('.');

					me = memberNames.Aggregate((Expression)refExpression, Expression.PropertyOrField);
				}
				else
				{
					var mi = refExpression.Type.GetMemberEx(column.MemberInfo);

					var objExpression = refExpression;
					if (mi == null)
					{
						if (column.MemberInfo.ReflectedType == null)
							continue;

						// Skip fake assignments
						if (flags.HasFlag(ProjectFlags.Expression))
							continue;

						objExpression = new ContextRefExpression(column.MemberInfo.ReflectedType, context);
						mi = column.MemberInfo;
					};

					me = Expression.MakeMemberAccess(objExpression, mi);
				}

				var sqlExpression = context.Builder.BuildSqlExpression(new Dictionary<Expression, Expression>(), context, me, flags);
				members.Add(new AssignmentInfo(column, sqlExpression));
			}

			var loadWith = GetLoadWith(context);

			if (loadWith != null)
			{
				var contextRef = new ContextRefExpression(objectType, context);

				foreach (var info in loadWith)
				{
					var memberInfo = info[0].MemberInfo;
					var expression = Expression.MakeMemberAccess(contextRef, memberInfo);
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
					members.Add(new AssignmentInfo(memberInfo, expression));
				}
			}

			return members;
		}

		Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, List<AssignmentInfo> members, ref List<LambdaExpression>? postProcess)
		{
			var constructor = SuggestConstructor(entityDescriptor);

			List<ColumnDescriptor>? ignoredColumns = null;
			var newExpression = BuildNewExpression(constructor, members, ref ignoredColumns);

			var initExpr = Expression.MemberInit(newExpression,
				members
					.Where(m => m.Column == null || ignoredColumns?.Contains(m.Column) != true)
					// IMPORTANT: refactoring this condition will affect hasComplex variable calculation below
					.Where(static m => m.Column?.MemberAccessor.IsComplex != true)
					.Select(static m => new
					{
						Storage = m.Column?.StorageInfo ?? m.Member,
						Expression = m.Expression
					})
					.Where(static m => m.Storage is not PropertyInfo prop || prop.CanWrite)
					.Select(static m => (MemberBinding)Expression.Bind(m.Storage, m.Expression))
			);

			foreach (var ai in members)
			{
				if (ai.Column?.MemberAccessor.IsComplex == true)
				{
					postProcess ??= new List<LambdaExpression>();

					var setter = ai.Column.MemberAccessor.SetterExpression!;
					setter = Expression.Lambda(setter.GetBody(setter.Parameters[0], ai.Expression), setter.Parameters[0]);

					postProcess.Add(setter);
				}
			}

			return initExpr;
		}

		private static ConstructorInfo SuggestConstructor(EntityDescriptor entityDescriptor)
		{
			var constructors = entityDescriptor.ObjectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public |
			                                                               BindingFlags.NonPublic);

			if (constructors.Length == 0)
			{
				throw new InvalidOperationException(
					$"No constructors found for '{entityDescriptor.ObjectType.Name}.'");
			}

			// public without parameters
			foreach (var info in constructors)
			{
				if (info.IsPublic && info.GetParameters().Length == 0)
					return info;
			}

			//TODO: Use MatchParameter to calculate which constructor is suitable
			// nonpublic without parameters
			foreach (var info in constructors)
			{
				if (!info.IsPublic && info.GetParameters().Length == 0)
					return info;
			}

			// first public with parameters
			foreach (var info in constructors)
			{
				if (info.IsPublic)
					return info;
			}

			// first nonpublic
			foreach (var info in constructors)
			{
				if (!info.IsPublic)
					return info;
			}

			throw new InvalidOperationException(
				$"Could not decide which constructor should be used for '{entityDescriptor.ObjectType.Name}.'");
		}

		private static int MatchParameter(ParameterInfo parameter, List<AssignmentInfo> members)
		{
			var found = members.FindIndex(x =>
				x.Column.MemberType == parameter.ParameterType &&
				x.Column.MemberName == parameter.Name);

			if (found < 0)
			{
				found = members.FindIndex(x =>
					x.Column.MemberType == parameter.ParameterType &&
					x.Column.MemberName.Equals(parameter.Name,
						StringComparison.InvariantCultureIgnoreCase));
			}

			return found;
		}

		NewExpression BuildNewExpression(ConstructorInfo constructor, List<AssignmentInfo> members, ref List<ColumnDescriptor>? ignoredColumns)
		{
			var parameters = constructor.GetParameters();

			if (parameters.Length <= 0)
			{
				return Expression.New(constructor);
			}

			var parameterValues = new List<Expression>();

			foreach (var parameterInfo in parameters)
			{
				var idx = MatchParameter(parameterInfo, members);

				if (idx >= 0)
				{
					var ai = members[idx];
					parameterValues.Add(ai.Expression);
					ignoredColumns ??= new List<ColumnDescriptor>();
					
					if (ai.Column != null)
					{
						ignoredColumns.Add(ai.Column);
					}
				}
				else
				{
					parameterValues.Add(Expression.Constant(
						MappingSchema.GetDefaultValue(parameterInfo.ParameterType), parameterInfo.ParameterType));
				}
			}

			return Expression.New(constructor, parameterValues);

		}

		Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, List<AssignmentInfo> members)
		{
			var ctor = entityDescriptor.ObjectType.GetConstructors().Single();

			var ignoredColumns = new List<ColumnDescriptor>();
			var newExpression  = BuildNewExpression(ctor, members, ref ignoredColumns);

			return newExpression;
		}

		#endregion Entity Construction

		#region Generic Entity Construction

		public SqlGenericConstructorExpression BuildFullEntityExpression(IBuildContext context, Type entityType, ProjectFlags flags)
		{
			entityType = GetTypeForInstantiation(entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			var members = BuildMembers(context, entityDescriptor, flags);

			var assignments = members
				.Select(x => new SqlGenericConstructorExpression.Assignment(x.Member, x.Expression, x.Column?.MemberAccessor.HasSetter == true))
				.ToList();

			if (!flags.HasFlag(ProjectFlags.Keys))
				BuildCalculatedColumns(context, entityDescriptor, entityType, assignments);

			return new SqlGenericConstructorExpression(true, entityType, null, new ReadOnlyCollection<SqlGenericConstructorExpression.Assignment>(assignments));
		}

		void BuildCalculatedColumns(IBuildContext context, EntityDescriptor entityDescriptor, Type objectType, List<SqlGenericConstructorExpression.Assignment> assignments)
		{
			if (!entityDescriptor.HasCalculatedMembers)
				return;

			var contextRef = new ContextRefExpression(objectType, context);

			foreach (var member in entityDescriptor.CalculatedMembers!)
			{
				var assignment = new SqlGenericConstructorExpression.Assignment(member.MemberInfo,
					Expression.MakeMemberAccess(contextRef, member.MemberInfo), true);

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

		private static int MatchParameter(ParameterInfo parameter, ReadOnlyCollection<SqlGenericConstructorExpression.Assignment> members)
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


		private Expression? TryWithConstructor(
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
							MappingSchema.GetDefaultValue(parameterInfo.ParameterType), parameterInfo.ParameterType));
					}
				}

				newExpression = Expression.New(constructorInfo, parameterValues);
			}


			if (loadedColumns.Count == constructorExpression.Assignments.Count)
			{
				// Everything is fit into parameters
				return newExpression;
			}

			var bindings = new List<MemberBinding>(constructorExpression.Assignments.Count - loadedColumns.Count);
			var ignored  = 0;

			for (int i = 0; i < constructorExpression.Assignments.Count; i++)
			{
				if (loadedColumns.Contains(i))
					continue;

				var assignment     = constructorExpression.Assignments[i];

				// handling inheritance
				if (assignment.MemberInfo.DeclaringType?.IsAssignableFrom(typeAccessor.Type) == true)
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
				else
				{
					++ignored;
				}
			}

			if (loadedColumns.Count + bindings.Count + ignored != constructorExpression.Assignments.Count)
				return null;

			return Expression.MemberInit(newExpression, bindings);
		}

		public Expression TryConstructFullEntity(IBuildContext context, SqlGenericConstructorExpression constructorExpression, ProjectFlags flags, bool checkInheritance = true)
		{
			var entityType       = constructorExpression.ObjectType;
			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			if (checkInheritance && flags.HasFlag(ProjectFlags.Expression))
			{
				var inheritanceMappings = entityDescriptor.InheritanceMapping;
				if (inheritanceMappings.Count > 0)
				{
					var defaultDescriptor = inheritanceMappings.FirstOrDefault(x => x.IsDefault);

					Expression defaultExpression;
					if (defaultDescriptor != null)
					{
						defaultExpression = TryConstructFullEntity(context, constructorExpression, flags, false);
					}
					else
					{
						var firstMapping = inheritanceMappings[0];

						var onType = firstMapping.Discriminator.MemberInfo.ReflectedType;
						if (onType == null)
						{
							throw new LinqToDBException("Could not get discriminator ReflectedType.");
						}

						var generator    = new ExpressionGenerator();

						Expression<Func<object, Type, Exception>> throwExpr = (code, et) =>
							new LinqException(
								"Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.",
								code, et);

						var access = Expression.MakeMemberAccess(
							new ContextRefExpression(onType, context), firstMapping.Discriminator.MemberInfo);

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

						var contextRef = new ContextRefExpression(inheritance.Type, context);

						var test = Equal(
							MappingSchema,
							Expression.MakeMemberAccess(contextRef, inheritance.Discriminator.MemberInfo),
							Expression.Constant(inheritance.Code));

						var subConstructor = BuildFullEntityExpression(context, inheritance.Type, flags);

						var tableExpr = Expression.Convert(TryConstructFullEntity(context, subConstructor, flags, false), current.Type);

						current = Expression.Condition(test, tableExpr, current);
					}

					return current;
				}
			}

			return ConstructObject(constructorExpression);
		}

		public Expression ConstructObject(SqlGenericConstructorExpression constructorExpression)
		{
			var typeAccessor = TypeAccessor.GetAccessor(constructorExpression.ObjectType);

			var constructors = constructorExpression.ObjectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			for (int i = 0; i < constructors.Length; i++)
			{
				var constructor   = constructors[i];
				var instantiation = TryWithConstructor(typeAccessor, constructor, constructorExpression, null);
				if (instantiation != null)
					return instantiation;
			}

			throw new NotImplementedException("ConstructObject all code paths");

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

		public Expression TryConstruct(SqlGenericConstructorExpression constructorExpression, IBuildContext context,  ProjectFlags flags)
		{
			switch (constructorExpression.ConstructType)
			{
				case SqlGenericConstructorExpression.CreateType.Full:
				{
					var expr = TryConstructFullEntity(context, constructorExpression, flags);

					return expr;
				}
				case SqlGenericConstructorExpression.CreateType.Unknown:
				{
					return ConstructObject(constructorExpression);
				}
				default:
					throw new NotImplementedException();
			}
		}
		

		#endregion

		#region Helpers

		static bool IsRecord(Attribute[] attrs, out int sequence)
		{
			sequence = -1;
			var compilationMappingAttr = attrs.FirstOrDefault(static attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute");
			var cliMutableAttr         = attrs.FirstOrDefault(static attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CLIMutableAttribute");

			if (compilationMappingAttr != null)
			{
				// https://github.com/dotnet/fsharp/blob/1fcb351bb98fe361c7e70172ea51b5e6a4b52ee0/src/fsharp/FSharp.Core/prim-types.fsi
				// entityType = 3
				if (Convert.ToInt32(((dynamic)compilationMappingAttr).SourceConstructFlags) == 3)
					return false;

				sequence = ((dynamic)compilationMappingAttr).SequenceNumber;
			}

			return compilationMappingAttr != null && cliMutableAttr == null;
		}

		bool IsAnonymous(Type type)
		{
			if (!type.IsPublic     &&
			    type.IsGenericType &&
			    (type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal) ||
			     type.Name.StartsWith("VB$AnonymousType",   StringComparison.Ordinal)))
			{
				return MappingSchema.GetAttribute<CompilerGeneratedAttribute>(type) != null;
			}

			return false;
		}			
			
		#endregion
		
	}
}
