﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools
{
	using System.Text;
	using Common;
	using Data;
	using LinqToDB.Mapping;
	using LinqToDB.SqlProvider;

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
			DataConnection      context,
			bool                useSequenceName = true,
			bool                useIdentity     = false)
			where T: notnull
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (context == null) throw new ArgumentNullException(nameof(context));

			IList<T>? sourceList = null;

			var entityDescriptor = context.MappingSchema.GetEntityDescriptor(typeof(T));

			foreach (var column in entityDescriptor.Columns)
			{
				if (column.IsIdentity)
				{
					sourceList ??= source.ToList();

					if (sourceList.Count == 0)
						return sourceList;

					var sqlBuilder = context.DataProvider.CreateSqlBuilder(context.MappingSchema);

					var sequenceName = useSequenceName && column.SequenceName != null ? column.SequenceName.SequenceName : null;

					if (sequenceName != null)
						GetColumnSequenceValues(context, sourceList, column, sqlBuilder, sequenceName);
					else
					{
						if (useIdentity)
						{
							var sb = new StringBuilder();
							sqlBuilder.BuildTableName(sb, entityDescriptor.ServerName, entityDescriptor.DatabaseName, entityDescriptor.SchemaName, entityDescriptor.TableName, entityDescriptor.TableOptions);
							var tableName = sb.ToString();

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

		private static void GetDefaultIdentityImpl<T>(DataConnection context, IList<T> sourceList, EntityDescriptor entityDescriptor, ColumnDescriptor column, ISqlBuilder sqlBuilder) where T : notnull
		{
			var sql      = sqlBuilder.GetMaxValueSql(entityDescriptor, column);
			var maxValue = context.Execute<object?>(sql);

			if (maxValue == null || maxValue == DBNull.Value)
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

		private static void GetColumnSequenceValues<T>(DataConnection context, IList<T> sourceList, ColumnDescriptor column, ISqlBuilder sqlBuilder, string sequenceName) where T : notnull
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
	}
}
