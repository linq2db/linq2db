using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public sealed class TranslationRegistration
	{
		public delegate Expression? TranslateFunc              (ITranslationContext translationContext, Expression           member,           TranslationFlags translationFlags);
		public delegate Expression? TranslateMethodFunc        (ITranslationContext translationContext, MethodCallExpression methodCall,       TranslationFlags translationFlags);
		public delegate Expression? TranslateMemberAccessFunc  (ITranslationContext translationContext, MemberExpression     memberExpression, TranslationFlags translationFlags);
		public delegate Expression? TranslateUnaryOperatorFunc (ITranslationContext translationContext, UnaryExpression      unaryExpression,  TranslationFlags translationFlags);
		public delegate Expression? TranslateBinaryOperatorFunc(ITranslationContext translationContext, BinaryExpression     binaryExpression, TranslationFlags translationFlags);

		public record MemberReplacement(LambdaExpression Pattern, LambdaExpression Replacement);

		readonly Dictionary<MemberHelper.MemberInfoWithType, TranslateFunc> _translations = new();

		Dictionary<MemberHelper.MemberInfoWithType, MemberReplacement>? _replacements;

		public void RegisterMethodInternal(LambdaExpression methodCallPattern, TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch)
		{
			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(methodCallPattern);
			var methodInfo = memberInfoWithType.MemberInfo as MethodInfo;

			if (methodInfo == null)
				throw new ArgumentException("MethodCallPattern must be a method call.");

			if (!isGenericTypeMatch && methodInfo.IsGenericMethod)
			{
				memberInfoWithType.MemberInfo = methodInfo.GetGenericMethodDefinitionCached();
			}

			_translations[memberInfoWithType] = (ctx, member, flags) => translateMethodFunc(ctx, (MethodCallExpression)member, flags);
		}

		public void RegisterMemberInternal(LambdaExpression memberAccessPattern, TranslateMemberAccessFunc translateMemberAccessFunc)
		{
			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(memberAccessPattern);
			var memberInfo = memberInfoWithType.MemberInfo;

			if (memberInfo.MemberType is not (MemberTypes.Field or MemberTypes.Property))
				throw new ArgumentException("MemberAccessPattern must be a field or property access.");

			_translations[memberInfoWithType] = (ctx, member, flags) => translateMemberAccessFunc(ctx, (MemberExpression)member, flags);
		}

		public void RegisterConstructorInternal(LambdaExpression memberAccessPattern, TranslateFunc translateConstructorFunc)
		{
			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(memberAccessPattern);
			var memberInfo         = memberInfoWithType.MemberInfo;

			if (memberInfo.MemberType is not (MemberTypes.Constructor))
				throw new ArgumentException("MemberAccessPattern must be a constructor access.");

			_translations[memberInfoWithType] = translateConstructorFunc;
		}

		public void RegisterUnaryOperatorInternal(LambdaExpression operatorAccessPattern, TranslateUnaryOperatorFunc translateOperatorFunc, bool isGenericTypeMatch)
		{
			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(operatorAccessPattern);

			if (memberInfoWithType.MemberInfo is not MethodInfo { MemberType: MemberTypes.Method, IsStatic: true } methodInfo)
				throw new ArgumentException("Unary operator access pattern must have static method call.");

			if (!isGenericTypeMatch && methodInfo.IsGenericMethod)
				memberInfoWithType.MemberInfo = methodInfo.GetGenericMethodDefinitionCached();

			_translations[memberInfoWithType] = (ctx, member, flags) => translateOperatorFunc(ctx, (UnaryExpression)member, flags);
		}

		public void RegisterBinaryOperatorInternal(LambdaExpression operatorAccessPattern, TranslateBinaryOperatorFunc translateOperatorFunc, bool isGenericTypeMatch)
		{
			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(operatorAccessPattern);

			if (memberInfoWithType.MemberInfo is not MethodInfo { MemberType: MemberTypes.Method, IsStatic: true } methodInfo)
				throw new ArgumentException("Binary operator access pattern must have static method call.");

			if (!isGenericTypeMatch && methodInfo.IsGenericMethod)
				memberInfoWithType.MemberInfo = methodInfo.GetGenericMethodDefinitionCached();

			_translations[memberInfoWithType] = (ctx, member, flags) => translateOperatorFunc(ctx, (BinaryExpression)member, flags);
		}

		public void RegisterMemberReplacement(LambdaExpression pattern, LambdaExpression replacement)
		{
			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(pattern);
			var memberInfo         = memberInfoWithType.MemberInfo;

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

			if (_replacements.ContainsKey(memberInfoWithType))
			{
				throw new InvalidOperationException($"Member replacement for {memberInfo.Name} is already registered.");
			}

			_replacements.Add(memberInfoWithType, new MemberReplacement(pattern, replacement));
		}

		public TranslateFunc? GetTranslation(MemberHelper.MemberInfoWithType memberInfoWithType)
		{
			if (memberInfoWithType.MemberInfo is MethodInfo mi)
			{
				if (_translations.TryGetValue(memberInfoWithType, out var concreteFunc))
					return concreteFunc;

				if (mi.IsGenericMethod)
					memberInfoWithType.MemberInfo = mi.GetGenericMethodDefinitionCached();
			}

			_translations.TryGetValue(memberInfoWithType, out var func);
			return func;
		}

		public MemberReplacement? GetMemberReplacementInfo(MemberHelper.MemberInfoWithType memberInfoWithType)
		{
			if (_replacements != null && _replacements.TryGetValue(memberInfoWithType, out var replacement))
				return replacement;
			return null;
		}

		public Expression? ProvideReplacement(Expression expression)
		{
			if (expression is not (MemberExpression or MethodCallExpression or NewExpression))
				return null;

			var memberInfoWithType = MemberHelper.GetMemberInfoWithType(expression);

			var replacementInfo = GetMemberReplacementInfo(memberInfoWithType);
			if (replacementInfo == null)
				return null;

			if (replacementInfo.Pattern.Parameters.Count == 0)
				return replacementInfo.Replacement.Body;

			var replacement = replacementInfo.Pattern.GetBody(replacementInfo.Replacement);
			return replacement;
		}
	}
}
