using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public interface IDataProvider
	{
		string           Name             { get; }
		Type             ConnectionType   { get; }
		Type             DataReaderType   { get; }
		MappingSchema    MappingSchema    { get; }
		SqlProviderFlags SqlProviderFlags { get; }

		IDbConnection    CreateConnection   (string connectionString);
		ISqlProvider     CreateSqlProvider  ();
		void             InitCommand        (DataConnection dataConnection);
		object           GetConnectionInfo  (DataConnection dataConnection, string parameterName);
		Expression       GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType);
		bool?            IsDBNullAllowed    (IDataReader reader, int idx);
		void             SetParameter       (IDbDataParameter parameter, string name, DataType dataType, object value);

		DatabaseSchema   GetSchema          (DataConnection dataConnection);
		int              BulkCopy<T>        (DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source);
	}
}
