using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore
{
	public partial class LinqToDBForEFTools
	{
		static void InitializeMapping()
		{
			Linq.Expressions.MapMember((DbFunctions f, string m, string p) => f.Like(m, p), (f, m, p) => Sql.Like(m, p));
		}

		#region Sql Server

		static Sql.DateParts? GetDatePart(string name)
		{
			return name switch
			{
				"Year"        => (Sql.DateParts?)Sql.DateParts.Year,
				"Day"         => (Sql.DateParts?)Sql.DateParts.Day,
				"Month"       => (Sql.DateParts?)Sql.DateParts.Month,
				"Hour"        => (Sql.DateParts?)Sql.DateParts.Hour,
				"Minute"      => (Sql.DateParts?)Sql.DateParts.Minute,
				"Second"      => (Sql.DateParts?)Sql.DateParts.Second,
				"Millisecond" => (Sql.DateParts?)Sql.DateParts.Millisecond,
				_             => null,
			};
		}

		/// <summary>
		/// Initializes SQL Server's DbFunctions dynamically to avoid dependency
		/// </summary>
		static void InitializeSqlServerMapping()
		{
			var type = Type.GetType("Microsoft.EntityFrameworkCore.SqlServerDbFunctionsExtensions, Microsoft.EntityFrameworkCore.SqlServer", false);

			if (type == null) 
				return;

			var sqlServerMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(m => m.GetParameters().Length > 0)
				.ToArray();

			var dbFunctionsParameter = Expression.Parameter(typeof(DateTime), "dbFunctions");

			var dateDiffStr = "DateDiff";
			var dateDiffMethods = sqlServerMethods.Where(m => m.Name.StartsWith(dateDiffStr)).ToArray();

			var dateDiffMethod = MemberHelper.MethodOf(() => Sql.DateDiff(Sql.DateParts.Day, (DateTime?)null, null));

			foreach (var method in dateDiffMethods)
			{
				var datePart = GetDatePart(method.Name.Substring(dateDiffStr.Length));
				if (datePart == null)
					continue;

				var parameters = method.GetParameters();
				if (parameters.Length < 3)
					continue;

				var boundaryType = parameters[1].ParameterType;
				if (boundaryType.ToUnderlying() != typeof(DateTime))
					continue;

				var startParameter = Expression.Parameter(boundaryType, "start");
				var endParameter   = Expression.Parameter(boundaryType, "end");

				var startExpr = startParameter.Type != typeof(DateTime?)
					? (Expression) Expression.Convert(startParameter, typeof(DateTime?))
					: startParameter;

				var endExpr = endParameter.Type != typeof(DateTime?)
					? (Expression) Expression.Convert(endParameter, typeof(DateTime?))
					: endParameter;

				var body   = Expression.Call(dateDiffMethod, Expression.Constant(datePart.Value), startExpr, endExpr);
				var lambda = Expression.Lambda(body, dbFunctionsParameter, startParameter, endParameter);

				Linq.Expressions.MapMember(method, lambda);
			}
		}
		
		#endregion
	}
}
