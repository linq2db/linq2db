using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools
{
	using Common;
	using Data;

	public static class DataExtensions
	{
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
