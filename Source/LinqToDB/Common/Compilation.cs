using System;
using System.Linq.Expressions;

#if SUPPORTS_COMPOSITE_FORMAT
using System.Text;
#endif

namespace LinqToDB.Common
{
	/// <summary>
	/// Contains LINQ expression compilation options.
	/// </summary>
	public static class Compilation
	{
		private static Func<LambdaExpression,Delegate?>? _compiler;

		/// <summary>
		/// Sets LINQ expression compilation method.
		/// </summary>
		/// <param name="compiler">Method to use for expression compilation or <c>null</c> to reset compilation logic to defaults.</param>
		public static void SetExpressionCompiler(Func<LambdaExpression, Delegate?>? compiler)
		{
			_compiler = compiler;
		}

		/// <summary>
		/// Internal API.
		/// </summary>
		public static TDelegate CompileExpression<TDelegate>(this Expression<TDelegate> expression)
			where TDelegate : Delegate
		{
			return ((TDelegate?)_compiler?.Invoke(expression))
#pragma warning disable RS0030 // Do not use banned APIs
				?? expression.Compile();
#pragma warning restore RS0030 // Do not use banned APIs
		}

		/// <summary>
		/// Internal API.
		/// </summary>
		public static Delegate CompileExpression(this LambdaExpression expression)
		{
			return _compiler?.Invoke(expression)
#pragma warning disable RS0030 // Do not use banned APIs
				?? expression.Compile();
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}
}
