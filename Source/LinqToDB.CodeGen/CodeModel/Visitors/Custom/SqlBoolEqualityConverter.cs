using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// This visitor wraps equality operation for some well-known types with cast to <see cref="Boolean"/> as they
	/// override equality operation to return <see cref="SqlBoolean"/> value.
	/// </summary>
	public class SqlBoolEqualityConverter : ConvertCodeModelVisitor
	{
		private readonly ISet<IType> _types;

		public SqlBoolEqualityConverter(IEqualityComparer<IType> typeComparer)
		{
			_types = new HashSet<IType>(typeComparer)
			{
				WellKnownTypes.System.Data.SqlTypes.SqlBinary,
				WellKnownTypes.System.Data.SqlTypes.SqlBoolean,
				WellKnownTypes.System.Data.SqlTypes.SqlByte,
				WellKnownTypes.System.Data.SqlTypes.SqlDateTime,
				WellKnownTypes.System.Data.SqlTypes.SqlDecimal,
				WellKnownTypes.System.Data.SqlTypes.SqlDouble,
				WellKnownTypes.System.Data.SqlTypes.SqlGuid,
				WellKnownTypes.System.Data.SqlTypes.SqlInt16,
				WellKnownTypes.System.Data.SqlTypes.SqlInt32,
				WellKnownTypes.System.Data.SqlTypes.SqlInt64,
				WellKnownTypes.System.Data.SqlTypes.SqlMoney,
				WellKnownTypes.System.Data.SqlTypes.SqlSingle,
				WellKnownTypes.System.Data.SqlTypes.SqlString,

				WellKnownTypes.Microsoft.SqlServer.Types.SqlHierarchyId,
			};
		}

		public static ConvertCodeModelVisitor Create(ILanguageProvider languageProvider)
		{
			return new SqlBoolEqualityConverter(languageProvider.TypeEqualityComparerWithoutNRT);
		}

		protected override ICodeElement Visit(CodeBinary expression)
		{
			if (expression.Operation == BinaryOperation.Equal
				&& _types.Contains(expression.Left.Type))
				return new CodeTypeCast(WellKnownTypes.System.Boolean, expression);

			return base.Visit(expression);
		}
	}
}
