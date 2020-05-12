using System;
using System.Collections;
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
			public static readonly MethodInfo ToList      = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.ToList());

			public static readonly MethodInfo Select      = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.Select(p => p));
			public static readonly MethodInfo Where       = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Where((Func<int, bool>)null!));
			public static readonly MethodInfo Take        = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Take(1));
			public static readonly MethodInfo Skip        = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Skip(1));

			public static readonly MethodInfo First                   = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.First());
			public static readonly MethodInfo FirstOrDefault          = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.FirstOrDefault());
			public static readonly MethodInfo FirstOrDefaultCondition = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.FirstOrDefault(e => true));

			public static readonly MethodInfo DefaultIfEmpty       = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.DefaultIfEmpty());
			public static readonly MethodInfo DefaultIfEmptyValue  = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.DefaultIfEmpty(0));

			public static readonly MethodInfo OfType               = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.OfType<double>());

			public static readonly MethodInfo SelectManySimple     = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.SelectMany(a => new int[0]));
			public static readonly MethodInfo SelectManyProjection = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.SelectMany(a => new int[0], (m, d) => d));
		}

		public static class Queryable
		{
			public static readonly MethodInfo ToArray      = MemberHelper.MethodOfGeneric<IQueryable<int>>(e => e.ToArray());
			public static readonly MethodInfo ToList       = MemberHelper.MethodOfGeneric<IQueryable<int>>(e => e.ToList());
			public static readonly MethodInfo AsQueryable  = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.AsQueryable());
			public static readonly MethodInfo AsEnumerable = MemberHelper.MethodOfGeneric<IQueryable<int>>(e => e.AsEnumerable());

			public static readonly MethodInfo Select     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Select(p => p));
			public static readonly MethodInfo Where      = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Where((Expression<Func<int, bool>>)null!));
			public static readonly MethodInfo Take       = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Take(1));
			public static readonly MethodInfo Skip       = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Skip(1));

			public static readonly MethodInfo First                   = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.First());
			public static readonly MethodInfo FirstOrDefault          = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.FirstOrDefault());
			public static readonly MethodInfo FirstOrDefaultCondition = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.FirstOrDefault(e => true));

			public static readonly MethodInfo DefaultIfEmpty       = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.DefaultIfEmpty());
			public static readonly MethodInfo DefaultIfEmptyValue  = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.DefaultIfEmpty(0));

			public static readonly MethodInfo OfType               = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.OfType<double>());

			public static readonly MethodInfo SelectManySimple     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SelectMany(a => new int[0]));
			public static readonly MethodInfo SelectManyProjection = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SelectMany(a => new int[0], (m, d) => d));
		}

		public static class LinqToDB
		{
			public static readonly MethodInfo GetTable = MemberHelper.MethodOfGeneric<IDataContext>(dc => dc.GetTable<object>());

			public static readonly MethodInfo LoadWith              = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Single2));
			public static readonly MethodInfo LoadWithSingleFilter  = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Single2, eq => eq.Where(e => e.Value2 == 1)));
			public static readonly MethodInfo LoadWithManyFilter    = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Many2,   eq => eq.Where(e => e.Value2 == 1)));
			
			public static readonly MethodInfo ThenLoadFromSingle             = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Single2).ThenLoad(e => e.Single3));
			public static readonly MethodInfo ThenLoadFromMany               = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Many2).ThenLoad(e => e.Single3));
			
			public static readonly MethodInfo ThenLoadFromSingleSingleFilter = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Single2).ThenLoad(e => e.Single3, eq => eq.Where(e => e.Value3 == 3)));
			public static readonly MethodInfo ThenLoadFromSingleManyFilter   = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Single2).ThenLoad(e => e.Many3,   eq => eq.Where(e => e.Value3 == 3)));
			public static readonly MethodInfo ThenLoadFromManySingleFilter   = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Many2).ThenLoad(e => e.Single3,   eq => eq.Where(e => e.Value3 == 3)));
			public static readonly MethodInfo ThenLoadFromManyManyFilter     = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.LoadWith(e => e.Many2).ThenLoad(e => e.Many3,     eq => eq.Where(e => e.Value3 == 3)));


			public static readonly MethodInfo RemoveOrderBy       = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.RemoveOrderBy());
			public static readonly MethodInfo IgnoreFilters       = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.IgnoreFilters());

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


			#region LoadWith/ThenLoad helper classes

			#pragma warning disable 649

			abstract class LW1
			{
				public int   Value1;

				public LW2   Single2 = null!;
				public LW2[] Many2   = null!;
			}

			abstract class LW2
			{
				public int   Value2;

				public LW3   Single3 = null!;
				public LW3[] Many3   = null!;
				
			}

			abstract class LW3
			{
				public int Value3;
			}

			#pragma warning restore 649

			#endregion

		}
	}
}
