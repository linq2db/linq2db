using System;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
	public class ValueConverterAttribute : MappingAttribute
	{
		/// <summary>
		/// ValueConverter for mapping Database Values to Model values.
		/// </summary>
		public IValueConverter? ValueConverter { get; set; }

		/// <summary>
		/// Returns <see cref="IValueConverter"/> for specific column.
		/// </summary>
		public virtual IValueConverter? GetValueConverter(ColumnDescriptor columnDescriptor)
		{
			if (ValueConverter != null)
				return ValueConverter;

			if (ConverterType == null)
				return null;

			var dynamicConverter = (IValueConverter)TypeAccessor.GetAccessor(ConverterType).CreateInstance();
			return dynamicConverter;
		}

		/// <summary>
		/// Gets or sets converter type. ConverterType should implement <see cref="IValueConverter"/> interface, should have public constructor with no parameters.
		/// </summary>
		public Type? ConverterType { get; set; }

		public override string GetObjectID()
		{
			return $".{Configuration}.{IdentifierBuilder.GetObjectID(ConverterType)}.";
		}
	}
}
