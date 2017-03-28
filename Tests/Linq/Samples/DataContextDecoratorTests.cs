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
			IDataContext  _context;
			MappingSchema _mappingSchema;

			public DataContextDecorator(IDataContext context, MappingSchema mappingSchema)
			{
				_context       = context;
				_mappingSchema = mappingSchema;
			}

			public string ContextID
			{
				get
				{
					return _context.ContextID;
				}
			}

			public Func<ISqlBuilder> CreateSqlProvider
			{
				get
				{
					return _context.CreateSqlProvider;
				}
			}

			public Type DataReaderType
			{
				get
				{
					return _context.DataReaderType;
				}
			}

			public Func<ISqlOptimizer> GetSqlOptimizer
			{
				get
				{
					return _context.GetSqlOptimizer;
				}
			}

			public bool InlineParameters
			{
				get
				{
					return _context.InlineParameters;
				}

				set
				{
					_context.InlineParameters = value;
				}
			}

			public MappingSchema MappingSchema
			{
				get
				{
					return _mappingSchema;
				}
			}

			public List<string> NextQueryHints
			{
				get
				{
					return _context.NextQueryHints;
				}
			}

			public List<string> QueryHints
			{
				get
				{
					return _context.QueryHints;
				}
			}

			public SqlProviderFlags SqlProviderFlags
			{
				get
				{
					return _context.SqlProviderFlags;
				}
			}

#pragma warning disable CS0067  
			public event EventHandler OnClosing;
#pragma warning restore CS0067

			public IDataContext Clone(bool forNestedQuery)
			{
				return _context.Clone(forNestedQuery);
			}

			public void Dispose()
			{
				_context.Dispose();
			}

			public int ExecuteNonQuery(object query)
			{
				return _context.ExecuteNonQuery(query);
			}

			public IDataReader ExecuteReader(object query)
			{
				return _context.ExecuteReader(query);
			}

			public object ExecuteScalar(object query)
			{
				return _context.ExecuteScalar(query);
			}

			public Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
			{
				return _context.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
			}

			public string GetSqlText(object query)
			{
				return _context.GetSqlText(query);
			}

			public bool? IsDBNullAllowed(IDataReader reader, int idx)
			{
				return _context.IsDBNullAllowed(reader, idx);
			}

			public void ReleaseQuery(object query)
			{
				_context.ReleaseQuery(query);
			}

			public object SetQuery(IQueryContext queryContext)
			{
				return _context.SetQuery(queryContext);
			}
		}

		public class Entity
		{
			public int    Id;
			public string Name;
		}

		[Test]
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
