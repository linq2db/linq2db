using System;
using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Linq;

namespace LinqToDB.Expressions
{
	class FormattableParameterExpression : Expression
	{
#if !NET45
		/// <summary>
		/// <see cref="FormattableString"/>-based constructor.
		/// </summary>
		/// <param name="formattable">Expression, that contains <see cref="FormattableString"/> value.</param>
		/// <param name="argIndex">Index of current parameter in <paramref name="formattable"/> arguments list.</param>
		public FormattableParameterExpression(ConstantExpression formattable, int argIndex)
		{
			_formattable = formattable;
			_argIndex    = argIndex;
			_type        = ((FormattableString)formattable.Value).GetArgument(_argIndex)?.GetType() ?? typeof(object);
		}
#endif

		/// <summary>
		/// <see cref="RawSqlString"/>-based constructor.
		/// </summary>
		/// <param name="formattable">Expression, that contains <see cref="RawSqlString"/> value.</param>
		/// <param name="parameters">Expression, that contains parameters array.</param>
		/// <param name="argIndex">Index of current parameter in <paramref name="parameters"/> array.</param>
		/// <param name="type">Type of current parameter.</param>
		public FormattableParameterExpression(Expression formattable, Expression parameters, int argIndex, Type type)
		{
			_formattable      = formattable;
			_rawSqlParameters = parameters;
			_argIndex         = argIndex;
			_type             = type;
		}

		readonly Type       _type;
		readonly Expression _formattable;
		readonly int        _argIndex;
		readonly Expression
// because I can
#if !NET45
?
#endif
			                _rawSqlParameters;

		public override Type Type               => _type;
		public override ExpressionType NodeType => ExpressionType.Extension;
		public override bool CanReduce          => true;

		public override int GetHashCode() => _formattable.GetHashCode() ^ _argIndex.GetHashCode();

		public override bool Equals(object obj)
		{
			return obj is FormattableParameterExpression other
				&& other._formattable.Equals(_formattable)
				&& other._argIndex == _argIndex;
		}

		public override Expression Reduce()
		{
#if !NET45
			if (_rawSqlParameters == null)
				return Convert(Call(_formattable, ReflectionHelper.Functions.FormattableString.GetArguments, Constant(_argIndex)), Type);
			else
#endif
			return Convert(ArrayIndex(_rawSqlParameters, Constant(_argIndex)), Type);
		}
	}
}
