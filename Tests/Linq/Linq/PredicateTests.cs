using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	public class PredicateTests : TestBase
	{
		#region DB Feature Tests

		sealed class FeatureTable
		{
			[PrimaryKey]
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

		[Sql.Expression("{0} = TRUE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? EqualTrue(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> TRUE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? NotEqualTrue(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = FALSE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? EqualFalse(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> FALSE", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? NotEqualFalse(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = UNKNOWN", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable)]
		static bool? EqualUnknown(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> UNKNOWN", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable)]
		static bool? NotEqualUnknown(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("({0} = {1})", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		static bool? Equal(int? left, int? right) => left is null || right is null ? null : left == right;
		[Sql.Expression("({0} <> {1})", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		static bool? NotEqual(int? left, int? right) => left is null || right is null ? null : left != right;

		[Sql.Expression("{0} = {1}", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.NotNullable, Precedence = Precedence.Comparison)]
		static bool Equal(bool left, bool right) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> {1}", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.NotNullable, Precedence = Precedence.Comparison)]
		static bool NotEqual(bool left, bool right) => throw new InvalidOperationException();

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

		[Sql.Expression("{0} = (1=1)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? EqualCalculatedTrue(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> (1=1)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? NotEqualCalculatedTrue(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = (1=0)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? EqualCalculatedFalse(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> (1=0)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable, Precedence = Precedence.Comparison)]
		static bool? NotEqualCalculatedFalse(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} = (1=null)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable, Precedence = Precedence.Comparison)]
		static bool? EqualCalculatedUnknown(bool? value) => throw new InvalidOperationException();
		[Sql.Expression("{0} <> (1=null)", IsPredicate = true, ServerSideOnly = true, IsNullable = Sql.IsNullableType.Nullable, Precedence = Precedence.Comparison)]
		static bool? NotEqualCalculatedUnknown(bool? value) => throw new InvalidOperationException();

		[Sql.Expression("{0} IS NULL", IsPredicate = true, ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Comparison)]
		static bool IsNull(object? value) => throw new InvalidOperationException();

		[Sql.Expression("({0} + {1})", ServerSideOnly = true, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		static int? Add(int? left, int? right) => left is null || right is null ? null : left + right;

		[Sql.Expression("(1=1)", IsPredicate = true, ServerSideOnly = true)]
		static bool TruePredicate() => true;

		// Supported: DB2, FB3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "<PREDICATE> IS [NOT] TRUE")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_IsTrue([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsTrue(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsTrue(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsTrue(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsTrue(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => IsNotTrue(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotTrue(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));
			}
		}

		// Supported: DB2, FB3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "<PREDICATE> IS [NOT] FALSE")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_IsFalse([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsFalse(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsFalse(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsFalse(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsFalse(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsFalse(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => IsNotFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotFalse(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));
			}
		}

		// Supported: Firebird3+, MySQL, PostgreSQL
		[Test(Description = "<PREDICATE> IS [NOT] UNKNOWN")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2, TestProvName.AllDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("System.Data.SQLite.SQLiteException", TestProvName.AllSQLiteClassic)]
		[ThrowsForProvider("Microsoft.Data.Sqlite.SqliteException", ProviderName.SQLiteMS)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_IsUnknown([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsUnknown(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsUnknown(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsUnknown(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsUnknown(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsUnknown(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotUnknown(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: Access, ClickHouse, DB2, Firebird3+, MySql, PostgreSQL, SQLite
		[Test(Description = "<PREDICATE> IS [NOT] NULL")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		public void Test_Feature_IsNull([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsNull(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNull(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNull(Equal(r.Null, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNull(Equal(r.One, r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNull(Equal(r.Zero, r.Null))).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNotNull(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotNull(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotNull(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotNull(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: Access, ClickHouse, DB2, Firebird3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "<PREDICATE> <>/= TRUE")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		public void Test_Feature_True([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualTrue(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualTrue(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: Access, ClickHouse, DB2, Firebird3+, MySQL, PostgreSQL, SQLite
		[Test(Description = "<PREDICATE> <>/= FALSE")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		public void Test_Feature_False([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualFalse(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualFalse(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: Firebird3+
		[Test(Description = "<PREDICATE> <>/= UNKNOWN")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("Npgsql.PostgresException", TestProvName.AllPostgreSQL)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql, TestProvName.AllMySqlConnector)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2, TestProvName.AllDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("System.Data.SQLite.SQLiteException", TestProvName.AllSQLiteClassic)]
		[ThrowsForProvider("Microsoft.Data.Sqlite.SqliteException", ProviderName.SQLiteMS)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("MySql.Data.MySqlClient.MySqlException", TestProvName.AllMySqlData)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_Unknown([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualUnknown(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualUnknown(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: ACCESS, CH, DB2, FB3+, MYSQL, PGSQL, SQLITE
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[Test(Description = "<PREDICATE> = (1=1)")]
		public void Test_Feature_CalculatedTrue([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedTrue(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedTrue(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: ACCESS, CH, DB2, FB3+, MYSQL, PGSQL, SQLITE
		[Test(Description = "<PREDICATE> = (1=0)")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		public void Test_Feature_CalculatedFalse([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.One, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedFalse(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.One, r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.Zero, r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedFalse(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: ACCESS, CH, DB2, FB3+, MYSQL, PGSQL, SQLITE
		[Test(Description = "<PREDICATE> = (1=null)")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		public void Test_Feature_CalculatedUnknown([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == EqualCalculatedUnknown(Equal(r.Zero, r.Null))).Count(), Is.Zero);

				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.One, r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.Zero, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.Null, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.One, r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.One, r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => true == NotEqualCalculatedUnknown(Equal(r.Zero, r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: DB2, Firebird, PostgreSQL, SQLite, SQLServer2022
		// ClickHouse: tracked by https://github.com/ClickHouse/ClickHouse/issues/58145
		[Test(Description = "<A> IS [NOT] DISTICT FROM <B>")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServer2019MinusSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServer2019MinusMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("MySqlConnector.MySqlException", TestProvName.AllMySqlConnector)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("MySql.Data.MySqlClient.MySqlException", TestProvName.AllMySqlData)]
		public void Test_Feature_DistinctFrom([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsDistinctFrom(r.One, r.One)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsDistinctFrom(r.Zero, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsDistinctFrom(r.Null, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsDistinctFrom(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctFrom(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctFrom(r.Zero, r.Null)).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => IsNotDistinctFrom(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.One, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.One, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotDistinctFrom(r.Zero, r.Null)).Count(), Is.Zero);
			}
		}

		// Supported: MySQL
		// ClickHouse: tracked by https://github.com/ClickHouse/ClickHouse/issues/58145
		[Test(Description = "<A> <=> <B>")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("Npgsql.PostgresException", TestProvName.AllPostgreSQL)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2, TestProvName.AllDB2)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("System.Data.SQLite.SQLiteException", TestProvName.AllSQLiteClassic)]
		[ThrowsForProvider("Microsoft.Data.Sqlite.SqliteException", ProviderName.SQLiteMS)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", TestProvName.AllFirebird)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_NullSaveEqual([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => NullSaveEqual(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NullSaveEqual(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NullSaveEqual(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NullSaveEqual(r.One, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => NullSaveEqual(r.One, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => NullSaveEqual(r.Zero, r.Null)).Count(), Is.Zero);

				Assert.That(tb.Where(r => NotNullSaveEqual(r.One, r.One)).Count(), Is.Zero);
				Assert.That(tb.Where(r => NotNullSaveEqual(r.Zero, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => NotNullSaveEqual(r.Null, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => NotNullSaveEqual(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotNullSaveEqual(r.Zero, r.Null)).Count(), Is.EqualTo(1));
			}
		}

		// Supported: SQLite
		[Test(Description = "<A> IS <B>")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", TestProvName.AllFirebird)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql, TestProvName.AllMySqlConnector)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("MySql.Data.MySqlClient.MySqlException", TestProvName.AllMySqlData)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("Npgsql.PostgresException", TestProvName.AllPostgreSQL)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2, TestProvName.AllDB2)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_Is([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsPredicate(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsPredicate(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsPredicate(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsPredicate(r.One, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsPredicate(r.One, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsPredicate(r.Zero, r.Null)).Count(), Is.Zero);

				Assert.That(tb.Where(r => IsNotPredicate(r.One, r.One)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotPredicate(r.Zero, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotPredicate(r.Null, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotPredicate(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotPredicate(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotPredicate(r.Zero, r.Null)).Count(), Is.EqualTo(1));
			}
		}

		// Supported: DB2, Oracle
		// Firebird: doesn't work for NULLs
		[Test(Description = "DECODE function")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc, TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql, TestProvName.AllMySqlConnector)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		[ThrowsForProvider("MySql.Data.MySqlClient.MySqlException", TestProvName.AllMySqlData)]
		[ThrowsForProvider("Npgsql.PostgresException", TestProvName.AllPostgreSQL)]
		[ThrowsForProvider("System.Data.SQLite.SQLiteException", TestProvName.AllSQLiteClassic)]
		[ThrowsForProvider("Microsoft.Data.Sqlite.SqliteException", ProviderName.SQLiteMS)]
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", ProviderName.Ydb)]
		public void Test_Feature_Decode([DataSources(false, TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => IsDistinctByDecode(r.One, r.One)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.Zero, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.Null, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsDistinctByDecode(r.One, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsDistinctByDecode(r.One, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsDistinctByDecode(r.Zero, r.Null)).Count(), Is.Zero);

				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.One, r.One)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.Zero, r.Zero)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.Null, r.Null)).Count(), Is.Zero);
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.One, r.Zero)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.One, r.Null)).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => IsNotDistinctByDecode(r.Zero, r.Null)).Count(), Is.EqualTo(1));
			}
		}

		// While test itself works for almost all databases, approach makes sense only for
		// databases with INTERSECT support:
		// DB2, Informix, MySql8, MariaDB, Oracle, PostgreSQL, SAP HANA, SQLite, SQL Server
		// Not for:
		// Firebird, MySql 5.7, SQL CE,
		// ASE (added in 16SP3)
		[Test(Description = "EXISTS INTERSECT")]
		[ThrowsForProvider("System.Data.OleDb.OleDbException", TestProvName.AllAccessOleDb)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", TestProvName.AllAccessOdbc)]
		[ThrowsForProvider("ClickHouse.Driver.ClickHouseServerException", ProviderName.ClickHouseDriver)]
		[ThrowsForProvider("MySqlConnector.MySqlException", ProviderName.ClickHouseMySql)]
		[ThrowsForProvider("Octonica.ClickHouseClient.Exceptions.ClickHouseServerException", ProviderName.ClickHouseOctonica)]
		public void Test_Feature_Intersect([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.One)))).Count(), Is.EqualTo(1));
				AssertIntersect();
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.EqualTo(1));
				AssertIntersect();
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.Null).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(1));
				AssertIntersect();
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.Zero);
				AssertIntersect();
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.Zero);
				AssertIntersect();
				Assert.That(tb.Where(r => Exists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.Zero);
				AssertIntersect();

				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.One)))).Count(), Is.Zero);
				AssertIntersect();
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.Zero);
				AssertIntersect();
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.Null).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.Zero);
				AssertIntersect();
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Zero)))).Count(), Is.EqualTo(1));
				AssertIntersect();
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.One).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(1));
				AssertIntersect();
				Assert.That(tb.Where(r => NotExists(db.SelectQuery(() => r.Zero).Intersect(db.SelectQuery(() => r.Null)))).Count(), Is.EqualTo(1));
				AssertIntersect();
			}

			void AssertIntersect()
			{
				if (context.IsAnyOf(TestProvName.AllDB2, TestProvName.AllInformix, TestProvName.AllMySql8Plus, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllSapHana, TestProvName.AllSQLite, TestProvName.AllSqlServer))
					Assert.That(db.LastQuery!.ToUpperInvariant(), Does.Contain("INTERSECT"));
				else
					Assert.That(db.LastQuery!.ToUpperInvariant(), Does.Not.Contain("INTERSECT"));
			}
		}

		// Supported: Access, CH, DB2, FB3+, MYSQL, SQLITE, PGSQL
		[Test(Description = "Equality: predicate vs predicate")]
		[ThrowsForProvider("FirebirdSql.Data.FirebirdClient.FbException", ProviderName.Firebird25)]
		[ThrowsForProvider("System.Data.SqlClient.SqlException", TestProvName.AllSqlServerSystem)]
		[ThrowsForProvider("Microsoft.Data.SqlClient.SqlException", TestProvName.AllSqlServerMS)]
		[ThrowsForProvider("AdoNetCore.AseClient.AseException", ProviderName.SybaseManaged)]
		[ThrowsForProvider("IBM.Data.Db2.DB2Exception", ProviderName.InformixDB2)]
		[ThrowsForProvider("Sap.Data.Hana.HanaException", ProviderName.SapHanaNative)]
		[ThrowsForProvider("System.Data.Odbc.OdbcException", ProviderName.SapHanaOdbc)]
		[ThrowsForProvider("Oracle.ManagedDataAccess.Client.OracleException", TestProvName.AllOracleManaged)]
		[ThrowsForProvider("System.Data.SqlServerCe.SqlCeException", ProviderName.SqlCe)]
		public void Test_Feature_PredicateComparison([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Where(r => Equal(IsNull(r.One), IsNull(r.One))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => Equal(IsNull(r.One), IsNull(r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => Equal(IsNull(r.One), IsNull(r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => Equal(IsNull(r.Zero), IsNull(r.Null))).Count(), Is.Zero);
				Assert.That(tb.Where(r => Equal(IsNull(r.Zero), IsNull(r.Zero))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => Equal(IsNull(r.Null), IsNull(r.Null))).Count(), Is.EqualTo(1));

				Assert.That(tb.Where(r => NotEqual(IsNull(r.One), IsNull(r.One))).Count(), Is.Zero);
				Assert.That(tb.Where(r => NotEqual(IsNull(r.One), IsNull(r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => NotEqual(IsNull(r.One), IsNull(r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotEqual(IsNull(r.Zero), IsNull(r.Null))).Count(), Is.EqualTo(1));
				Assert.That(tb.Where(r => NotEqual(IsNull(r.Zero), IsNull(r.Zero))).Count(), Is.Zero);
				Assert.That(tb.Where(r => NotEqual(IsNull(r.Null), IsNull(r.Null))).Count(), Is.Zero);
			}
		}

		// Supported: Access, ClickHouse, DB2, FB3+, IFX, MYSQL, PGSQL, SQLITE
		[Test(Description = "Boolean value as predicate")]
		public void Test_Feature_BooleanAsPredicate_True([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.That(tb.Where(r => AsIs(r.True)).Count(), Is.EqualTo(1));

			if (context.IsAnyOf(ProviderName.Firebird25, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSybase))
				Assert.That(db.LastQuery, Does.Contain(" = "));
			else
				Assert.That(db.LastQuery, Does.Not.Contain(" = "));
		}

		// Supported: Access, ClickHouse, DB2, FB3+, IFX, MYSQL, PGSQL, SQLITE
		[Test(Description = "Boolean value as predicate")]
		public void Test_Feature_BooleanAsPredicate_False([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.That(tb.Where(r => AsIs(r.False)).Count(), Is.Zero);

			if (context.IsAnyOf(ProviderName.Firebird25, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSybase))
				Assert.That(db.LastQuery, Does.Contain(" = "));
			else
				Assert.That(db.LastQuery, Does.Not.Contain(" = "));
		}

		// Supported: Access, ClickHouse, DB2, FB3+, IFX, MYSQL, PGSQL, SQLITE
		[Test(Description = "Boolean value as predicate")]
		public void Test_Feature_BooleanAsPredicate_Null([DataSources(false, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable(FeatureTable.Data);

			Assert.That(tb.Where(r => AsIs(r.BoolNull)).Count(), Is.Zero);

			if (context.IsAnyOf(ProviderName.Firebird25, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSybase))
				Assert.That(db.LastQuery, Does.Contain(" = "));
			else
				Assert.That(db.LastQuery, Does.Not.Contain(" = "));
		}

		#endregion

		#region Translation Tests
		sealed class BooleanTable
		{
			[PrimaryKey] public int Id { get; set; }

			public int Value1 { get; set; }
			public int Value2 { get; set; }
			public int? Value4 { get; set; }
			public int? Value5 { get; set; }

			static IReadOnlyCollection<BooleanTable> GetData()
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

				return testData;
			}

			public static readonly IReadOnlyCollection<BooleanTable> Data = GetData();
		}

		[Test]
		public void Test_PredicateWithBoolean([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			db.InlineParameters = inline;

			var   True   = true;
			var   False  = false;
			bool? TrueN  = true;
			bool? FalseN = false;
			bool? Null   = null;

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

			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) == True));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) == False));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) == Null));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) == Null));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) == Null));

			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) != True));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) != False));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value2) != Null));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 < r.Value4) != Null));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value5 < r.Value4) != Null));

			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) == True));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) == False));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) == Null));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) == Null));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) == True));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) == TrueN));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) == False));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) == FalseN));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) == Null));

			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) != True));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) != False));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value2) != Null));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value1 <= r.Value4) != Null));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) != True));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) != TrueN));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) != False));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) != FalseN));
			AssertQuery(tb.Where(r => (r.Value5 <= r.Value4) != Null));
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
		public void Test_PredicateVsPredicate_ThreePlusNestingLevels([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			AssertQuery(tb.Where(r => ((r.Value1 != r.Value2) == (r.Value1 == r.Value4)) == ((r.Value4 == r.Value5) == (r.Value2 != r.Value4))));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != ((r.Value4 == r.Value1) == ((r.Value4 != r.Value5) != (r.Value4 != r.Value5)))));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != ((r.Value4 == r.Value1) == ((r.Value4 != r.Value5) == (r.Value4 != r.Value5)))));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != ((r.Value4 == r.Value1) != ((r.Value4 != r.Value5) != (r.Value4 != r.Value5)))));
			AssertQuery(tb.Where(r => (r.Value1 == r.Value2) != ((r.Value4 == r.Value1) != ((r.Value4 != r.Value5) == (r.Value4 != r.Value5)))));
			AssertQuery(tb.Where(r => (((r.Value1 == r.Value2) == (r.Value2 != r.Value4)) != (r.Value1 == r.Value4)) != ((r.Value4 != r.Value5) == (r.Value4 != r.Value5))));
			AssertQuery(tb.Where(r => ((r.Value1 != r.Value2) != (r.Value2 == r.Value5)) != ((r.Value4 != r.Value1) == (r.Value4 == r.Value5))));
		}

		[Test]
		[YdbCteAsSource]
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
		[YdbMemberNotFound]
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

		[Test]
		public void Test_ComplexPredicateComparisonWithUnknown([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);
			
			var cnt = tb.Where(r => r.Value1 == 1).Count();

			// nullable == nullable(complex)
			AssertQuery(tb.Where(r => (Equal(r.Value1, r.Value4) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));
			AssertQuery(tb.Where(r => (NotEqual(r.Value1, r.Value4) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));

			// non-nullable == nullable(complex)
			AssertQuery(tb.Where(r => (Equal(r.Value1, r.Value2) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));
			AssertQuery(tb.Where(r => (NotEqual(r.Value1, r.Value2) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));

			// NESTED: nullable == nullable(complex)
			AssertQuery(tb.Where(r => TruePredicate() == ((Equal(r.Value1, r.Value4) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));
			AssertQuery(tb.Where(r => TruePredicate() == ((NotEqual(r.Value1, r.Value4) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));

			// NESTED: non-nullable == nullable(complex)
			AssertQuery(tb.Where(r => TruePredicate() == ((Equal(r.Value1, r.Value2) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));
			AssertQuery(tb.Where(r => TruePredicate() == ((NotEqual(r.Value1, r.Value2) == Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));

			// nullable != nullable(complex)
			AssertQuery(tb.Where(r => (Equal(r.Value1, r.Value4) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));
			AssertQuery(tb.Where(r => (NotEqual(r.Value1, r.Value4) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));

			// non-nullable != nullable(complex)
			AssertQuery(tb.Where(r => (Equal(r.Value1, r.Value2) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));
			AssertQuery(tb.Where(r => (NotEqual(r.Value1, r.Value2) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt)))));

			// NESTED: nullable != nullable(complex)
			AssertQuery(tb.Where(r => TruePredicate() == ((Equal(r.Value1, r.Value4) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));
			AssertQuery(tb.Where(r => TruePredicate() == ((NotEqual(r.Value1, r.Value4) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));

			// NESTED: non-nullable != nullable(complex)
			AssertQuery(tb.Where(r => TruePredicate() == ((Equal(r.Value1, r.Value2) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));
			AssertQuery(tb.Where(r => TruePredicate() == ((NotEqual(r.Value1, r.Value2) != Equal(tb.Where(r => r.Value1 == 1).Count(), Add(r.Value5, cnt))))));

			// nullable COMP nullable(complex)
			AssertQuery(tb.Where(r => (Add(r.Value4, cnt) >= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));
			AssertQuery(tb.Where(r => (Add(r.Value4, cnt) > Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));
			AssertQuery(tb.Where(r => (Add(r.Value4, cnt) <= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));
			AssertQuery(tb.Where(r => (Add(r.Value4, cnt) < Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));

			// non-nullable COMP nullable(complex)
			AssertQuery(tb.Where(r => (Add(r.Value2, cnt) >= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));
			AssertQuery(tb.Where(r => (Add(r.Value2, cnt) > Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));
			AssertQuery(tb.Where(r => (Add(r.Value2, cnt) <= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));
			AssertQuery(tb.Where(r => (Add(r.Value2, cnt) < Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5))));

			// NESTED: nullable COMP nullable(complex)
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value4, cnt) >= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value4, cnt) > Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value4, cnt) <= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value4, cnt) < Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));

			// NESTED: non-nullable COMP nullable(complex)
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value2, cnt) >= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value2, cnt) > Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value2, cnt) <= Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
			AssertQuery(tb.Where(r => TruePredicate() == ((Add(r.Value2, cnt) < Add(tb.Where(r => r.Value1 == 1).Count(), r.Value5)))));
		}

		// tests optimization of IIF(cond, A, B) op X where X and A or/and B are evaluable
		[Test]
		public void Test_ConditionOptimization([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

#pragma warning disable CS0464 // Comparing with null of struct type always produces 'false'
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) == 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) != 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) > 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) >= 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) < 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) <= 0));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) == 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) != 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) > 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) >= 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) < 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 1) <= 1));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) == 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) != 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) > 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) >= 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) < 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) <= 0));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) == null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) != null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) > null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) >= null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) < null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : null) <= null));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 0) == null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 0) != null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 0) > null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 0) >= null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 0) < null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? r.Value5 : 0) <= null));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) == 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) != 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) > 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) >= 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) < 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) <= 0));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) == 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) != 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) > 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) >= 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) < 1));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 1 : r.Value5) <= 1));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) == 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) != 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) > 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) >= 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) < 0));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) <= 0));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) == null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) != null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) > null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) >= null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) < null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? null : r.Value5) <= null));

			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 0 : r.Value5) == null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 0 : r.Value5) != null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 0 : r.Value5) > null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 0 : r.Value5) >= null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 0 : r.Value5) < null));
			AssertQuery(tb.Where(r => (r.Value1 > r.Value4 ? 0 : r.Value5) <= null));
