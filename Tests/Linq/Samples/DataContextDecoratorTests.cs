using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

using NUnit.Framework;

namespace Tests.Samples
{
	using Model;
	/// <summary>
	/// This sample demonstrates how can we use <see cref="IDataContext"/> decoration
	/// to deal with different <see cref="MappingSchema"/> objects in one <see cref="IDbConnection"/>
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

			public string              ContextID         => _context.ContextID;
			public Func<ISqlOptimizer> GetSqlOptimizer   => _context.GetSqlOptimizer;
			public Type                DataReaderType    => _context.DataReaderType;
			public Func<ISqlBuilder>   CreateSqlProvider => _context.CreateSqlProvider;
			public List<string>        NextQueryHints    => _context.NextQueryHints;
			public List<string>        QueryHints        => _context.QueryHints;
			public SqlProviderFlags    SqlProviderFlags  => _context.SqlProviderFlags;

			public MappingSchema       MappingSchema { get; }
			public bool                CloseAfterUse { get; set; }

			public bool InlineParameters
			{
				get => _context.InlineParameters;
				set => _context.InlineParameters = value;
			}

#pragma warning disable 0067
			public event EventHandler OnClosing;
#pragma warning restore 0067

			public IDataContext Clone(bool forNestedQuery)
			{
				return _context.Clone(forNestedQuery);
			}

			public void Close()
			{
				_context.Close();
			}

			public void Dispose()
			{
				_context.Dispose();
			}

			public IQueryRunner GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
			{
				return _context.GetQueryRunner(query, queryNumber, expression, parameters);
			}

			public Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
			{
				return _context.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
			}

			public bool? IsDBNullAllowed(IDataReader reader, int idx)
			{
				return _context.IsDBNullAllowed(reader, idx);
			}
		}

		public class Entity
		{
			public int    Id;
			public string Name;
		}

//		[Test]
		public void Sample()
		{
			using (var db = new TestDataConnection())
			{
				var ms = new MappingSchema();
				var b  = ms.GetFluentMappingBuilder();
				var dc = new DataContextDecorator(db, ms);

				b.Entity<Entity>()
					.Property(_ => _.Id  ).HasColumnName("EntityId")
					.Property(_ => _.Name).HasColumnName("EntityName");

				var q1 = db.GetTable<Entity>().Select(_ => _).ToString();
				var q2 = dc.GetTable<Entity>().Select(_ => _).ToString();

				Assert.AreNotEqual(q1, q2);
				Assert.That(q2.Contains("EntityId"));
				Assert.That(q2.Contains("EntityName"));
			}
		}
	}
}
