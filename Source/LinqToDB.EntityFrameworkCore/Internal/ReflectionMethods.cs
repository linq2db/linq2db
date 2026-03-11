using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace LinqToDB.EntityFrameworkCore.Internal
{
	/// <summary>
	/// Contains reflection methods for EF Core.
	/// </summary>
	public static class ReflectionMethods
	{
#if EF31
		public static readonly MethodInfo FromSqlOnQueryableMethodInfo = typeof(RelationalQueryableExtensions).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Single(x => string.Equals(x.Name, "FromSqlOnQueryable", StringComparison.Ordinal)).GetGenericMethodDefinition();
#endif

		public static readonly MethodInfo IgnoreQueryFiltersMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.IgnoreQueryFilters());
		public static readonly MethodInfo IncludeMethodInfo            = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.Include(o => o));
		public static readonly MethodInfo IncludeMethodInfoString      = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.Include(string.Empty));
		public static readonly MethodInfo ThenIncludeMethodInfo        = MemberHelper.MethodOfGeneric<IIncludableQueryable<object, object>>(q => q.ThenInclude<object, object, object>(null!));
		public static readonly MethodInfo TagWithMethodInfo            = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.TagWith(string.Empty));

#if !EF31
		public static readonly MethodInfo AsSplitQueryMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsSplitQuery());

		public static readonly MethodInfo AsSingleQueryMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsSingleQuery());
#endif

		public static readonly MethodInfo ThenIncludeEnumerableMethodInfo = MemberHelper.MethodOfGeneric<IIncludableQueryable<object, IEnumerable<object>>>(q => q.ThenInclude<object, object, object>(null!));
		public static readonly MethodInfo AsNoTrackingMethodInfo          = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsNoTracking());
		public static readonly MethodInfo AsTrackingMethodInfo            = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsTracking());
#if !EF31
		public static readonly MethodInfo AsNoTrackingWithIdentityResolutionMethodInfo = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsNoTrackingWithIdentityResolution());
#endif

		public static readonly MethodInfo      EFProperty               = MemberHelper.MethodOfGeneric(() => EF.Property<object>(1, ""));
		public static readonly MethodInfo      L2DBFromSqlMethodInfo    = MemberHelper.MethodOfGeneric<IDataContext>(dc => dc.FromSql<object>(new RawSqlString()));
		public static readonly ConstructorInfo RawSqlStringConstructor  = MemberHelper.ConstructorOf(() => new RawSqlString(""));
		public static readonly ConstructorInfo DataParameterConstructor = MemberHelper.ConstructorOf(() => new DataParameter("", "", DataType.Undefined, ""));
		public static readonly MethodInfo      ToSql                    = MemberHelper.MethodOfGeneric(() => Sql.ToSql(1));
		public static readonly MethodInfo      ToLinqToDBTable          = MemberHelper.MethodOfGeneric<DbSet<object>>(q => q.ToLinqToDBTable());

#if !EF31
		public static readonly MethodInfo AsSqlServerTable    = MemberHelper.MethodOfGeneric<ITable<object>>(q => SqlServerSpecificExtensions.AsSqlServer(q));
		public static readonly MethodInfo TemporalAsOfTable   = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableAsOf(t, default));
		public static readonly MethodInfo TemporalFromTo      = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableFromTo(t, default, default));
		public static readonly MethodInfo TemporalBetween     = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableBetween(t, default, default));
		public static readonly MethodInfo TemporalContainedIn = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableContainedIn(t, default, default));
		public static readonly MethodInfo TemporalAll         = MemberHelper.MethodOfGeneric<ISqlServerSpecificTable<object>>(t => SqlServerHints.TemporalTableAll(t));
#endif

		public static readonly Func<object?, object?> ContextDependenciesGetValueMethod = (typeof(RelationalQueryContextFactory)
#if !EF31
			.GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new LinqToDBForEFToolsException($"Can not find protected property '{nameof(RelationalQueryContextFactory)}.Dependencies' in current EFCore Version.")
#else
			.GetField("_dependencies", BindingFlags.NonPublic | BindingFlags.Instance) 
				?? throw new LinqToDBForEFToolsException($"Can not find private property '{nameof(RelationalQueryContextFactory)}._dependencies' in current EFCore Version.")
#endif
			).GetValue;

	}
}
