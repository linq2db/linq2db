using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Services;

namespace LinqToDB.ServiceModel
{
	using Common;
	using Data;
	using Expressions;
	using Extensions;
	using Linq;
	using LinqToDB.DataProvider;
	using Mapping;
	using SqlQuery;

	[ServiceBehavior  (InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	[WebService       (Namespace  = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class LinqService : ILinqService
	{
		public LinqService()
		{
		}

		public LinqService(MappingSchema? mappingSchema)
		{
			_mappingSchema = mappingSchema;
		}

		public bool AllowUpdates { get; set; }

		private MappingSchema? _mappingSchema;
		public MappingSchema? MappingSchema
		{
			get => _mappingSchema;
			set
			{
				_mappingSchema = value;
				_serializationMappingSchema = new SerializationMappingSchema(_mappingSchema);
			}
		}

		private  MappingSchema? _serializationMappingSchema;
		internal MappingSchema   SerializationMappingSchema => _serializationMappingSchema ??= new SerializationMappingSchema(_mappingSchema);

		public static Func<string,Type?> TypeResolver = _ => null;

		public virtual DataConnection CreateDataContext(string? configuration)
		{
			return MappingSchema != null ? new DataConnection(configuration, MappingSchema) : new DataConnection(configuration);
		}

		protected virtual void ValidateQuery(LinqServiceQuery query)
		{
			if (AllowUpdates == false && query.Statement.QueryType != QueryType.Select)
				throw new LinqException("Insert/Update/Delete requests are not allowed by the service policy.");
		}

		protected virtual void HandleException(Exception exception)
		{
		}

		#region ILinqService Members

		[WebMethod]
		public virtual LinqServiceInfo GetInfo(string? configuration)
		{
			using var ctx = CreateDataContext(configuration);

			return new LinqServiceInfo
			{
				MappingSchemaType     = ctx.DataProvider.MappingSchema.GetType().AssemblyQualifiedName,
				SqlBuilderType        = ctx.DataProvider.CreateSqlBuilder(ctx.MappingSchema).GetType().AssemblyQualifiedName,
				SqlOptimizerType      = ctx.DataProvider.GetSqlOptimizer().GetType().AssemblyQualifiedName,
				SqlProviderFlags      = ctx.DataProvider.SqlProviderFlags,
				SupportedTableOptions = ctx.DataProvider.SupportedTableOptions
			};
		}

		class QueryContext : IQueryContext
		{
			public SqlStatement    Statement   { get; set; } = null!;
			public object?         Context     { get; set; }
			public SqlParameter[]? Parameters  { get; set; }
			public AliasesContext? Aliases     { get; set; }
			public List<string>?   QueryHints  { get; set; }
		}

		[WebMethod]
		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				return DataConnection.QueryRunner.ExecuteNonQuery(db, new QueryContext
				{
					Statement  = query.Statement,
					QueryHints = query.QueryHints
				}, new SqlParameterValues());
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		[WebMethod]
		public object? ExecuteScalar(string? configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				return DataConnection.QueryRunner.ExecuteScalar(db, new QueryContext
				{
					Statement  = query.Statement,
					QueryHints = query.QueryHints
				}, null);
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		[WebMethod]
		public string ExecuteReader(string? configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);
				using var rd = DataConnection.QueryRunner.ExecuteReader(db, new QueryContext
				{
					Statement  = query.Statement,
					QueryHints = query.QueryHints,
				}, SqlParameterValues.Empty);

				var reader = DataReaderWrapCache.TryUnwrapDataReader(db.MappingSchema, rd.DataReader!);

				var ret = new LinqServiceResult
				{
					QueryID    = Guid.NewGuid(),
					FieldCount = rd.DataReader!.FieldCount,
					FieldNames = new string[rd.DataReader!.FieldCount],
					FieldTypes = new Type  [rd.DataReader!.FieldCount],
					Data       = new List<string[]>(),
				};

				var names = new HashSet<string>();
				var select = query.Statement.QueryType switch
				{
					QueryType.Select => query.Statement.SelectQuery!,
					QueryType.Insert => ((SqlInsertStatement)query.Statement).Output!.OutputQuery!,
					QueryType.Delete => ((SqlDeleteStatement)query.Statement).Output!.OutputQuery!,
					_ => throw new NotImplementedException($"Query type not supported: {query.Statement.QueryType}"),
				};

				for (var i = 0; i < ret.FieldCount; i++)
				{
					var name = rd.DataReader!.GetName(i);
					var idx  = 0;

					if (names.Contains(name))
					{
						while (names.Contains(name = "c" + ++idx))
						{
						}
					}

					names.Add(name);

					ret.FieldNames[i] = name;
					// ugh...
					// still if it fails here due to empty columns - it is a bug in columns generation

					var fieldType = select.Select.Columns[i].SystemType!;

					// async compiled query support
					if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Task<>))
						fieldType = fieldType.GetGenericArguments()[0];


					if (fieldType.IsEnum || fieldType.IsNullable() && fieldType.ToNullableUnderlying().IsEnum)
					{
						var stringConverter = db.MappingSchema.GetConverter(new DbDataType(typeof(string)), new DbDataType(fieldType), false);
						if (stringConverter != null)
							fieldType = typeof(string);
						else
						{
							var type = Converter.GetDefaultMappingFromEnumType(db.MappingSchema, fieldType);
							if (type != null)
							{
								fieldType = type;
							}
						}
					}

					ret.FieldTypes[i] = fieldType;
				}

				var columnReaders = new ConvertFromDataReaderExpression.ColumnReader[rd.DataReader!.FieldCount];

				for (var i = 0; i < ret.FieldCount; i++)
					columnReaders[i] = new ConvertFromDataReaderExpression.ColumnReader(db, db.MappingSchema,
						ret.FieldTypes[i], i, QueryHelper.GetValueConverter(select.Select.Columns[i]), true);

				while (rd.DataReader!.Read())
				{
					var data = new string  [rd.DataReader!.FieldCount];

					ret.RowCount++;

					for (var i = 0; i < ret.FieldCount; i++)
					{
						if (!rd.DataReader!.IsDBNull(i))
						{
							var value = columnReaders[i].GetValue(reader);

							if (value != null)
								data[i] = SerializationConverter.Serialize(SerializationMappingSchema, value);
						}
					}

					ret.Data.Add(data);
				}

				return LinqServiceSerializer.Serialize(SerializationMappingSchema, ret);
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		[WebMethod]
		public int ExecuteBatch(string? configuration, string queryData)
		{
			try
			{
				var data    = LinqServiceSerializer.DeserializeStringArray(SerializationMappingSchema, queryData);
				var queries = data.Select(r => LinqServiceSerializer.Deserialize(SerializationMappingSchema, r)).ToArray();

				foreach (var query in queries)
					ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				db.BeginTransaction();

				foreach (var query in queries)
				{
					DataConnection.QueryRunner.ExecuteNonQuery(db, new QueryContext
					{
						Statement  = query.Statement,
						QueryHints = query.QueryHints
					}, null);
				}

				db.CommitTransaction();

				return queryData.Length;
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		#endregion
	}
}
