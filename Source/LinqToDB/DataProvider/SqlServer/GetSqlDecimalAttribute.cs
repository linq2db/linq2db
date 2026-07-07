using System;
using System.Data.SqlTypes;
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// Configures SQL Server decimal value materialization through provider-specific <see cref="SqlDecimal"/> reader.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class GetSqlDecimalAttribute : ValueConverterAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GetSqlDecimalAttribute"/> class, configured to read SQL Server <see cref="decimal"/> values through the provider-specific <see cref="SqlDecimal"/> reader.
		/// </summary>
		public GetSqlDecimalAttribute()
		{
			Configuration  = ProviderName.SqlServer;
			ValueConverter = SqlDecimalConverter.Instance;
		}

		sealed class SqlDecimalConverter : IValueConverter
		{
			public static readonly SqlDecimalConverter Instance = new();

			static readonly Expression<Func<decimal, decimal>>    ToProviderExpressionValue        = static value => value;
			static readonly Expression<Func<SqlDecimal, decimal>> FromSqlDecimalProviderExpression = static value => SqlServerDecimalUtils.ConvertSqlDecimal(value);

			SqlDecimalConverter()
			{
			}

			public bool HandlesNulls => false;

			public LambdaExpression FromProviderExpression => FromSqlDecimalProviderExpression;

			public LambdaExpression ToProviderExpression => ToProviderExpressionValue;
		}
	}
}
