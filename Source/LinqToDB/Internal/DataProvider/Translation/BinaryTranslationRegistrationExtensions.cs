using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public static class BinaryTranslationRegistrationExtensions
	{
		#region RegisterBinary

		public static void RegisterBinary<TResult>(
			this BinaryTranslationRegistration                registration,
			Expression<Func<TResult>>                         binaryPattern,
			BinaryTranslationRegistration.TranslateBinaryFunc translateBinaryFunc)
		{
			registration.RegisterBinaryInternal(binaryPattern, translateBinaryFunc);
		}

		public static void RegisterBinary<TLeft, TRight, TResult>(
			this BinaryTranslationRegistration                registration,
			Expression<Func<TLeft, TResult>>                  binaryPattern,
			BinaryTranslationRegistration.TranslateBinaryFunc translateBinaryFunc)
		{
			registration.RegisterBinaryInternal(binaryPattern, translateBinaryFunc);
		}

		public static void RegisterBinary<TLeft, TRight, TResult>(
			this BinaryTranslationRegistration                registration,
			Expression<Func<TLeft, TRight, TResult>>          binaryPattern,
			BinaryTranslationRegistration.TranslateBinaryFunc translateBinaryFunc)
		{
			registration.RegisterBinaryInternal(binaryPattern, translateBinaryFunc);
		}

		#endregion

		#region RegisterGenericBinary

		public static void RegisterGenericBinary<TResult>(
			this BinaryTranslationRegistration                registration,
			Expression<Func<TResult>>                         binaryPattern,
			BinaryTranslationRegistration.TranslateBinaryFunc translateBinaryFunc)
		{
			registration.RegisterBinaryInternal(binaryPattern, translateBinaryFunc, isGenericTypeMatch: true);
		}

		public static void RegisterGenericBinary<TLeft, TRight, TResult>(
			this BinaryTranslationRegistration                registration,
			Expression<Func<TLeft, TResult>>                  binaryPattern,
			BinaryTranslationRegistration.TranslateBinaryFunc translateBinaryFunc)
		{
			registration.RegisterBinaryInternal(binaryPattern, translateBinaryFunc, isGenericTypeMatch: true);
		}

		public static void RegisterGenericBinary<TLeft, TRight, TResult>(
			this BinaryTranslationRegistration                registration,
			Expression<Func<TLeft, TRight, TResult>>          binaryPattern,
			BinaryTranslationRegistration.TranslateBinaryFunc translateBinaryFunc)
		{
			registration.RegisterBinaryInternal(binaryPattern, translateBinaryFunc, isGenericTypeMatch: true);
		}

		#endregion
	}
}