#pragma warning restore CS0464 // Comparing with null of struct type always produces 'false'
		}

		[Test]
		public void Test_PredicateAsNonConditionBooleanValue_Test1([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			AssertQuery(tb.Select(r => new
			{
				Id = r.Id,
				Value11 = r.Value1 == r.Value2,
				Value12 = r.Value1 != r.Value2,
				Value13 = r.Value1 > r.Value2,
				Value14 = r.Value1 < r.Value2,
				Value15 = r.Value1 >= r.Value2,
				Value16 = r.Value1 <= r.Value2,

				Value21 = r.Value4 == r.Value5,
				Value22 = r.Value4 != r.Value5,
				Value23 = r.Value4 > r.Value5,
				Value24 = r.Value4 < r.Value5,
				Value25 = r.Value4 >= r.Value5,
				Value26 = r.Value4 <= r.Value5,

				Value31 = r.Value1 == r.Value4,
				Value32 = r.Value1 != r.Value4,
				Value33 = r.Value1 > r.Value4,
				Value34 = r.Value1 < r.Value4,
				Value35 = r.Value1 >= r.Value4,
				Value36 = r.Value1 <= r.Value4,

				Value41 = r.Value5 == r.Value2,
				Value42 = r.Value5 != r.Value2,
				Value43 = r.Value5 > r.Value2,
				Value44 = r.Value5 < r.Value2,
				Value45 = r.Value5 >= r.Value2,
				Value46 = r.Value5 <= r.Value2,
			}).Where(r => r.Id != -1));
		}

		[Test]
		public void Test_PredicateAsNonConditionBooleanValue_Test2([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			AssertQuery(from r in tb group r by r.Value1 == r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 != r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 > r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 < r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 >= r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 <= r.Value2 into g select new { g.Key, Count = g.Count() });

			AssertQuery(from r in tb group r by r.Value4 == r.Value5 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value4 != r.Value5 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value4 > r.Value5 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value4 < r.Value5 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value4 >= r.Value5 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value4 <= r.Value5 into g select new { g.Key, Count = g.Count() });

			AssertQuery(from r in tb group r by r.Value1 == r.Value4 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 != r.Value4 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 > r.Value4 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 < r.Value4 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 >= r.Value4 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value1 <= r.Value4 into g select new { g.Key, Count = g.Count() });

			AssertQuery(from r in tb group r by r.Value5 == r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value5 != r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value5 > r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value5 < r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value5 >= r.Value2 into g select new { g.Key, Count = g.Count() });
			AssertQuery(from r in tb group r by r.Value5 <= r.Value2 into g select new { g.Key, Count = g.Count() });
		}

		[Test]
		public void Test_PredicateOptimization([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			// A OR (A AND B) => A
			AssertQuery(tb.Where(r => r.Value1 == r.Value2 || (r.Value1 == r.Value2 && r.Value1 == r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 || (r.Value1 == r.Value5 && r.Value1 == r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 || (r.Value1 == r.Value5 && r.Value1 == r.Value4)));

			AssertQuery(tb.Where(r => r.Value1 != r.Value2 || (r.Value1 != r.Value2 && r.Value1 != r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 != r.Value5 || (r.Value1 != r.Value5 && r.Value1 != r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 != r.Value5 || (r.Value1 != r.Value5 && r.Value1 != r.Value4)));

			AssertQuery(tb.Where(r => r.Value1 > r.Value2 || (r.Value1 > r.Value2 && r.Value1 > r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 > r.Value5 || (r.Value1 > r.Value5 && r.Value1 > r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 > r.Value5 || (r.Value1 > r.Value5 && r.Value1 > r.Value4)));

			// A AND (A OR B) => A
			AssertQuery(tb.Where(r => r.Value1 == r.Value2 && (r.Value1 == r.Value2 || r.Value1 == r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 && (r.Value1 == r.Value5 || r.Value1 == r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 && (r.Value1 == r.Value5 || r.Value1 == r.Value4)));

			AssertQuery(tb.Where(r => r.Value1 != r.Value2 && (r.Value1 != r.Value2 || r.Value1 != r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 != r.Value5 && (r.Value1 != r.Value5 || r.Value1 != r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 != r.Value5 && (r.Value1 != r.Value5 || r.Value1 != r.Value4)));

			AssertQuery(tb.Where(r => r.Value1 >= r.Value2 && (r.Value1 >= r.Value2 || r.Value1 >= r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 >= r.Value5 && (r.Value1 >= r.Value5 || r.Value1 >= r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 >= r.Value5 && (r.Value1 >= r.Value5 || r.Value1 >= r.Value4)));

			// A OR (!A AND B) => A OR B
			AssertQuery(tb.Where(r => r.Value1 == r.Value2 || (r.Value1 != r.Value2 && r.Value1 == r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 || (r.Value1 != r.Value5 && r.Value1 == r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 || (r.Value1 != r.Value5 && r.Value1 == r.Value4)));

			AssertQuery(tb.Where(r => r.Value1 > r.Value2 || (r.Value1 <= r.Value2 && r.Value1 > r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 > r.Value5 || (r.Value1 <= r.Value5 && r.Value1 > r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 > r.Value5 || (r.Value1 <= r.Value5 && r.Value1 > r.Value4)));

			// A AND (!A OR B) => A AND B
			AssertQuery(tb.Where(r => r.Value1 == r.Value2 && (r.Value1 != r.Value2 || r.Value1 == r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 && (r.Value1 != r.Value5 || r.Value1 == r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 == r.Value5 && (r.Value1 != r.Value5 || r.Value1 == r.Value4)));

			AssertQuery(tb.Where(r => r.Value1 < r.Value2 && (r.Value1 >= r.Value2 || r.Value1 < r.Value4)));
			AssertQuery(tb.Where(r => r.Value1 < r.Value5 && (r.Value1 >= r.Value5 || r.Value1 < r.Value2)));
			AssertQuery(tb.Where(r => r.Value1 < r.Value5 && (r.Value1 >= r.Value5 || r.Value1 < r.Value4)));

			// A OR !A
			AssertQuery(tb.Where(r => r.Value1 == r.Value2 || r.Value1 != r.Value2));
			AssertQuery(tb.Where(r => r.Value1 > r.Value2 || r.Value1 <= r.Value2));
			AssertQuery(tb.Where(r => r.Value1 >= r.Value2 || r.Value1 < r.Value2));

			AssertQuery(tb.Where(r => r.Value1 == r.Value4 || r.Value1 != r.Value4));
			AssertQuery(tb.Where(r => r.Value1 < r.Value4 || r.Value1 >= r.Value4));
			AssertQuery(tb.Where(r => r.Value1 > r.Value4 || r.Value1 <= r.Value4));

			AssertQuery(tb.Where(r => r.Value5 == r.Value4 || r.Value5 != r.Value4));
			AssertQuery(tb.Where(r => r.Value5 > r.Value4 || r.Value5 <= r.Value4));
			AssertQuery(tb.Where(r => r.Value5 >= r.Value4 || r.Value5 < r.Value4));

			// A AND !A
			AssertQuery(tb.Where(r => r.Value1 == r.Value2 && r.Value1 != r.Value2));
			AssertQuery(tb.Where(r => r.Value1 > r.Value2 && r.Value1 <= r.Value2));
			AssertQuery(tb.Where(r => r.Value1 >= r.Value2 && r.Value1 < r.Value2));

			AssertQuery(tb.Where(r => r.Value1 == r.Value4 && r.Value1 != r.Value4));
			AssertQuery(tb.Where(r => r.Value1 < r.Value4 && r.Value1 >= r.Value4));
			AssertQuery(tb.Where(r => r.Value1 > r.Value4 && r.Value1 <= r.Value4));

			AssertQuery(tb.Where(r => r.Value5 == r.Value4 && r.Value5 != r.Value4));
			AssertQuery(tb.Where(r => r.Value5 > r.Value4 && r.Value5 <= r.Value4));
			AssertQuery(tb.Where(r => r.Value5 >= r.Value4 && r.Value5 < r.Value4));
		}
		#endregion
	}
}
