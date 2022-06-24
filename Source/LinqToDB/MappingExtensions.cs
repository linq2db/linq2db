using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	using Mapping;

	public static class MappingExtensions
	{
		public static bool UseNodaTime(this MappingSchema mappingSchema)
		{
			var loc  = typeof(MappingExtensions).Assembly.Location;
			var path = Path.Combine(Path.GetDirectoryName(loc)!, "NodaTime.dll");

			if (File.Exists(path))
			{
				Type? type;

				try
				{
					type = Type.GetType("NodaTime.LocalDateTime, NodaTime", false);
				}
				catch
				{
					type = null;
				}

				if (type != null)
				{
					UseNodaTime(mappingSchema, type);
					return true;
				}
			}

			return false;
		}

		public static void UseNodaTime(this MappingSchema mappingSchema, Type localDateTimeType)
		{
			mappingSchema.SetDataType(localDateTimeType, new SqlDataType(new DbDataType(typeof(DateTime), DataType.DateTime)));

			// ms.SetConvertExpression<LocalDateTime, DataParameter>(timeStamp =>
			//     new DataParameter
			//     {
			//         Value = new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour,
			//             timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond),
			//         DataType = DataType.DateTime
			//     });

			var ldtParameter = Expression.Parameter(localDateTimeType, "timeStamp");

			var newDateTime = Expression.New(
				MemberHelper.ConstructorOf(() => new DateTime(0, 0, 0, 0, 0, 0, 0)),
				new Expression[]
				{
					Expression.PropertyOrField(ldtParameter, "Year"),
					Expression.PropertyOrField(ldtParameter, "Month"),
					Expression.PropertyOrField(ldtParameter, "Day"),
					Expression.PropertyOrField(ldtParameter, "Hour"),
					Expression.PropertyOrField(ldtParameter, "Minute"),
					Expression.PropertyOrField(ldtParameter, "Second"),
					Expression.PropertyOrField(ldtParameter, "Millisecond")
				});

			mappingSchema.SetConvertExpression(
				localDateTimeType,
				typeof(DataParameter),
				Expression.Lambda(
					Expression.MemberInit(
						Expression.New(typeof(DataParameter)),
						Expression.Bind(
							MemberHelper.PropertyOf<DataParameter>(p => p.Value),
							Expression.Convert(
								newDateTime,
								typeof(object))),
						Expression.Bind(
							MemberHelper.PropertyOf<DataParameter>(p => p.DataType),
							Expression.Constant(
								DataType.DateTime,
								typeof(DataType)))),
					ldtParameter));

			// ms.SetConvertExpression<LocalDateTime, DateTime>(timeStamp =>
			//     new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour,
			//         timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond));

			mappingSchema.SetConvertExpression(
				localDateTimeType,
				typeof(DateTime),
				Expression.Lambda(newDateTime, ldtParameter));

			// LocalDateTime.FromDateTime(TestData.DateTime),

			var dtParameter = Expression.Parameter(typeof(DateTime), "dt");

			mappingSchema.SetConvertExpression(
				typeof(DateTime),
				localDateTimeType,
				Expression.Lambda(
					Expression.Call(
						localDateTimeType.GetMethod("FromDateTime", new[] { typeof(DateTime) })!,
						dtParameter),
					dtParameter));

			var sParameter = Expression.Parameter(typeof(string), "str");

			mappingSchema.SetConvertExpression(
				typeof(string),
				localDateTimeType,
				Expression.Lambda(
					Expression.Call(
						localDateTimeType.GetMethod("FromDateTime", new[] { typeof(DateTime) })!,
						Expression.Call(
							MethodHelper.GetMethodInfo(DateTime.Parse, ""),
							sParameter)),
					sParameter));

			var p  = Expression.Parameter(typeof(object), "obj");
			var ex = Expression.Lambda<Func<object,DateTime>>(
				Expression.Block(
					new[] { ldtParameter },
					Expression.Assign(ldtParameter, Expression.Convert(p, localDateTimeType)),
					newDateTime),
				p);

			var l = ex.Compile();

			mappingSchema.SetValueToSqlConverter(localDateTimeType, (sb, _, v) => sb.Append('\'').Append(l(v).ToString()).Append('\''));
		}
	}
}
