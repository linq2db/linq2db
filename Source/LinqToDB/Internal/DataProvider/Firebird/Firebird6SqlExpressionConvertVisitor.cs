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

		protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
		{
			// Write mirror of the UUID_TO_CHAR read wrap above. A Guid bound as a parameter is compared
			// against / written to a CHAR(16) CHARACTER SET OCTETS column. Binding it in any binary form
			// (FbDbType.Guid, byte[], any FbParameter.Charset incl. Octets/None) fails "Malformed string"
			// (SQLSTATE 22000) when the value hits a matching row on Firebird 6. Instead wrap the placeholder
			// in CHAR_TO_UUID and bind the canonical 36-char text (FirebirdDataProvider.SetParameter rebinds
			// the Guid value to a VARCHAR string on v6+). Inline Guid constants stay SqlValue (emitted as an
			// X'..' literal, which works), so only bound parameters are rewritten here.
			if (sqlParameter.IsQueryParameter
				&& sqlParameter.Type.SystemType.ToUnderlying() == typeof(Guid))
			{
				// CHAR_TO_UUID supplies the octets typing; the parameter itself must not be cast to the Guid
				// column type (BINARY(16)) — it is bound as a VARCHAR string, so an inner CAST AS BINARY(16)
				// (emitted for NeedsCast parameters in UPDATE SET) would corrupt it.
				sqlParameter.NeedsCast = false;
				return new SqlFunction(sqlParameter.Type, "CHAR_TO_UUID", sqlParameter);
			}

			return base.VisitSqlParameter(sqlParameter);
		}

		protected internal override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			// The wrapper created in VisitSqlParameter is CHAR_TO_UUID(<Guid query parameter>). The visitor
			// re-descends into a replaced element's children, so without this its argument would be routed
			// back through VisitSqlParameter and wrapped again indefinitely. The argument is already final —
			// return the wrapper without descending. A user-authored CHAR_TO_UUID (string -> Guid cast) has a
			// non-parameter argument, so it falls through to normal processing.
			if (element.Name == "CHAR_TO_UUID"
				&& element.Parameters is [SqlParameter { IsQueryParameter: true } arg]
				&& arg.Type.SystemType.ToUnderlying() == typeof(Guid))
			{
				return element;
			}

			return base.VisitSqlFunction(element);
		}
	}
}
