using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	partial class ExpressionBuilder
	{
		#region Entity Construction

		public Type GetTypeForInstantiation(Type entityType)
		{
			// choosing type that can be instantiated
			if ((entityType.IsInterface || entityType.IsAbstract) && !(entityType.IsInterface || entityType.IsAbstract))
			{
				throw new NotImplementedException();
			}
			return entityType;
		}


		public Expression BuildEntityExpression(IBuildContext context, Type entityType, ProjectFlags flags)
		{
			entityType = GetTypeForInstantiation(entityType);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(entityType);

			List<LambdaExpression>? postProcess = null;

			var members = BuildMembers(context, entityDescriptor, flags);

			var expr =
				IsRecord(MappingSchema.GetAttributes<Attribute>(entityType), out var _) ?
					BuildRecordConstructor (entityDescriptor, members) :
					IsAnonymous(entityType) ?
						BuildRecordConstructor (entityDescriptor, members) :
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

		List<(ColumnDescriptor column, Expression expr)> BuildMembers(IBuildContext context,
			EntityDescriptor entityDescriptor, ProjectFlags projectFlags)
		{
			var members       = new List<(ColumnDescriptor column, Expression expr)>();
			var objectType    = entityDescriptor.ObjectType;
			var refExpression = new ContextRefExpression(objectType, context);

			foreach (var column in entityDescriptor.Columns)
			{
				Expression me;
				if (column.MemberName.Contains('.'))
				{
					var memberNames = column.MemberName.Split('.');

					me = memberNames.Aggregate((Expression)refExpression, Expression.PropertyOrField);
				}
				else
				{
					me = Expression.MakeMemberAccess(refExpression, column.MemberInfo);
				}

				var sqlExpression = context.Builder.BuildSqlExpression(new Dictionary<Expression, Expression>(), context, me, projectFlags);
				members.Add((column, sqlExpression));
			}

			return members;
		}

		Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, List<(ColumnDescriptor column, Expression expr)> members, ref List<LambdaExpression>? postProcess)
		{
			var constructor = SuggestConstructor(entityDescriptor);

			List<ColumnDescriptor>? ignoredColumns = null;
			var newExpression = BuildNewExpression(constructor, members, ref ignoredColumns);

			var initExpr = Expression.MemberInit(newExpression,
				members
					.Where(m => ignoredColumns?.Contains(m.column) != true)
					// IMPORTANT: refactoring this condition will affect hasComplex variable calculation below
					.Where(static m => !m.column.MemberAccessor.IsComplex)
					.Select(static m => (MemberBinding)Expression.Bind(m.column.StorageInfo, m.expr))
			);

			//var loadWith = GetLoadWith();

			foreach (var (column, expr) in members)
			{
				if (column.MemberAccessor.IsComplex)
				{
					postProcess ??= new List<LambdaExpression>();

					var setter = column.MemberAccessor.SetterExpression!;
					setter = Expression.Lambda(setter.GetBody(setter.Parameters[0], expr), setter.Parameters[0]);

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

		private static int MatchParameter(ParameterInfo parameter, List<(ColumnDescriptor column, Expression expr)> members)
		{
			var found = members.FindIndex(x =>
				x.column.MemberType == parameter.ParameterType &&
				x.column.MemberName == parameter.Name);

			if (found < 0)
			{
				found = members.FindIndex(x =>
					x.column.MemberType == parameter.ParameterType &&
					x.column.MemberName.Equals(parameter.Name,
						StringComparison.InvariantCultureIgnoreCase));
			}

			return found;
		}

		NewExpression BuildNewExpression(ConstructorInfo constructor, List<(ColumnDescriptor column, Expression expr)> members, ref List<ColumnDescriptor>? ignoredColumns)
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
					var (column, expr) = members[idx];
					parameterValues.Add(expr);
					ignoredColumns ??= new List<ColumnDescriptor>();
					ignoredColumns.Add(column);
				}
				else
				{
					parameterValues.Add(Expression.Constant(
						MappingSchema.GetDefaultValue(parameterInfo.ParameterType), parameterInfo.ParameterType));
				}
			}

			return Expression.New(constructor, parameterValues);

		}

		Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, List<(ColumnDescriptor column, Expression expr)> members)
		{
			var ctor = entityDescriptor.ObjectType.GetConstructors().Single();

			var ignoredColumns = new List<ColumnDescriptor>();
			var newExpression  = BuildNewExpression(ctor, members, ref ignoredColumns);

			return newExpression;
		}

		#endregion Entity Construction

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
