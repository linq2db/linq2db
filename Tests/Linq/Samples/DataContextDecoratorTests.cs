using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

using NUnit.Framework;

namespace Tests.Samples
{
	/// <summary>
	/// This sample demonstrates how can we use <see cref="IDataContext"/> decoration
	/// to deal with different <see cref="MappingSchema"/> objects in one <see cref="DbConnection"/>.
	/// </summary>
	[TestFixture]
	public class DataContextDecoratorTests
	{
		public class DataContextDecorator : IDataContext
		{
			readonly IDataContext _context;

			public DataContextDecorator(IDataContext context, MappingSchema mappingSchema)
			{
				_context      = context;
				MappingSchema = mappingSchema;
			}

			public string                          ContextName           => _context.ContextName;
			public int                             ConfigurationID       => _context.ConfigurationID;
			public Func<DataOptions,ISqlOptimizer> GetSqlOptimizer       => _context.GetSqlOptimizer;
			public Type                            DataReaderType        => _context.DataReaderType;
			public Func<ISqlBuilder>               CreateSqlProvider     => _context.CreateSqlProvider;
			public List<string>                    NextQueryHints        => _context.NextQueryHints;
			public List<string>                    QueryHints            => _context.QueryHints;
			public SqlProviderFlags                SqlProviderFlags      => _context.SqlProviderFlags;
			public TableOptions                    SupportedTableOptions => _context.SupportedTableOptions;
			public string?                         ConfigurationString   => _context.ConfigurationString;

			public MappingSchema MappingSchema { get; }
			public bool          CloseAfterUse { get; set; }

			public bool InlineParameters
			{
				get => _context.InlineParameters;
				set => _context.InlineParameters = value;
			}

			public IDataContext Clone(bool forNestedQuery)
			{
				return _context.Clone(forNestedQuery);
			}

			public void Close()
			{
				_context.Close();
			}

			public Task CloseAsync()
			{
				return _context.CloseAsync();
			}

			public void Dispose()
			{
				_context.Dispose();
			}

			public ValueTask DisposeAsync()
			{
				return _context.DisposeAsync();
			}

			public IQueryRunner GetQueryRunner(Query query, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
			{
				return _context.GetQueryRunner(query, queryNumber, expression, parameters, preambles);
			}

			public DataOptions Options => _context.Options;

			public Expression GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
			{
				return _context.GetReaderExpression(reader, idx, readerExpression, toType);
			}

			public bool? IsDBNullAllowed(DbDataReader reader, int idx)
			{
				return _context.IsDBNullAllowed(reader, idx);
			}

			public void AddInterceptor   (IInterceptor interceptor) => _context.AddInterceptor(interceptor);
			public void RemoveInterceptor(IInterceptor interceptor) => _context.RemoveInterceptor(interceptor);

			public IUnwrapDataObjectInterceptor? UnwrapDataObjectInterceptor { get; }
		}

		public class Entity
		{
			public int     Id;
			public string? Name;
		}

//		[Test]
		public void Sample()
		{
			using (var db = new DataConnection())
			{
				var ms = new MappingSchema();
				var b  = new FluentMappingBuilder(ms);
				using var dc = new DataContextDecorator(db, ms);

				b.Entity<Entity>()
					.Property(_ => _.Id  ).HasColumnName("EntityId")
					.Property(_ => _.Name).HasColumnName("EntityName")
					.Build();

				var q1 = db.GetTable<Entity>().Select(_ => _).ToString();
				var q2 = dc.GetTable<Entity>().Select(_ => _).ToString()!;

				Assert.That(q2, Is.Not.EqualTo(q1));
				Assert.That(q2, Does.Contain("EntityId"));
				Assert.That(q2, Does.Contain("EntityName"));
			}
		}
	}
}
