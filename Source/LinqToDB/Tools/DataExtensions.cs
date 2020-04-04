using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools
{
	using Common;
	using Data;
	using LinqToDB.Mapping;

	public static class DataExtensions
	{
		/// <summary>
		/// Initializes source columns, marked with <see cref="ColumnAttribute.IsIdentity"/> with values.
		/// If column had sequence name set using <see cref="SequenceNameAttribute"/> and <paramref name="useSequenceName"/> set to <c>true</c>, values from sequence used.
		/// Otherwise column initialized with values, incremented by 1 starting with max value from database for this column plus 1.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="source">Ordered list of entities to initialize.</param>
		/// <param name="context">Data connection to use to retrieve sequence values of max used value for column.</param>
		/// <param name="useSequenceName">Enables identity values retrieval from sequence for columns with sequence name specified in mapping using <see cref="SequenceNameAttribute"/>.</param>
		/// <returns>Returns new collection of identity fields initialized or <paramref name="source"/> if entity had no identity columns.</returns>
		public static IEnumerable<T> RetrieveIdentity<T>(
			this IEnumerable<T> source,
			DataConnection      context,
			bool                useSequenceName = true)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (context == null) throw new ArgumentNullException(nameof(context));

			IList<T>? sourceList = null;

			var entityDescriptor = context.MappingSchema.GetEntityDescriptor(typeof(T));

			foreach (var column in entityDescriptor.Columns)
			{
				if (column.IsIdentity)
				{
					sourceList = sourceList ?? source.ToList();

					if (sourceList.Count == 0)
						return sourceList;

					var sequenceName = useSequenceName && column.SequenceName != null ? column.SequenceName.SequenceName : null;

					if (sequenceName != null)
					{
						var sql       = context.DataProvider.CreateSqlBuilder(context.MappingSchema).GetReserveSequenceValuesSql(sourceList.Count, sequenceName);
						var sequences = context.Query<object>(sql).ToList();

						for (var i = 0; i < sourceList.Count; i++)
						{
							var item  = sourceList[i];
							var value = Converter.ChangeType(sequences[i], column.MemberType);
							column.MemberAccessor.SetValue(item!, value);
						}
					}
					else
					{
						var sql      = context.DataProvider.CreateSqlBuilder(context.MappingSchema).GetMaxValueSql(entityDescriptor, column);
						var maxValue = context.Execute<object?>(sql);

						if (maxValue == null || maxValue == DBNull.Value)
							maxValue = 0;

						var type = Type.GetTypeCode(maxValue.GetType());

						foreach (var item in sourceList)
						{
							switch (type)
							{
								case TypeCode.Byte   : maxValue = (byte   )maxValue + 1; break;
								case TypeCode.SByte  : maxValue = (sbyte  )maxValue + 1; break;
								case TypeCode.Int16  : maxValue = (short  )maxValue + 1; break;
								case TypeCode.Int32  : maxValue = (int    )maxValue + 1; break;
								case TypeCode.Int64  : maxValue = (long   )maxValue + 1; break;
								case TypeCode.UInt16 : maxValue = (ushort )maxValue + 1; break;
								case TypeCode.UInt32 : maxValue = (uint   )maxValue + 1; break;
								case TypeCode.UInt64 : maxValue = (ulong  )maxValue + 1; break;
								case TypeCode.Single : maxValue = (float  )maxValue + 1; break;
								case TypeCode.Decimal: maxValue = (decimal)maxValue + 1; break;
								default:
									throw new NotImplementedException(    );
							}

							var value = Converter.ChangeType(maxValue, column.MemberType);
							column.MemberAccessor.SetValue(item!, value);
						}
					}
				}
			}

			return sourceList ?? source;
		}
	}
}
