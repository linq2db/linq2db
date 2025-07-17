using System;
using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	public class ValueConverterFunc<TModel, TProvider> : IValueConverter
	{
		public ValueConverterFunc(
			Func<TModel, TProvider> convertToProviderFunc,
			Func<TProvider, TModel> convertFromProviderFunc, bool handlesNulls)
		{
			var modelParam    = Expression.Parameter(typeof(TModel),    "model");
			var providerParam = Expression.Parameter(typeof(TProvider), "prov");

			FromProviderExpression = Expression.Lambda(Expression.Invoke(Expression.Constant(convertFromProviderFunc), providerParam), providerParam);
			ToProviderExpression   = Expression.Lambda(Expression.Invoke(Expression.Constant(convertToProviderFunc),   modelParam),    modelParam);
			HandlesNulls           = handlesNulls;
		}

		public bool             HandlesNulls           { get; }
		public LambdaExpression FromProviderExpression { get; }
		public LambdaExpression ToProviderExpression   { get; }
	}
}
