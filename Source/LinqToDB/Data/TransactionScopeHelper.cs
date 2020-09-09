using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	internal static class TransactionScopeHelper
	{
		static Func<object?> _getCurrentScopeDelegate = GetTransactionScopeFunc();

		public static bool IsInsideTransactionScope
		{
			get
			{
				var ts = _getCurrentScopeDelegate();
				return ts != null;
			}
		}

		static Func<object?> GetTransactionScopeFunc()
		{
			var assembly = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == "System.Transactions");

			if (assembly != null)
			{
				var t = assembly.GetType("System.Transactions.Transaction");
				var currentDataProperty = t.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
				if (currentDataProperty != null)
				{
					var body   = Expression.MakeMemberAccess(null, currentDataProperty);
					var lambda = Expression.Lambda<Func<object?>>(body);
					return lambda.Compile;
				}
			}

			return () => null;
		}
	}
}
