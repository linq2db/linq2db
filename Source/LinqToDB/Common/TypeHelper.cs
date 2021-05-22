﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Common
{
	public static class TypeHelper
	{
		/// <summary>
		/// Registers type transformation for generic arguments.
		/// </summary>
		/// <param name="templateType">Type from generic definition.</param>
		/// <param name="replaced">Concrete type which needs mapping to generic definition.</param>
		/// <param name="templateArguments">Generic arguments of generic definition.</param>
		/// <param name="typeMappings">Accumulator dictionary for registered mappings.</param>
		public static void RegisterTypeRemapping(Type templateType, Type replaced, Type[] templateArguments, Dictionary<Type, Type> typeMappings)
		{
			foreach (var pair in EnumTypeRemapping(templateType, replaced, templateArguments))
			{
				if (!typeMappings.TryGetValue(pair.Item1, out var value))
				{
					typeMappings.Add(pair.Item1, pair.Item2);
				}
				else
				{
					if (value != pair.Item2)
						throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Enumerates type transformation for generic arguments.
		/// </summary>
		/// <param name="templateType">Type from generic definition.</param>
		/// <param name="replaced">Concrete type which needs mapping to generic definition.</param>
		/// <param name="templateArguments">Generic arguments of generic definition.</param>
		public static IEnumerable<Tuple<Type, Type>> EnumTypeRemapping(Type templateType, Type replaced, Type[] templateArguments)
		{
			if (templateType.IsGenericType)
			{
				var currentTemplateArguments = templateType.GetGenericArguments();
				var replacedArguments        = replaced.GetGenericArguments();
				if (replacedArguments.Length == 0 && replaced.IsArray)
					replacedArguments = new[] { replaced.GetElementType()! };

				for (int i = 0; i < currentTemplateArguments.Length; i++)
				{
					foreach (var pair in EnumTypeRemapping(currentTemplateArguments[i], replacedArguments[i], templateArguments))
					{
						yield return pair;
					}
				}
			}
			else
			{
				var idx = Array.IndexOf(templateArguments, templateType);
				if (idx >= 0)
				{
					yield return Tuple.Create(templateType, replaced);
				}

				if (templateType.IsArray && replaced.IsArray)
				{
					var templateElement = templateType.GetElementType()!;
					var replacedElement = replaced.GetElementType()!;
					foreach (var pair in EnumTypeRemapping(templateElement, replacedElement, templateArguments))
					{
						yield return pair;
					}
				}
			}
		}

		/// <summary>
		/// Creates MethodCallExpression without specifying generic parameters.
		/// </summary>
		/// <param name="methodInfo"></param>
		/// <param name="arguments"></param>
		/// <returns>New MethodCallExpression.</returns>
		public static MethodCallExpression MakeMethodCall(MethodInfo methodInfo, params Expression[] arguments)
		{
			var callMethodInfo = MakeGenericMethod(methodInfo, arguments);
			var callExpression = Expression.Call(callMethodInfo, arguments);

			return callExpression;
		}

		/// <summary>
		/// Makes generic method based on type of arguments.
		/// </summary>
		/// <param name="methodInfo"></param>
		/// <param name="arguments"></param>
		/// <returns>New MethodCallExpression.</returns>
		public static MethodInfo MakeGenericMethod(MethodInfo methodInfo, Expression[] arguments)
		{
			if (!methodInfo.IsGenericMethod)
				return methodInfo;

			if (!methodInfo.IsGenericMethodDefinition)
				methodInfo = methodInfo.GetGenericMethodDefinition();
		
			var genericArguments = methodInfo.GetGenericArguments();
			var typesMapping     = new Dictionary<Type, Type>();

			for (var i = 0; i < methodInfo.GetParameters().Length; i++)
			{
				var parameter = methodInfo.GetParameters()[i];
				RegisterTypeRemapping(parameter.ParameterType, arguments[i].Type, genericArguments, typesMapping);
			}

			var newGenericArguments = genericArguments.Select((t, i) =>
			{
				if (!typesMapping.TryGetValue(t, out var replaced))
					throw new LinqToDBException($"Not found type mapping for generic argument '{t.Name}'.");
				return replaced;
			}).ToArray();

			var callMethodInfo = methodInfo.MakeGenericMethod(newGenericArguments);

			return callMethodInfo;
		}
		
	}
}
