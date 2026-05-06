using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public sealed class TranslationRegistration
	{
		public delegate Expression? TranslateFunc              (ITranslationContext translationContext, Expression           member,           TranslationFlags translationFlags);
		public delegate Expression? TranslateMethodFunc        (ITranslationContext translationContext, MethodCallExpression methodCall,       TranslationFlags translationFlags);
		public delegate Expression? TranslateMemberAccessFunc  (ITranslationContext translationContext, MemberExpression     memberExpression, TranslationFlags translationFlags);
		public delegate Expression? TranslateBinaryFunc        (ITranslationContext translationContext, BinaryExpression     binaryExpression, TranslationFlags translationFlags);
		public delegate Expression? TranslateUnaryFunc         (ITranslationContext translationContext, UnaryExpression      unaryExpression,  TranslationFlags translationFlags);

		public record MemberReplacement(LambdaExpression Pattern, LambdaExpression Replacement);

		// Method / member / constructor / new — keyed by MemberInfoWithType.
		readonly Dictionary<MemberHelper.MemberInfoWithType, TranslateFunc> _translations = new();

		// Binary operator translators — keyed by (NodeType, LeftType, RightType). Operand-typed
		// keying avoids collision with the method registry: `string.Concat(string, string)` is
		// already registered as a method translator with PreserveNull = false (C# semantics);
		// `a + b` on strings dispatches here with PreserveNull = true (SQL null propagation).
		Dictionary<(ExpressionType nodeType, Type leftType, Type rightType), TranslateBinaryFunc>? _binaryTranslations;

		// Unary operator translators — keyed by (NodeType, OperandType).
		Dictionary<(ExpressionType nodeType, Type operandType), TranslateUnaryFunc>? _unaryTranslations;

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

		public void RegisterBinaryInternal(ExpressionType binaryType, Type leftType, Type rightType, TranslateBinaryFunc translateFunc)
		{
			_binaryTranslations            ??= new();
			_binaryTranslations[(binaryType, leftType, rightType)] = translateFunc;
		}

		public void RegisterBinaryInternal(LambdaExpression binaryPattern, TranslateBinaryFunc translateBinaryFunc, bool isGenericTypeMatch = false)
		{
			var (expressionType, leftType, rightType) = GetBinaryPatternInfo(binaryPattern);

			RegisterBinaryInternal(expressionType, leftType, rightType, translateBinaryFunc);

			if (isGenericTypeMatch)
			{
				if (leftType.IsGenericType)
				{
					var leftGenericType = leftType.GetGenericTypeDefinition();
					RegisterBinaryInternal(expressionType, leftGenericType, rightType, translateBinaryFunc);
				}

				if (rightType.IsGenericType)
				{
					var rightGenericType = rightType.GetGenericTypeDefinition();
					RegisterBinaryInternal(expressionType, leftType, rightGenericType, translateBinaryFunc);
				}

				if (leftType.IsGenericType && rightType.IsGenericType)
				{
					var leftGenericType  = leftType.GetGenericTypeDefinition();
					var rightGenericType = rightType.GetGenericTypeDefinition();
					RegisterBinaryInternal(expressionType, leftGenericType, rightGenericType, translateBinaryFunc);
				}
			}
		}

		static (ExpressionType expressionType, Type leftType, Type rightType) GetBinaryPatternInfo(LambdaExpression binaryPattern)
		{
			if (binaryPattern.Body.UnwrapConvertToObject() is not BinaryExpression binaryExpression)
				throw new ArgumentException("BinaryPattern must be a binary expression.");

			return (binaryExpression.NodeType, binaryExpression.Left.Type, binaryExpression.Right.Type);
		}

		public void RegisterUnaryInternal(ExpressionType unaryType, Type operandType, TranslateUnaryFunc translateFunc)
		{
			_unaryTranslations            ??= new();
			_unaryTranslations[(unaryType, operandType)] = translateFunc;
		}

		public void RegisterUnaryInternal(LambdaExpression unaryPattern, TranslateUnaryFunc translateUnaryFunc, bool isGenericTypeMatch = false)
		{
			var (expressionType, operandType) = GetUnaryPatternInfo(unaryPattern);

			RegisterUnaryInternal(expressionType, operandType, translateUnaryFunc);

			if (isGenericTypeMatch && operandType.IsGenericType)
			{
				var operandGenericType = operandType.GetGenericTypeDefinition();
				RegisterUnaryInternal(expressionType, operandGenericType, translateUnaryFunc);
			}
		}

		static (ExpressionType expressionType, Type operandType) GetUnaryPatternInfo(LambdaExpression unaryPattern)
		{
			if (unaryPattern.Body.UnwrapConvertToObject() is not UnaryExpression unaryExpression)
				throw new ArgumentException("UnaryPattern must be a unary expression.");

			return (unaryExpression.NodeType, unaryExpression.Operand.Type);
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

		public TranslateBinaryFunc? GetBinaryTranslation(ExpressionType binaryType, Type leftType, Type rightType)
		{
			if (_binaryTranslations != null && _binaryTranslations.TryGetValue((binaryType, leftType, rightType), out var func))
				return func;

			return null;
		}

		public TranslateUnaryFunc? GetUnaryTranslation(ExpressionType unaryType, Type operandType)
		{
			if (_unaryTranslations != null && _unaryTranslations.TryGetValue((unaryType, operandType), out var func))
				return func;

			return null;
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
