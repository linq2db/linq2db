using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ValueConversion
{
	public sealed class IdValueConverterSelector : ValueConverterSelector
	{
		public IdValueConverterSelector(ValueConverterSelectorDependencies dependencies) : base(dependencies)
		{
		}

		public override IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type? providerClrType = null)
		{
			var baseConverters = base.Select(modelClrType, providerClrType);
			foreach (var converter in baseConverters)
				yield return converter;

			modelClrType = Unwrap(modelClrType);
			providerClrType = Unwrap(providerClrType);

			if (!modelClrType.IsGenericType)
				yield break;

			if (modelClrType.GetGenericTypeDefinition() != typeof(Id<,>))
				yield break;

			var t = modelClrType.GetGenericArguments();
			var key = t[1];
			providerClrType ??= key;
			if (key != providerClrType)
				yield break;

			var ct =
				
				key == typeof(long)
				? typeof(IdValueConverter<>).MakeGenericType(t[0])
				: 
				
				typeof(IdValueConverter<,>).MakeGenericType(key, t[0]);
			yield return new ValueConverterInfo
			(
				modelClrType,
				providerClrType,
				i => (ValueConverter)Activator.CreateInstance(ct, i.MappingHints)!
			);

			[return: NotNullIfNotNull(nameof(type))]
			static Type? Unwrap(Type? type) => type == null ? null : Nullable.GetUnderlyingType(type) ?? type;
		}
	}}
