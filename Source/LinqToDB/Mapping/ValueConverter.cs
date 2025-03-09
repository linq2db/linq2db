using System;
using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	public class ValueConverter<TModel, TProvider> : IValueConverter
	{
		public ValueConverter(
			Expression<Func<TModel, TProvider>> convertToProviderExpression,
			Expression<Func<TProvider, TModel>> convertFromProviderExpression, bool handlesNulls)
		{
			FromProviderExpression = convertFromProviderExpression;
			ToProviderExpression   = convertToProviderExpression;
			HandlesNulls           = handlesNulls;
		}

		public bool             HandlesNulls           { get; }
		public LambdaExpression FromProviderExpression { get; }
		public LambdaExpression ToProviderExpression   { get; }
	}
}
