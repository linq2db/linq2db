using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public sealed class BinaryTranslationRegistration
	{
		public delegate Expression? TranslateBinaryFunc(
			ITranslationContext translationContext,
			BinaryExpression binaryExpression,
			TranslationFlags translationFlags);

		// Key: (ExpressionType, LeftType, RightType)
		readonly Dictionary<(ExpressionType expressionType, Type leftType, Type rightType), TranslateBinaryFunc> _translations = new();

		/// <summary>
		/// Registers a translation function for a specific binary expression type and operand types.
		/// </summary>
		/// <param name="binaryType">The ExpressionType of the binary operation (e.g., ExpressionType.Add).</param>
		/// <param name="leftType">The type of the left operand.</param>
		/// <param name="rightType">The type of the right operand.</param>
		/// <param name="translateFunc">The translation function.</param>
		public void RegisterBinaryInternal(ExpressionType binaryType, Type leftType, Type rightType, TranslateBinaryFunc translateFunc)
		{
			_translations[(binaryType, leftType, rightType)] = translateFunc;
		}

		/// <summary>
		/// Gets the translation function for a specific binary expression type and operand types, if registered.
		/// </summary>
		/// <param name="binaryType">The ExpressionType of the binary operation.</param>
		/// <param name="leftType">The type of the left operand.</param>
		/// <param name="rightType">The type of the right operand.</param>
		/// <returns>The translation function, or null if not registered.</returns>
		public TranslateBinaryFunc? GetTranslation(ExpressionType binaryType, Type leftType, Type rightType)
		{
			if (_translations.TryGetValue((binaryType, leftType, rightType), out var func))
			{
				return  func;
			}

			return null;
		}

		static (ExpressionType expressionType, Type leftType, Type rightType) GetBinaryPatternInfo(LambdaExpression binaryPattern)
		{
			if (binaryPattern.Body.UnwrapConvertToObject() is not BinaryExpression binaryExpression)
				throw new ArgumentException("BinaryPattern must be a binary expression.");
			var leftType  = binaryExpression.Left.Type;
			var rightType = binaryExpression.Right.Type;
			return (binaryExpression.NodeType, leftType, rightType);
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

	}
}
