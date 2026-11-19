using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.DataProvider.Sybase;
using LinqToDB.Interceptors;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The read-only and single-statement checks generated from <c>ReadOnlySql.gram</c>, called per behaviour class so
	/// that classes no provider is routed to yet are covered as well.
	/// </summary>
	[TestFixture]
	public sealed class ReadOnlySqlGuardTests
	{
		const string TSql     = "TSql";
		const string Ase      = "Ase";
		const string Bracket  = "Bracket";
		const string MySql    = "MySql";
		const string Ydb      = "Ydb";
		const string Informix = "Informix";
		const string Postgres = "Postgres";
		const string Standard = "Standard";

		static readonly string[] _classes = [TSql, Ase, Bracket, MySql, Ydb, Informix, Postgres, Standard];

		static bool Check(string dialect, string sql)
		{
			return dialect switch
			{
				TSql     => TempScriptAscii(ReadOnlySqlGrammar.TryCheckTSql(sql, out var writesTSql), writesTSql, sql, true),
				Ase      => TempScriptAscii(ReadOnlySqlGrammar.TryCheckAse (sql, out var writesAse),  writesAse,  sql, false),
				Bracket  => ReadOnlySqlGrammar.TryCheckBracket (sql, out _),
				MySql    => ReadOnlySqlGrammar.TryCheckMySql   (sql, out _),
				Ydb      => ReadOnlySqlGrammar.TryCheckYdb     (sql, out _),
				Informix => ReadOnlySqlGrammar.TryCheckInformix(sql, out _),
				Postgres => ReadOnlySqlGrammar.TryCheckPostgres(sql, out _),
				Standard => ReadOnlySqlGrammar.TryCheckStandard(sql, out _),
				_        => throw new ArgumentOutOfRangeException(nameof(dialect)),
			};
		}

		// The guard reads a script that writes temporary tables as ASCII only outside string literals and comments.
		static bool TempScriptAscii(bool accepted, bool writesTemporaryTables, string sql, bool crEndsLineComment)
		{
			return accepted && (!writesTemporaryTables || ReadOnlySqlGuard.FindNonAsciiCode(sql, crEndsLineComment) < 0);
		}

		static bool Single(string dialect, string sql)
		{
			return dialect switch
			{
				TSql     => ReadOnlySqlGrammar.TrySingleTSql    (sql, out _),
				Ase      => ReadOnlySqlGrammar.TrySingleAse     (sql, out _),
				Bracket  => ReadOnlySqlGrammar.TrySingleBracket (sql, out _),
				MySql    => ReadOnlySqlGrammar.TrySingleMySql   (sql, out _),
				Ydb      => ReadOnlySqlGrammar.TrySingleYdb     (sql, out _),
				Informix => ReadOnlySqlGrammar.TrySingleInformix(sql, out _),
				Postgres => ReadOnlySqlGrammar.TrySinglePostgres(sql, out _),
				Standard => ReadOnlySqlGrammar.TrySingleStandard(sql, out _),
				_        => throw new ArgumentOutOfRangeException(nameof(dialect)),
			};
		}

		// What the query command decides: the read-only check, which admits a single statement only.
		static bool Query(string dialect, string sql)
		{
			return Check(dialect, sql);
		}

		static void AssertQuery(string sql, bool expected, params string[] dialects)
		{
			using (Assert.EnterMultipleScope())
			{
				foreach (var dialect in dialects.Length == 0 ? _classes : dialects)
					Query(dialect, sql).ShouldBe(expected, $"{dialect}: {sql}");
			}
		}

		#region Seams

		[TestCase("select a delete")]
		[TestCase("select a from t delete t")]
		[TestCase("select a /**/delete")]
		[TestCase("select a into x")]
		[TestCase("select 'it''s' into x")]
		public void SeamRefused(string sql)
		{
			AssertQuery(sql, false);
		}

		[TestCase("select x = 1")]
		[TestCase("select (select 1)")]
		[TestCase("select a  ,b")]
		[TestCase("select deleted, UpdatedAt, intox from t")]
		[TestCase("select a from t with (nolock)")]
		[TestCase("select top 5 with ties a from t order by a")]
		[TestCase("select replace(a,'x','y')")]
		public void SeamAccepted(string sql)
		{
			AssertQuery(sql, true);
		}

		#endregion

		#region Conditions: the same text read differently by class

		[Test]
		public void BracketsAreQuotedIdentifiersOnlyWhereTheyAre()
		{
			AssertQuery("select [delete] from t",   true,  TSql, Ase, Bracket);
			AssertQuery("select [delete] from t",   false, MySql, Ydb, Informix, Postgres, Standard);
			AssertQuery("select arr[nextval('s')]", false, MySql, Ydb, Informix, Postgres, Standard);
			AssertQuery("select arr[1] from t",     true,  MySql, Ydb, Informix, Postgres, Standard);
		}

		[Test]
		public void BackslashEscapesOnlyWhereTheyAre()
		{
			AssertQuery("select 'C:\\', 1",  true,  TSql, Ase, Bracket, Informix, Postgres, Standard);
			AssertQuery("select 'C:\\', 1",  false, Ydb);
			AssertQuery("select 'it\\'s', 1", true,  Ydb);
			AssertQuery("select 'it\\'s', 1", false, TSql, Ase, Bracket, Informix, Postgres, Standard);

			// MySQL's sql_mode decides what a backslash does there.
			AssertQuery("select 'C:\\', 1",  false, MySql);
			AssertQuery("select 'it\\'s', 1", false, MySql);
			AssertQuery("select E'it\\'s', U&'d\\0061ta', \"a\\b\", 1", true, Postgres);
		}

		[Test]
		public void HashCommentsOnlyInMySql()
		{
			AssertQuery("select 1 # delete",                          true,  MySql);
			AssertQuery("select 1 # delete",                          false, TSql, Ase, Bracket, Ydb, Informix, Postgres, Standard);
			AssertQuery("select data #> '{a}', nextval('s') from t", false, Postgres, Standard);
			AssertQuery("select data #> '{a}' from t",               true,  Postgres, Standard);
			AssertQuery("select * from #t",                           true,  TSql, Ase);
		}

		[Test]
		public void BraceCommentsOnlyInInformix()
		{
			AssertQuery("select {'k': nextval('s')}", false, Postgres, Standard, TSql, MySql);
			AssertQuery("select {'k': nextval('s')}", true,  Informix);
			AssertQuery("select {'k': 1}",            true,  Postgres, Standard);
		}

		[Test]
		public void RawStringsOnlyInYdb()
		{
			AssertQuery("select @@ delete @@", true,  Ydb);
			AssertQuery("select @@ delete @@", false, TSql, MySql, Postgres, Standard);
			AssertQuery("select @@version, 1", true,  TSql, Ase);
		}

		[Test]
		public void StatementWordsAnywhereOnlyWhereStatementsNeedNoSeparator()
		{
			AssertQuery("select call, open, close from t", true,  Bracket, MySql, Ydb, Informix, Postgres, Standard);
			AssertQuery("select call, open, close from t", false, TSql, Ase);
			AssertQuery("select 1 drop table t",           false, TSql, Ase);
		}

		[TestCase("select 1 /* a /* b */ from t")]
		[TestCase("select 1 /* unterminated")]
		public void UnterminatedCommentsRefusedEverywhere(string sql)
		{
			AssertQuery(sql, false);
		}

		// Each class reads the strings, quoted names and comments of its own servers only. A form of another dialect is
		// code there, as it is to the server; where the servers of one class read a text differently, it is refused.
		// The second argument lists the classes that accept the text; every other class refuses it.
		[TestCase("select 'a' /* x */ from t -- y",                   "TSql Ase Bracket MySql Ydb Informix Postgres Standard")]
		[TestCase("select 1 /* a /* b */ c */ from t",                "TSql Ase Postgres")]                         // nested on these servers; flat on MySQL, Oracle, Firebird, SQLite
		[TestCase("select 1 /*! , nextval(s) */ from t",              "TSql Ase Bracket Ydb Informix Postgres Standard")] // MySQL runs it by version
		[TestCase("select 1 /*M! , nextval(s) */ from t",             "TSql Ase Bracket Ydb Informix Postgres Standard")]
		[TestCase("select $$ delete $$",                              "Postgres")]
		[TestCase("select $$ x $$, $t$ it's $t$",                     "Postgres")]
		[TestCase("select $$ x $$",                                   "TSql Ase Bracket Ydb Informix Postgres")]   // code where $$ is no string; ClickHouse and PostgreSQL-like servers of the other two classes read a string
		[TestCase("select E'it\\'s delete'",                        "Ydb Postgres")]                        // one string under backslash escapes
		[TestCase("select E'x', N'x', X'0F', U&'d\\0061ta'",        "TSql Ase Bracket Ydb Informix Postgres Standard")]
		[TestCase("select E'C:\\' from t",                          "TSql Ase Bracket Informix")]                 // Postgres reads an open string; Standard: the readings differ
		[TestCase("select q'[ delete ]', Q'{ x }', nq'< y >' from t", "TSql Ase Bracket MySql Ydb Informix Postgres Standard")] // the same string under both readings
		[TestCase("select q'[' || x || ']' from t",                   "TSql Ase Bracket MySql Ydb Informix Postgres")] // Oracle and Firebird: one string
		[TestCase("select \"delete\", `update` from t",              "Bracket MySql Ydb")]               // a backtick is no symbol in the other classes
		[TestCase("select `a\\` from t",                            "Bracket")]                         // MySQL ends the name at the backtick, ClickHouse escapes it
		[TestCase("select 1 --1\n from t",                           "TSql Ase Bracket Ydb Informix Postgres Standard")] // MySQL: minus minus one; ClickHouse: a comment
		[TestCase("select 1 -- x\n from t",                          "TSql Ase Bracket MySql Ydb Informix Postgres Standard")]
		[TestCase("select 1 --",                                      "TSql Ase Bracket MySql Ydb Informix Postgres Standard")]
		[TestCase("select 1 # delete\n from t",                      "MySql")]
		[TestCase("select 'C:\\' from t",                           "TSql Ase Bracket Informix Postgres Standard")]
		[TestCase("select 'it\\'s', \"it\\\"s\" from t",       "Ydb")]
		[TestCase("select [delete] from t",                           "TSql Ase Bracket")]
		[TestCase("select {delete} from t",                           "Informix")]
		[TestCase("select @@x delete @@ from t",                      "Ydb")]
		[TestCase("select V$SESSION.SID, RDB$GET_CONTEXT('SYSTEM', 'DB_NAME') from t", "TSql Ase Bracket MySql Ydb Informix Postgres Standard")]
		[TestCase("select имя, n\u0303ame from таблица",             "TSql Ase Bracket MySql Ydb Informix Postgres Standard")]
		[TestCase("select\u00A0a\u2003from\u2028t",                "TSql Ase Bracket MySql Ydb Informix Postgres Standard")]
		public void QuoteAndCommentFormsPerClass(string sql, string accepting)
		{
			var accepted = accepting.Split(' ');

			using (Assert.EnterMultipleScope())
			{
				foreach (var dialect in _classes)
					Query(dialect, sql).ShouldBe(accepted.Contains(dialect), $"{dialect}: {sql}");
			}
		}

		#endregion

		#region Lists

		// One name per entry; a name ending in a wildcard is stood for by one it covers.
		static readonly string[] _routeFunctions =
		[
			"openquery", "openrowset", "opendatasource",
			"reserve_identity", "set_appcontext", "rm_appcontext", "lct_admin",
			"set_config", "lo_import", "lo_export", "lo_unlink", "lo_create", "lo_creat", "lo_from_bytea", "lo_put", "lowrite",
			"lo_truncate", "dblink", "dblink_exec", "dblink_send_query", "pg_notify", "pg_stat_statements_reset",
			"pg_wal_replay_pause", "pg_log_backend_memory_contexts", "brin_summarize_new_values", "brin_summarize_range",
			"brin_desummarize_range", "gin_clean_pending_list", "pg_terminate_backend", "pg_cancel_backend", "pg_reload_conf",
			"pg_rotate_logfile", "pg_stat_reset", "pg_stat_reset_shared", "pg_switch_wal", "pg_create_restore_point",
			"pg_backup_start", "pg_create_logical_replication_slot", "pg_drop_replication_slot", "pg_replication_origin_create",
			"pg_logical_emit_message", "pg_promote", "pg_import_system_collations", "pg_file_write", "pg_file_rename",
			"pg_file_unlink", "pg_background_launch",
			"asynchronous_connection_failover_add_source", "group_replication_set_as_primary", "keyring_key_store",
			"version_tokens_set", "version_tokens_edit", "version_tokens_delete", "audit_log_filter_set_filter", "sys_exec",
			"sys_eval",
			"RDB$SET_CONTEXT",
			"LOTOFILE", "LOCOPY",
			"load_extension",
			"url", "urlCluster", "remote", "remoteSecure", "cluster", "clusterAllReplicas", "mysql", "postgresql", "mongodb",
			"redis", "jdbc", "odbc", "executable",
			"Shell", "Kill", "FileCopy", "MkDir", "RmDir", "SaveSetting", "DeleteSetting", "SetAttr", "CreateObject",
			"GetObject", "SendKeys", "CallByName", "ChDir", "ChDrive",
		];

		static readonly string[] _routePackages =
		[
			"DBMS_PIPE", "DBMS_SCHEDULER", "DBMS_JOB", "DBMS_SQL", "DBMS_SYS_SQL", "DBMS_XMLSTORE", "DBMS_AQ", "DBMS_DDL",
			"DBMS_ALERT", "UTL_FILE", "UTL_SMTP", "UTL_MAIL", "UTL_TCP", "RDB$PROFILER", "cron",
		];

		[Test]
		public void WriteRouteFunctions()
		{
			foreach (var name in _routeFunctions)
			{
				AssertQuery($"select {name}(1)",          false);
				AssertQuery($"select s.{name}(1)",        false);
				AssertQuery($"select \"{name}\"(1)",      false);
				AssertQuery($"select {name} from t",      true, name is "Kill" ? [Bracket, MySql, Ydb, Informix, Postgres, Standard] : _classes);
				AssertQuery($"select [{name}](1) from t", false, TSql, Ase, Bracket);
			}
		}

		[Test]
		public void WriteRoutePackages()
		{
			foreach (var name in _routePackages)
			{
				AssertQuery($"select {name}.f('p')",     false);
				AssertQuery($"select \"{name}\".\"F\"(1)", false);
				AssertQuery($"select x from {name}.job", false);
				AssertQuery($"select {name} from t",     true);
			}
		}

		[Test]
		public void InformixAdministrationPair()
		{
			AssertQuery("select sysadmin:task('x')",           false);
			AssertQuery("select sysadmin:informix.admin('x')", false);
			AssertQuery("select task('x'), admin from t",      true);
		}

		[TestCase("select next value for s")]
		[TestCase("select NEXT  VALUE\nFOR dbo.s")]
		[TestCase("select nextval for s from t")]
		[TestCase("select s.nextval from dual")]
		[TestCase("select \"S\".NEXTVAL from dual")]
		[TestCase("select nextval('s')")]
		[TestCase("select setval('s', 1)")]
		[TestCase("select gen_id(g, 1) from rdb$database")]
		[TestCase("select gen_id(g, 00) from rdb$database")]
		[TestCase("select GEN_ID(\"G\", -1) from rdb$database")]
		public void SequenceCallsRefused(string sql)
		{
			AssertQuery(sql, false);
		}

		[TestCase("select gen_id(g, 0) from rdb$database")]
		[TestCase("select GEN_ID(\"G\" , 0 ) from rdb$database")]
		[TestCase("select nextval, next, value from t")]
		[TestCase("select currval('s')")]
		public void SequenceReadsAccepted(string sql)
		{
			AssertQuery(sql, true);
		}

		// Options whose value set is closed. The transaction isolation level is kept by SqlClient on a pooled connection,
		// and AseClient keeps every option, so those classes refuse what the driver would carry to the next query.
		// SqlClient resets ROWCOUNT and TEXTSIZE when it hands a pooled connection out again.
		static readonly string[] _sessionOptions =
		[
			"SET ROWCOUNT 5", "SET TEXTSIZE 100",
			"SET NOCOUNT ON", "SET ANSI_NULLS OFF", "SET ANSINULL ON", "SET ANSI_PADDING ON", "SET ANSI_WARNINGS ON",
			"SET ANSI_NULL_DFLT_ON ON", "SET ANSI_NULL_DFLT_OFF OFF", "SET ARITHABORT ON", "SET ARITHIGNORE OFF",
			"SET CONCAT_NULL_YIELDS_NULL ON", "SET NUMERIC_ROUNDABORT OFF", "SET QUOTED_IDENTIFIER ON", "SET DATEFORMAT dmy",
			"SET DATEFIRST 1", "SET LANGUAGE us_english", "SET LANGUAGE N'British'", "SET STATISTICS IO ON",
			"SET STATISTICS TIME OFF",
			"SET DateStyle = 'ISO, MDY'", "SET IntervalStyle TO postgres", "SET TimeZone = 'UTC'", "SET TIME ZONE 'UTC'",
			"SET TIME ZONE LOCAL", "SET extra_float_digits = 3", "SET application_name = 'cli'",
			"SET client_min_messages TO warning", "SET bytea_output = 'hex'", "SET statement_timeout = '5s'",
			"SET work_mem = '64MB'", "SET jit = off", "SET search_path TO a, \"$user\", public", "SET SESSION search_path = a",
			"SET LOCAL statement_timeout = 1000",
			"SET enable_seqscan = off", "SET enable_indexscan TO on", "SET enable_bitmapscan = false", "SET enable_hashjoin = off",
			"SET enable_mergejoin = off", "SET enable_nestloop = off", "SET enable_hashagg = off", "SET enable_sort = off",
			"SET enable_material = off", "SET enable_indexonlyscan = off", "SET enable_tidscan = off",
			"SET enable_partitionwise_join = on", "SET enable_partitionwise_aggregate = on", "SET enable_parallel_hash = off",
			"SET enable_parallel_append = off", "SET enable_memoize = off", "SET enable_incremental_sort = off",
			"SET enable_presorted_aggregate = off", "SET enable_gathermerge = off", "SET enable_async_append = off",
			"SET enable_partition_pruning = off",
			"SET time_zone = '+00:00'", "SET SESSION max_execution_time = 1000", "SET @@session.max_statement_time = 1",
			"SET group_concat_max_len = 100000", "SET div_precision_increment = 8",
			"SET optimizer_switch = 'index_merge=off,mrr=on'", "SET NAMES utf8mb4", "SET NAMES 'utf8mb4' COLLATE utf8mb4_bin",
		];

		static readonly string[] _isolationLevels =
		[
			"SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED", "SET TRANSACTION ISOLATION LEVEL READ COMMITTED",
			"SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ", "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE",
			"SET TRANSACTION ISOLATION LEVEL SNAPSHOT", "SET TRANSACTION ISOLATION LEVEL 1",
		];

		[Test]
		public void SessionOptionsAloneAndStacked()
		{
			string[] keeping   = [TSql, Bracket, MySql, Ydb, Informix, Postgres, Standard];
			string[] isolation = [Bracket, MySql, Ydb, Informix, Postgres, Standard];

			foreach (var option in _sessionOptions)
			{
				AssertQuery($"{option}\nselect 1",  true,  keeping);
				AssertQuery($"{option};\nselect 1", true,  keeping);
				AssertQuery($"{option}\nselect 1",  false, Ase);
			}

			foreach (var option in _isolationLevels)
			{
				AssertQuery($"{option};\nselect 1", true,  isolation);
				AssertQuery($"{option};\nselect 1", false, TSql, Ase);
			}

			AssertQuery(string.Join(";\n", _sessionOptions) + ";\nselect 1",                         true, keeping);
			AssertQuery(string.Join("\n",  _sessionOptions.Concat(_isolationLevels)) + "\nselect 1", true, isolation);
		}

		[TestCase("SET ROWCOUNT @n")]
		[TestCase("SET TEXTSIZE -1x")]
		[TestCase("SET sql_select_limit = 1")]
		[TestCase("SET IDENTITY_INSERT t ON")]
		[TestCase("SET NOCOUNT ON, XACT_ABORT ON")]
		[TestCase("SET NOCOUNT MAYBE")]
		[TestCase("SET enable_external_access = true")]
		[TestCase("SET enable_anything = off")]
		[TestCase("SET GLOBAL max_connections = 1")]
		[TestCase("SET PERSIST max_connections = 1")]
		[TestCase("SET PERSIST_ONLY max_connections = 1")]
		[TestCase("SET @@global.read_only = 0")]
		[TestCase("SET @@persist.max_connections = 1")]
		[TestCase("SET SESSION AUTHORIZATION u")]
		[TestCase("SET ROLE r")]
		[TestCase("SET TRANSACTION ISOLATION LEVEL READ COMMITTED, READ WRITE")]
		[TestCase("SET TRANSACTION ISOLATION LEVEL READ COMMITTED READ WRITE")]
		[TestCase("SET TRANSACTION READ WRITE")]
		[TestCase("SET search_path = a, GLOBAL max_connections = 5")]
		[TestCase("SET time_zone = '+00:00', GLOBAL max_connections = 5")]
		[TestCase("SET @p = 1, GLOBAL max_connections = 777")]
		[TestCase("SET @q = 2, @@global.max_connections = 778")]
		[TestCase("SET @r = 3, PERSIST max_connections = 779")]
		[TestCase("SET @p = 1\n, GLOBAL max_connections = 777")]
		[TestCase("SET @p = (select 1)")]
		[TestCase("SET @p = 1 + (select max(a) from t)")]
		[TestCase("SET @p = nextval('s')")]
		[TestCase("SET @@p = 1")]
		[TestCase("DECLARE @c CURSOR")]
		[TestCase("DECLARE @p Int DELETE t")]
		public void PrefixRefused(string prefix)
		{
			AssertQuery(prefix + "\nselect 1", false);
			AssertQuery(prefix + ";\nselect 1", false);
		}

		// A table variable is a temporary table of SQL Server and ASE scripts; elsewhere it is no parameter.
		[Test]
		public void TableVariableOnlyInTSql()
		{
			AssertQuery("DECLARE @t TABLE (a int)\nselect 1", true,  TSql, Ase);
			AssertQuery("DECLARE @t TABLE (a int)\nselect 1", false, Bracket, MySql, Ydb, Informix, Postgres, Standard);
		}

		[TestCase("select 1; SET NOCOUNT ON")]
		[TestCase("select 1; SET @p = 1")]
		[TestCase("select 1 SET NOCOUNT ON")]
		public void SetAfterQueryRefused(string sql)
		{
			AssertQuery(sql, false, sql.Contains(';', StringComparison.Ordinal) ? _classes : [TSql, Ase]);
		}

		[TestCase("SET search_path = a, b")]
		[TestCase("SET @p = ', '")]
		[TestCase("SET @p = 1\n-- comment\n")]
		public void PrefixAccepted(string prefix)
		{
			AssertQuery(prefix + "\nselect 1", true, TSql, Bracket, Postgres, Standard);
		}

		#endregion

		#region Preamble

		[TestCase("DECLARE @id Integer -- Int32\nSET @id = 1")]
		[TestCase("DECLARE $Has  -- Boolean\nSET $Has = True")]
		[TestCase("DECLARE @p  -- Int64\nSET @p = NULL")]
		[TestCase("DECLARE $Name Text(6) -- String\nSET $Name = 'Manuel'u")]
		[TestCase("DECLARE @PersonID Integer(4) -- Int32")]
		[TestCase("DECLARE @p DateTime2\nSET @p = DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7)")]
		[TestCase("DECLARE @p Udt -- Object\nSET @p = LINESTRING (100 100, 20 180, 180 180)")]
		[TestCase("DECLARE @hid Udt -- Object\nSET @hid = /1/3/")]
		[TestCase("DECLARE @p NVarChar(4000) -- String\nSET @p = N'%[[]0%'")]
		[TestCase("DECLARE @p NVarChar(4000) -- String\nSET @p = N'first line\nsecond line'")]
		[TestCase("DECLARE @p Decimal(18, 2)\nSET @p = -1.5E+10")]
		[TestCase("DECLARE @table [dbo].[TestTableType] -- Structured -- Object\nSET @table = ")]
		[TestCase("DECLARE $p AS Int32\nSET $p = 1")]
		[TestCase("SET @p = 1\nDECLARE @q Int\nSET @q = 2\nDECLARE @r Int")]
		[TestCase("DECLARE @p Int;\nSET @p = 1;")]
		public void PreambleAccepted(string preamble)
		{
			AssertQuery(preamble + "\nSELECT [t].[a] FROM [t] WHERE [t].[a] = @p", true, TSql, Ase, Bracket);
			AssertQuery(preamble + "\nSELECT t.a FROM t WHERE t.a = @p",           true);
			AssertQuery(preamble + "\n;WITH c AS (select 1 a) select a from c",    true);
		}

		[Test]
		public void PrefixBeforeOtherStatementsInExecuteMode()
		{
			foreach (var dialect in _classes)
			{
				using (Assert.EnterMultipleScope())
				{
					Single(dialect, "DECLARE @p Int -- Int32\nSET @p = 1\nUPDATE t SET a = @p").ShouldBeTrue(dialect);
					Single(dialect, "SET @p = 0\n[dbo].[p]").ShouldBeTrue(dialect);
					Single(dialect, "SET @p = 0\nexec p").ShouldBeTrue(dialect);
					Single(dialect, "update t set a = 1").ShouldBeTrue(dialect);
					Single(dialect, "update t set a = 1;").ShouldBeTrue(dialect);
					Single(dialect, ";update t set a = 1").ShouldBeTrue(dialect);
					Single(dialect, "update t set a = 1; delete t").ShouldBeFalse(dialect);
					Single(dialect, "DECLARE @p Int\nSET @p = 1\nupdate t set a = @p; delete t").ShouldBeFalse(dialect);
				}
			}
		}

		#endregion

		#region One statement

		[TestCase(";WITH c AS (select 1 a) select a from c")]
		[TestCase(";;select 1;;")]
		[TestCase("select 1 union all select 2 except select 3 intersect select 4")]
		[TestCase("(select 1) union (select 2)")]
		[TestCase("with c as (select 1 a), d (b) as (select 2) select * from c, d")]
		[TestCase("with xmlnamespaces ('urn:x' as x) select 1")]
		public void OneStatementAccepted(string sql)
		{
			AssertQuery(sql, true);
		}

		[TestCase("select 1; select 2")]
		[TestCase("select 1; delete t")]
		[TestCase("select 1 ;\n;select 2")]
		public void SeparatedStatementsRefused(string sql)
		{
			AssertQuery(sql, false);
		}

		[TestCase("select 1 select 2")]
		[TestCase("select a from t\nselect b from u")]
		[TestCase("(select 1) select 2")]
		[TestCase("with c as (select 1 a) select a from c with d as (select 2 b) select b from d")]
		[TestCase("select a from t with c as (select 1) select 1")]
		[TestCase("select 1 throw 50000, 'x', 1")]
		[TestCase("select 1 l:")]
		[TestCase("select * from t receive * from q")]
		[TestCase("select 1 get conversation group @g from q")]
		[TestCase("select 1 move conversation @h to @g")]
		[TestCase("select 1 send on conversation @h")]
		[TestCase("select 1 end conversation @h")]
		[TestCase("select 1 enable trigger tr on t")]
		[TestCase("select 1 disable trigger tr on t")]
		public void UnseparatedStatementsRefusedInTSql(string sql)
		{
			AssertQuery(sql, false, TSql, Ase);
		}

		[TestCase("select get, move, send, receiver from t")]
		[TestCase("select case when a = 1 then 'x' else 'y' end from t")]
		[TestCase("select iif(a = 1, 1, 0), openjson, openxml from t")]
		[TestCase("select a from t order by a offset 5 rows fetch next 5 rows only")]
		[TestCase("select a from t order by a offset 1 row fetch first 1 row only")]
		[TestCase("select a from t inner merge join u on 1 = 1 option (merge join, merge union, use hint('DISABLE_OPTIMIZER_ROWGOAL'))")]
		[TestCase("select a from t with (nolock) union all select b from u with (index (ix))")]
		[TestCase("select a, count(*) from t group by a with rollup")]
		[TestCase("select * from t for xml path('r'), root('x')")]
		[TestCase("select REPLACE (a, 'x', 'y'), LEFT(a, 1) from t")]
		[TestCase("select * from #t join ##g on 1 = 1")]
		[TestCase("select 1\nGO")]
		[TestCase("select 1;\ngo\n")]
		public void TSqlQueryAccepted(string sql)
		{
			AssertQuery(sql, true, TSql, Ase);
		}

		[TestCase("select 1\nGO\nselect 2")]
		[TestCase("select 1\nGO\ndelete t")]
		[TestCase("select 1\nGO 5\nselect 2")]
		public void GoFollowedByStatementRefused(string sql)
		{
			AssertQuery(sql, false, TSql, Ase);
		}

		#endregion

		#region Refused anywhere

		[TestCase("update t set a = 1")]
		[TestCase("select * from t for update")]
		[TestCase("select * from t for no key update")]
		[TestCase("with c as (select 1) update t set a = 1")]
		[TestCase("with c as (select 1) delete from t")]
		[TestCase("with c as (select 1) insert into t select * from c")]
		[TestCase("with c as (select 1) upsert into t select * from c")]
		[TestCase("with c as (select 1) merge into t using c on 1 = 1 when matched then delete")]
		[TestCase("select * from t where exists (delete from u returning *)")]
		[TestCase("select delete (1)")]
		[TestCase("select update (1)")]
		[TestCase("select merge (1)")]
		[TestCase("replace into t values (1)")]
		[TestCase("select 1 replace into t values (1)")]
		[TestCase("select * into t2 from t")]
		[TestCase("select * from t into outfile '/tmp/x'")]
		[TestCase("select * from t union select * into z from u")]
		[TestCase("with function f(x number) return number is begin return x; end; select f(1) from dual")]
		[TestCase("with procedure p is begin null; end; select 1 from dual")]
		[TestCase("select dblink_send_query('c', 'insert into t values (1)')")]
		[TestCase("select 1delete from t")]
		public void RefusedEverywhere(string sql)
		{
			AssertQuery(sql, false);
		}

		[TestCase("with function as (select 1 a) select a from function")]
		[TestCase("with function (a) as (select 1) select a from function")]
		[TestCase("select insert('abc', 1, 1, 'x'), truncate(1.5, 0), left('a', 1) from t")]
		public void FunctionShapesAccepted(string sql)
		{
			AssertQuery(sql, true, Bracket, MySql, Ydb, Informix, Postgres, Standard);
		}

		[TestCase("call p()")]
		[TestCase("exec p")]
		[TestCase("begin select 1 end")]
		[TestCase("commit")]
		[TestCase("declare x int")]
		[TestCase("create table t (a int)")]
		[TestCase("pragma writable_schema = 1")]
		[TestCase("copy t to '/tmp/x'")]
		[TestCase("values (1)")]
		[TestCase("")]
		[TestCase("  -- nothing\n")]
		public void FirstWordMustBeSelectWithOrBracket(string sql)
		{
			AssertQuery(sql, false);
		}

		#endregion

		#region Execute mode, T-SQL

		[TestCase("CREATE OR ALTER VIEW v AS SELECT 1 AS a\nGO")]
		[TestCase("IF NOT EXISTS (SELECT 1 FROM t) CREATE TABLE t (a int)")]
		[TestCase("WHILE @i < 10 BEGIN SET @i = @i + 1 END")]
		[TestCase("BEGIN TRY UPDATE t SET a = 1 END TRY BEGIN CATCH SELECT ERROR_MESSAGE() END CATCH")]
		[TestCase("INSERT INTO t (a) SELECT a FROM u UNION ALL SELECT b FROM v")]
		[TestCase("INSERT t EXEC p")]
		[TestCase("INSERT INTO t DEFAULT VALUES")]
		[TestCase("INSERT INTO t (a) OUTPUT inserted.a INTO @t VALUES (1)")]
		[TestCase("UPDATE t SET a = 1 OUTPUT deleted.a FROM t JOIN u ON t.id = u.id WHERE u.b = 2")]
		[TestCase("DELETE t FROM t JOIN u ON t.id = u.id WHERE u.b = 2")]
		[TestCase("MERGE INTO t USING u ON t.id = u.id WHEN MATCHED THEN UPDATE SET a = u.a WHEN NOT MATCHED THEN INSERT (a) VALUES (u.a) WHEN NOT MATCHED BY SOURCE THEN DELETE OUTPUT $action;")]
		[TestCase("WITH c AS (SELECT 1 a) UPDATE t SET a = c.a FROM c")]
		[TestCase("SELECT * INTO #t FROM t")]
		[TestCase("ALTER TABLE t ADD c int")]
		[TestCase("ALTER TABLE t DROP COLUMN c")]
		[TestCase("ALTER TABLE t ENABLE TRIGGER ALL")]
		[TestCase("ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON")]
		[TestCase("ALTER TABLE t ADD CONSTRAINT fk FOREIGN KEY (a) REFERENCES u (b) ON DELETE CASCADE ON UPDATE NO ACTION")]
		[TestCase("DROP TABLE IF EXISTS t")]
		[TestCase("GRANT SELECT, INSERT, UPDATE, DELETE ON t TO u")]
		[TestCase("DECLARE c CURSOR FOR SELECT a FROM t FOR UPDATE OF a")]
		[TestCase("BACKUP DATABASE d TO DISK = 'x' WITH INIT")]
		[TestCase("UPDATE t SET a = CASE WHEN b = 1 THEN 1 ELSE 2 END WHERE c = 3")]
		[TestCase("[dbo].[p]")]
		[TestCase("sp_dboption d, 'x', 'true'")]
		[TestCase(";UPDATE t SET a = 1;\nGO")]
		[TestCase("SELECT a FROM t ORDER BY a OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY")]
		public void TSqlExecuteOneStatement(string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				Single(TSql, sql).ShouldBeTrue();
				Single(Ase,  sql).ShouldBeTrue();
			}
		}

		// A module or a block holds statements separated by ';'. SQL Server reads it as one statement; ASE counts the
		// separators, as the CLI always did there.
		[TestCase("CREATE PROCEDURE p AS SELECT 1; SELECT 2; UPDATE t SET a = 1")]
		[TestCase("IF @x = 1 SELECT 1 ELSE IF @x = 2 SELECT 2 ELSE BEGIN UPDATE t SET a = 1; DELETE u END")]
		public void TSqlExecuteSeparatorsInsideOneStatement(string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				Single(TSql, sql).ShouldBeTrue();
				Single(Ase,  sql).ShouldBeFalse();
			}
		}

		// SQL Server counts statements by the kind of each; ASE by ';' and GO only, so ASE takes the unseparated rows.
		[TestCase("update t set a=1 delete u",                                              true)]
		[TestCase("UPDATE t SET a = 1 SELECT 1",                                            true)]
		[TestCase("INSERT INTO t VALUES (1) SELECT SCOPE_IDENTITY()",                       true)]
		[TestCase("INSERT INTO t SELECT 1 SELECT 2",                                        true)]
		[TestCase("BEGIN TRAN UPDATE t SET a = 1 COMMIT",                                   true)]
		[TestCase("DECLARE @x int SET @x = (SELECT 1) SELECT @x",                           true)]
		[TestCase("UPDATE t SET a = 1\nIF @@ROWCOUNT = 0\nBEGIN INSERT INTO t (a) VALUES (1) END", true)]
		[TestCase("CREATE TABLE t (a int) GRANT SELECT ON t TO u",                          true)]
		[TestCase("CREATE PROCEDURE p AS SELECT 1\nGO\nEXEC p",                             false)]
		[TestCase("select 1; select 2",                                                     false)]
		[TestCase("select 1\nGO\nselect 2",                                                 false)]
		[TestCase("select 1\ngo -- end of batch\nselect 2",                                 false)]
		[TestCase("update t set a = 1\nGO;\ndelete u",                                      false)]
		[TestCase("EXEC p THROW 50000, 'x', 1",                                             true)]
		[TestCase("EXEC p\nl:",                                                             true)]
		[TestCase("SET NOCOUNT ON\nUPDATE t SET a = 1 SET NOCOUNT OFF",                     true)]
		public void TSqlExecuteSeveralStatements(string sql, bool oneOnAse)
		{
			using (Assert.EnterMultipleScope())
			{
				Single(TSql, sql).ShouldBeFalse();
				Single(Ase,  sql).ShouldBe(oneOnAse);
			}
		}

		// linq2db's own ASE commands: several statements in one text, without separators.
		[TestCase("DECLARE @Value Integer -- Int32\nSET     @Value = 10\nDECLARE @ID Integer -- Int32\nSET     @ID = 5\n\nUPDATE\n\t[TestDataCore]..[Issue681Table]\nSET\n\t[Value] = @Value\nFROM\n\t[TestDataCore]..[Issue681Table] [t1]\nWHERE\n\t[t1].[ID] = @ID\n\nIF @@ROWCOUNT = 0\nBEGIN\n\tINSERT INTO [TestDataCore]..[Issue681Table]\n\t(\n\t\t[ID],\n\t\t[Value]\n\t)\n\tVALUES\n\t(\n\t\t@ID,\n\t\t@Value\n\t)\nEND\n")]
		[TestCase("DECLARE @Has Bit -- Boolean\nSET     @Has = 1\n\nINSERT INTO [Issue1438]\n(\n\t[Has]\n)\nVALUES\n(\n\t@Has\n)\n\nSELECT @@IDENTITY\n")]
		[TestCase("SET IDENTITY_INSERT [TableWithIdentity] ON\nMERGE INTO [TableWithIdentity] [Target]\nUSING (\n\tSELECT 1 AS [Id], 2 AS [Value_1]) [Source]\n(\n\t[Id],\n\t[Value_1]\n)\nON ([Target].[Id] = [Source].[Id])\n\nWHEN MATCHED THEN\nUPDATE\nSET\n\t[Value] = [Source].[Value_1]\n\nWHEN NOT MATCHED THEN\nINSERT\n(\n\t[Id],\n\t[Value]\n)\nVALUES\n(\n\t123,\n\t321\n)\nSET IDENTITY_INSERT [TableWithIdentity] OFF\n")]
		[TestCase("insert into t values (1)\nselect go, ago, gone from t")]
		[TestCase("select a, go\nfrom t")]
		[TestCase("select a, go -- a column\nfrom t")]
		[TestCase("select a /* x */ go\nfrom t\nGO")]
		public void AseExecuteCountsSeparatorsOnly(string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				Single(Ase, sql).ShouldBeTrue();
				ReadOnlySqlGuard.ValidateSingleStatement("Sybase.Managed", sql).IsAllowed.ShouldBeTrue();
			}
		}

		#endregion

		#region Temporary-table scripts (SQL Server and ASE)

		static bool WritesTemporaryTables(string dialect, string sql)
		{
			var writes = false;

			var accepted = dialect switch
			{
				TSql => ReadOnlySqlGrammar.TryCheckTSql(sql, out writes),
				Ase  => ReadOnlySqlGrammar.TryCheckAse (sql, out writes),
				_    => throw new ArgumentOutOfRangeException(nameof(dialect)),
			};

			accepted.ShouldBeTrue($"{dialect}: {sql}");

			if (writes)
				ReadOnlySqlGuard.FindNonAsciiCode(sql, dialect == TSql).ShouldBe(-1, $"{dialect}: {sql}");

			return writes;
		}

		[TestCase("select a, b into #t from t where a > 1\nselect * from #t")]
		[TestCase("SELECT a INTO #t FROM t;\nSELECT a FROM #t;")]
		[TestCase("select a into #t from t union all select b from u\nselect * from #t\nGO")]
		[TestCase("with c as (select 1 a) select a into #t from c\nselect * from #t")]
		[TestCase("create table #t (id int not null primary key, name nvarchar(50) collate Latin1_General_CI_AS default N'x', d decimal(18, 2) null)\ninsert into #t (id, name) select id, name from t with (nolock)\nselect * from #t")]
		[TestCase("CREATE TABLE #t (a int IDENTITY(1, 1), b varchar(10), INDEX ix_b (b))\nINSERT #t (b) VALUES ('x'), ('y')\nSELECT * FROM #t")]
		[TestCase("create table #t (a int)\ncreate unique clustered index ix on #t (a)\ncreate index [ix 2] on #t (a) include (a) where a > 0 with (fillfactor = 80)\nselect a from #t")]
		[TestCase("insert into #t default values\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = a + 1 where a in (select b from u)\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t with (rowlock) set a = u.b from #t join u on u.id = #t.a where #t.a > @p\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set #t.a = x.b from #t as t1 cross apply (select top 1 b from u) as x\nselect * from #t")]
		[TestCase("select a into #t from t\ndelete from #t where a is null\ndelete #t from #t join u on u.a = #t.a\nselect * from #t")]
		[TestCase("select a into #t from t\ntruncate table #t\nselect * from #t")]
		[TestCase("DECLARE @t TABLE (a int primary key)\nINSERT INTO @t (a) SELECT a FROM t\nSELECT a FROM @t")]
		[TestCase("DECLARE @t AS TABLE (a int)\nDECLARE @log TABLE (a int)\nINSERT @t (a) OUTPUT inserted.a INTO @log (a) VALUES (1)\nUPDATE @t SET a = 2 OUTPUT deleted.a, inserted.a INTO @log\nDELETE @t OUTPUT deleted.a INTO #gone WHERE a = 2\nSELECT * FROM @log")]
		[TestCase("DECLARE @p Int -- Int32\nSET @p = 1\nSELECT a INTO #t FROM t WHERE a = @p\nSELECT * FROM #t")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop table #t")]
		[TestCase("select a into #t from t; select a into #u from t;\nselect * from #t, #u;\ndrop table if exists #t, #u;")]
		[TestCase("with c as (select 1 a) insert into #t select a from c\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 where a = (select max(b) from u order by b offset 0 rows fetch next 1 rows only)\nselect * from #t")]
		[TestCase("select a into #t from t\nselect top 1 with ties #t.a into #u from #t order by a\nselect * from #u")]
		[TestCase("select a into #t from t\nselect top (2) percent #t.a into #u from #t order by a\nselect * from #u")]
		[TestCase("select a into #t from t\ninsert #u select top @n #t.a from #t\nselect * from #u")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from u cross apply #t outer apply #v where u.a = #t.a\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = case when #t.a > 1 then #t.a else -#t.a end, b = #t.a * 2 from #t where #t.a between 1 and #t.b or not #t.a like 'x%'\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from #t join #u on #u.a = #t.a where exists (select 1 from #v where #v.a in (#t.a, 2))\nselect * from #t")]
		[TestCase("INSERT INTO #t\n(\n\t[Id],\n\t[Name]\n)\nSELECT\n\t[t1].[PersonID],\n\t[t1].[FirstName]\nFROM\n\t[dbo].[Person] [t1]\n\t\tINNER JOIN [Doctor] [d] ON [d].[PersonID] = [t1].[PersonID]\nSELECT [t1].[Id] FROM #t [t1]")]
		[TestCase("select x.[a] as [x], \"b\" as \"Total Count\", [_y$1] into #t from dbo.t as [x]\nupdate #t set a = 1 from #t join dbo.u [t1] on [t1].a = #t.a\nselect * from #t")]
		[TestCase("select a into #t from dbo.[output]\nupdate #t set a = 1 from #t join (dbo.output o join #u v on 1 = 1) on 1 = 1\nselect * from #t")]
		// Outside ASCII only inside string literals and comments, which the server never reads as names.
		[TestCase("select a into #t from t where b = N'\u0418\u043C\u044F \uFF03t' -- \u0418\u043C\u044F \u0640#t\nselect * from #t /* \u3007#t /* \u200D */ \uFEFF */")]
		[TestCase("select a, 'it''s \uFF03t' b into #t from t\nupdate #t set a = 1 from #t where b like N'%\u3007%'\nselect * from #t")]
		public void TempScriptsAccepted(string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				WritesTemporaryTables(TSql, sql).ShouldBeTrue(sql);
				WritesTemporaryTables(Ase,  sql).ShouldBeTrue(sql);
			}
		}

		[TestCase("select * from #t")]
		[TestCase("select a from t #x")]
		[TestCase("select a #n from t as #x")]
		[TestCase("DECLARE @p Int\nSET @p = 1\nselect * from t where a = @p")]
		public void QueriesWriteNoTemporaryTables(string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				WritesTemporaryTables(TSql, sql).ShouldBeFalse(sql);
				WritesTemporaryTables(Ase,  sql).ShouldBeFalse(sql);
			}
		}

		[TestCase("select a into #t from t\nselect * from #t")]
		[TestCase("create table #t (a int)\nselect * from #t")]
		[TestCase("insert @t values (1)\nselect * from @t")]
		public void TempScriptsOnlyInTSql(string sql)
		{
			AssertQuery(sql, false, Bracket, MySql, Ydb, Informix, Postgres, Standard);
		}

		// ASE takes no session option even when the call's connection is closed afterwards.
		[Test]
		public void SessionOptionBeforeTempScriptOnlyOnSqlServer()
		{
			const string sql = "SET NOCOUNT ON\nselect a into #t from t\nselect * from #t";

			using (Assert.EnterMultipleScope())
			{
				WritesTemporaryTables(TSql, sql).ShouldBeTrue();
				Query(Ase, sql).ShouldBeFalse();
			}
		}

		// Each write word must be followed directly by a temporary table or a table variable.
		[TestCase("insert into t values (1)\nselect 1")]
		[TestCase("insert t select 1\nselect 1")]
		[TestCase("insert into ##g values (1)\nselect 1")]
		[TestCase("insert into tempdb..x values (1)\nselect 1")]
		[TestCase("insert into tempdb.dbo.x values (1)\nselect 1")]
		[TestCase("insert into [#t] values (1)\nselect 1")]
		[TestCase("insert into \"#t\" values (1)\nselect 1")]
		[TestCase("insert into #t.x values (1)\nselect 1")]
		[TestCase("insert into @@t values (1)\nselect 1")]
		[TestCase("insert #t exec p\nselect * from #t")]
		[TestCase("insert #t execute ('select 1')\nselect * from #t")]
		[TestCase("insert #t select 1 delete t\nselect * from #t")]
		[TestCase("insert #t select 1 into t2\nselect * from #t")]
		[TestCase("insert #t (a) values (next value for s)\nselect * from #t")]
		[TestCase("insert #t select * from openquery(s, 'x')\nselect * from #t")]
		[TestCase("update t set a = 1\nselect 1")]
		[TestCase("update top (1) #t set a = 1\nselect 1")]
		[TestCase("update #t set a = 1 from t #t\nselect * from #t")]
		[TestCase("update #t set a = 1 from t as #t\nselect * from #t")]
		[TestCase("update #t set a = 1 from t [#t]\nselect * from #t")]
		[TestCase("update #t set a = 1 from t \"#t\"\nselect * from #t")]
		[TestCase("update #t set a = 1 from (select * from t) #t\nselect * from #t")]
		[TestCase("update #t set a = 1 from #u join (t #t join u on 1 = 1) on 1 = 1\nselect * from #t")]
		[TestCase("update @t set a = 1 from t as @t\nselect * from @t")]
		[TestCase("with [#t] as (select * from t) update #t set a = 1\nselect 1")]
		[TestCase("with c as (select * from t) update c set a = 1\nselect 1")]
		[TestCase("update #t set a = 1 output inserted.a\nselect * from #t")]
		[TestCase("update #t set a = 1 output inserted.a select b into #u from t\nselect * from #t")]
		[TestCase("update #t set a = 1 output inserted.a into t\nselect * from #t")]
		[TestCase("update #t set a = 1 output inserted.a into dbo.t\nselect * from #t")]
		[TestCase("update #t set a = 1 where a = (delete from t)\nselect * from #t")]
		[TestCase("delete t\nselect 1")]
		[TestCase("delete from t where a = 1\nselect 1")]
		[TestCase("delete #t from t #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from dbo.ties #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from apply #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from #u join dbo.using #t on 1 = 1\nselect * from #t")]
		[TestCase("select a into #t from t\ndelete #t from dbo.ties #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from t for system_time as of '2020-01-01' #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from t for system_time as of @d #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from t for system_time all #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from t for system_time from 1 to 2 #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from t tablesample (10 percent) #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from ##g #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from @v #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from [dbo].[t] #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from {oj t left outer join u on 1 = 1} #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from t with ties #t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 1 from 1 #t\nselect * from #t")]
		// The server compares the target with an alias, a table or a CTE under the database collation: U+FF03 and U+FF20
		// equal # and @, U+FEFF, U+200D and U+0640 are ignored. Outside string literals and comments such a script is ASCII
		// only, and a quoted name must begin with an ASCII letter, a digit, _ or $.
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [\uFF03t]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real \"\uFF03t\"\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real as [\uFF03t]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [\uFF03\uFF54]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from #u join dbo.real [\uFF03t] on 1 = 1\nselect * from #t")]
		[TestCase("select a into #t from t\ndelete #t from dbo.real [\uFF03t]\nselect * from #t")]
		[TestCase("declare @v table (a int)\nupdate @v set a = 42 from dbo.real [\uFF20v]\nselect * from @v")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [\uFEFF#t]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [\u200D#t]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from (select a from dbo.real) [\uFF03t]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.[\uFF03t]\nselect * from #t")]
		[TestCase("with [\uFF03t] as (select a from dbo.real) update #t set a = 42\nselect 1")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [ t1]\nselect * from #t")]
		[TestCase("select [\u0418\u043C\u044F] into #t from t\nselect * from #t")]
		[TestCase("select \u0418\u043C\u044F, [t1].a into #t from t [t1]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [x\u00E9]\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real \"it's \u00E9\"\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real [it's \u3007]#t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real r -- x\r\n\u3007#t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real r /* x */ \u3007#t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real \u00A0#t\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real \u007F#t\nselect * from #t")]
		[TestCase("DECLARE @\u0640p Int\nselect a into #t from t\nselect * from #t")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop table #\u3007")]
		[TestCase("declare @v table (a int)\nupdate @v set a = 42 from dbo.real \u0640@v\nselect * from @v")]
		[TestCase("declare @v table (a int)\nupdate @v set a = 42 from dbo.real as \u0640@v\nselect * from @v")]
		[TestCase("declare @v table (a int)\nupdate @v set a = 42 from (select a from dbo.real) \u0640@v\nselect * from @v")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from dbo.real \u0640#t\nselect * from #t")]
		// OUTPUT is not reserved: dbo.output is a table, and a bracketed join exposes its alias to the target.
		[TestCase("select a into #t from t\nupdate #t set a = 42 from #u join (dbo.output #t join #u v on 1 = 1) on 1 = 1\nselect * from #t")]
		[TestCase("select a into #t from t\ndelete #t from #u join (dbo.output #t join #u v on 1 = 1) on 1 = 1\nselect * from #t")]
		[TestCase("select a into #t from t\nupdate #t set a = 42 from #u join (output #t join #u v on 1 = 1) on 1 = 1\nselect * from #t")]
		[TestCase("delete #t output deleted.*\nselect 1")]
		[TestCase("truncate table t\nselect 1")]
		[TestCase("truncate table ##g\nselect 1")]
		[TestCase("create table t (a int)\nselect 1")]
		[TestCase("create table ##g (a int)\nselect 1")]
		[TestCase("create table tempdb..x (a int)\nselect 1")]
		[TestCase("create table #t (a int default (next value for s))\nselect 1")]
		[TestCase("create index ix on t (a)\nselect 1")]
		[TestCase("create index ix on ##g (a)\nselect 1")]
		[TestCase("create view v as select 1\nselect 1")]
		[TestCase("create procedure #p as select 1\nselect 1")]
		[TestCase("select a into t2 from t\nselect 1")]
		[TestCase("select a into ##g from t\nselect 1")]
		[TestCase("select a into tempdb..x from t\nselect 1")]
		[TestCase("select a into #t from t into #u\nselect 1")]
		[TestCase("merge #t using t on 1 = 1 when matched then delete;\nselect * from #t")]
		[TestCase("merge into #t using t on 1 = 1 when not matched then insert values (1);\nselect * from #t")]
		[TestCase("declare @t table (a int) insert @t values (1) select * from @t select 2")]
		[TestCase("select a into #t from t\nselect * from #t\nselect * from #t")]
		[TestCase("select a into #t from t\nselect * from #t\ninsert #t values (1)")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop table t")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop table ##g")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop table #t, t")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop view #t")]
		[TestCase("select a into #t from t\nselect * from #t\ndrop table #t\nselect 1")]
		[TestCase("select a into #t from t")]
		[TestCase("create table #t (a int)")]
		[TestCase("select a into #t from t\nexec p\nselect * from #t")]
		[TestCase("select a into #t from t\nSET ROWCOUNT 1\nselect * from #t")]
		[TestCase("select a into #t from t\nalter table #t add b int\nselect * from #t")]
		[TestCase("select a into #t from t\nbegin tran\nselect * from #t")]
		public void TempScriptsRefused(string sql)
		{
			AssertQuery(sql, false, TSql, Ase);
		}

		// The server ends a token at every control character and at U+200B, so a #name after one stands where an alias
		// does. U+200B is outside ASCII, so a script that writes temporary tables may not hold it at all. The character is
		// passed as a number: a filtered test run silently skips some cases whose name holds one.
		[TestCase(0x00)]
		[TestCase(0x01)]
		[TestCase(0x08)]
		[TestCase(0x0E)]
		[TestCase(0x1C)]
		[TestCase(0x1F)]
		[TestCase(0x200B)]
		public void TempScriptSeparators(int code)
		{
			var c = (char)code;

			using (Assert.EnterMultipleScope())
			{
				var separated = $"select a into #t from t\nupdate #t set a = 1 from #t{c}join #u{c}on{c}#u.a = #t.a\nselect * from #t";

				if (code < 0x80)
					WritesTemporaryTables(TSql, separated).ShouldBeTrue();
				else
					Query(TSql, separated).ShouldBeFalse();

				foreach (var dialect in new[] { TSql, Ase })
				{
					Query(dialect, $"select a into #t from t\nupdate #t set a = 42 from dbo.real{c}#t\nselect * from #t").   ShouldBeFalse(dialect);
					Query(dialect, $"select a into #t from t\nupdate #t set a = 42 from dbo.real as{c}#t\nselect * from #t").ShouldBeFalse(dialect);
					Query(dialect, $"declare @v table (a int)\nupdate @v set a = 42 from dbo.real{c}\u0640@v\nselect * from @v").ShouldBeFalse(dialect);
				}
			}
		}

		// SQL Server reads format characters (Cf), letter numbers (Nl) and Ethiopic digits (No) into a name, and the
		// default collations ignore them: after U+0640 (an ignorable letter) or on their own they make an alias equal to
		// #t or @v, and the statement writes the permanent table. Each of these wrote dbo.real live through the CLI. The
		// script is refused at the character, whatever its class. Code points are numbers, so a filtered run keeps them.
		[TestCase(0x200D)] // ZERO WIDTH JOINER
		[TestCase(0x200E)] // LEFT-TO-RIGHT MARK
		[TestCase(0x200F)] // RIGHT-TO-LEFT MARK
		[TestCase(0xFEFF)] // ZERO WIDTH NO-BREAK SPACE
		[TestCase(0x2060)] // WORD JOINER
		[TestCase(0x1369)] // ETHIOPIC DIGIT ONE
		[TestCase(0x180E)] // MONGOLIAN VOWEL SEPARATOR
		[TestCase(0x202E)] // RIGHT-TO-LEFT OVERRIDE
		[TestCase(0x06DD)] // ARABIC END OF AYAH
		[TestCase(0xFFF9)] // INTERLINEAR ANNOTATION ANCHOR
		public void TempScriptIgnorableInsideAliasRefused(int code)
		{
			var x = "\u0640" + (char)code;

			AssertTempScriptRefusedAt(code, $"select a into #t from dbo.real\nupdate #t set a = 42 from dbo.real {x}#t\nselect a from #t",     x);
			AssertTempScriptRefusedAt(code, $"select a into #t from dbo.real\ndelete #t from dbo.real as {x}#t\nselect a from #t",         x);
			AssertTempScriptRefusedAt(code, $"declare @v table (a int)\nupdate @v set a = 42 from dbo.real {x}@v\nselect a from @v",       x);
			AssertTempScriptRefusedAt(code, $"select a into #t from dbo.real\nupdate #t set a = 42 from dbo.real {(char)code}#t\nselect a from #t", ((char)code).ToString());
		}

		[TestCase(0x3007)] // IDEOGRAPHIC NUMBER ZERO
		[TestCase(0x3038)] // HANGZHOU NUMERAL TEN
		[TestCase(0x16EE)] // RUNIC ARLAUG SYMBOL
		public void TempScriptLetterNumberAliasRefused(int code)
		{
			var x = ((char)code).ToString();

			AssertTempScriptRefusedAt(code, $"select a into #t from dbo.real\nupdate #t set a = 42 from dbo.real {x}#t\nselect a from #t", x);
			AssertTempScriptRefusedAt(code, $"select a into #t from dbo.real\ndelete #t from dbo.real {x}#t\nselect a from #t",         x);
			AssertTempScriptRefusedAt(code, $"declare @v table (a int)\nupdate @v set a = 42 from dbo.real {x}@v\nselect a from @v",   x);
		}

		static void AssertTempScriptRefusedAt(int code, string sql, string first)
		{
			var position = sql.IndexOf(first, StringComparison.Ordinal);
			var label    = code.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

			using (Assert.EnterMultipleScope())
			{
				foreach (var provider in new[] { "SqlServer.2022", "Sybase.Managed" })
				{
					var result = ReadOnlySqlGuard.Validate(provider, sql);

					result.IsAllowed.ShouldBeFalse($"{provider} {label}");

					// The grammar may refuse such a text first (a #name after a symbol it reads); once it accepts the
					// script, the refusal names the first non-ASCII character.
					if (result.Error!.Contains("non-ASCII", StringComparison.Ordinal))
						result.Error.ShouldContain($"at 2:{position - sql.IndexOf('\n', StringComparison.Ordinal)}.", customMessage: $"{provider} {label}");
				}

				ReadOnlySqlGuard.FindNonAsciiCode(sql, true).ShouldBe(position, label);
			}
		}

		// A plain query, which writes no temporary table, keeps reading names outside ASCII; so do other providers.
		[TestCase("select \u0418\u043C\u044F, [\u0421\u0443\u043C\u043C\u0430], \"\u00E9\" from dbo.\u0417\u0430\u043A\u0430\u0437\u044B \u0437")]
		[TestCase("select a from dbo.real \u3007")]
		[TestCase("select #t.a from #t \u0640\u200D join dbo.real r on 1 = 1")]
		public void PlainQueriesKeepNonAsciiNames(string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				// The ASE driver sends the text in the character set the server names at login; outside ASCII it needs
				// Charset=utf8.
				foreach (var provider in new[] { "SqlServer.2022", "SqlCe" })
				{
					var result = ReadOnlySqlGuard.Validate(provider, sql);

					result.IsAllowed.ShouldBeTrue($"{provider}: {result.Error}");
					result.WritesTemporaryTables.ShouldBeFalse(provider);
				}

				ReadOnlySqlGuard.Validate("Sybase.Managed", sql).IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.Validate("Sybase.Managed", sql, serverCharset: "utf8", driverEncoding: "utf-8").IsAllowed.ShouldBeTrue();
			}
		}

		// The server's reading of literals and comments: '' inside a string, -- to the end of the line, nested /* */,
		// and quoted names, whose content is code.
		[TestCase("select 'it''s \u00E9' a into #t", -1)]
		[TestCase("select N'\u00E9' a into #t -- \u00E9", -1)]
		[TestCase("select 1 a into #t /* \u00E9 /* \u00E9 */ \u00E9 */", -1)]
		[TestCase("select 1 a into #t /* \u00E9 /* */ */ \u00E9", 33)]
		[TestCase("select 1 [it's \u00E9] into #t -- '", 15)]
		[TestCase("select 1 [a]]\u00E9] into #t", 13)]
		[TestCase("select 1 \"a\"\"\u00E9\" into #t", 13)]
		[TestCase("select 1 a into #t -- x\r\u00E9", 24)]
		[TestCase("select 1 a into #t\u0001\t\u007E", -1)]
		[TestCase("select 1 a into #t\u007F", 18)]
		[TestCase("select 'open \u00E9", -1)]
		public void NonAsciiCodeIsFoundOutsideLiteralsAndComments(string sql, int expected)
		{
			ReadOnlySqlGuard.FindNonAsciiCode(sql, true).ShouldBe(expected);
		}

		// ASE ends -- at a line feed only, so a lone carriage return stays inside the comment there.
		[TestCase("select 1 a into #t -- x\r\u00E9", -1)]
		[TestCase("select 1 a into #t -- x\r\n\u00E9", 25)]
		public void NonAsciiCodeOnAseReadsCommentsToLineFeed(string sql, int expected)
		{
			ReadOnlySqlGuard.FindNonAsciiCode(sql, false).ShouldBe(expected);
		}

		#region The text the server receives

		// AdoNetCore.AseClient sends the command text in the connection's character set (iso_1 unless the server or
		// Charset= names another) through .NET's encoder, which replaces what the set does not hold, often by a lookalike:
		// U+2019 by ', U+FF02 by ", U+FF0A by *, U+2010 by -, a fullwidth letter by its ASCII letter. Each of these texts
		// wrote a table live on ASE 16.1 through the CLI or the driver. Code points are numbers, so a filtered run keeps them.
		[TestCase(0x2019)] // RIGHT SINGLE QUOTATION MARK
		[TestCase(0x02BC)] // MODIFIER LETTER APOSTROPHE
		[TestCase(0xFF07)] // FULLWIDTH APOSTROPHE
		[TestCase(0x2032)] // PRIME
		public void AseBestFitQuoteEndsLiteral(int code)
		{
			var sql    = "select 'a" + (char)code + ", 1 b update tempdb..wt set a = 7 --' c";
			var result = ReadOnlySqlGuard.Validate("Sybase.Managed", sql);

			using (Assert.EnterMultipleScope())
			{
				result.IsAllowed.ShouldBeFalse(code.ToString("X4", CultureInfo.InvariantCulture));
				result.Error!.ShouldContain(string.Create(CultureInfo.InvariantCulture, $"character U+{code:X4} at 1:10 is outside ASCII."), customMessage: result.Error);

				// SqlClient sends UTF-16: SQL Server reads the character inside the literal, as the check does.
				ReadOnlySqlGuard.Validate("SqlServer.2022", "select 'a" + (char)code + "' c").IsAllowed.ShouldBeTrue();
			}
		}

		[Test]
		public void AseBestFitFullwidthKeywords()
		{
			static string Fullwidth(string word)
			{
				return string.Concat(word.Select(c => (char)(0xFF41 + c - 'a')));
			}

			ReadOnlySqlGuard.Validate("Sybase.Managed", "select 1 a " + Fullwidth("update") + " tempdb..wt " + Fullwidth("set") + " a = 2")
				.IsAllowed.ShouldBeFalse();
		}

		[TestCase(0x201D, "select \"a{0}, 1 b update tempdb..wt set a = 8 --\" c")]
		[TestCase(0xFF02, "select \"a{0}, 1 b update tempdb..wt set a = 8 --\" c")]
		[TestCase(0xFF0A, "select 1 a /* {0}/ update tempdb..wt set a = 9 -- */")]
		[TestCase(0xFF0F, "select 1 a /* *{0} update tempdb..wt set a = 9 -- */")]
		[TestCase(0x2010, "select 1 a {0}{0} '\nupdate tempdb..wt set a = 6 --'")]
		[TestCase(0x2013, "select 1 a {0}{0} '\nupdate tempdb..wt set a = 6 --'")]
		public void AseBestFitOtherForms(int code, string format)
		{
			ReadOnlySqlGuard.Validate("Sybase.Managed", string.Format(CultureInfo.InvariantCulture, format, (char)code))
				.IsAllowed.ShouldBeFalse(code.ToString("X4", CultureInfo.InvariantCulture));
		}

		// The ASCII rule of temporary-table scripts lets a character stand inside a literal; on ASE it would end it.
		[Test]
		public void AseTempScriptAsciiRuleSidestepped()
		{
			ReadOnlySqlGuard.Validate("Sybase.Managed", "select a into #t from tempdb..wt\nselect 'x\u2019 update tempdb..wt set a = 5 --' c from #t")
				.IsAllowed.ShouldBeFalse();
		}

		// The server names the character set at login, and only that answer counts: before it, a text outside ASCII awaits
		// it (refused, with the routing of the call that would run); after it, only utf8 carries such a text unchanged.
		[TestCase(null,     "select '\u00E9' a",                false)]
		[TestCase("utf8",   "select '\u00E9' a",                true)]
		[TestCase("UTF8",   "select '\u2019', N'\u0418' a",      true)]
		[TestCase("utf8",   "select 1 a \uFF55\uFF50\uFF44\uFF41\uFF54\uFF45 t \uFF53\uFF45\uFF54 a = 2", true)]  // names to both sides; the server cannot convert them (live: an error, nothing written)
		[TestCase("iso_1",  "select '\u00E9' a",                false)]
		[TestCase("utf-8",  "select '\u00E9' a",                false)] // ASE has no set of that name
		[TestCase("",       "select '\u00E9' a",                false)]
		[TestCase("iso_1",  "select 'e' a",                     true)]
		public void AseServerCharset(string? serverCharset, string sql, bool accepted)
		{
			using (Assert.EnterMultipleScope())
			{
				ReadOnlySqlGuard.Validate("Sybase.Managed", sql, serverCharset: serverCharset, driverEncoding: "utf-8").IsAllowed.ShouldBe(accepted);
				ReadOnlySqlGuard.ValidateSingleStatement("Sybase.Managed", sql, serverCharset: serverCharset, driverEncoding: "utf-8").IsAllowed.ShouldBe(accepted);
				ReadOnlySqlGuard.Validate("Sybase", sql, serverCharset: serverCharset, driverEncoding: "utf-8").IsAllowed.ShouldBe(accepted);   // SAP's driver
			}
		}

		// The driver fixes its encoder at login. A login script's set char_convert utf8 makes the server report utf8 while
		// the driver still best-fits into iso_1 (live: the U+2019 and fullwidth probes wrote a table through the CLI). Both
		// must say UTF-8; a driver the CLI cannot read (SAP's) counts as not UTF-8.
		[TestCase("utf-8",      true)]
		[TestCase("iso-8859-1", false)]
		[TestCase("us-ascii",   false)]
		[TestCase(null,         false)]
		public void AseDriverEncoderMustBeUtf8Too(string? driverEncoding, bool accepted)
		{
			const string sql = "select 'a\u2019, 1 b update tempdb..wt set a = 7 --' c";

			var result = ReadOnlySqlGuard.Validate("Sybase.Managed", sql, serverCharset: "utf8", driverEncoding: driverEncoding);

			using (Assert.EnterMultipleScope())
			{
				result.IsAllowed.ShouldBe(accepted);

				if (!accepted)
					result.Error!.ShouldContain("The server reports utf8 for this session, but the driver sends the text");
			}
		}

		[Test]
		public void AseAwaitingServerCharsetKeepsTheRouting()
		{
			var temp    = ReadOnlySqlGuard.Validate("Sybase.Managed", "select a into #t from t\nselect '\u00E9' e, a from #t");
			var plain   = ReadOnlySqlGuard.Validate("Sybase.Managed", "select '\u00E9' e");
			var refused = ReadOnlySqlGuard.Validate("Sybase.Managed", "select '\u00E9' e delete t");

			using (Assert.EnterMultipleScope())
			{
				temp.IsAllowed.ShouldBeFalse();
				temp.AwaitsServerCharset.ShouldBeTrue();
				temp.WritesTemporaryTables.ShouldBeTrue();
				temp.RequiresUnpooledConnection.ShouldBeTrue();
				plain.AwaitsServerCharset.ShouldBeTrue();
				plain.RequiresUnpooledConnection.ShouldBeFalse();
				plain.Error!.ShouldContain("is outside ASCII.");

				// A text the check refuses for another reason is refused at once.
				refused.AwaitsServerCharset.ShouldBeFalse();
				refused.Error!.ShouldContain("statement word DELETE");

				// SQL Server's driver sends UTF-16: nothing to ask.
				ReadOnlySqlGuard.Validate("SqlServer.2022", "select '\u00E9' e").IsAllowed.ShouldBeTrue();
			}
		}

		// AdoNetCore.AseClient reads a connection string with its own tokeniser, which stops at a key with an empty value
		// or a value with a doubled quote and drops the rest; the server then answers with its own set. Each row wrote a
		// table live through the CLI or the driver (ASE 16.1). The connection string decides nothing; the server's answer does.
		const string AseBase = "Data Source=localhost;Port=5000;Database=tempdb;Uid=sa;Password=myPassword;";

		[TestCase(AseBase + "Application Name=;Charset=utf8",           "select 1 a \uFF55\uFF50\uFF44\uFF41\uFF54\uFF45 tempdb..wt \uFF53\uFF45\uFF54 a = 2")]
		[TestCase(AseBase + "Application Name=;Charset=utf8",           "select 'a\u2019, 1 b update tempdb..wt set a = 7 --' c")]
		[TestCase(AseBase + "Charset=;Charset=utf8",                    "select 'a\u2019, 1 b update tempdb..wt set a = 7 --' c")]
		[TestCase(AseBase + "Application Name=\"a\"\"b\";Charset=utf8", "select 'a\u2019, 1 b update tempdb..wt set a = 7 --' c")]
		[TestCase(AseBase + "Application Name='a''b';Charset=utf8",     "select 'a\u2019, 1 b update tempdb..wt set a = 7 --' c")]
		[TestCase(AseBase + "X=;Charset=utf8;",                         "select 1 a \uFF55\uFF50\uFF44\uFF41\uFF54\uFF45 tempdb..wt \uFF53\uFF45\uFF54 a = 2")]
		public void AseCharsetReadAsTheServerGrantedIt(string connectionString, string sql)
		{
			using (Assert.EnterMultipleScope())
			{
				// Before login: refused, awaiting the server's answer.
				ReadOnlySqlGuard.Validate("Sybase.Managed", sql, connectionString).IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.ValidateSingleStatement("Sybase.Managed", sql, connectionString).IsAllowed.ShouldBeFalse();

				// After login the server answered iso_1, as it did live: refused, and the message says why utf8 was not granted.
				var result = ReadOnlySqlGuard.Validate("Sybase.Managed", sql, connectionString, "iso_1");

				result.IsAllowed.ShouldBeFalse();
				result.AwaitsServerCharset.ShouldBeFalse();
				result.Error!.ShouldContain("the server named the character set 'iso_1' for this connection");
				result.Error!.ShouldContain("The connection string asks for Charset=utf8, but the server did not grant it");
			}
		}

		// UTF-8 carries every character but an unpaired surrogate, which the encoder replaces by U+FFFD. Built in code: an
		// attribute argument does not keep one.
		[Test]
		public void AseUtf8UnpairedSurrogateRefused()
		{
			var result = ReadOnlySqlGuard.Validate("Sybase.Managed", "select 'a" + (char)0xD800 + "' a", serverCharset: "utf8", driverEncoding: "utf-8");

			using (Assert.EnterMultipleScope())
			{
				result.IsAllowed.ShouldBeFalse();
				result.Error!.ShouldContain("character U+D800 at 1:10 reaches the server as '\uFFFD'. The driver sends the text in utf-8");
			}
		}

		// ASE, MySQL, MariaDB, SQLite and ClickHouse end -- (and MySQL #) at a line feed only; SQL Server and PostgreSQL
		// at a carriage return too. Each refused text wrote a table live on the servers named.
		[Test]
		public void AseLoneCarriageReturnDoesNotEndComment()
		{
			using (Assert.EnterMultipleScope())
			{
				ReadOnlySqlGuard.Validate("Sybase.Managed", "select 1 a --\r'\nupdate tempdb..wt set a = 3 --'")
					.IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.Validate("Sybase.Managed", "select a into #t from tempdb..wt\nselect a from #t --\r'\nupdate tempdb..wt set a = 4 --'")
					.IsAllowed.ShouldBeFalse();

				// SQL Server ends the comment at the carriage return, and the string runs to the end of the text.
				ReadOnlySqlGuard.Validate("SqlServer.2022", "select 1 a --\r'\nupdate tempdb..wt set a = 3 --'").IsAllowed.ShouldBeTrue();
			}
		}

		[TestCase("select 1 a #\r'\n; update wt set a = 2; -- '")]
		[TestCase("select 1 a -- \r'\n; update wt set a = 2; -- '")]
		public void MySqlLoneCarriageReturnDoesNotEndComment(string sql)
		{
			ReadOnlySqlGrammar.TryCheckMySql(sql, out _).ShouldBeFalse();
		}

		[Test]
		public void BracketLoneCarriageReturnDoesNotEndComment()
		{
			using (Assert.EnterMultipleScope())
			{
				ReadOnlySqlGrammar.TryCheckBracket("select 1 a --\r'\n; update wt set a = 3; --'", out _).ShouldBeFalse();
				ReadOnlySqlGrammar.TryCheckBracket("select 1 a --\r\n from t", out _).ShouldBeTrue();
			}
		}

		// MySQL and MariaDB read no backslash escape under NO_BACKSLASH_ESCAPES and "…" as a name under ANSI_QUOTES; the
		// check cannot see sql_mode, so a backslash in either is refused. Each wrote live under that mode.
		[TestCase("select 'a\\', 1 b; update wt set a = 3; -- '")]
		[TestCase("select 1 as \"a\\\", 1 b; update wt set a = 4; -- \"")]
		public void MySqlModesRefused(string sql)
		{
			ReadOnlySqlGrammar.TryCheckMySql(sql, out _).ShouldBeFalse();
		}

		// PostgreSQL's dollar-quote tag takes any character outside ASCII. live, psql on PostgreSQL 17: wt 6.
		[TestCase(0x20AC)]
		[TestCase(0x0301)]
		public void PostgresDollarTagNonLetter(int code)
		{
			var tag = code == 0x0301 ? "a" + (char)code : ((char)code).ToString();

			using (Assert.EnterMultipleScope())
			{
				ReadOnlySqlGrammar.TryCheckPostgres("select $" + tag + "$ ' $" + tag + "$; update wt set a = 6; -- '", out _).ShouldBeFalse();
				ReadOnlySqlGrammar.TryCheckPostgres("select $" + tag + "$ ' $" + tag + "$ a", out _).ShouldBeTrue();
			}
		}

		#endregion

		[Test]
		public void TempScriptsRouteToAnUnpooledConnectionOnAseOnly()
		{
			const string temp  = "select a into #t from t\nselect * from #t";
			const string plain = "select * from t";

			var sqlServerTemp  = ReadOnlySqlGuard.Validate("SqlServer.2022", temp);
			var sqlServerPlain = ReadOnlySqlGuard.Validate("SqlServer.2022", plain);
			var aseTemp        = ReadOnlySqlGuard.Validate("Sybase.Managed", temp);
			var asePlain       = ReadOnlySqlGuard.Validate("Sybase.Managed", plain);
			var aseExecute     = ReadOnlySqlGuard.ValidateSingleStatement("Sybase", "update t set a = 1");
			var sqlExecute     = ReadOnlySqlGuard.ValidateSingleStatement("SqlServer", "update t set a = 1");

			using (Assert.EnterMultipleScope())
			{
				sqlServerTemp. IsAllowed.                 ShouldBeTrue();
				sqlServerTemp. WritesTemporaryTables.     ShouldBeTrue();
				sqlServerTemp. RequiresUnpooledConnection.ShouldBeFalse();

				sqlServerPlain.WritesTemporaryTables.     ShouldBeFalse();
				sqlServerPlain.RequiresUnpooledConnection.ShouldBeFalse();

				aseTemp.       IsAllowed.                 ShouldBeTrue();
				aseTemp.       WritesTemporaryTables.     ShouldBeTrue();
				aseTemp.       RequiresUnpooledConnection.ShouldBeTrue();

				asePlain.      IsAllowed.                 ShouldBeTrue();
				asePlain.      RequiresUnpooledConnection.ShouldBeFalse();

				// Execute admits any statement, and so any session state.
				aseExecute.    IsAllowed.                 ShouldBeTrue();
				aseExecute.    RequiresUnpooledConnection.ShouldBeTrue();
				sqlExecute.    RequiresUnpooledConnection.ShouldBeFalse();
			}
		}

		// The flag alone keeps nothing out of the pool: the connection string the call opens with must carry it.
		[Test]
		public void UnpooledRouteReachesTheConnectionString()
		{
			const string connectionString = "Data Source=ase;Port=5000;Pooling=true;";

			var provider = SybaseTools.GetDataProvider(SybaseProvider.DataAction);
			var options  = new DataOptions().UseConnectionString(provider, connectionString);

			var temp    = QueryExecutionExecutor.RouteConnection(options, provider, ReadOnlySqlGuard.Validate("Sybase.Managed", "select a into #t from t\nselect * from #t")).Options!;
			var execute = QueryExecutionExecutor.RouteConnection(options, provider, ReadOnlySqlGuard.ValidateSingleStatement("Sybase.Managed", "update t set a = 1")).Options!;
			var plain   = QueryExecutionExecutor.RouteConnection(options, provider, ReadOnlySqlGuard.Validate("Sybase.Managed", "select * from t")).Options!;

			using (Assert.EnterMultipleScope())
			{
				temp.   ConnectionOptions.ConnectionString.ShouldBe("Pooling=false;Data Source=ase;Port=5000;Pooling=true;Pooling=false");
				temp.   ConnectionOptions.DataProvider.ShouldBeSameAs(provider);
				execute.ConnectionOptions.ConnectionString.ShouldBe(temp.ConnectionOptions.ConnectionString);
				plain.  ShouldBeSameAs(options);

				// The appended key is the one a connection-string parser keeps.
				new DbConnectionStringBuilder { ConnectionString = temp.ConnectionOptions.ConnectionString }["Pooling"].ShouldBe("false");
			}
		}

		// AdoNetCore.AseClient stops reading at a key with an empty value or a value with a doubled quote, so a key appended
		// after one would not count: the route puts Pooling=false first as well, and the driver's own parser decides. A
		// Pooling=true before such a key cannot be overridden, and the call is refused.
		const string AseRouteBase = "Data Source=ase;Port=5000;Database=tempdb;Uid=sa;Password=p;";

		[TestCase(AseRouteBase,                                    true)]
		[TestCase(AseRouteBase + "X=;Charset=utf8",                true)]
		[TestCase(AseRouteBase + "Application Name='a''b';Pooling=true", true)]
		[TestCase(AseRouteBase + "Pooling=true",                   true)]
		[TestCase(AseRouteBase + "Pooling=true;X=;",               false)]
		[TestCase(AseRouteBase + "Pooling=true;Application Name=\"a\"\"b\";", false)]
		public void UnpooledRouteAsTheDriverReadsIt(string connectionString, bool routed)
		{
			var provider = SybaseTools.GetDataProvider(SybaseProvider.DataAction);
			var options  = new DataOptions().UseConnectionString(provider, connectionString);
			var (routedOptions, error) = QueryExecutionExecutor.RouteConnection(options, provider, ReadOnlySqlGuard.Validate("Sybase.Managed", "select a into #t from t\nselect * from #t"));

			using (Assert.EnterMultipleScope())
			{
				(routedOptions != null).ShouldBe(routed);

				if (routed)
					DriverReadsPooling(routedOptions!.ConnectionOptions.ConnectionString!).ShouldBeFalse();
				else
					error!.ShouldContain("the driver would read this connection string with pooling on");
			}
		}

		// --user and --password go into the {0}/{1} template as one value each, so they cannot add keys. Each row is read
		// by DbConnectionStringBuilder and by AdoNetCore.AseClient's own parser: both see the password as given and no
		// other key.
		[TestCase("myPassword;X=;Charset=utf8", "\"myPassword;X=;Charset=utf8\"")]
		[TestCase("a=b",                        "\"a=b\"")]
		[TestCase("it's",                       "\"it's\"")]
		[TestCase("say \"x\"",                 "'say \"x\"'")]
		[TestCase(" padded ",                   "\" padded \"")]
		[TestCase("plain",                      "plain")]
		[TestCase("Pooling=true;Charset=utf8",  "\"Pooling=true;Charset=utf8\"")]
		public void CredentialsCannotAddKeys(string password, string written)
		{
			const string template = "Data Source=ase;Port=5000;Database=tempdb;Uid={0};Password={1};Pooling=false";

			var connectionString = global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials(template, "sa", password, false, out var error)!;

			using (Assert.EnterMultipleScope())
			{
				error.ShouldBeNull();
				connectionString.ShouldBe($"Data Source=ase;Port=5000;Database=tempdb;Uid=sa;Password={written};Pooling=false");

				var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

				builder["Password"].ShouldBe(password);
				builder.ContainsKey("Charset").ShouldBeFalse();
				builder["Pooling"].ShouldBe("false");

				var parameters = DriverParameters(connectionString);

				parameters.GetType().GetProperty("Password")!.GetValue(parameters).ShouldBe(password);
				parameters.GetType().GetProperty("Charset")!.GetValue(parameters).ShouldBeNull();
				parameters.GetType().GetProperty("Pooling")!.GetValue(parameters).ShouldBe(false);
			}
		}

		[TestCase("Uid={0};Password={1};",     "sa", "a'b\"c", "both kinds of quote")]
		[TestCase("Uid={0};Password=\"{1}\";", "sa", "a\"b",   "holds the quote (\")")]
		[TestCase("Uid={0};Password='{1}';",   "sa", "a'b",    "holds the quote (')")]
		[TestCase("Uid={0};Password={1};",     "s;a", "p",     null)]
		[TestCase("Uid={0};Password=\"{1}\";", "sa", "p;q",    null)]
		public void CredentialsThatCannotBeWritten(string template, string user, string password, string? message)
		{
			var connectionString = global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials(template, user, password, false, out var error);

			using (Assert.EnterMultipleScope())
			{
				if (message == null)
				{
					error.ShouldBeNull();

					var builder = new DbConnectionStringBuilder { ConnectionString = connectionString! };

					builder["Uid"].ShouldBe(user);
					builder["Password"].ShouldBe(password);
				}
				else
				{
					connectionString.ShouldBeNull();
					error!.ShouldContain(message);
				}
			}
		}

		// An empty value stays empty, as before: DbConnectionStringBuilder drops the key, AdoNetCore.AseClient stops reading
		// there. What the driver then does not read decides nothing the check relies on (the server's character set is asked
		// after login, and the unpooled route is confirmed by the driver's own parser).
		[Test]
		public void EmptyCredentialStaysEmpty()
		{
			global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials("Uid={0};Password={1};", "sa", "", false, out var error)
				.ShouldBe("Uid=sa;Password=;");
			error.ShouldBeNull();
		}

		// The placeholder must be the whole value of its key: inside a longer value the quotes the CLI adds are not where a
		// parser looks for them, and the value adds keys (live: a --user logged in as sa with Charset and Application Name
		// of its own). The same for template quotes that do not enclose it tightly.
		[TestCase("Data Source=h;Port=5000;Uid=DOMAIN\\{0};Password={1};Database=d")]
		[TestCase("Data Source=h;Port=5000;Uid=s{0};Password={1};Database=d")]
		[TestCase("Data Source=h;Port=5000;Uid={0}@srv;Password={1};Database=d")]
		[TestCase("Data Source=h;Uid={0};Password=pre{1};")]
		[TestCase("Data Source=h;Uid={0};Password=\" {1}\";")]
		[TestCase("Data Source=h;Uid={0};Password=\"{1} \";")]
		[TestCase("Data Source=h;Uid={0};Password=' {1}';")]
		[TestCase("Data Source=h;Uid={0};Password={{{1}}};")]
		[TestCase("Data Source=h;Uid={0};{1}=x;")]
		[TestCase("Data Source=h;Uid={0};Password=a={1};")]
		[TestCase("Data Source=h;Uid={0};{1}")]
		[TestCase("Data Source=h;Uid={0:x};Password={1};")]
		public void PlaceholderMustBeTheWholeValue(string template)
		{
			var result = global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials(template, "x;Charset=iso_1;Z=a", "pw;Application Name=EVIL", false, out var error);

			using (Assert.EnterMultipleScope())
			{
				result.ShouldBeNull();
				error.ShouldNotBeNull();
			}
		}

		[TestCase("Uid={0};Password={1};",       "Uid=\"u;x\";Password=\"p;q\";")]
		[TestCase("Uid = {0} ;Password=\"{1}\";", "Uid = \"u;x\" ;Password=\"p;q\";")]
		[TestCase("Uid={0};Password='{1}'",      "Uid=\"u;x\";Password='p;q'")]
		[TestCase("Application Name=\"a;b={{0}}\";Uid={0};Password={1}", "Application Name=\"a;b={0}\";Uid=\"u;x\";Password=\"p;q\"")]
		public void PlaceholderAsTheWholeValue(string template, string expected)
		{
			global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials(template, "u;x", "p;q", false, out var error)
				.ShouldBe(expected, error);
		}

		// ODBC quotes with {...} and doubles a closing brace; " and ' are ordinary characters there, so a template quote
		// encloses nothing and is refused. System.Data.Odbc's own parser reads each value as one.
		[TestCase("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq=/x.accdb;Uid=admin;Pwd={1};", "a;Dbq=/evil.accdb", "Pwd={a;Dbq=/evil.accdb};")]
		[TestCase("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq=/x.accdb;Uid=admin;Pwd={1};", "{a;Dbq=/evil}",     "Pwd={{a;Dbq=/evil}}};")]
		[TestCase("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq=/x.accdb;Uid=admin;Pwd={1};", "a}b;c",             "Pwd={a}}b;c};")]
		[TestCase("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq=/x.accdb;Uid=admin;Pwd={{{1}}};", "a}b;c",         "Pwd={a}}b;c};")]
		[TestCase("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq=/x.accdb;Uid=admin;Pwd={1};", "plain",             "Pwd=plain;")]
		public void OdbcValuesQuotedWithBraces(string template, string password, string expectedTail)
		{
			var connectionString = global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials(template, "admin", password, true, out var error)!;

			using (Assert.EnterMultipleScope())
			{
				error.ShouldBeNull();
				connectionString.ShouldEndWith(expectedTail);

				// The ODBC builder keeps a braced value as written, braces included; the driver manager unquotes it. What
				// counts here: Pwd is one value and no key was added.
				var builder = new System.Data.Odbc.OdbcConnectionStringBuilder(connectionString);

				builder["Pwd"].ShouldBe(expectedTail.Substring(4, expectedTail.Length - 5));
				builder["Dbq"].ShouldBe("/x.accdb");
				builder.Count.ShouldBe(4);
			}
		}

		[TestCase("Driver={{x}};Pwd=\"{1}\";")]
		[TestCase("Driver={{x}};Pwd='{1}';")]
		public void OdbcTemplateQuotesRefused(string template)
		{
			global::LinqToDB.CommandLine.Commands.Connection.ConnectionSettingsResolver.FormatCredentials(template, "u", "a;b", true, out var error).ShouldBeNull();
			error.ShouldNotBeNull();
		}

		static object DriverParameters(string connectionString)
		{
			var parse = typeof(AdoNetCore.AseClient.AseConnection).Assembly.GetType("AdoNetCore.AseClient.Internal.ConnectionParameters")!.GetMethod("Parse")!;

			return parse.Invoke(null, [connectionString])!;
		}

		// The driver's own reading of Pooling (an internal type of AdoNetCore.AseClient).
		static bool DriverReadsPooling(string connectionString)
		{
			var parse      = typeof(AdoNetCore.AseClient.AseConnection).Assembly.GetType("AdoNetCore.AseClient.Internal.ConnectionParameters")!.GetMethod("Parse")!;
			var parameters = parse.Invoke(null, [connectionString])!;

			return (bool)parameters.GetType().GetProperty("Pooling")!.GetValue(parameters)!;
		}

		// The executor opens the call's connection with the routed options, not with the ones it was given.
		[TestCase(false, "select a into #t from t\nselect * from #t", "false")]
		[TestCase(true,  "update t set a = 1",                       "false")]
		[TestCase(false, "select * from t",                          "true")]
		public async Task ExecutorOpensTheRoutedConnection(bool execute, string sql, string pooling)
		{
			var mode     = execute ? QueryExecutionMode.Execute : QueryExecutionMode.Query;
			var provider = SybaseTools.GetDataProvider(SybaseProvider.DataAction);
			var opening  = new OpeningRecorder();
			var options  = new DataOptions().UseConnectionString(provider, "Data Source=ase;Port=5000;Pooling=true;").UseInterceptor(opening);
			var settings = new QueryExecutionSettings(
				"ase", "Sybase.Managed", null, null, null, "Data Source=ase;Port=5000;Pooling=true;", null, null, 0, null, "json", null, false,
				mode, execute, TextWriter.Null, false, default, sql);

			await Assert.ThatAsync(
				() => new QueryExecutionExecutor(settings).ExecuteValidatedDatabaseLoop(options, provider, sql, TextWriter.Null, CancellationToken.None),
				Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(OpeningRecorder.StopMessage));

			opening.ConnectionString.ShouldNotBeNull();
			new DbConnectionStringBuilder { ConnectionString = opening.ConnectionString }["Pooling"].ShouldBe(pooling);
		}

		// The server's character set is known only after login, so a text outside ASCII on ASE opens the connection (and asks
		// the server, live in the report) whatever the connection string says; a text the check refuses for another reason
		// opens nothing.
		[TestCase("Data Source=ase;Port=5000;",              "select '\u00E9' a",          true)]
		[TestCase("Data Source=ase;Port=5000;Charset=utf8;", "select '\u00E9' a",          true)]
		[TestCase("Data Source=ase;Port=5000;Charset=utf8;", "select '\u00E9' a delete t", false)]
		public async Task ExecutorAsksTheServerForItsCharset(string connectionString, string sql, bool opens)
		{
			var provider = SybaseTools.GetDataProvider(SybaseProvider.DataAction);
			var opening  = new OpeningRecorder();
			var options  = new DataOptions().UseConnectionString(provider, connectionString).UseInterceptor(opening);
			var settings = new QueryExecutionSettings(
				"ase", "Sybase.Managed", null, null, null, connectionString, null, null, 0, null, "json", null, false,
				QueryExecutionMode.Query, false, TextWriter.Null, false, default, sql);
			var executor = new QueryExecutionExecutor(settings);

			if (opens)
			{
				await Assert.ThatAsync(
					() => executor.ExecuteValidatedDatabaseLoop(options, provider, sql, TextWriter.Null, CancellationToken.None),
					Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(OpeningRecorder.StopMessage));
			}
			else
			{
				var result = await executor.ExecuteValidatedDatabaseLoop(options, provider, sql, TextWriter.Null, CancellationToken.None);

				result.Error!.ShouldContain("statement word DELETE");
				opening.ConnectionString.ShouldBeNull();
			}
		}

		// Records the connection string a connection opens with, and stops the call before it reaches a server.
		sealed class OpeningRecorder : ConnectionInterceptor
		{
			public const string StopMessage = "Stopped before the server.";

			public string? ConnectionString { get; private set; }

			public override void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
			{
				ConnectionString = connection.ConnectionString;
				throw new InvalidOperationException(StopMessage);
			}

			public override Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				ConnectionString = connection.ConnectionString;
				throw new InvalidOperationException(StopMessage);
			}
		}

		#endregion

		#region Independent T-SQL corpus

		// Every statement form below was put after a query as "select 1 <form>" and as "select * from t" + LF + "<form>",
		// and each text was read once by Microsoft's T-SQL parser (ScriptDom), which no longer ships with the CLI. The file
		// keeps that parser's verdicts: "split" is a SELECT and a statement of another kind, "selects" two SELECTs, "one"
		// a single statement and "error" a text it refused.
		static IEnumerable<(string Verdict, string Sql)> ReadCorpus(string file)
		{
			var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "ReadOnlySql", file);

			foreach (var line in File.ReadLines(path))
			{
				if (line.Length == 0 || line.StartsWith('#'))
					continue;

				var parts = line.Split('\t', 3);

				yield return (parts[0], "select 1 " + parts[2]);
				yield return (parts[1], "select * from t\n" + parts[2]);
			}
		}

		[Test]
		public void TSqlCorpusSecondStatementsRefused()
		{
			var rows = ReadCorpus("TSqlStatements.txt").ToList();

			rows.Count(r => r.Verdict == "split").ShouldBeGreaterThan(400);

			using (Assert.EnterMultipleScope())
			{
				foreach (var (verdict, sql) in rows)
				{
					if (verdict is "split")
						Query(TSql, sql).ShouldBeFalse(sql);

					if (verdict is "split" or "selects")
						Single(TSql, sql).ShouldBeFalse(sql);
				}
			}
		}

		// Sybase ASE: the commands chapter of the ASE reference, each of which must be refused after a query.
		[Test]
		public void AseCommandsRefused()
		{
			var rows = ReadCorpus("AseCommands.txt").ToList();

			rows.Count.ShouldBeGreaterThan(150);

			using (Assert.EnterMultipleScope())
			{
				foreach (var (_, sql) in rows)
					Query(Ase, sql).ShouldBeFalse(sql);
			}
		}

		#endregion

		#region Messages and routing

		[TestCase("SqlServer",        "select 1 delete t",                      "Query is not read-only: statement word DELETE at 1:10.")]
		[TestCase("SqlServer.2019",   "select a\n  into x from t",              "Query is not read-only: INTO at 2:3.")]
		[TestCase("SqlServer",        "select * from openquery(s, 'x')",        "Query is not read-only: function OPENQUERY at 1:15 is on the write-route list.")]
		[TestCase("SqlServer",        "select dbms_pipe.send_message('p')",     "Query is not read-only: DBMS_PIPE at 1:8 is on the write-route list.")]
		[TestCase("SqlServer",        "select next value for s",                "Query is not read-only: sequence call NEXT VALUE FOR at 1:8.")]
		[TestCase("SqlServer",        "SET XACT_ABORT ON\nselect 1",            "Query is not read-only: session option XACT_ABORT at 1:1 is not allowed.")]
		[TestCase("Sybase.Managed",   "SET ROWCOUNT 5\nselect 1",               "Query is not read-only: session option ROWCOUNT at 1:1 is not allowed.")]
		[TestCase("SqlServer",        "SET @p = (select 1)\nselect 1",          "Parameter assignment SET @p at 1:1 is not a plain value.")]
		[TestCase("SqlCe",            "DECLARE @t TABLE (a int)\nselect 1",     "Query is not read-only: parameter declaration DECLARE @t at 1:1 is not allowed.")]
		[TestCase("SqlServer",        "DECLARE @c CURSOR\nselect 1",            "Query is not read-only: parameter declaration DECLARE @c at 1:1 is not allowed.")]
		[TestCase("SqlServer",        "update t set a = 1\nselect * from t",    "Query is not read-only: statement word UPDATE at 1:1.")]
		[TestCase("SqlServer",        "select a into t2 from t\nselect 1",      "Query is not read-only: INTO at 1:10.")]
		[TestCase("Sybase.Managed",   "insert ##g values (1)\nselect 1",        "Query is not read-only: statement word INSERT at 1:1.")]
		[TestCase("SqlServer",        "update #t set a = 1 from t #t\nselect 1", "Only SELECT queries are allowed: unexpected '#t' at 1:28. In a script that writes temporary tables, a name that starts with # may not stand where an alias does.")]
		[TestCase("SqlServer",        "create table ##g (a int)\nselect 1",     "Query is not read-only: statement word CREATE at 1:1.")]
		[TestCase("SqlServer",        "select a into #t from t\nupdate #t set a = 1 from t [\uFF03t]\nselect 1", "Only SELECT queries are allowed: unexpected '[\uFF03t]' at 2:28. In a script that writes temporary tables, a quoted name must begin with an ASCII letter, a digit, _ or $.")]
		[TestCase("SqlServer",        "declare @v table (a int)\nupdate @v set a = 1 from t \u0640@v\nselect 1", "Only SELECT queries are allowed: character U+0640 at 2:28. A script that writes temporary tables accepts non-ASCII characters only inside string literals ('...', N'...') and comments; names, quoted names included, must be ASCII.")]
		[TestCase("Sybase.Managed",   "select a into #t from t\nupdate #t set a = 1 from t \u3007#t\nselect 1",   "Only SELECT queries are allowed: character U+3007 at 2:28. A script that writes temporary tables accepts non-ASCII characters only")]
		[TestCase("Sybase.Managed",   "select 1 a --\r'\nupdate t set a = 3 --'",   "Query is not read-only: statement word UPDATE at 2:1.")]
		[TestCase("SqlServer",        "select a into #t from t -- \U0001F600\nselect [\U0001F600] from #t", "Only SELECT queries are allowed: character U+1F600 at 2:9.")]
		[TestCase("SqlServer",        "select a into #t from t\nupdate #t set a = 1 from t\u200B#t\nselect 1", "Only SELECT queries are allowed: unexpected '#t' at 2:28. In a script that writes temporary tables, a name that starts with # may not stand where an alias does.")]
		[TestCase("SqlServer",        "exec dbo.DoWork",                        "Query is not read-only: statement word EXEC at 1:1.")]
		[TestCase("SqlServer",        "select 1 /* open",                       "Query is not valid SQL")]
		[TestCase("Sybase.Managed",   "select open from t",                     "Query is not read-only: statement word OPEN at 1:8.")]
		[TestCase("SqlCe",            "select [open] from t delete t",          "Query is not read-only: statement word DELETE at 1:22.")]
		[TestCase("SqlCe",            "call p()",                               "Only SELECT queries are allowed: unexpected 'call' at 1:1.")]
		[TestCase("SqlCe",            "select 1 /* a /* b */ c */ from t",      "Query is not accepted: the comment at 1:10 holds /*; databases that nest comments")]
		[TestCase("SqlServer",        "select 1 a /*! ' */ update t set a = 1 --'", "Query is not read-only: statement word UPDATE at 1:21.")]
		[TestCase("SqlServer",        "select q'[', 1 b from (select 1 q) z update t set a = 7 --]'", "Query is not read-only: statement word UPDATE at 1:38.")]
		[TestCase("SqlServer",        "select E'\\', 1 b from (select 1 E) z update t set a = 8 --'", "Only SELECT queries are allowed: unexpected")]
		public void QueryMessages(string provider, string sql, string message)
		{
			var result = ReadOnlySqlGuard.Validate(provider, sql);

			using (Assert.EnterMultipleScope())
			{
				result.IsAllowed.ShouldBeFalse();
				result.Error!.   ShouldContain(message);
			}
		}

		[TestCase("SqlServer", "select 1 select 2",                 "Only single SQL statement is allowed: more than one statement at 1:10.")]
		[TestCase("SqlServer", "update t set a = 1\ndelete u",      "Only single SQL statement is allowed: more than one statement at 2:1.")]
		[TestCase("SqlCe",     "update t set a = 1; delete u",      "Only single SQL statement is allowed: more than one statement at 1:19.")]
		[TestCase("Sybase",    "select 'unterminated",              "Query is not valid SQL")]
		public void SingleStatementMessages(string provider, string sql, string message)
		{
			var result = ReadOnlySqlGuard.ValidateSingleStatement(provider, sql);

			using (Assert.EnterMultipleScope())
			{
				result.IsAllowed.ShouldBeFalse();
				result.Error!.   ShouldContain(message);
			}
		}

		[Test]
		public void ProvidersMapToTheirClass()
		{
			using (Assert.EnterMultipleScope())
			{
				ReadOnlySqlGuard.Validate("SqlServer.2022.MS", "select [delete] from t").IsAllowed.ShouldBeTrue();
				ReadOnlySqlGuard.Validate("SqlServer.2022.MS", "select open from t").    IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.Validate("Sybase",            "select open from t").    IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.Validate("Sybase.Managed",    "SET NOCOUNT ON select 1").IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.Validate("SqlServer",         "SET NOCOUNT ON select 1").IsAllowed.ShouldBeTrue();
				ReadOnlySqlGuard.Validate("SqlCe",             "select open from t").    IsAllowed.ShouldBeTrue();
				ReadOnlySqlGuard.Validate("SqlCe",             "select [delete] from t").IsAllowed.ShouldBeTrue();

				// Providers without a grammar class keep the generic validator for now.
				ReadOnlySqlGuard.Validate("SQLite", "select 1 # x").IsAllowed.ShouldBeFalse();
				ReadOnlySqlGuard.Validate("SQLite", "select 1 # x").Error!.ShouldContain("ambiguous SQL syntax");
			}
		}

		#endregion

		#region Threads and depth

		[Test]
		public void ParallelChecksAgree()
		{
			var texts = new List<string>();

			foreach (var name in _routeFunctions)
			{
				texts.Add($"select {name}(1)");
				texts.Add($"select {name} from t");
			}

			texts.AddRange(_sessionOptions.Select(o => o + "\nselect 1"));
			texts.Add("select " + new string('(', 3000) + "1" + new string(')', 3000));

			var expected = texts.Select(t => _classes.Select(c => Query(c, t)).ToArray()).ToArray();
			var mismatch = 0;

			Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
			{
				for (var repeat = 0; repeat < 5; repeat++)
				{
					for (var i = 0; i < texts.Count; i++)
					{
						for (var c = 0; c < _classes.Length; c++)
						{
							if (Query(_classes[c], texts[i]) != expected[i][c])
								System.Threading.Interlocked.Increment(ref mismatch);
						}
					}
				}
			});

			mismatch.ShouldBe(0);
		}

		[Test]
		public void NestingIsReadInLinearTime()
		{
			static string Nested(int depth)
			{
				return "select " + new string('(', depth) + "1" + new string(')', depth);
			}

			static double Time(string sql)
			{
				var watch = Stopwatch.StartNew();

				ReadOnlySqlGrammar.TryCheckTSql(sql, out _).ShouldBeTrue();

				return watch.Elapsed.TotalMilliseconds;
			}

			var shallow     = Nested(1_000);
			var deep        = Nested(10_000);
			var bestShallow = double.MaxValue;
			var bestDeep    = double.MaxValue;

			// The two depths are timed in turn, so that both are measured at the same stage of tiered compilation.
			for (var i = 0; i < 40; i++)
			{
				bestShallow = Math.Min(bestShallow, Time(shallow));
				bestDeep    = Math.Min(bestDeep,    Time(deep));
			}

			(bestDeep / bestShallow).ShouldBeLessThanOrEqualTo(15);

			ReadOnlySqlGrammar.TryCheckTSql(deep + ")", out _).ShouldBeFalse();
		}

		[Test]
		public void NestingBeyondTheStackLimitIsRefusedCleanly()
		{
			// The same grammar compiled with one extra stack, so that a modest depth runs out of them.
			var shallow = new ReadOnlySqlGuard.SqlDialect(
				ShallowReadOnlySqlGrammar.TryCheckTSql,                     sql => ShallowReadOnlySqlGrammar.TryCheckTSql (sql).Position,
				sql => ShallowReadOnlySqlGrammar.TrySingleTSql(sql, out _), sql => ShallowReadOnlySqlGrammar.TrySingleTSql(sql).Position);

			var sql    = "select " + new string('(', 1_000_000) + "1" + new string(')', 1_000_000);
			var result = ReadOnlySqlGuard.Validate(shallow, sql, false);

			using (Assert.EnterMultipleScope())
			{
				result.IsAllowed.ShouldBeFalse();
				result.Error.    ShouldBe("Query is nested too deeply to be checked.");
			}
		}

		#endregion
	}
}
