using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.Linq.Translation
{
	public class TranslationRegistration
	{
		public delegate Expression? TranslateFunc(ITranslationContext             translationContext, Expression           member,           TranslationFlags translationFlags);
		public delegate Expression? TranslateMethodFunc(ITranslationContext       translationContext, MethodCallExpression methodCall,       TranslationFlags translationFlags);
		public delegate Expression? TranslateMemberAccessFunc(ITranslationContext translationContext, MemberExpression     memberExpression, TranslationFlags translationFlags);

		public record MemberReplacement(LambdaExpression Pattern, LambdaExpression Replacement);

		Dictionary<MemberInfo, TranslateFunc> _translations = new();

		Dictionary<MemberInfo, MemberReplacement>? _replacements;

		public void RegisterMethodInternal(LambdaExpression methodCallPattern, TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch)
		{
			var methodInfo = MemberHelper.GetMemberInfo(methodCallPattern) as MethodInfo;
			if (methodInfo == null)
				throw new ArgumentException("MethodCallPattern must be a method call.");

			if (!isGenericTypeMatch && methodInfo.IsGenericMethod)
				methodInfo = methodInfo.GetGenericMethodDefinitionCached();

			_translations.Remove(methodInfo);
			_translations[methodInfo] = (ctx, member, flags) => translateMethodFunc(ctx, (MethodCallExpression)member, flags);
		}

		public void RegisterMemberInternal(LambdaExpression memberAccessPattern, TranslateMemberAccessFunc translateMemberAccessFunc)
		{
			var memberInfo = MemberHelper.GetMemberInfo(memberAccessPattern);

			if (memberInfo.MemberType is not (MemberTypes.Field or MemberTypes.Property))
				throw new ArgumentException("MemberAccessPattern must be a field or property access.");

			_translations.Remove(memberInfo);
			_translations[memberInfo] = (ctx, member, flags) => translateMemberAccessFunc(ctx, (MemberExpression)member, flags);
		}

		public void RegisterConstructorInternal(LambdaExpression memberAccessPattern, TranslateFunc translateConstructorFunc)
		{
			var memberInfo = MemberHelper.GetMemberInfo(memberAccessPattern);

			if (memberInfo.MemberType is not (MemberTypes.Constructor))
				throw new ArgumentException("MemberAccessPattern must be a constructor access.");

			_translations.Remove(memberInfo);
			_translations[memberInfo] = translateConstructorFunc;
		}

		public void RegisterMemberReplacement(LambdaExpression pattern, LambdaExpression replacement)
		{
			var memberInfo = MemberHelper.GetMemberInfo(pattern);

			if (memberInfo.MemberType is not (MemberTypes.Field or MemberTypes.Property or MemberTypes.Method or MemberTypes.Constructor))
				throw new ArgumentException("MemberAccessPattern must be a method, field or property access.");

			if (pattern.Parameters.Count != replacement.Parameters.Count)
				throw new ArgumentException("MemberAccessPattern and replacement must have the same number of parameters.");

			for (int i = 0; i < pattern.Parameters.Count; i++)
			{
				if (pattern.Parameters[i].Type != replacement.Parameters[i].Type)
					throw new ArgumentException("MemberAccessPattern and replacement must have the same parameter types.");
			}

			_replacements ??= new();

			if (_replacements.ContainsKey(memberInfo))
			{
				throw new InvalidOperationException($"Member replacement for {memberInfo.Name} is already registered.");
			}

			_replacements.Add(memberInfo, new MemberReplacement(pattern, replacement));
		}

		public TranslateFunc? GetTranslation(MemberInfo member)
		{
			if (member is MethodInfo mi)
			{
				if (_translations.TryGetValue(member, out var concreteFunc))
					return concreteFunc;

				if (mi.IsGenericMethod)
					member = mi.GetGenericMethodDefinitionCached();
			}

			_translations.TryGetValue(member, out var func);
			return func;
		}

		public MemberReplacement? GetMemberReplacementInfo(MemberInfo member)
		{
			if (_replacements != null && _replacements.TryGetValue(member, out var replacement))
				return replacement;
			return null;
		}

		public Expression? ProvideReplacement(Expression expression)
		{
			MemberInfo memberInfo;
			if (expression is MemberExpression memberExpression)
				memberInfo = memberExpression.Member;
			else if (expression is MethodCallExpression methodCallExpression)
				memberInfo = methodCallExpression.Method;
			else if (expression is NewExpression { Constructor: { } } newExpression)
				memberInfo = newExpression.Constructor;
			else
				return null;

			var replacementInfo = GetMemberReplacementInfo(memberInfo);
			if (replacementInfo == null)
				return null;

			var replacement = replacementInfo.Pattern.GetBody(replacementInfo.Replacement);
			return replacement;
		}
	}
}
