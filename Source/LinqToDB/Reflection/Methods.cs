using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Reflection
{
	using Expressions;

	/// <summary>
	/// This API supports the LinqToDB infrastructure and is not intended to be used  directly from your code.
	/// This API may change or be removed in future releases.
	/// </summary>
	public static class Methods
	{
		public static class Enumerable
		{
			public static readonly MethodInfo ToArray     = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.ToArray());
			public static readonly MethodInfo AsQueryable = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.AsQueryable());
			public static readonly MethodInfo Select      = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.Select(p => p));
		}

		public static class Queryable
		{
			public static readonly MethodInfo Select     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Select(p => p));
			public static readonly MethodInfo Where      = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Where((Expression<Func<int, bool>>)null!));
			public static readonly MethodInfo Take       = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Take(1));
			public static readonly MethodInfo Skip       = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Skip(1));

			public static readonly MethodInfo SelectManySimple     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SelectMany(a => new int[0]));
			public static readonly MethodInfo SelectManyProjection = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SelectMany(a => new int[0], (m, d) => d));
		}

		public static class LinqToDB
		{
			public static readonly MethodInfo GetTable = MemberHelper.MethodOfGeneric<IDataContext>(dc => dc.GetTable<object>());

			public static readonly MethodInfo LoadWith             = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.LoadWith(a => 1));
			public static readonly MethodInfo LoadWithQuerySingle  = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.LoadWith(a => new int[0], (IQueryable<int> s) => s));
			public static readonly MethodInfo LoadWithQueryMany    = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.LoadWith(a => new int[0], (IQueryable<int> s) => s));

			public static readonly MethodInfo ThenLoadSingle       = MemberHelper.MethodOfGeneric<LinqExtensions.ILoadWithQueryable<object, string>>(q => q.ThenLoad(e => e.ToString()));
			public static readonly MethodInfo ThenLoadMultiple     = MemberHelper.MethodOfGeneric<LinqExtensions.ILoadWithQueryable<object, IEnumerable<string>>>(q => q.ThenLoad(s => s.Length));
			public static readonly MethodInfo ThenLoadMultipleFunc = MemberHelper.MethodOfGeneric<LinqExtensions.ILoadWithQueryable<object, IEnumerable<string>>>(q => q.ThenLoad(s => new string [0], qq => qq.Where(_ => true)));

			public static class Table
			{
				public static readonly MethodInfo TableName    = MemberHelper.MethodOfGeneric<ITable<int>>(t => t.TableName(null!));
				public static readonly MethodInfo SchemaName   = MemberHelper.MethodOfGeneric<ITable<int>>(t => t.SchemaName(null!));
				public static readonly MethodInfo DatabaseName = MemberHelper.MethodOfGeneric<ITable<int>>(t => t.DatabaseName(null!));

				public static readonly MethodInfo With                = MemberHelper.MethodOfGeneric<ITable<int>>(t => t.With(""));
				public static readonly MethodInfo WithTableExpression = MemberHelper.MethodOfGeneric<ITable<int>>(t => t.WithTableExpression(""));
			}

			public static class Tools
			{
				public static readonly MethodInfo CreateEmptyQuery  = MemberHelper.MethodOfGeneric(() => Common.Tools.CreateEmptyQuery<int>());
			}
			
		}
	}
}
