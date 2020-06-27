﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using SqlQuery;
	using Mapping;
	using Expressions;
	using Linq;

	public interface IPostgreSQLExtensions
	{
	}

	public static class PostgreSQLExtensions
	{
		public static IPostgreSQLExtensions? PostgreSQL(this Sql.ISqlExtension? ext) => null;

		#region Analytic Functions

		class ApplyAggregateModifier: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var modifier = builder.GetValue<Sql.AggregateModifier>("modifier");
				switch (modifier)
				{
					case Sql.AggregateModifier.None :
						break;
					case Sql.AggregateModifier.Distinct :
						builder.AddExpression("modifier", "DISTINCT");
						break;
					case Sql.AggregateModifier.All :
						builder.AddExpression("modifier", "ALL");
						break;
					default :
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		#region array_agg

		[Sql.Extension("ARRAY_AGG({expr})", TokenName = AnalyticFunctions.FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static AnalyticFunctions.IAnalyticFunctionWithoutWindow<T[]> ArrayAggregate<T>(this Sql.ISqlExtension? ext, 
			[ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(ArrayAggregate)}' is server-side method.");
		}
		
		[Sql.Extension("ARRAY_AGG({modifier?}{_}{expr})", TokenName = AnalyticFunctions.FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsAggregate = true)]
		public static AnalyticFunctions.IAnalyticFunctionWithoutWindow<T[]> ArrayAggregate<T>(this Sql.ISqlExtension? ext, 
			[ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(ArrayAggregate)}' is server-side method.");
		}
		
		[Sql.Extension("ARRAY_AGG({modifier?}{_}{expr}{_}{order_by_clause?})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 10)]
		public static Sql.IAggregateFunctionNotOrdered<TEntity, TV[]> ArrayAggregate<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(ArrayAggregate)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_AGG({expr}{_}{order_by_clause?})", IsAggregate = true, ChainPrecedence = 10)]
		public static Sql.IAggregateFunctionNotOrdered<TEntity, TV[]> ArrayAggregate<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (expr    == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			var query = currentSource.Provider.CreateQuery<TV[]>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ArrayAggregate, source, expr),
					currentSource.Expression, Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<TEntity, TV[]>(query);
		}

		[Sql.Extension("ARRAY_AGG({modifier?}{_}{expr}{_}{order_by_clause?})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 10)]
		public static Sql.IAggregateFunctionNotOrdered<TEntity, TV[]> ArrayAggregate<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (expr    == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			var query = currentSource.Provider.CreateQuery<TV[]>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ArrayAggregate, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));

			return new Sql.AggregateFunctionNotOrderedImpl<TEntity, TV[]>(query);
		}

		#endregion

		#endregion

		#region unnest

#if !NET45
		[ExpressionMethod(nameof(UnnestImpl))]
		public static IQueryable<T> Unnest<T>(this IDataContext dc, T[] array)
		{
			//TODO: can be executable when we finish queryable arrays
			throw new LinqException($"'{nameof(Unnest)}' is server-side method.");
		}

		static Expression<Func<IDataContext, T[], IQueryable<T>>> UnnestImpl<T>()
		{
			return (dc, array) => dc.FromSqlScalar<T>($"UNNEST({array})");
		}
		public class Ordinality<T>
		{
			[Column(Name = "value")]                  public T    Value = default!;
			[Column(Name = "idx", CanBeNull = false)] public long Index;
		}

		[ExpressionMethod(nameof(UnnestWithOrdinalityImpl))]
		public static IQueryable<Ordinality<T>> UnnestWithOrdinality<T>(this IDataContext dc, T[] array)
		{
			//TODO: can be executable when we finish queryable arrays
			throw new LinqException($"'{nameof(UnnestWithOrdinality)}' is server-side method.");
		}

		static Expression<Func<IDataContext, T[], IQueryable<Ordinality<T>>>> UnnestWithOrdinalityImpl<T>()
		{
			return (dc, array) => dc.FromSql<Ordinality<T>>($"UNNEST({array}) WITH ORDINALITY {Sql.AliasExpr()}(value, idx)");
		}
#endif
		
		#endregion

		#region Arrays

		[Sql.Extension("{arrays, ' || '}", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Additive)]
		public static T[] ConcatArrays<T>(this IPostgreSQLExtensions? ext, [ExprParameter] params T[][] arrays)
		{
			throw new LinqException($"'{nameof(ConcatArrays)}' is server-side method.");
		}


		[Sql.Extension("{array1} || {array2}", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Additive)]
		public static T[] ConcatArrays<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[][] array2)
		{
			throw new LinqException($"'{nameof(ConcatArrays)}' is server-side method.");
		}

		[CLSCompliant(false)]
		[Sql.Extension("{array1} || {array2}", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Additive)]
		public static T[] ConcatArrays<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[][] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(ConcatArrays)}' is server-side method.");
		}

		[Sql.Extension("{array1} < {array2}", ServerSideOnly = true, CanBeNull = true, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool LessThan<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(LessThan)}' is server-side method.");
		}

		[Sql.Extension("{array1} <= {array2}", ServerSideOnly = true, CanBeNull = false, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool LessThanOrEqual<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(LessThanOrEqual)}' is server-side method.");
		}

		[Sql.Extension("{array1} > {array2}", ServerSideOnly = true, CanBeNull = false, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool GreaterThan<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(GreaterThan)}' is server-side method.");
		}

		[Sql.Extension("{array1} > {array2}", ServerSideOnly = true, CanBeNull = false, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool GreaterThanOrEqual<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(GreaterThanOrEqual)}' is server-side method.");
		}

		[Sql.Extension("{array1} @> {array2}", ServerSideOnly = true, CanBeNull = false, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool Contains<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		[Sql.Extension("{array1} <@ {array2}", ServerSideOnly = true, CanBeNull = false, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool ContainedBy<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(ContainedBy)}' is server-side method.");
		}

		[Sql.Extension("{array1} && {array2}", ServerSideOnly = true, CanBeNull = false, IsPredicate = true, Precedence = Precedence.Comparison)]
		public static bool Overlaps<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(Overlaps)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_APPEND({array}, {element})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static T[] ArrayAppend<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] T element)
		{
			throw new LinqException($"'{nameof(ArrayAppend)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_CAT({array1}, {array2})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static T[] ArrayCat<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array1, [ExprParameter] T[] array2)
		{
			throw new LinqException($"'{nameof(ArrayCat)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_NDIMS({array})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int ArrayNDims<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array)
		{
			throw new LinqException($"'{nameof(ArrayNDims)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_DIMS({array})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static string ArrayDims<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array)
		{
			throw new LinqException($"'{nameof(ArrayDims)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_LENGTH({array}, {dimension})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int ArrayLength<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] int dimension)
		{
			throw new LinqException($"'{nameof(ArrayLength)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_LOWER({array}, {dimension})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int ArrayLower<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] int dimension)
		{
			throw new LinqException($"'{nameof(ArrayLower)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_POSITION({array}, {element})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int ArrayPosition<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] T element)
		{
			throw new LinqException($"'{nameof(ArrayPosition)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_POSITION({array}, {element}, {start})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int ArrayPosition<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] T element, [ExprParameter] int start)
		{
			throw new LinqException($"'{nameof(ArrayPosition)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_POSITIONS({array}, {element})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int[] ArrayPositions<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] T element)
		{
			throw new LinqException($"'{nameof(ArrayPositions)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_PREPEND({element}, {array})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static T[] ArrayPrepend<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T element, [ExprParameter] T[] array)
		{
			throw new LinqException($"'{nameof(ArrayPrepend)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_REMOVE({array}, {element})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static T[] ArrayRemove<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] T element)
		{
			throw new LinqException($"'{nameof(ArrayRemove)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_REPLACE({array}, {oldElement}, {newElement})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static T[] ArrayReplace<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] T oldElement, [ExprParameter] T newElement)
		{
			throw new LinqException($"'{nameof(ArrayReplace)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_UPPER({array}, {dimension})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static T[] ArrayUpper<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] int dimension)
		{
			throw new LinqException($"'{nameof(ArrayUpper)}' is server-side method.");
		}

		[Sql.Extension("CARDINALITY({array})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static int Cardinality<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array)
		{
			throw new LinqException($"'{nameof(Cardinality)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_TO_STRING({array}, {delimiter})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static string ArrayToString<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] string delimiter)
		{
			throw new LinqException($"'{nameof(ArrayToString)}' is server-side method.");
		}


		[Sql.Extension("STRING_TO_ARRAY({str}, {delimiter})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static string[] StringToArray(this IPostgreSQLExtensions? ext, [ExprParameter] string str, [ExprParameter] string delimiter)
		{
			throw new LinqException($"'{nameof(StringToArray)}' is server-side method.");
		}

		[Sql.Extension("STRING_TO_ARRAY({str}, {delimiter}, {nullString})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static string[] StringToArray(this IPostgreSQLExtensions? ext, [ExprParameter] string str, [ExprParameter] string delimiter, [ExprParameter] string nullString)
		{
			throw new LinqException($"'{nameof(StringToArray)}' is server-side method.");
		}

		[Sql.Extension("ARRAY_TO_STRING({array}, {delimiter}, {nullString})", ServerSideOnly = true, CanBeNull = true, Precedence = Precedence.Primary)]
		public static string ArrayToString<T>(this IPostgreSQLExtensions? ext, [ExprParameter] T[] array, [ExprParameter] string delimiter, [ExprParameter] string nullString)
		{
			throw new LinqException($"'{nameof(ArrayToString)}' is server-side method.");
		}

		#endregion

		#region generate_series

#if !NET45
		static Func<IDataContext, int, int, IQueryable<int>>? _generateSeriesIntFunc;

		[ExpressionMethod(nameof(GenerateSeriesIntImpl))]
		public static IQueryable<int> GenerateSeries(this IDataContext dc, [ExprParameter] int start, [ExprParameter] int stop)
		{
			return (_generateSeriesIntFunc ??= GenerateSeriesIntImpl().Compile())(dc, start, stop);
		}

		static Expression<Func<IDataContext, int, int, IQueryable<int>>> GenerateSeriesIntImpl()
		{
			return (dc, start, stop) => dc.FromSqlScalar<int>($"GENERATE_SERIES({start}, {stop})");
		}


		static Func<IDataContext, int, int, int, IQueryable<int>>? _generateSeriesIntStepFunc;

		[ExpressionMethod(nameof(GenerateSeriesIntStepImpl))]
		public static IQueryable<int> GenerateSeries(this IDataContext dc, [ExprParameter] int start, [ExprParameter] int stop, [ExprParameter] int step)
		{
			return (_generateSeriesIntStepFunc ??= GenerateSeriesIntStepImpl().Compile())(dc, start, stop, step);
		}

		static Expression<Func<IDataContext, int, int, int, IQueryable<int>>> GenerateSeriesIntStepImpl()
		{
			return (dc, start, stop, step) => dc.FromSqlScalar<int>($"GENERATE_SERIES({start}, {stop}, {step})");
		}


		static Func<IDataContext, DateTime, DateTime, TimeSpan, IQueryable<DateTime>>? _generateSeriesDateFunc;

		[ExpressionMethod(nameof(GenerateSeriesDateImpl))]
		public static IQueryable<DateTime> GenerateSeries(this IDataContext dc, [ExprParameter] DateTime start, [ExprParameter] DateTime stop, [ExprParameter] TimeSpan step)
		{
			return (_generateSeriesDateFunc ??= GenerateSeriesDateImpl().Compile())(dc, start, stop, step);
		}

		static Expression<Func<IDataContext, DateTime, DateTime, TimeSpan, IQueryable<DateTime>>> GenerateSeriesDateImpl()
		{
			return (dc, start, stop, step) => dc.FromSqlScalar<DateTime>($"GENERATE_SERIES({start}, {stop}, {step})");
		}
#endif
		#endregion

		#region generate_subscripts

#if !NET45
		[ExpressionMethod(nameof(GenerateSubscriptsImpl))]
		public static IQueryable<int> GenerateSubscripts<T>(this IDataContext dc, T[] array, int dimension)
		{
			//TODO: can be executable when we finish queryable arrays
			throw new LinqException($"'{nameof(GenerateSubscripts)}' is server-side method.");
		}

		static Expression<Func<IDataContext, T[], int, IQueryable<int>>> GenerateSubscriptsImpl<T>()
		{
			return (dc, array, dimension) => dc.FromSqlScalar<int>($"GENERATE_SUBSCRIPTS({array}, {dimension})");
		}

		[ExpressionMethod(nameof(GenerateSubscriptsReverseImpl))]
		public static IQueryable<int> GenerateSubscripts<T>(this IDataContext dc, T[] array, int dimension, bool reverse)
		{
			//TODO: can be executable when we finish queryable arrays
			throw new LinqException($"'{nameof(GenerateSubscripts)}' is server-side method.");
		}

		static Expression<Func<IDataContext, T[], int, bool, IQueryable<int>>> GenerateSubscriptsReverseImpl<T>()
		{
			return (dc, array, dimension, reverse) => dc.FromSqlScalar<int>($"GENERATE_SUBSCRIPTS({array}, {dimension}, {reverse})");
		}
#endif

		#endregion

		#region System Functions

		[Sql.Extension("VERSION()", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string Version(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.Version(dc));
		}

		[Sql.Extension("CURRENT_CATALOG", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string CurrentCatalog(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.CurrentCatalog(dc));
		}

		[Sql.Extension("CURRENT_DATABASE()", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string CurrentDatabase(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.CurrentDatabase(dc));
		}

		[Sql.Extension("CURRENT_ROLE", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string CurrentRole(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.CurrentRole(dc));
		}

		[Sql.Extension("CURRENT_SCHEMA", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string CurrentSchema(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.CurrentSchema(dc));
		}

		[Sql.Extension("CURRENT_SCHEMAS()", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string[] CurrentSchemas(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.CurrentSchemas(dc));
		}

		[Sql.Extension("CURRENT_SCHEMAS({includeImplicit})", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string[] CurrentSchemas(this IPostgreSQLExtensions? ext, IDataContext dc, [ExprParameter] bool includeImplicit)
		{
			return dc.Select(() => ext.CurrentSchemas(dc, includeImplicit));
		}

		[Sql.Extension("CURRENT_USER", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string CurrentUser(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.CurrentUser(dc));
		}

		[Sql.Extension("SESSION_USER", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static string SessionUser(this IPostgreSQLExtensions? ext, IDataContext dc)
		{
			return dc.Select(() => ext.SessionUser(dc));
		}

		#endregion


	}
}
