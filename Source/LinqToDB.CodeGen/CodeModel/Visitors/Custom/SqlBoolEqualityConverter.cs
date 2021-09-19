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
				WellKnownTypes.SqlTypes.SqlBinary,
				WellKnownTypes.SqlTypes.SqlBoolean,
				WellKnownTypes.SqlTypes.SqlByte,
				WellKnownTypes.SqlTypes.SqlDateTime,
				WellKnownTypes.SqlTypes.SqlDecimal,
				WellKnownTypes.SqlTypes.SqlDouble,
				WellKnownTypes.SqlTypes.SqlGuid,
				WellKnownTypes.SqlTypes.SqlInt16,
				WellKnownTypes.SqlTypes.SqlInt32,
				WellKnownTypes.SqlTypes.SqlInt64,
				WellKnownTypes.SqlTypes.SqlMoney,
				WellKnownTypes.SqlTypes.SqlSingle,
				WellKnownTypes.SqlTypes.SqlString,

				WellKnownTypes.SqlServerTypes.SqlHierarchyId,
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
				return new CodeTypeCast(WellKnownTypes.Boolean, expression);

			return base.Visit(expression);
		}
	}
}
