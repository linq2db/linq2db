using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Remote;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

namespace LinqToDB.Remote
{
	public class LinqService : ILinqService
	{
		private MappingSchema? _serializationMappingSchema;
		private MappingSchema? _mappingSchema;

		public bool    AllowUpdates    { get; set; }
		public string? RemoteClientTag { get; set; }

		public MappingSchema? MappingSchema
		{
			get => _mappingSchema;
			set
			{
				_mappingSchema = value;
				_serializationMappingSchema = value != null
					? MappingSchema.CombineSchemas(Internal.Remote.SerializationMappingSchema.Instance, value)
					: Internal.Remote.SerializationMappingSchema.Instance;
			}
		}

		internal MappingSchema SerializationMappingSchema => _serializationMappingSchema ??=
			_mappingSchema != null
				? MappingSchema.CombineSchemas(Internal.Remote.SerializationMappingSchema.Instance, _mappingSchema)
				: Internal.Remote.SerializationMappingSchema.Instance;

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
			var dc = new DataConnection(configuration) { Tag = RemoteClientTag };
			if (MappingSchema != null)
				dc.AddMappingSchema(MappingSchema);
			return dc;
		}

		protected virtual void ValidateQuery(LinqServiceQuery query)
		{
			if (AllowUpdates == false && query.Statement.QueryType != QueryType.Select)
				throw new LinqToDBException("Insert/Update/Delete requests are not allowed by the service policy.");
		}

		protected virtual void HandleException(Exception exception)
		{
		}

		#region ILinqService Members

		public virtual Task<LinqServiceInfo> GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			using var ctx = CreateDataContext(configuration);

			var serviceProvider = ((IInfrastructure<IServiceProvider>)ctx.DataProvider).Instance;

