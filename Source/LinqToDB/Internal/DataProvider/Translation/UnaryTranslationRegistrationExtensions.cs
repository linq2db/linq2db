using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public static class UnaryTranslationRegistrationExtensions
	{
		#region RegisterUnary

		public static void RegisterUnary<TResult>(
			this UnaryTranslationRegistration               registration,
			Expression<Func<TResult>>                       unaryPattern,
			UnaryTranslationRegistration.TranslateUnaryFunc translateUnaryFunc)
		{
			registration.RegisterUnaryInternal(unaryPattern, translateUnaryFunc);
		}

		public static void RegisterUnary<T, TResult>(
			this UnaryTranslationRegistration               registration,
			Expression<Func<T, TResult>>                    unaryPattern,
			UnaryTranslationRegistration.TranslateUnaryFunc translateUnaryFunc)
		{
			registration.RegisterUnaryInternal(unaryPattern, translateUnaryFunc);
		}

		#endregion

		#region RegisterGenericUnary

		public static void RegisterGenericUnary<TResult>(
			this UnaryTranslationRegistration               registration,
			Expression<Func<TResult>>                       unaryPattern,
			UnaryTranslationRegistration.TranslateUnaryFunc translateUnaryFunc)
		{
			registration.RegisterUnaryInternal(unaryPattern, translateUnaryFunc, isGenericTypeMatch: true);
		}

		public static void RegisterGenericUnary<T, TResult>(
			this UnaryTranslationRegistration               registration,
			Expression<Func<T, TResult>>                    unaryPattern,
			UnaryTranslationRegistration.TranslateUnaryFunc translateUnaryFunc)
		{
			registration.RegisterUnaryInternal(unaryPattern, translateUnaryFunc, isGenericTypeMatch: true);
		}

		#endregion
	}
}
