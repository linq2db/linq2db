using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class UniqueParametersNormalizerTests : TestBase
	{
		// Test sending a few unique strings
		[Test]
		public void NormalizeUniqueStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var uniqueStrings = new[] { "test1", "test2", "test3" };

			foreach (var str in uniqueStrings)
			{
				var normalizedStr = normalizer.Normalize(str);
				Assert.That(normalizedStr, Is.EqualTo(str));
			}
		}

		// Test sending some duplicated strings (and validate that capitalization is ignored by default)
		[Test]
		public void NormalizeDuplicatedStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var duplicatedStrings = new[] {
				"test", "test", "TEST", "hello", "hello", "HELLO",
				"test", "test", "test", "test", "test", "test", "test", "test",
			};
			var expectedStrings = new[] {
				"test", "test_1", "TEST_2", "hello", "hello_1", "HELLO_2",
				"test_3", "test_4", "test_5", "test_6", "test_7", "test_8", "test_9", "test_10",
			};

			for (int i = 0; i < duplicatedStrings.Length; i++)
			{
				var normalizedStr = normalizer.Normalize(duplicatedStrings[i]);
				Assert.That(normalizedStr, Is.EqualTo(expectedStrings[i]));
			}
		}

		// Test sending a few unique strings that are 52 characters long
		[Test]
		public void NormalizeUniqueLongStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var uniqueLongStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"bcdefghijklmnopqrstuvwxyz12345678901234567890abcdefg",
				"cdefghijklmnopqrstuvwxyz12345678901234567890abcdefgh",
			};

			var expectedStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcd",
				"bcdefghijklmnopqrstuvwxyz12345678901234567890abcde",
				"cdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
			};

			for (int i = 0; i < uniqueLongStrings.Length; i++)
			{
				var normalizedStr = normalizer.Normalize(uniqueLongStrings[i]);
				Assert.That(normalizedStr, Is.EqualTo(expectedStrings[i]));
			}
		}

		// Test sending some duplicated strings that are 52 characters long
		[Test]
		public void NormalizeDuplicatedLongStrings()
		{
			var normalizer = new UniqueParametersNormalizer();
			var duplicatedLongStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcdef",
			};
			var expectedStrings = new[]
			{
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abcd",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890abc",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_1",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_2",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_3",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_4",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_5",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_6",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_7",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_8",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890ab_9",
				"abcdefghijklmnopqrstuvwxyz12345678901234567890a",
			};

			for (int i = 0; i < duplicatedLongStrings.Length; i++)
			{
				var normalizedStr = normalizer.Normalize(duplicatedLongStrings[i]);
				Assert.That(normalizedStr, Is.EqualTo(expectedStrings[i]));
			}
		}

		// Test sending "abcd" string 23 times and expecting specific responses
		[Test]
		public void NoInfiniteLoop()
		{
			var normalizer = new TestNormalizer(3);
			var inputString = "abcd";
			var expectedStrings = new[]
			{
				"abc", "ab", "a", "a_1", "a_2", "a_3", "a_4", "a_5", "a_6", "a_7", "a_8", "a_9",
				"p", "p_1", "p_2", "p_3", "p_4", "p_5", "p_6", "p_7", "p_8", "p_9",
			};

			for (int i = 0; i < 22; i++)
			{
				var normalizedStr = normalizer.Normalize(inputString);
				Assert.That(normalizedStr, Is.EqualTo(expectedStrings[i]));
			}

			// Expect an InvalidOperationException when sending "abcd" an additional time
			Assert.Throws<InvalidOperationException>(() => normalizer.Normalize(inputString));
		}

		// Test sending strings with a variety of uppercase, lowercase, numbers, $ and _ characters
		[TestCase("1ABCD", "ABCD")]
		[TestCase("$abcd", "abcd")]
		[TestCase("AB1cd", "AB1cd")]
		[TestCase("AbC$", "AbC")]
		[TestCase("$$ABcD", "ABcD")]
		[TestCase("abc$$", "abc")]
		[TestCase("!@#%^&*()ABcd", "ABcd")]
		[TestCase("abC!@#%^&*()", "abC")]
		[TestCase("$!@#%^&*()aBcD", "aBcD")]
		[TestCase("Abc$!@#%^&*()", "Abc")]
		[TestCase("123", "p")]
		[TestCase("!@#%^&*()", "p")]
		[TestCase("$1$2$3", "p")]
		[TestCase("$!@#%^&*()", "p")]
		[TestCase("A1", "A1")]
		[TestCase("!", "p")]
		[TestCase("$", "p")]
		[TestCase("_ab", "ab")]
		[TestCase("_1ab", "ab")]
		[TestCase("_", "p")]
		[TestCase("AB_CD", "AB_CD")]
		[TestCase("ab_1_cd", "ab_1_cd")]
		[TestCase("$ab_cd$", "ab_cd")]
		[TestCase("_ab_cd_", "ab_cd_")]
		public void NormalizeSpecialCharacters(string input, string expected)
		{
			var normalizer = new UniqueParametersNormalizer();
			var normalizedStr = normalizer.Normalize(input);
			Assert.That(normalizedStr, Is.EqualTo(expected));
		}

		//Test normalizing a string that does not fit on the stack
		[Test]
		public void NormalizeVeryLongString()
		{
			var input = new string('a', 600) + "$" + new string('b', 600);
			var normalizer = new TestNormalizer(int.MaxValue);
			var actual = normalizer.Normalize(input);
			var expected = new string('a', 600) + new string('b', 600);
			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase("")]
		[TestCase(null)]
		public void DefaultName(string? input)
		{
			Assert.That(new UniqueParametersNormalizer().Normalize(input), Is.EqualTo("p"));
		}

		//Test with invalid properties; results are undefined, but just ensure there is no infinite loop or stack overflow
		[TestCase(0, "p")]
		[TestCase(-1, "p")]
		[TestCase(50, "")]
		[TestCase(50, null)]
		[TestCase(1, "")]
		[TestCase(1, null)]
		public void InvalidProperties(int maxLength, string? defaultName)
		{
			var normalizer = new TestNormalizer(maxLength, defaultName!);
			try
			{
				// specify null to use default name
				normalizer.Normalize(null);
				normalizer.Normalize(null);
			}
			catch // note: stack overflow exceptions cannot be caught
			{
			}
		}

		private class TestNormalizer : UniqueParametersNormalizer
		{
			private readonly int _maxLength;
			private readonly string _defaultName;
			public TestNormalizer(int maxLength, string defaultName = "p")
			{
				_maxLength = maxLength;
				_defaultName = defaultName;
			}
			protected override int MaxLength => _maxLength;
			protected override string DefaultName => _defaultName;
		}

		// Ensures that "search" is always passed to IQueryParametersNormalizer.Normalize as the originalName
		[Test]
		public async Task CalledWithCorrectNames([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context, o => o.UseDataProvider(new WrapperProvider(GetDataProvider(context), (normalizerBase) => new ValidateOriginalNameNormalizer(normalizerBase))));

			await using var dbTable1 = db.CreateLocalTable<Table1>("table1");
			await using var dbTable2 = db.CreateLocalTable<Table2>("table2");
			await using var dbTable3 = db.CreateLocalTable<Table3>("table3");

			var query1 = GenerateQuery("test");
			_ = await query1.ToListAsync();

			IQueryable<int> GenerateQuery(string search) =>
				(
					from row1 in dbTable1
					join row2 in dbTable2 on row1.Id equals row2.Table1Id
					where row2.Field2.StartsWith(search)
					select row1.Id)
				.Union(
					from row1 in dbTable1
					join row3 in dbTable3 on row1.Id equals row3.Table1Id
					where row3.Field3 == search
					select row1.Id)
				.Union(
					from row1 in dbTable1
					where row1.Field1!.StartsWith(search)
					select row1.Id);
		}

		// Ensures that subsequent executions of the same query execute the same SQL
		[Test]
		public async Task ExecutesDeterministically([DataSources(false)] string context)
		{
			string? lastSql = null;

			using var db = GetDataConnection(context, o => o.UseTracing(info =>
			{
				if (info.TraceInfoStep == TraceInfoStep.BeforeExecute)
				{
					lastSql = info.SqlText;
				}

				DataConnection.DefaultOnTraceConnection(info);
			}));

			await using var dbTable1 = db.CreateLocalTable<Table1>("table1");
			await using var dbTable2 = db.CreateLocalTable<Table2>("table2");
			await using var dbTable3 = db.CreateLocalTable<Table3>("table3");

			var query1 = GenerateQuery("test");
			_ = await query1.ToListAsync();
			var sql1 = lastSql;
			lastSql = null;

			var query2 = GenerateQuery("test"); // should execute identical SQL
			_ = await query2.ToListAsync();
			var sql2 = lastSql;

			Assert.That(sql2, Is.EqualTo(sql1));

			IQueryable<int> GenerateQuery(string search) =>
				(
					from row1 in dbTable1
					join row2 in dbTable2 on row1.Id equals row2.Table1Id
					where row2.Field2.StartsWith(search)
					select row1.Id)
				.Union(
					from row1 in dbTable1
					join row3 in dbTable3 on row1.Id equals row3.Table1Id
					where row3.Field3 == search
					select row1.Id)
				.Union(
					from row1 in dbTable1
					where row1.Field1!.StartsWith(search)
					select row1.Id);
		}

		/// <summary>
		/// Validates that the 'originalName' passed to <see cref="IQueryParametersNormalizer.Normalize(string?)"/> is "search".
		/// </summary>
		private class ValidateOriginalNameNormalizer : IQueryParametersNormalizer
		{
			private readonly IQueryParametersNormalizer _normalizerBase;

			public ValidateOriginalNameNormalizer(IQueryParametersNormalizer normalizerBase)
			{
				_normalizerBase = normalizerBase;
			}

			public string? Normalize(string? originalName)
			{
				if (originalName != "search")
					throw new InvalidOperationException($"Expected originalName is 'search' but instead was '{originalName}'.");
				return _normalizerBase.Normalize(originalName);
			}
		}

		/// <summary>
		/// Wraps another <see cref="IDataProvider"/> instance, overriding only the <see cref="IDataProvider.GetQueryParameterNormalizer"/> function.
		/// </summary>
		private class WrapperProvider : IDataProvider, IInfrastructure<IServiceProvider>
		{
			private readonly IDataProvider _baseProvider;
			private readonly Func<IQueryParametersNormalizer, IQueryParametersNormalizer> _normalizerFactory;
			public WrapperProvider(IDataProvider baseProvider, Func<IQueryParametersNormalizer, IQueryParametersNormalizer> normalizerFactory)
			{
				_baseProvider = baseProvider;
				_normalizerFactory = normalizerFactory;
			}

			public string Name => _baseProvider.Name;
			public int ID => _baseProvider.ID;
			public string? ConnectionNamespace => _baseProvider.ConnectionNamespace;
			public Type DataReaderType => _baseProvider.DataReaderType;
			public MappingSchema MappingSchema => _baseProvider.MappingSchema;
			public SqlProviderFlags SqlProviderFlags => _baseProvider.SqlProviderFlags;
			public TableOptions SupportedTableOptions => _baseProvider.SupportedTableOptions;
			public bool TransactionsSupported => _baseProvider.TransactionsSupported;
			public BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source) where T : notnull => _baseProvider.BulkCopy(options, table, source);
			public Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table, IEnumerable<T> source, CancellationToken cancellationToken) where T : notnull => _baseProvider.BulkCopyAsync(options, table, source, cancellationToken);
			public Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table, IAsyncEnumerable<T> source, CancellationToken cancellationToken) where T : notnull => _baseProvider.BulkCopyAsync(options, table, source, cancellationToken);
			public Type ConvertParameterType(Type type, DbDataType dataType) => _baseProvider.ConvertParameterType(type, dataType);
			public DbConnection CreateConnection(string connectionString) => _baseProvider.CreateConnection(connectionString);
			public ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions) => _baseProvider.CreateSqlBuilder(mappingSchema, dataOptions);
			public void DisposeCommand(DbCommand command) => _baseProvider.DisposeCommand(command);
			public ValueTask DisposeCommandAsync(DbCommand command) => _baseProvider.DisposeCommandAsync(command);
			public IExecutionScope? ExecuteScope(DataConnection dataConnection) => _baseProvider.ExecuteScope(dataConnection);
			public CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior) => _baseProvider.GetCommandBehavior(commandBehavior);
			// TODO: Remove in v7
			[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
			public object? GetConnectionInfo(DataConnection dataConnection, string parameterName) => _baseProvider.GetConnectionInfo(dataConnection, parameterName);
			public IQueryParametersNormalizer GetQueryParameterNormalizer() => _normalizerFactory(_baseProvider.GetQueryParameterNormalizer());
			public Expression GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType) => _baseProvider.GetReaderExpression(reader, idx, readerExpression, toType);
			public ISchemaProvider GetSchemaProvider() => _baseProvider.GetSchemaProvider();
			public ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _baseProvider.GetSqlOptimizer(dataOptions);
			public DbCommand InitCommand(DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters) => _baseProvider.InitCommand(dataConnection, command, commandType, commandText, parameters, withParameters);
			public void InitContext(IDataContext dataContext) => _baseProvider.InitContext(dataContext);
			public bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx) => _baseProvider.IsDBNullAllowed(options, reader, idx);
			public void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value) => _baseProvider.SetParameter(dataConnection, parameter, name, dataType, value);

			public IServiceProvider Instance => ((IInfrastructure<IServiceProvider>)_baseProvider).Instance;
		}

		public class Table1
		{
			public int Id { get; set; }
			public string Field1 { get; set; } = null!;
		}

		public class Table2
		{
			public int Table1Id { get; set; }
			public string Field2 { get; set; } = null!;
		}

		public class Table3
		{
			public int Table1Id { get; set; }
			public string Field3 { get; set; } = null!;
		}
	}
}
