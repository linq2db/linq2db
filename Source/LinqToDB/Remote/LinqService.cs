﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqToDB.Remote
{
	using System.Threading;
	using Common;
	using Data;
	using Expressions;
	using Extensions;
	using Linq;
	using Mapping;
	using SqlQuery;

	public class LinqService : ILinqService
	{
		private MappingSchema? _serializationMappingSchema;
		private MappingSchema? _mappingSchema;

		public bool AllowUpdates
		{
			get;
			set;
		}

		public MappingSchema? MappingSchema
		{
			get => _mappingSchema;
			set
			{
				_mappingSchema = value;
				_serializationMappingSchema = new SerializationMappingSchema(_mappingSchema);
			}
		}

		internal MappingSchema SerializationMappingSchema => _serializationMappingSchema ??= new SerializationMappingSchema(_mappingSchema);

		public static Func<string, Type?> TypeResolver = _ => null;

		public LinqService()
		{
		}

		public LinqService(MappingSchema? mappingSchema)
		{
			_mappingSchema = mappingSchema;
		}

		public virtual DataConnection CreateDataContext(string? configuration)
		{
			var dc = new DataConnection(configuration);
			if (MappingSchema != null)
				dc.AddMappingSchema(MappingSchema);
			return dc;
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

		public virtual LinqServiceInfo GetInfo(string? configuration)
		{
			using var ctx = CreateDataContext(configuration);

			return new LinqServiceInfo
			{
				MappingSchemaType     = ctx.DataProvider.MappingSchema.GetType().AssemblyQualifiedName!,
				SqlBuilderType        = ctx.DataProvider.CreateSqlBuilder(ctx.MappingSchema).GetType().AssemblyQualifiedName!,
				SqlOptimizerType      = ctx.DataProvider.GetSqlOptimizer().GetType().AssemblyQualifiedName!,
				SqlProviderFlags      = ctx.DataProvider.SqlProviderFlags,
				SupportedTableOptions = ctx.DataProvider.SupportedTableOptions
			};
		}

		public int ExecuteNonQuery(string? configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				return DataConnection.QueryRunner.ExecuteNonQuery(db, new QueryContext
				{
					Statement = query.Statement
				}, new SqlParameterValues());
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		public async Task<string?> ExecuteScalarAsync(
			string? configuration,
			string queryData,
			CancellationToken cancellationToken
			)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				var scalar = await DataConnection.QueryRunner.ExecuteScalarAsync(
					db,
					new QueryContext
					{
						Statement  = query.Statement
					},
					null,
					cancellationToken
					).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				string? result = null;
				if (scalar != null)
				{
					var lsr = new LinqServiceResult
					{
						QueryID    = Guid.NewGuid(),
						FieldCount = 1,
						RowCount = 1,
						FieldNames = new string[] { "scalar" },
						FieldTypes = new Type[] { scalar.GetType() },
						Data       = new List<string[]>
						{
							new []
							{
								SerializationConverter.Serialize(SerializationMappingSchema, scalar)
							}
						},
					};

					result = LinqServiceSerializer.Serialize(SerializationMappingSchema, lsr);
				}

				return result;
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		public string? ExecuteScalar(string? configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				var scalar =  DataConnection.QueryRunner.ExecuteScalar(db, new QueryContext
				{
					Statement  = query.Statement
				}, null);

				string? result = null;
				if (scalar != null)
				{
					var lsr = new LinqServiceResult
					{
						QueryID    = Guid.NewGuid(),
						FieldCount = 1,
						RowCount = 1,
						FieldNames = new string[] { "scalar" },
						FieldTypes = new Type[] { scalar.GetType() },
						Data       = new List<string[]>
						{
							new []
							{
								SerializationConverter.Serialize(SerializationMappingSchema, scalar)
							}
						},
					};

					result = LinqServiceSerializer.Serialize(SerializationMappingSchema, lsr);
				}

				return result;
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		public string ExecuteReader(string? configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, queryData);

				ValidateQuery(query);

				using var db = CreateDataContext(configuration);
				using var _  = db.DataProvider.ExecuteScope(db);

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				using var rd = DataConnection.QueryRunner.ExecuteReader(db, new QueryContext
				{
					Statement  = query.Statement
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
				var select = GetQuerySelect(query);

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

		private static SelectQuery GetQuerySelect(LinqServiceQuery query)
		{
			return query.Statement.QueryType switch
			{
				QueryType.Select => query.Statement.SelectQuery!,
				QueryType.Insert => ((SqlInsertStatement)query.Statement).Output!.OutputQuery!,
				QueryType.Delete => ((SqlDeleteStatement)query.Statement).Output!.OutputQuery!,
				QueryType.Update => ((SqlUpdateStatement)query.Statement).Output!.OutputQuery!,
				QueryType.Merge => ((SqlMergeStatement)query.Statement).Output!.OutputQuery!,
				_ => throw new NotImplementedException($"Query type not supported: {query.Statement.QueryType}"),
			};
		}

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
					if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

					DataConnection.QueryRunner.ExecuteNonQuery(db, new QueryContext
					{
						Statement  = query.Statement
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

		#region private classes

		private class QueryContext : IQueryContext
		{
			public SqlStatement Statement { get; set; } = null!;
			public object? Context { get; set; }
			public SqlParameter[]? Parameters { get; set; }
			public AliasesContext? Aliases { get; set; }
		}

		#endregion
	}
}