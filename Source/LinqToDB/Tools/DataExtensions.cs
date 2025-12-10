using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Tools
{
	// TODO: looks like this API needs refactoring. Why we even create collection copy here???
	public static class DataExtensions
	{
		/// <summary>
		/// Initializes source columns, marked with <see cref="ColumnAttribute.IsIdentity"/> or <see cref="IdentityAttribute" /> with identity values:
		/// <list type="bullet">
		/// <item>if column had sequence name set using <see cref="SequenceNameAttribute"/> and <paramref name="useSequenceName"/> set to <c>true</c>, values from sequence used. Implemented for: Oracle, PostgreSQL</item>
		/// <item>if table has identity configured and <paramref name="useIdentity"/> set to <c>true</c>, values from sequence used. Implemented for: SQL Server 2005+</item>
		/// <item>Otherwise column initialized with values, incremented by 1 starting with max value from database for this column plus 1.</item>
		/// </list>
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Ordered list of entities to initialize.</param>
		/// <param name="context">Data connection to use to retrieve sequence values of max used value for column.</param>
		/// <param name="useSequenceName">Enables identity values retrieval from sequence for columns with sequence name specified in mapping using <see cref="SequenceNameAttribute"/>. Implemented for Oracle and PostgreSQL.</param>
		/// <param name="useIdentity">Enables identity values retrieval from table with identity column. Implemented for SQL Server 2005+.</param>
		/// <returns>Returns new collection of identity fields initialized or <paramref name="source"/> if entity had no identity columns.</returns>
		public static IEnumerable<T> RetrieveIdentity<T>(
			this IEnumerable<T> source,
			IDataContext        context,
			bool                useSequenceName = true,
			bool                useIdentity     = false)
			where T: notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(context);

			var dataProvider = context.GetDataProvider();

			IList<T>? sourceList = null;

			var entityDescriptor = context.MappingSchema.GetEntityDescriptor(typeof(T), context.Options.ConnectionOptions.OnEntityDescriptorCreated);

			foreach (var column in entityDescriptor.Columns)
			{
				if (column.IsIdentity)
				{
					sourceList ??= source.ToList();

					if (sourceList.Count == 0)
						return sourceList;

					var sqlBuilder = dataProvider.CreateSqlBuilder(context.MappingSchema, context.Options);

					var sequenceName = useSequenceName && column.SequenceName != null ? column.SequenceName.SequenceName : null;

					if (sequenceName != null)
						GetColumnSequenceValues(context, sourceList, column, sqlBuilder, sequenceName);
					else
					{
						if (useIdentity)
						{
							var tableName = sqlBuilder.BuildObjectName(new (), entityDescriptor.Name, tableOptions: entityDescriptor.TableOptions).ToString();

							var identity = context.Select(() => new { last = Sql.CurrentIdentity(tableName), step = Sql.IdentityStep(tableName) });

							if (identity.last != null && identity.step != null)
							{
								GetIdentityValues(sourceList, column, identity.last, identity.step);
								// current implementations (sql server) support single identity per table
								return sourceList;
							}
						}

						GetDefaultIdentityImpl(context, sourceList, entityDescriptor, column, sqlBuilder);
					}
				}
			}

			return sourceList ?? source;
		}

		/// <summary>
		/// Initializes source columns, marked with <see cref="ColumnAttribute.IsIdentity"/> or <see cref="IdentityAttribute" /> with identity values:
		/// <list type="bullet">
		/// <item>if column had sequence name set using <see cref="SequenceNameAttribute"/> and <paramref name="useSequenceName"/> set to <c>true</c>, values from sequence used. Implemented for: Oracle, PostgreSQL</item>
		/// <item>if table has identity configured and <paramref name="useIdentity"/> set to <c>true</c>, values from sequence used. Implemented for: SQL Server 2005+</item>
		/// <item>Otherwise column initialized with values, incremented by 1 starting with max value from database for this column plus 1.</item>
		/// </list>
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Ordered list of entities to initialize.</param>
		/// <param name="context">Data connection to use to retrieve sequence values of max used value for column.</param>
		/// <param name="useSequenceName">Enables identity values retrieval from sequence for columns with sequence name specified in mapping using <see cref="SequenceNameAttribute"/>. Implemented for Oracle and PostgreSQL.</param>
		/// <param name="useIdentity">Enables identity values retrieval from table with identity column. Implemented for SQL Server 2005+.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns new collection of identity fields initialized or <paramref name="source"/> if entity had no identity columns.</returns>
		public static async Task<IEnumerable<T>> RetrieveIdentityAsync<T>(
			this IEnumerable<T> source,
			IDataContext        context,
			bool                useSequenceName   = true,
			bool                useIdentity       = false,
			CancellationToken   cancellationToken = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(context);

			var dataProvider = context.GetDataProvider();

			IList<T>? sourceList = null;

			var entityDescriptor = context.MappingSchema.GetEntityDescriptor(typeof(T), context.Options.ConnectionOptions.OnEntityDescriptorCreated);

			foreach (var column in entityDescriptor.Columns)
			{
				if (column.IsIdentity)
				{
					sourceList ??= source.ToList();

					if (sourceList.Count == 0)
						return sourceList;

					var sqlBuilder = dataProvider.CreateSqlBuilder(context.MappingSchema, context.Options);

					var sequenceName = useSequenceName && column.SequenceName != null ? column.SequenceName.SequenceName : null;

					if (sequenceName != null)
					{
						await GetColumnSequenceValuesAsync(context, sourceList, column, sqlBuilder, sequenceName, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						if (useIdentity)
						{
							var tableName = sqlBuilder.BuildObjectName(new (), entityDescriptor.Name, tableOptions: entityDescriptor.TableOptions).ToString();

							var identity = await context.SelectAsync(() => new { last = Sql.CurrentIdentity(tableName), step = Sql.IdentityStep(tableName) }, cancellationToken)
								.ConfigureAwait(false);

							if (identity.last != null && identity.step != null)
							{
								GetIdentityValues(sourceList, column, identity.last, identity.step);
								// current implementations (sql server) support single identity per table
								return sourceList;
							}
						}

						await GetDefaultIdentityImplAsync(context, sourceList, entityDescriptor, column, sqlBuilder, cancellationToken)
							.ConfigureAwait(false);
					}
				}
			}

			return sourceList ?? source;
		}

		private static void GetDefaultIdentityImpl<T>(IDataContext context, IList<T> sourceList, EntityDescriptor entityDescriptor, ColumnDescriptor column, ISqlBuilder sqlBuilder) where T : notnull
		{
			var sql      = sqlBuilder.GetMaxValueSql(entityDescriptor, column);
			var maxValue = context.Execute<object?>(sql);

			if (maxValue.IsNullValue())
				maxValue = 0;

			var type = Type.GetTypeCode(maxValue.GetType());

			foreach (var item in sourceList)
			{
				maxValue = type switch
				{
					TypeCode.Byte    => (byte   )maxValue + 1,
					TypeCode.SByte   => (sbyte  )maxValue + 1,
					TypeCode.Int16   => (short  )maxValue + 1,
					TypeCode.Int32   => (int    )maxValue + 1,
					TypeCode.Int64   => (long   )maxValue + 1,
					TypeCode.UInt16  => (ushort )maxValue + 1,
					TypeCode.UInt32  => (uint   )maxValue + 1,
					TypeCode.UInt64  => (ulong  )maxValue + 1,
					TypeCode.Single  => (float  )maxValue + 1,
					TypeCode.Decimal => (decimal)maxValue + 1,
					_                => throw new NotImplementedException(),
				};
				var value = Converter.ChangeType(maxValue, column.MemberType);
				column.MemberAccessor.SetValue(item!, value);
			}
		}

		private static async ValueTask GetDefaultIdentityImplAsync<T>(IDataContext context, IList<T> sourceList, EntityDescriptor entityDescriptor, ColumnDescriptor column, ISqlBuilder sqlBuilder, CancellationToken cancellationToken) where T : notnull
		{
			var sql      = sqlBuilder.GetMaxValueSql(entityDescriptor, column);
			var maxValue = await context.ExecuteAsync<object?>(sql, cancellationToken).ConfigureAwait(false);

			if (maxValue.IsNullValue())
				maxValue = 0;

			var type = Type.GetTypeCode(maxValue.GetType());

			foreach (var item in sourceList)
			{
				maxValue = type switch
				{
					TypeCode.Byte    => (byte   )maxValue + 1,
					TypeCode.SByte   => (sbyte  )maxValue + 1,
					TypeCode.Int16   => (short  )maxValue + 1,
					TypeCode.Int32   => (int    )maxValue + 1,
					TypeCode.Int64   => (long   )maxValue + 1,
					TypeCode.UInt16  => (ushort )maxValue + 1,
					TypeCode.UInt32  => (uint   )maxValue + 1,
					TypeCode.UInt64  => (ulong  )maxValue + 1,
					TypeCode.Single  => (float  )maxValue + 1,
					TypeCode.Decimal => (decimal)maxValue + 1,
					_                => throw new NotImplementedException(),
				};
				var value = Converter.ChangeType(maxValue, column.MemberType);
				column.MemberAccessor.SetValue(item!, value);
			}
		}

		private static void GetIdentityValues<T>(IList<T> sourceList, ColumnDescriptor column, object last, object step)
			where T: notnull
		{
			var type = Type.GetTypeCode(last.GetType());

			for (var i = 0; i < sourceList.Count; i++)
			{
				object nextValue = type switch
				{
					TypeCode.Byte    => (byte   )last + (i + 1) * (byte   )step,
					TypeCode.SByte   => (sbyte  )last + (i + 1) * (sbyte  )step,
					TypeCode.Int16   => (short  )last + (i + 1) * (short  )step,
					TypeCode.Int32   => (int    )last + (i + 1) * (int    )step,
					TypeCode.Int64   => (long   )last + (i + 1) * (long   )step,
					TypeCode.UInt16  => (ushort )last + (i + 1) * (ushort )step,
					TypeCode.UInt32  => (uint   )last + (i + 1) * (uint   )step,
					TypeCode.UInt64  => (ulong  )last + (ulong)(i + 1) * (ulong)step,
					TypeCode.Single  => (float  )last + (i + 1) * (float  )step,
					TypeCode.Decimal => (decimal)last + (i + 1) * (decimal)step,
					_                => throw new NotImplementedException(),
				};

				var value = Converter.ChangeType(nextValue, column.MemberType);
				column.MemberAccessor.SetValue(sourceList[i], value);
			}
		}

		private static void GetColumnSequenceValues<T>(IDataContext context, IList<T> sourceList, ColumnDescriptor column, ISqlBuilder sqlBuilder, string sequenceName) where T : notnull
		{
			var sql       = sqlBuilder.GetReserveSequenceValuesSql(sourceList.Count, sequenceName);
			var sequences = context.Query<object>(sql).ToList();

			for (var i = 0; i < sourceList.Count; i++)
			{
				var item  = sourceList[i];
				var value = Converter.ChangeType(sequences[i], column.MemberType);
				column.MemberAccessor.SetValue(item!, value);
			}
		}

		private static async ValueTask GetColumnSequenceValuesAsync<T>(IDataContext context, IList<T> sourceList, ColumnDescriptor column, ISqlBuilder sqlBuilder, string sequenceName, CancellationToken cancellationToken) where T : notnull
		{
			var sql       = sqlBuilder.GetReserveSequenceValuesSql(sourceList.Count, sequenceName);
			var sequences = await context.QueryToListAsync<object>(sql, cancellationToken).ConfigureAwait(false);

			for (var i = 0; i < sourceList.Count; i++)
			{
				var item  = sourceList[i];
				var value = Converter.ChangeType(sequences[i], column.MemberType);
				column.MemberAccessor.SetValue(item!, value);
			}
		}
	}
}
