using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// This visitor overrides equality operation for some well-known types which doesn't implement "bool operator==" and "bool operator!=".
	/// </summary>
	public sealed class ProviderSpecificStructsEqualityFixer : ConvertCodeModelVisitor
	{
		private readonly ISet<IType> _types;

		/// <summary>
		/// Creates instance of converter.
		/// </summary>
		/// <param name="languageProvider">Current language provider.</param>
		/// <returns>Converter instance.</returns>
		public ProviderSpecificStructsEqualityFixer(ILanguageProvider languageProvider)
		{
			// 2. System.Data.SqlTypes.* and SqlHierarchyId implement operators but they return SqlBoolean
			// 3. DB2 types doesn't implement operators at all
			var types = new HashSet<IType>()
			{
				// System.Data.SqlTypes.* implement operators but they return SqlBoolean
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

				// SqlHierarchyId implement operators but they return SqlBoolean
				WellKnownTypes.Microsoft.SqlServer.Types.SqlHierarchyId,

				// DB2 types doesn't implement operators at all except DB2TimeSpan and DB2DateTime
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Binary"      , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Date"        , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Decimal"     , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2DecimalFloat", true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Double"      , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int16"       , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int32"       , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int64"       , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Real"        , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Real370"     , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2RowId"       , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2String"      , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Time"        , true),
				languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2TimeStamp"   , true)
			};

			// all handled types are structs, so we need to handle Nullable<T> comparisons too
			_types = new HashSet<IType>(types.Concat(types.Select(t => t.WithNullability(true))), languageProvider.TypeEqualityComparerWithoutNRT);
		}

		protected override ICodeElement Visit(CodeBinary expression)
		{
			if ((  expression.Operation == BinaryOperation.Equal
				|| expression.Operation == BinaryOperation.NotEqual)
				&& _types.Contains(expression.Left.Type)
				&& _types.Contains(expression.Right.Type))
			{
				// Equals handles both nullable and non-nullable structs
				// x == y => x.Equals(y)
				// x != y => !x.Equals(y)
				ICodeExpression result = new CodeCallExpression(false, expression.Left, WellKnownTypes.System.Object_Equals, Array.Empty<IType>(), true, new []{ expression.Right }, null, WellKnownTypes.System.Boolean);
				if (expression.Operation == BinaryOperation.NotEqual)
					result = new CodeUnary(result, UnaryOperation.Not);

				return result;
			}

			return base.Visit(expression);
		}
	}
}
