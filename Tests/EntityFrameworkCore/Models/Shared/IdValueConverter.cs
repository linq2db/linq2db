using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public sealed class IdValueConverter<TId, T> : ValueConverter<Id<T, TId>, TId>
		where T : IHasId<T, TId>
		where TId : notnull
	{
		public IdValueConverter(ConverterMappingHints? mappingHints = null)
			: base(id => id, id =>  id.AsId<T, TId>()) { }
	}

	public sealed class IdValueConverterSelector : ValueConverterSelector
	{
		public IdValueConverterSelector([System.Diagnostics.CodeAnalysis.NotNull] ValueConverterSelectorDependencies dependencies) : base(dependencies)
		{
		}

		public override IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type? providerClrType = null)
		{
			var baseConverters = base.Select(modelClrType, providerClrType);
			foreach (var converter in baseConverters)
				yield return converter;

			modelClrType = modelClrType.UnwrapNullable();
			providerClrType = providerClrType.UnwrapNullable();

			if (!modelClrType.IsGenericType)
				yield break;

			if (modelClrType.GetGenericTypeDefinition() != typeof(Id<,>))
				yield break;

			var t = modelClrType.GetGenericArguments();
			var key = t[1];
			providerClrType ??= key;
			if (key != providerClrType)
				yield break;

			var ct = typeof(IdValueConverter<,>).MakeGenericType(key, t[0]);
			yield return new ValueConverterInfo
			(
				modelClrType,
				providerClrType,
				i => (ValueConverter)Activator.CreateInstance(ct, i.MappingHints)!
			);
		}
	}
}
