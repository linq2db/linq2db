using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Linq;
	using Mapping;
	using SqlProvider;

	public interface IDataContext : IDisposable
	{
		string              ContextID         { get; }
		Func<ISqlBuilder>   CreateSqlProvider { get; }
		Func<ISqlOptimizer> GetSqlOptimizer   { get; }
		SqlProviderFlags    SqlProviderFlags  { get; }
		Type                DataReaderType    { get; }
		MappingSchema       MappingSchema     { get; }
		bool                InlineParameters  { get; set; }

		Expression          GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType);
		bool?               IsDBNullAllowed    (IDataReader reader, int idx);

		IQueryContext       GetQueryContext(Query query);

		object              SetQuery           (IQueryContextOld queryContext);
		int                 ExecuteNonQuery    (object query);
		object              ExecuteScalar      (object query);
		IDataReader         ExecuteReader      (object query);
		void                ReleaseQuery       (object query);

		string              GetSqlText         (object query);
		IDataContext        Clone              (bool forNestedQuery);

		event EventHandler  OnClosing;
	}
}
