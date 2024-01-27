using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;
using LinqToDB.Data;

namespace LinqToDB.Tools
{
	using System.Globalization;

	using Common;
	using Comparers;
	using Expressions;
	using Linq;
	using Mapping;
	using Reflection;
	using SqlQuery;

	[PublicAPI]
	public static class MappingSchemaExtensions
	{
		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="mappingSchema">Instance of <see cref="MappingSchema" />.</param>
		/// <param name="columnPredicate">A function to filter columns to compare.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			this MappingSchema mappingSchema,
			[InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			if (mappingSchema   == null) throw new ArgumentNullException(nameof(mappingSchema));
			if (columnPredicate == null) throw new ArgumentNullException(nameof(columnPredicate));

			var cols = new HashSet<MemberAccessor>(
				mappingSchema.GetEntityDescriptor(typeof(T)).Columns
					.Where(columnPredicate).Select(c => c.MemberAccessor));

			return ComparerBuilder.GetEqualityComparer<T>(cols.Contains);
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="mappingSchema">Instance of <see cref="MappingSchema" />.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>(this MappingSchema mappingSchema)
		{
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			var cols = new HashSet<MemberAccessor>(
				mappingSchema.GetEntityDescriptor(typeof(T)).Columns
					.Select(c => c.MemberAccessor));

			return ComparerBuilder.GetEqualityComparer<T>(cols.Contains);
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="mappingSchema">Instance of <see cref="MappingSchema" />.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>(this MappingSchema mappingSchema)
		{
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			var cols = new HashSet<MemberAccessor>(
				mappingSchema.GetEntityDescriptor(typeof(T)).Columns
					.Where(c => c.IsPrimaryKey).Select(c => c.MemberAccessor));

			if (cols.Count > 0)
				return mappingSchema.GetEqualityComparer<T>(c => c.IsPrimaryKey);

			return mappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="IDataContext" />.</param>
		/// <param name="columnPredicate">A function to filter columns to compare.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			this IDataContext dataContext,
			[InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
		{
			if (dataContext     == null) throw new ArgumentNullException(nameof(dataContext));
			if (columnPredicate == null) throw new ArgumentNullException(nameof(columnPredicate));

			return dataContext.MappingSchema.GetEqualityComparer<T>(columnPredicate);
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="IDataContext" />.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>(this IDataContext dataContext)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			return dataContext.MappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="dataContext">Instance of <see cref="IDataContext" />.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>(this IDataContext dataContext)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			return dataContext.MappingSchema.GetKeyEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="ITable{T}" />.</param>
		/// <param name="columnPredicate">A function to filter columns to compare.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEqualityComparer<T>(
			this ITable<T> table,
			[InstantHandle] Func<ColumnDescriptor,bool> columnPredicate)
			where T : notnull
		{
			if (table           == null) throw new ArgumentNullException(nameof(table));
			if (columnPredicate == null) throw new ArgumentNullException(nameof(columnPredicate));

			return table.DataContext.MappingSchema.GetEqualityComparer<T>(columnPredicate);
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="ITable{T}" />.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetEntityEqualityComparer<T>(this ITable<T> table)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			return table.DataContext.MappingSchema.GetEntityEqualityComparer<T>();
		}

		/// <summary>
		/// Returns implementations of the <see cref="IEqualityComparer{T}" /> generic interface
		/// based on provided entity primary key columns equality.
		/// </summary>
		/// <param name="table">Instance of <see cref="ITable{T}" />.</param>
		/// <returns>Instance of <see cref="IEqualityComparer{T}" />.</returns>
		/// <typeparam name="T">The type of entity to compare.</typeparam>
		[Pure]
		public static IEqualityComparer<T> GetKeyEqualityComparer<T>(this ITable<T> table)
			where T : notnull
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			return table.DataContext.MappingSchema.GetKeyEqualityComparer<T>();
		}

		public static bool UseNodaTime(this MappingSchema mappingSchema, MappingSchema? remoteContextSerializationSchema = null)
		{
			if (Type.GetType("NodaTime.LocalDateTime, NodaTime", false) is {} type)
			{
				UseNodaTime(mappingSchema, type, remoteContextSerializationSchema);
				return true;
			}

			return false;
		}

		public static void UseNodaTime(this MappingSchema mappingSchema, Type localDateTimeType, MappingSchema? remoteContextSerializationSchema = null)
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
							MemberHelper.PropertyOf<DataParameter>(dp => dp.Value),
							Expression.Convert(
								newDateTime,
								typeof(object))),
						Expression.Bind(
							MemberHelper.PropertyOf<DataParameter>(dp => dp.DataType),
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

			// LocalDateTime.FromDateTime(DateTime),

			var dtParameter = Expression.Parameter(typeof(DateTime), "dt");

			mappingSchema.SetConvertExpression(
				typeof(DateTime),
				localDateTimeType,
				Expression.Lambda(
					Expression.Call(
						localDateTimeType.GetMethod("FromDateTime", new[] { typeof(DateTime) })!,
						dtParameter),
					dtParameter));

			// LocalDateTime.FromDateTime(DateTimeOffset.LocalDateTime),

			var dtoParameter = Expression.Parameter(typeof(DateTimeOffset), "dto");

			mappingSchema.SetConvertExpression(
				typeof(DateTimeOffset),
				localDateTimeType,
				Expression.Lambda(
					Expression.Call(
						localDateTimeType.GetMethod("FromDateTime", new[] { typeof(DateTime) })!,
						Expression.Property(dtoParameter, "LocalDateTime")),
					dtoParameter));

			// LocalDateTime.FromDateTime(DateTime.Parse(string, IvariantInfo)),

			var sParameter = Expression.Parameter(typeof(string), "str");

			mappingSchema.SetConvertExpression(
				typeof(string),
				localDateTimeType,
				Expression.Lambda(
					Expression.Call(
						localDateTimeType.GetMethod("FromDateTime", new[] { typeof(DateTime) })!,
						Expression.Call(
							MethodHelper.GetMethodInfo(DateTime.Parse, "", (IFormatProvider)null!),
							sParameter,
							Expression.Constant(DateTimeFormatInfo.InvariantInfo))),
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

			if (remoteContextSerializationSchema != null)
			{
				var localDateTimePattern = localDateTimeType.Assembly.GetType("NodaTime.Text.LocalDateTimePattern", true)!;
				var pattern = Expression.Property((Expression?)null, localDateTimePattern, "FullRoundtrip");

				// ldt => LocalDateTimePattern.FullRoundtrip.Format(ldt)

				var ldt = Expression.Parameter(localDateTimeType, "ldt");

				var serializer = Expression.Lambda(
					Expression.Call(pattern, "Format", [], ldt),
					ldt);

				remoteContextSerializationSchema.SetConvertExpression(localDateTimeType, typeof(string), serializer);

				// str => LocalDateTimePattern.FullRoundtrip.Parse(str).Value

				var str = Expression.Parameter(typeof(string), "str");

				var deserializer = Expression.Lambda(
					Expression.Property(
						Expression.Call(pattern, "Parse", [], str),
						"Value"),
					str);

				remoteContextSerializationSchema.SetConvertExpression(typeof(string), localDateTimeType, deserializer);
			}
		}
	}
}