			return Task.FromResult(new LinqServiceInfo()
			{
				MappingSchemaType        = ctx.DataProvider.MappingSchema.GetType().AssemblyQualifiedName!,
				MethodCallTranslatorType = serviceProvider.GetRequiredService<IMemberTranslator>().GetType().AssemblyQualifiedName!,
				SqlBuilderType           = ctx.DataProvider.CreateSqlBuilder(ctx.MappingSchema, ctx.Options).GetType().AssemblyQualifiedName!,
				SqlOptimizerType         = ctx.DataProvider.GetSqlOptimizer(ctx.Options).GetType().AssemblyQualifiedName!,
				SqlProviderFlags         = ctx.DataProvider.SqlProviderFlags,
				SupportedTableOptions    = ctx.DataProvider.SupportedTableOptions
			});
		}

		public async Task<int> ExecuteNonQueryAsync(
			string?           configuration,
			string            queryData,
			CancellationToken cancellationToken)
		{
			try
			{
#pragma warning disable CA2007
				await using var db = CreateDataContext(configuration);
#pragma warning restore CA2007

				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, MappingSchema ?? SerializationMappingSchema, db.Options, queryData);

				ValidateQuery(query);

#pragma warning disable CA2007
				await using var _ = db.DataProvider.ExecuteScope(db);
#pragma warning restore CA2007

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				return await DataConnection.QueryRunner.ExecuteNonQueryAsync(
					db,
					new QueryContext(query.Statement, query.DataOptions),
					new SqlParameterValues(),
					cancellationToken
					).ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		public async Task<string?> ExecuteScalarAsync(
			string?           configuration,
			string            queryData,
			CancellationToken cancellationToken)
		{
			try
			{
#pragma warning disable CA2007
				await using var db = CreateDataContext(configuration);
#pragma warning restore CA2007

				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, MappingSchema ?? SerializationMappingSchema, db.Options, queryData);

				ValidateQuery(query);

#pragma warning disable CA2007
				await using var _ = db.DataProvider.ExecuteScope(db);
#pragma warning restore CA2007

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				var scalar = await DataConnection.QueryRunner.ExecuteScalarAsync(
					db,
					new QueryContext(query.Statement, query.DataOptions),
					null,
					cancellationToken
					).ConfigureAwait(false);

				var result = ProcessScalar(scalar);

				return result;
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		private string? ProcessScalar(object? scalar)
		{
			string? result = null;

			if (scalar != null)
			{
				var lsr = new LinqServiceResult
				{
					QueryID    = Guid.NewGuid(),
					FieldCount = 1,
					RowCount   = 1,
					FieldNames = ["scalar"],
					FieldTypes = [scalar.GetType()],
					Data       =
					[
						[
							scalar == DBNull.Value
								? string.Empty
								: SerializationConverter.Serialize(SerializationMappingSchema, scalar)
						]
					],
				};

				result = LinqServiceSerializer.Serialize(SerializationMappingSchema, lsr);
			}

			return result;
		}

		public async Task<string> ExecuteReaderAsync(
			string?           configuration,
			string            queryData,
			CancellationToken cancellationToken)
		{
			try
			{
#pragma warning disable CA2007
				await using var db = CreateDataContext(configuration);

				var query = LinqServiceSerializer.Deserialize(SerializationMappingSchema, MappingSchema ?? SerializationMappingSchema, db.Options, queryData);

				ValidateQuery(query);

				await using var _ = db.DataProvider.ExecuteScope(db);

				if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

				await using var rd = await DataConnection.QueryRunner.ExecuteReaderAsync(
					db,
					new QueryContext(query.Statement, query.DataOptions),
					SqlParameterValues.Empty,
					cancellationToken
					).ConfigureAwait(false);

				var ret = ProcessDataReaderWrapper(query, db, rd);

				return LinqServiceSerializer.Serialize(SerializationMappingSchema, ret);
#pragma warning restore CA2007
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		public async Task<int> ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
		{
			try
			{
#pragma warning disable CA2007
				await using var db = CreateDataContext(configuration);
#pragma warning restore CA2007

				var data    = LinqServiceSerializer.DeserializeStringArray(SerializationMappingSchema, MappingSchema ?? SerializationMappingSchema, db.Options, queryData);
				var queries = data.Select(r => LinqServiceSerializer.Deserialize(SerializationMappingSchema, MappingSchema ?? SerializationMappingSchema, db.Options, r)).ToArray();

				foreach (var query in queries)
					ValidateQuery(query);

#pragma warning disable CA2007
				await using var _ = db.DataProvider.ExecuteScope(db);
#pragma warning restore CA2007

				await db.BeginTransactionAsync(cancellationToken)
					.ConfigureAwait(false);

				foreach (var query in queries)
				{
					if (query.QueryHints?.Count > 0) db.NextQueryHints.AddRange(query.QueryHints);

					await DataConnection.QueryRunner.ExecuteNonQueryAsync(db, new QueryContext(query.Statement, query.DataOptions), null, cancellationToken)
						.ConfigureAwait(false);
				}

				await db.CommitTransactionAsync(cancellationToken)
					.ConfigureAwait(false);

				return queryData.Length;
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		#endregion

		private LinqServiceResult ProcessDataReaderWrapper(LinqServiceQuery query, DataConnection db, DataReaderWrapper rd)
		{
			DbDataReader reader;

			if (db is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
			{
				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
					reader = interceptor.UnwrapDataReader(db, rd.DataReader!);
			}
			else
			{
				reader = rd.DataReader!;
			}

			var ret = new LinqServiceResult
			{
				QueryID    = Guid.NewGuid(),
				FieldCount = rd.DataReader!.FieldCount,
				FieldNames = new string[rd.DataReader!.FieldCount],
				FieldTypes = new Type  [rd.DataReader!.FieldCount],
				Data       = new List<string[]>(),
			};

			var names             = new HashSet<string>();
			var selectExpressions = query.Statement.QueryType switch
			{
				QueryType.Select => query.Statement.SelectQuery!.Select.Columns.Select(c => c.Expression).ToList(),
				QueryType.Insert => ((SqlInsertStatement)query.Statement).Output!.OutputColumns!,
				QueryType.Delete => ((SqlDeleteStatement)query.Statement).Output!.OutputColumns!,
				QueryType.Update => ((SqlUpdateStatement)query.Statement).Output!.OutputColumns!,
				QueryType.Merge  => ((SqlMergeStatement )query.Statement).Output!.OutputColumns!,
				_ => throw new NotImplementedException($"Query type not supported: {query.Statement.QueryType}"),
			};

			for (var i = 0; i < ret.FieldCount; i++)
			{
				var name = rd.DataReader!.GetName(i);
				var idx  = 0;

				if (names.Contains(name))
				{
					while (names.Contains(name = FormattableString.Invariant($"c{++idx}")))
					{
					}
				}

				names.Add(name);

				ret.FieldNames[i] = name;
				// ugh...
				// still if it fails here due to empty columns - it is a bug in columns generation

				var fieldType      = selectExpressions[i].SystemType!;
				var valueConverter = QueryHelper.GetValueConverter(selectExpressions[i]);
				if (valueConverter != null)
				{
					// value converter applied on client side for both directions
					// here on read we need to prepare expected by converter type
					fieldType = valueConverter.FromProviderExpression.Parameters[0].Type;
				}

				// async compiled query support
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Task<>))
					fieldType = fieldType.GetGenericArguments()[0];

				if (fieldType.IsEnum || fieldType.IsNullable() && fieldType.ToNullableUnderlying().IsEnum)
				{
					var stringConverter = db.MappingSchema.GetConverter(new DbDataType(typeof(string)), new DbDataType(fieldType), false, ConversionType.Common);
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
				columnReaders[i] = new ConvertFromDataReaderExpression.ColumnReader(db.MappingSchema,
					// converter must be null, see notes above
					ret.FieldTypes[i], i, converter: null, true);

			while (rd.DataReader!.Read())
			{
				var data = new string[rd.DataReader!.FieldCount];

				ret.RowCount++;

				for (var i = 0; i < ret.FieldCount; i++)
				{
					if (!reader.IsDBNull(i))
					{
						var value = columnReaders[i].GetValue(db, reader);

						if (value != null)
							data[i] = SerializationConverter.Serialize(SerializationMappingSchema, value);
					}
				}

				ret.Data.Add(data);
			}

			return ret;
		}

		#region private classes

		sealed class QueryContext(SqlStatement statement, DataOptions dataOptions) : IQueryContext
		{
			public SqlStatement    Statement       { get; } = statement;
			public object?         Context         { get; set; }
			public bool            IsContinuousRun { get; set; }
			public SqlParameter[]? Parameters      { get; set; }
			public AliasesContext? Aliases         { get; set; }
			public DataOptions     DataOptions     { get; } = dataOptions;
		}

		#endregion
	}
}
