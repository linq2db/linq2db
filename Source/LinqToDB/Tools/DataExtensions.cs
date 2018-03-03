using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools
{
	using Common;
	using Data;

	public static class DataExtensions
	{
		public static IEnumerable<T> RetrieveIdentity<T>(this IEnumerable<T> source, DataConnection context, bool useSequenceName = true)
		{
			IList<T> sourceList = null;

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
						var sql       = context.DataProvider.CreateSqlBuilder().GetReserveSequenceValuesSql(sourceList.Count, sequenceName);
						var sequences = context.Query<object>(sql).ToList();

						for (var i = 0; i < sourceList.Count; i++)
						{
							var item  = sourceList[i];
							var value = Converter.ChangeType(sequences[i], column.MemberType);
							column.MemberAccessor.SetValue(item, value);
						}
					}
					else
					{
						var sql      = context.DataProvider.CreateSqlBuilder().GetMaxValueSql(entityDescriptor, column);
						var maxValue = context.Execute<object>(sql);

						if (maxValue == null || maxValue == DBNull.Value)
							maxValue = 0;

						var type = Type.GetTypeCode(maxValue.GetType());

						foreach (var item in sourceList)
						{
							switch (type)
							{
								case TypeCode.Byte   : maxValue = (Byte)   maxValue + 1; break;
								case TypeCode.SByte  : maxValue = (SByte)  maxValue + 1; break;
								case TypeCode.Int16  : maxValue = (Int16)  maxValue + 1; break;
								case TypeCode.Int32  : maxValue = (Int32)  maxValue + 1; break;
								case TypeCode.Int64  : maxValue = (Int64)  maxValue + 1; break;
								case TypeCode.UInt16 : maxValue = (UInt16) maxValue + 1; break;
								case TypeCode.UInt32 : maxValue = (UInt32) maxValue + 1; break;
								case TypeCode.UInt64 : maxValue = (UInt64) maxValue + 1; break;
								case TypeCode.Single : maxValue = (Single) maxValue + 1; break;
								case TypeCode.Decimal: maxValue = (Decimal)maxValue + 1; break;
								default:
									throw new NotImplementedException();
							}

							var value = Converter.ChangeType(maxValue, column.MemberType);
							column.MemberAccessor.SetValue(item, value);
						}
					}
				}
			}

			return sourceList ?? source;
		}
	}
}
