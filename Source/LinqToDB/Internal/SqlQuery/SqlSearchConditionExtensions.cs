using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Internal.SqlQuery
{
	internal static class SqlSearchConditionExtensions
	{
		extension(SqlSearchCondition sqlSearchCondition)
		{
			public bool IsTrue =>
				sqlSearchCondition.Predicates switch
				{
					[] or [{ ElementType: QueryElementType.TruePredicate }] => true,

					_ => false,
				};

			public bool IsFalse =>
				sqlSearchCondition.Predicates is [{ ElementType: QueryElementType.FalsePredicate }];
		}
	}
}
