using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public static class TranslationRegistrationExtensions
	{
		#region RegisterMethod

		public static void RegisterMethod(this TranslationRegistration registration, Expression<Action> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		public static void RegisterMethod<T, TResult>(this TranslationRegistration registration, Expression<Func<T, TResult>> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		public static void RegisterMethod<T1, T2, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, TResult>> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		public static void RegisterMethod<T1, T2, T3, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, TResult>> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		public static void RegisterMethod<T1, T2, T3, T4, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, TResult>> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		public static void RegisterMethod<T1, T2, T3, T4, T5, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, T5, TResult>> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		public static void RegisterMethod<T1, T2, T3, T4, T5, T6, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> methodCallPattern, TranslationRegistration.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
			=> registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

		#endregion

		#region RegisterMember

		public static void RegisterMember<TResult>(this TranslationRegistration registration, Expression<Func<TResult>> memberAccessPattern, TranslationRegistration.TranslateMemberAccessFunc translateMemberAccessFunc)
			=> registration.RegisterMemberInternal(memberAccessPattern, translateMemberAccessFunc);

		public static void RegisterMember<T, TResult>(this TranslationRegistration registration, Expression<Func<T, TResult>> memberAccessPattern, TranslationRegistration.TranslateMemberAccessFunc translateMemberAccessFunc)
			=> registration.RegisterMemberInternal(memberAccessPattern, translateMemberAccessFunc);

		#endregion

		#region Register Member Replacement

		public static void RegisterReplacement<TResult>(this TranslationRegistration registration, Expression<Func<TResult>> pattern, Expression<Func<TResult>> replacement)
			=> registration.RegisterMemberReplacement(pattern, replacement);

		public static void RegisterReplacement<T, TResult>(this TranslationRegistration registration, Expression<Func<T, TResult>> pattern, Expression<Func<T, TResult>> replacement)
			=> registration.RegisterMemberReplacement(pattern, replacement);

		public static void RegisterReplacement<T1, T2, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, TResult>> pattern, Expression<Func<T1, T2, TResult>> replacement)
			=> registration.RegisterMemberReplacement(pattern, replacement);

		public static void RegisterReplacement<T1, T2, T3, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, TResult>> pattern, Expression<Func<T1, T2, T3, TResult>> replacement)
			=> registration.RegisterMemberReplacement(pattern, replacement);

		#endregion

		#region Register Constructor

		public static void RegisterConstructor<TResult>(this TranslationRegistration registration, Expression<Func<TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T, TResult>(this TranslationRegistration registration, Expression<Func<T, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T1, T2, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T1, T2, T3, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T1, T2, T3, T4, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T1, T2, T3, T4, T5, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, T5, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T1, T2, T3, T4, T5, T6, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		public static void RegisterConstructor<T1, T2, T3, T4, T5, T6, T7, TResult>(this TranslationRegistration registration, Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> constructorAccessPattern, TranslationRegistration.TranslateFunc translateConstructorFunc)
			=> registration.RegisterConstructorInternal(constructorAccessPattern, translateConstructorFunc);

		#endregion
	}
}
