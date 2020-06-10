using System;
using System.Linq.Expressions;

namespace LinqToDB.Common
{
	public interface IValueConverter
	{
		LambdaExpression FromProviderExpression { get; }
		LambdaExpression ToProviderExpression   { get; }
	}

	public interface IValueConverter<TModel, TProvider> : IValueConverter
	{
	}

	public class ValueConverter<TModel, TProvider> : IValueConverter<TModel, TProvider>
	{
		public ValueConverter(
			Expression<Func<TModel, TProvider>> convertToProviderExpression,
			Expression<Func<TProvider, TModel>> convertFromProviderExpression)
		{
			FromProviderExpression = convertFromProviderExpression;
			ToProviderExpression   = convertToProviderExpression;
		}

		public ValueConverter(
			Func<TModel, TProvider> convertToProviderFunc,
			Func<TProvider, TModel> convertFromProviderFunc)
		{
			var modelParam    = Expression.Parameter(typeof(TModel),    "model");
			var providerParam = Expression.Parameter(typeof(TProvider), "prov");

			FromProviderExpression = Expression.Lambda(Expression.Invoke(Expression.Constant(convertFromProviderFunc), providerParam), providerParam);
			ToProviderExpression   = Expression.Lambda(Expression.Invoke(Expression.Constant(convertToProviderFunc),   modelParam),    modelParam);
		}

		public LambdaExpression FromProviderExpression { get; }
		public LambdaExpression ToProviderExpression   { get; }
	}
}
