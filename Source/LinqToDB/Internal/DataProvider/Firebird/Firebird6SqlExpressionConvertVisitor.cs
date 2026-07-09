using System;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class Firebird6SqlExpressionConvertVisitor : Firebird3SqlExpressionConvertVisitor
	{
		public Firebird6SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			// Read a Guid as its canonical text form via UUID_TO_CHAR. A Guid is stored / produced as
			// CHAR(16) CHARACTER SET OCTETS (GEN_UUID, and how ConvertGuidToSql writes it); on Firebird 6
			// reading those raw bytes back fails under a non-NONE connection charset ("Malformed string",
			// SQLSTATE 22000) because the client transliterates the column to the connection charset during
			// fetch. The ASCII text form is charset-independent. Only Firebird 6 needs this — earlier versions
			// fetch the binary form without transliteration, so the wrap stays scoped to this visitor.
			if (expr.SystemType?.ToUnderlying() == typeof(Guid)
				&& QueryHelper.GetDbDataType(expr, MappingSchema).DataType is not (DataType.Char or DataType.NChar or DataType.VarChar or DataType.NVarChar))
			{
				var textType = MappingSchema.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(36);
				return new SqlFunction(textType, "UUID_TO_CHAR", expr);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
