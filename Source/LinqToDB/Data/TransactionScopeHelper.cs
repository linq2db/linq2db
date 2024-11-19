using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	using LinqToDB.Common;
	using LinqToDB.Expressions;

	internal static class TransactionScopeHelper
	{
		static readonly Func<bool> _getInScopeFunc = GetTransactionScopeFunc();

		public static bool IsInsideTransactionScope => _getInScopeFunc();

		static Func<bool> GetTransactionScopeFunc()
		{
			// netfx:   "System.Transactions"
			// netcore: "System.Transactions.Local"
			// check for both names as I'm not sure how it will work with netstandard builds
			var assembly = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a =>
				{
					var n = a.GetName().Name;
					return n == "System.Transactions" || n == "System.Transactions.Local";
				});

			if (assembly != null)
			{
				var t = assembly.GetType("System.Transactions.Transaction");

				var currentDataProperty = t?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
				if (currentDataProperty != null)
				{
					var body   = Expression.NotEqual(Expression.MakeMemberAccess(null, currentDataProperty),
						ExpressionInstances.UntypedNull);
					var lambda = Expression.Lambda<Func<bool>>(body);
					return lambda.CompileExpression();
				}
			}

			return () => false;
		}
	}
}
