using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
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
			return string.Create(CultureInfo.InvariantCulture, $".{Configuration}.{IdentifierBuilder.GetObjectID(ConverterType)}.{GetConverterID(ValueConverter)}.");
		}

		// Two schemas that convert one member differently must not share an identity, or whichever reaches the
		// mapping caches first converts for both.
		static string GetConverterID(IValueConverter? converter)
		{
			if (converter == null)
				return string.Empty;

			return string.Create(
				CultureInfo.InvariantCulture,
				$"{IdentifierBuilder.GetObjectID(converter.GetType())}:{(converter.HandlesNulls ? 1 : 0)}:{GetLambdaID(converter.ToProviderExpression)}:{GetLambdaID(converter.FromProviderExpression)}");

			// An expression is identified by its text, which shows a captured variable by name and a delegate by type
			// only; read as their values, and delegates by their methods, one helper called with different arguments
			// gives different identities.
			static string GetLambdaID(LambdaExpression lambda)
			{
				var delegates = new List<Delegate>();

				var expression = lambda.Transform(delegates, static (found, e) =>
				{
					if (e is MemberExpression && e.TryEvaluateCapturedValue(out var value))
					{
						if (value is Delegate captured)
							found.Add(captured);

						return Expression.Constant(value, e.Type);
					}

					if (e is ConstantExpression { Value: Delegate constant })
						found.Add(constant);

					return e;
				});

				var id = IdentifierBuilder.GetObjectID(expression).ToString(CultureInfo.InvariantCulture);

				foreach (var d in delegates)
					id += "+" + IdentifierBuilder.GetObjectID(d);

				return id;
			}
		}
	}
}
