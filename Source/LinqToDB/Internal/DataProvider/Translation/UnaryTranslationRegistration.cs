using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public sealed class UnaryTranslationRegistration
	{
		public delegate Expression? TranslateUnaryFunc(
			ITranslationContext translationContext,
			UnaryExpression unaryExpression,
			TranslationFlags translationFlags);

		// Key: (ExpressionType, OperandType)
		readonly Dictionary<(ExpressionType expressionType, Type operandType), TranslateUnaryFunc> _translations = new();

		/// <summary>
		/// Registers a translation function for a specific unary expression type and operand type.
		/// </summary>
		/// <param name="unaryType">The ExpressionType of the unary operation (e.g., ExpressionType.Negate).</param>
		/// <param name="operandType">The type of the operand.</param>
		/// <param name="translateFunc">The translation function.</param>
		public void RegisterUnaryInternal(ExpressionType unaryType, Type operandType, TranslateUnaryFunc translateFunc)
		{
			_translations[(unaryType, operandType)] = translateFunc;
		}

		/// <summary>
		/// Gets the translation function for a specific unary expression type and operand type, if registered.
		/// </summary>
		/// <param name="unaryType">The ExpressionType of the unary operation.</param>
		/// <param name="operandType">The type of the operand.</param>
		/// <returns>The translation function, or null if not registered.</returns>
		public TranslateUnaryFunc? GetTranslation(ExpressionType unaryType, Type operandType)
		{
			if (_translations.TryGetValue((unaryType, operandType), out var func))
			{
				return func;
			}

			return null;
		}

		static (ExpressionType expressionType, Type operandType) GetUnaryPatternInfo(LambdaExpression unaryPattern)
		{
			if (unaryPattern.Body.UnwrapConvertToObject() is not UnaryExpression unaryExpression)
				throw new ArgumentException("UnaryPattern must be a unary expression.");

			return (unaryExpression.NodeType, unaryExpression.Operand.Type);
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
	}
}
