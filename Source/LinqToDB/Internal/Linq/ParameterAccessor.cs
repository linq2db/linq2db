using System;

using LinqToDB.Internal.SqlQuery;

#if DEBUG
using System.Linq.Expressions;
#endif

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	sealed class ParameterAccessor
	{
		public ParameterAccessor(
			int                                                       accessorId,
			Func<IQueryExpressions, IDataContext?,object?[]?,object?> clientValueAccessor,
			Func<object?,object?>?                                    clientToProviderConverter,
			Func<object?,object?>?                                    itemAccessor,
			Func<object?,DbDataType>?                                 dbDataTypeAccessor,
			SqlParameter                                              sqlParameter)
		{
			AccessorId                = accessorId;
			ClientToProviderConverter = clientToProviderConverter;
			ClientValueAccessor       = clientValueAccessor;
			ItemAccessor              = itemAccessor;
			DbDataTypeAccessor        = dbDataTypeAccessor;
			SqlParameter              = sqlParameter;
		}

		public readonly int                                                      AccessorId;
		public readonly Func<IQueryExpressions,IDataContext?,object?[]?,object?> ClientValueAccessor;
		public readonly Func<object?,object?>?                                   ClientToProviderConverter;
		public readonly Func<object?,object?>?                                   ItemAccessor;
		public readonly Func<object?,DbDataType>?                                DbDataTypeAccessor;
		public readonly SqlParameter                                             SqlParameter;
#if DEBUG
		public Expression<Func<IQueryExpressions,IDataContext?,object?[]?,object?>>? AccessorExpr;
#endif
	}
}
