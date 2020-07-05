using System;
using System.Collections.Generic;

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
		
	}
}
