using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

using AdoNetCore.AseClient;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Tools;

using Microsoft.Data.SqlClient;

using NUnit.Framework;

namespace Tests.Linq
{
	public class PredicateTests : TestBase
	{
		#region DB Feature Tests

		sealed class FeatureTable
		{
			public int Id { get; set; }
			public int? One { get; set; }
			public int? Zero { get; set; }
			public int? Null { get; set; }
			[NotNull(Configuration = ProviderName.Sybase)]
			public bool? True { get; set; }
			[NotNull(Configuration = ProviderName.Sybase)]
			public bool? False { get; set; }
			[NotColumn(Configuration = ProviderName.Sybase)]
			public bool? BoolNull { get; set; }

			public static readonly FeatureTable[] Data =
			[
				new FeatureTable() { Id = 1, One = 1, Zero = 0, True = true, False = false }
			];
		}

		[Sql.Expression("{0}", IsPredicate = true, ServerSideOnly = true)]
		static bool AsIs(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS NULL", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNull(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} IS NOT NULL", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotNull(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS UNKNOWN", IsPredicate = true, ServerSideOnly = true)]
		static bool IsUnknown(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} IS NOT UNKNOWN", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotUnknown(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS TRUE", IsPredicate = true, ServerSideOnly = true)]
		static bool IsTrue(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} IS NOT TRUE", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotTrue(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS FALSE", IsPredicate = true, ServerSideOnly = true)]
		static bool IsFalse(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} IS NOT FALSE", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotFalse(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = TRUE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? EqualTrue(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> TRUE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? NotEqualTrue(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = FALSE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? EqualFalse(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> FALSE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? NotEqualFalse(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = UNKNOWN", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable)]
		static bool? EqualUnknown(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> UNKNOWN", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable)]
		static bool? NotEqualUnknown(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("({0} = {1})", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		static bool? Equal(int? left, int? right) => throw new InvalidOperationException();
		[Sql.Expression("({0} != {1})", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		static bool? NotEqual(int? left, int? right) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS DISTINCT FROM {1}", IsPredicate = true, ServerSideOnly = true)]
		static bool IsDistinctFrom(int? left, int? right) => throw new InvalidOperationException();
		[Sql.Expression("{0} IS NOT DISTINCT FROM {1}", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotDistinctFrom(int? left, int? right) => throw new InvalidOperationException();

		[Sql.Expression("{0} <=> {1}", IsPredicate = true, ServerSideOnly = true)]
		static bool NullSaveEqual(int? left, int? right) => throw new InvalidOperationException();
		[Sql.Expression("NOT({0} <=> {1})", IsPredicate = true, ServerSideOnly = true)]
		static bool NotNullSaveEqual(int? left, int? right) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS {1}", IsPredicate = true, ServerSideOnly = true)]
		static bool IsPredicate(int? left, int? right) => throw new InvalidOperationException();
		[Sql.Expression("{0} IS NOT {1}", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotPredicate(int? left, int? right) => throw new InvalidOperationException();

		[Sql.Expression("DECODE({0}, {1}, 0, 1) = 0", IsPredicate = true, ServerSideOnly = true)]
		static bool IsDistinctByDecode(int? left, int? right) => throw new InvalidOperationException();
		[Sql.Expression("DECODE({0}, {1}, 0, 1) <> 0", IsPredicate = true, ServerSideOnly = true)]
		static bool IsNotDistinctByDecode(int? left, int? right) => throw new InvalidOperationException();

		[Sql.Expression("EXISTS{0}", IsPredicate = true, ServerSideOnly = true)]
		static bool Exists(IQueryable<int?> values) => throw new InvalidOperationException();
		[Sql.Expression("NOT EXISTS{0}", IsPredicate = true, ServerSideOnly = true)]
		static bool NotExists(IQueryable<int?> values) => throw new InvalidOperationException();

		[Sql.Expression("{0} = (1=1)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? EqualCalculatedTrue(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> (1=1)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? NotEqualCalculatedTrue(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = (1=0)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? EqualCalculatedFalse(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> (1=0)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Unknown)]
		static bool? NotEqualCalculatedFalse(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = (1=null)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable, Precedence = Precedence.Unknown)]
		static bool? EqualCalculatedUnknown(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> (1=null)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable, Precedence = Precedence.Unknown)]
		static bool? NotEqualCalculatedUnknown(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS NULL", IsPredicate = true, ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Unknown)]
		static bool IsNull(object? value) => throw new InvalidOperationException();

		// Supported: DB2, FB3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "Unary predicate: IS [NOT] TRUE")]
		public void Test_Feature_IsTrue(
			[DataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => IsNotTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));
			});
		}

		// Supported: DB2, FB3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "Unary predicate: IS [NOT] FALSE")]
		public void Test_Feature_IsFalse(
			[DataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => IsNotFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));
			});
		}

		// Supported: Firebird3+, MySQL, PostgreSQL
		[Test(Description = "Unary predicate: IS [NOT] UNKNOWN")]
		public void Test_Feature_IsUnknown(
			[DataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: Access, ClickHouse, DB2, Firebird3+, MySql, PostgreSQL, SQLite
		[Test(Description = "Unary predicate 'IS [NOT] NULL' with BOOLEAN")]
		public void Test_Feature_IsNull(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsNull(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNotNull(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: Access, ClickHouse, DB2, Firebird3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "TRUE literal")]
		public void Test_Feature_True(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				ProviderName.SqlCe,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: Access, ClickHouse, DB2, Firebird3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "FALSE literal")]
		public void Test_Feature_False(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				ProviderName.SqlCe,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: Firebird3+
		[Test(Description = "UNKNOWN literal")]
		public void Test_Feature_Unknown(
			[DataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				ProviderName.Firebird25,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: ACCESS, CH, DB2, FB3+, MYSQL, PGSQL, SQLITE
		[Test(Description = "predicate = (1=1)")]
		public void Test_Feature_CalculatedTrue(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: ACCESS, CH, DB2, FB3+, MYSQL, PGSQL, SQLITE
		[Test(Description = "predicate = (1=0)")]
		public void Test_Feature_CalculatedFalse(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: ACCESS, CH, DB2, FB3+, MYSQL, PGSQL, SQLITE
		[Test(Description = "predicate = (1=null)")]
		public void Test_Feature_CalculatedUnknown(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(0));
			});
		}

		// Supported: DB2, Firebird, PostgreSQL, SQLite, SQLServer2022
		// ClickHouse: tracked by https://github.com/ClickHouse/ClickHouse/issues/58145
		[Test(Description = "IS [NOT] DISTICT FROM predicate")]
		public void Test_Feature_DistinctFrom(
			[DataSources(false,
				TestProvName.AllAccess,
				ProviderName.SqlCe,
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				TestProvName.AllSqlServer2019Minus,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsDistinctFrom(r.One, r.One)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsDistinctFrom(r.Zero, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsDistinctFrom(r.Null, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsDistinctFrom(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctFrom(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctFrom(r.Zero, r.Null)).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNotDistinctFrom(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.One, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.One, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.Zero, r.Null)).Count(), Is.EqualTo(0));
			});
		}

		// Supported: MySQL
		// ClickHouse: tracked by https://github.com/ClickHouse/ClickHouse/issues/58145
		[Test(Description = "<=> predicate")]
		public void Test_Feature_NullSaveEqual(
			[DataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => NullSaveEqual(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NullSaveEqual(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NullSaveEqual(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NullSaveEqual(r.One, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NullSaveEqual(r.One, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NullSaveEqual(r.Zero, r.Null)).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => NotNullSaveEqual(r.One, r.One)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.Zero, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.Null, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.Zero, r.Null)).Count(), Is.EqualTo(1));
			});
		}

		// Supported: SQLite
		[Test(Description = "IS predicate")]
		public void Test_Feature_Is(
			[DataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsPredicate(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsPredicate(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsPredicate(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsPredicate(r.One, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsPredicate(r.One, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsPredicate(r.Zero, r.Null)).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => IsNotPredicate(r.One, r.One)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotPredicate(r.Zero, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotPredicate(r.Null, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotPredicate(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotPredicate(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotPredicate(r.Zero, r.Null)).Count(), Is.EqualTo(1));
			});
		}

		// Supported: DB2, Oracle
		// Firebird: doesn't work for NULLs
		[Test(Description = "DECODE function")]
		public void Test_Feature_Decode(
			[DataSources(false,
				TestProvName.AllAccess,
				ProviderName.SqlCe,
				TestProvName.AllClickHouse,
				TestProvName.AllFirebird,
				TestProvName.AllMySql,
				TestProvName.AllSapHana,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSqlServer,
				TestProvName.AllSQLite,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsDistinctByDecode(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.One, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.One, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.Zero, r.Null)).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.One, r.One)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.Zero, r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.Null, r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.Zero, r.Null)).Count(), Is.EqualTo(1));
			});
		}

		// While test itself works for almost all databases, approach makes sense only for
		// databases with INTERSECT support:
		// DB2, Informix, MySql8, MariaDB, Oracle, PostgreSQL, SAP HANA, SQLite, SQL Server
		// Not for:
		// Firebird, MySql 5.7, SQL CE,
		// ASE (added in 16SP3)
		[Test(Description = "EXISTS INTERSECT")]
		public void Test_Feature_Intersect(
			[DataSources(false, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.One)))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.Null).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(0));

				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.One)))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.Null).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(1));
			});
		}

		// Supported: CH, DB2, FB3+, MYSQL, SQLITE
		[Test(Description = "Equality: predicate vs predicate")]
		public void Test_Feature_PredicateComparison(
			[DataSources(false,
				TestProvName.AllAccess,
				ProviderName.Firebird25,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.Multiple(() =>
			{
				Assert.That(tb.Where(r => IsNull(r.One) == IsNull(r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(r.One) == IsNull(r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(r.One) == IsNull(r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(r.Zero) == IsNull(r.Null)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(r.Zero) == IsNull(r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(r.Null) == IsNull(r.Null)).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNull(r.One) != IsNull(r.One)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(r.One) != IsNull(r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(r.One) != IsNull(r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(r.Zero) != IsNull(r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(r.Zero) != IsNull(r.Zero)).Count(), Is.EqualTo(0));
				Assert.That(tb.Where(r => IsNull(r.Null) != IsNull(r.Null)).Count(), Is.EqualTo(0));
			});
		}

		// Supported: Access, ClickHouse, DB2, FB3+, IFX, MYSQL, PGSQL, SQLITE
		[Test(Description = "Boolean value as predicate")]
		public void Test_Feature_BooleanAsPredicate_True(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.That(tb.Where(r => AsIs(r.True)).Count(), Is.EqualTo(1));
		}

		// Supported: Access, ClickHouse, DB2, FB3+, IFX, MYSQL, PGSQL, SQLITE
		[Test(Description = "Boolean value as predicate")]
		public void Test_Feature_BooleanAsPredicate_False(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.That(tb.Where(r => AsIs(r.False)).Count(), Is.EqualTo(0));
		}

		// Supported: Access, ClickHouse, DB2, FB3+, IFX, MYSQL, PGSQL, SQLITE
		[Test(Description = "Boolean value as predicate")]
		public void Test_Feature_BooleanAsPredicate_Null(
			[DataSources(false,
				ProviderName.Firebird25,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.That(tb.Where(r => AsIs(r.BoolNull)).Count(), Is.EqualTo(0));
		}

		#endregion

		#region Translation Tests
		sealed class BooleanTable
		{
			public int Id { get; set; }

			public int Value1 { get; set; }
			public int Value2 { get; set; }
			public int? Value4 { get; set; }
			public int? Value5 { get; set; }

			static BooleanTable()
			{
				var testData = new List<BooleanTable>();
				var smallTestData = new List<BooleanTable>();

				var values1 = new int[] { 0, 1 };
				var values2 = new int?[] { null, 0, 1 };

				var id = 1;
				foreach (var v1 in values1)
					foreach (var v2 in values1)
						foreach (var v4 in values2)
							foreach (var v5 in values2)
							{
								testData.Add(new BooleanTable()
								{
									Id = id++,
									Value1 = v1,
									Value2 = v2,
									Value4 = v4,
									Value5 = v5,
								});
							}

				Data = testData;
			}

			public static readonly IReadOnlyCollection<BooleanTable> Data;
		}

		[Test]
		public void Test_PredicateWithBoolean([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			db.InlineParameters = inline;

			var True = true;
			var False = false;
			bool? TrueN = true;
			bool? FalseN = false;
			bool? Null = null;

			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) == True));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) == Null));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) == Null));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) == Null));

			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != True));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != False));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != Null));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value4) != Null));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value5 == r.Value4) != Null));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) == True));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) == False));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) == Null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) == Null));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) == Null));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) != True));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) != False));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value2) != Null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4) != Null));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value5 > r.Value4) != Null));

			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == True));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == False));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == Null));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) == Null));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) == Null));

			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != True));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != False));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != Null));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value4) != Null));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value5 >= r.Value4) != Null));
		}

		[Test]
		public void Test_PredicateWithPredicate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) == (r.Value4 == r.Value5)));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value5) == (r.Value4 == r.Value2)));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != (r.Value4 == r.Value5)));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value5) != (r.Value4 == r.Value2)));

			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) == (r.Value2 == r.Value1)));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != (r.Value2 == r.Value1)));
			AssertQuery(tb.Where(r => (r.Value4 == r.Value5) == (r.Value5 == r.Value4)));
			AssertQuery(tb.Where(r => (r.Value4 == r.Value5) != (r.Value5 == r.Value4)));

			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == (r.Value4 != r.Value5)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value5) == (r.Value4 != r.Value2)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != (r.Value4 != r.Value5)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value5) != (r.Value4 != r.Value2)));

			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == (r.Value2 != r.Value1)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != (r.Value2 != r.Value1)));
			AssertQuery(tb.Where(r => (r.Value4 >= r.Value5) == (r.Value5 != r.Value4)));
			AssertQuery(tb.Where(r => (r.Value4 >= r.Value5) != (r.Value5 != r.Value4)));
		}

		[Test]
		public void Test_PredicateVsPredicate_TriStrate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			AssertQuery(tb.Where(r => Equal(r.Value1, r.Value2) == Equal(r.Value4, r.Value5)));
			AssertQuery(tb.Where(r => Equal(r.Value1, r.Value5) == Equal(r.Value4, r.Value2)));
			AssertQuery(tb.Where(r => Equal(r.Value1, r.Value2) != Equal(r.Value4, r.Value5)));
			AssertQuery(tb.Where(r => Equal(r.Value1, r.Value5) != Equal(r.Value4, r.Value2)));

			AssertQuery(tb.Where(r => Equal(r.Value1, r.Value2) == Equal(r.Value2, r.Value1)));
			AssertQuery(tb.Where(r => Equal(r.Value1, r.Value2) != Equal(r.Value2, r.Value1)));
			AssertQuery(tb.Where(r => Equal(r.Value4, r.Value5) == Equal(r.Value5, r.Value4)));
			AssertQuery(tb.Where(r => Equal(r.Value4, r.Value5) != Equal(r.Value5, r.Value4)));

			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == NotEqual(r.Value4, r.Value5)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value5) == NotEqual(r.Value4, r.Value2)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != NotEqual(r.Value4, r.Value5)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value5) != NotEqual(r.Value4, r.Value2)));

			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) == NotEqual(r.Value2, r.Value1)));
			AssertQuery(tb.Where(r => (r.Value1 >= r.Value2) != NotEqual(r.Value2, r.Value1)));
			AssertQuery(tb.Where(r => (r.Value4 >= r.Value5) == NotEqual(r.Value5, r.Value4)));
			AssertQuery(tb.Where(r => (r.Value4 >= r.Value5) != NotEqual(r.Value5, r.Value4)));
		}

		[Test]
		public void Test_FieldInSubquery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			var sub = tb.Select(r => r.Value1);
			var subN = tb.Select(r => r.Value4);

			AssertQuery(tb.Where(r => r.Value2.In(sub)));
			AssertQuery(tb.Where(r => subN.Contains(r.Value2)));
			AssertQuery(tb.Where(r => sub.Select(v => (int?)v).Contains(r.Value5)));
			AssertQuery(tb.Where(r => subN.Contains(r.Value5)));

			AssertQuery(tb.Where(r => !r.Value2.In(sub)));
			AssertQuery(tb.Where(r => !subN.Contains(r.Value2)));
			AssertQuery(tb.Where(r => !sub.Select(v => (int?)v).Contains(r.Value5)));
			AssertQuery(tb.Where(r => !subN.Contains(r.Value5)));
		}

		[Test]
		public void Test_VariableInSubquery([DataSources(TestProvName.AllClickHouse)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			db.InlineParameters = inline;

			var One = 1;
			var Zero = 0;
			int? OneN = 1;
			int? ZeroN = 0;
			int? Null = null;


			AssertQuery(tb.Where(r => tb.Where(s => s.Id > r.Id).Select(s => s.Value1).Contains(One)));
			AssertQuery(tb.Where(r => tb.Where(s => s.Id > r.Id).Select(s => s.Value1).Contains(Zero)));
			AssertQuery(tb.Where(r => tb.Where(s => s.Id > r.Id).Select(s => (int?)s.Value1).Contains(OneN)));
			AssertQuery(tb.Where(r => tb.Where(s => s.Id > r.Id).Select(s => (int?)s.Value1).Contains(ZeroN)));
			AssertQuery(tb.Where(r => tb.Where(s => s.Id > r.Id).Select(s => (int?)s.Value1).Contains(Null)));

			AssertQuery(tb.Where(r => !tb.Where(s => s.Id > r.Id).Select(s => s.Value1).Contains(One)));
			AssertQuery(tb.Where(r => !tb.Where(s => s.Id > r.Id).Select(s => s.Value1).Contains(Zero)));
			AssertQuery(tb.Where(r => !tb.Where(s => s.Id > r.Id).Select(s => (int?)s.Value1).Contains(OneN)));
			AssertQuery(tb.Where(r => !tb.Where(s => s.Id > r.Id).Select(s => (int?)s.Value1).Contains(ZeroN)));
			AssertQuery(tb.Where(r => !tb.Where(s => s.Id > r.Id).Select(s => (int?)s.Value1).Contains(Null)));
		}

		[Test]
		public void Test_FieldInList([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			db.InlineParameters = inline;

			var list = new int[] {0, 1 };
			var listN = new int?[]{ 0, null, 1 };

			AssertQuery(tb.Where(r => r.Value2.In(list)));
			AssertQuery(tb.Where(r => listN.Contains(r.Value2)));
			AssertQuery(tb.Where(r => list.Select(v => (int?)v).Contains(r.Value5)));
			AssertQuery(tb.Where(r => listN.Contains(r.Value5)));

			AssertQuery(tb.Where(r => !r.Value2.In(list)));
			AssertQuery(tb.Where(r => !listN.Contains(r.Value2)));
			AssertQuery(tb.Where(r => !list.Select(v => (int?)v).Contains(r.Value5)));
			AssertQuery(tb.Where(r => !listN.Contains(r.Value5)));
		}

		[Test]
		public void Test_VariableInList([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			db.InlineParameters = inline;

			var One = 1;
			var Zero = 0;
			int? OneN = 1;
			int? ZeroN = 0;
			int? Null = null;

			var list = new int[] {0, 1 };
			var listN = new int?[]{ 0, null, 1 };

			AssertQuery(tb.Where(r => list.Contains(One)));
			AssertQuery(tb.Where(r => list.Contains(Zero)));
			AssertQuery(tb.Where(r => list.Select(i => (int?)i).Contains(OneN)));
			AssertQuery(tb.Where(r => list.Select(i => (int?)i).Contains(ZeroN)));
			AssertQuery(tb.Where(r => list.Select(i => (int?)i).Contains(Null)));
			AssertQuery(tb.Where(r => listN.Contains(One)));
			AssertQuery(tb.Where(r => listN.Contains(Zero)));
			AssertQuery(tb.Where(r => listN.Contains(OneN)));
			AssertQuery(tb.Where(r => listN.Contains(ZeroN)));
			AssertQuery(tb.Where(r => listN.Contains(Null)));

			AssertQuery(tb.Where(r => !list.Contains(One)));
			AssertQuery(tb.Where(r => !list.Contains(Zero)));
			AssertQuery(tb.Where(r => !list.Select(i => (int?)i).Contains(OneN)));
			AssertQuery(tb.Where(r => !list.Select(i => (int?)i).Contains(ZeroN)));
			AssertQuery(tb.Where(r => !list.Select(i => (int?)i).Contains(Null)));
			AssertQuery(tb.Where(r => !listN.Contains(One)));
			AssertQuery(tb.Where(r => !listN.Contains(Zero)));
			AssertQuery(tb.Where(r => !listN.Contains(OneN)));
			AssertQuery(tb.Where(r => !listN.Contains(ZeroN)));
			AssertQuery(tb.Where(r => !listN.Contains(Null)));
		}
		#endregion
	}
}
