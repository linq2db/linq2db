using System;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.Interceptors;
using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider
{
	public static class DataProviderExtensions
	{
		/// <summary>
		/// Sets the reader expression for a field in the data provider.
		/// </summary>
		/// <typeparam name="TDbDataReader">
		/// Type of the database data reader, e.g. <see cref="DbDataReader"/>.
		/// </typeparam>
		/// <typeparam name="T">
		/// Type of the field to be read, e.g. <see cref="decimal"/> or <see cref="string"/>.
		/// </typeparam>
		/// <param name="dataProvider">
		/// The data provider instance where the reader expression will be set.
		/// </param>
		/// <param name="dataReaderType">
		/// Type of <see cref="DbDataReader"/> implementation. Could not match Type, implemented by ADO.NET provider if wrapper like MiniProfiler used without proper <see cref="IUnwrapDataObjectInterceptor"/> registration provided.
		/// </param>
		/// <param name="toType">
		/// Expected type (e.g. type of property in mapped entity class). For nullable value types doesn't include <see cref="Nullable{T}"/> wrapper.
		/// </param>
		/// <param name="providerFieldType">
		/// Type, returned by <see cref="DbDataReader.GetProviderSpecificFieldType(int)"/> for column.
		/// </param>
		/// <param name="fieldType">
		/// Type, returned by <see cref="DbDataReader.GetFieldType(int)"/> for column.
		/// </param>
		/// <param name="dataTypeName">
		/// Type name, returned by <see cref="DbDataReader.GetDataTypeName(int)"/> for column.
		/// </param>
		/// <param name="expr"></param>
		public static void SetFieldReaderExpression<TDbDataReader,T>(
			this IDataProvider                    dataProvider,
			Type?                                 dataReaderType,
			Type?                                 toType,
			Type?                                 providerFieldType,
			Type?                                 fieldType,
			string?                               dataTypeName,
			Expression<Func<TDbDataReader,int,T>> expr)
			where TDbDataReader : DbDataReader
		{
			((DataProviderBase)dataProvider).ReaderExpressions[new ReaderInfo
			{
				DataReaderType    = dataReaderType,
				ToType            = toType,
				ProviderFieldType = providerFieldType,
				FieldType         = fieldType,
				DataTypeName      = dataTypeName
			}] = expr;
		}

		/// <summary>
		/// Sets the reader expression for a field of type <typeparamref name="T"/> in the data provider.
		/// <code>
		/// var dataProvider = SqlServerTools.GetDataProvider(SqlServerVersion.v2019);
		///
		/// dataProvider.SetFieldReaderExpression&lt;SqlDataReader,decimal&gt;((r, i) => GetDecimal(r, i));
		///
		/// static decimal GetDecimal(SqlDataReader rd, int index)
		/// {
		///     var value = rd.GetSqlDecimal(index);
		///
		///     if (value.Precision > 29)
		///     {
		///         var str = value.ToString();
		///         var val = decimal.Parse(str);
		///         return val;
		///     }
		///
		///     return value.Value;
		/// }
		/// </code>
		/// </summary>
		/// <typeparam name="TDbDataReader">
		/// Type of the database data reader, e.g. <see cref="DbDataReader"/>.
		/// </typeparam>
		/// <typeparam name="T">
		/// Type of the field to be read, e.g. <see cref="decimal"/> or <see cref="string"/>.
		/// </typeparam>
		/// <param name="dataProvider">
		/// The data provider instance where the reader expression will be set.
		/// </param>
		/// <param name="expr">
		/// The expression that defines how to read the field from the data reader.
		/// The expression should take two parameters: the data reader and the index of the field.
		/// </param>
		public static void SetFieldReaderExpression<TDbDataReader,T>(this IDataProvider dataProvider, Expression<Func<TDbDataReader,int,T>> expr)
		where TDbDataReader : DbDataReader
		{
			dataProvider.SetFieldReaderExpression(null, null, null, fieldType : typeof(T), null, expr);
		}

		/// <summary>
		/// Sets the reader expression for a field of type <typeparamref name="T"/> in the data provider.
		/// <code>
		/// var dataProvider = SqlServerTools.GetDataProvider(SqlServerVersion.v2019);
		///
		/// dataProvider.SetFieldReaderExpression&lt;DbDataReader,DateTime&gt;("time", (r, i) => GetDateTimeAsTime(r.GetDateTime(i)));
		///
		/// static DateTime GetDateTimeAsTime(DateTime value)
		/// {
		///     if (value is { Year: 1900, Month: 1, Day: 1 })
		///         return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);
		///     return value;
		/// }
		/// </code>
		/// </summary>
		/// <typeparam name="TDbDataReader">
		/// Type of the database data reader, e.g. <see cref="DbDataReader"/>.
		/// </typeparam>
		/// <typeparam name="T">
		/// Type of the field to be read, e.g. <see cref="decimal"/> or <see cref="string"/>.
		/// </typeparam>
		/// <param name="dataProvider">
		/// The data provider instance where the reader expression will be set.
		/// </param>
		/// <param name="dataTypeName">
		/// The name of the data type as returned by <see cref="DbDataReader.GetDataTypeName(int)"/>.
		/// </param>
		/// <param name="expr">
		/// The expression that defines how to read the field from the data reader.
		/// The expression should take two parameters: the data reader and the index of the field.
		/// </param>
		public static void SetFieldReaderExpression<TDbDataReader,T>(this IDataProvider dataProvider, string dataTypeName, Expression<Func<TDbDataReader,int,T>> expr)
		where TDbDataReader : DbDataReader
		{
			dataProvider.SetFieldReaderExpression(null, null, null, fieldType: typeof(T), dataTypeName: dataTypeName, expr);
		}

		/// <summary>
		/// Sets the reader expression for a field of type <typeparamref name="T"/> in the data provider.
		/// <code>
		/// var dataProvider = SqlServerTools.GetDataProvider(SqlServerVersion.v2019);
		///
		/// dataProvider.SetFieldReaderExpression&lt;DbDataReader,string&gt;(typeof(byte[]), "vector", (r, i) => r.GetString(i));
		/// </code>
		/// </summary>
		/// <typeparam name="TDbDataReader">
		/// Type of the database data reader, e.g. <see cref="DbDataReader"/>.
		/// </typeparam>
		/// <typeparam name="T">
		/// Type of the field to be read, e.g. <see cref="decimal"/> or <see cref="string"/>.
		/// </typeparam>
		/// <param name="dataProvider">
		/// The data provider instance where the reader expression will be set.
		/// </param>
		/// <param name="dataTypeName">
		/// The name of the data type as returned by <see cref="DbDataReader.GetDataTypeName(int)"/>.
		/// </param>
		/// <param name="fieldType">
		/// The type of the field as returned by <see cref="DbDataReader.GetFieldType(int)"/>.
		/// </param>
		/// <param name="expr">
		/// The expression that defines how to read the field from the data reader.
		/// The expression should take two parameters: the data reader and the index of the field.
		/// </param>
		public static void SetFieldReaderExpression<TDbDataReader,T>(this IDataProvider dataProvider, Type fieldType, string dataTypeName, Expression<Func<TDbDataReader,int,T>> expr)
		where TDbDataReader : DbDataReader
		{
			dataProvider.SetFieldReaderExpression(null, null, null, fieldType: fieldType, dataTypeName: dataTypeName, expr);
		}
	}
}
