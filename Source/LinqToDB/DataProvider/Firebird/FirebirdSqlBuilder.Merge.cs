namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.SqlQuery;
	using System.Globalization;
	using System.Text;

	public partial class FirebirdSqlBuilder
	{
		// source subquery select list shouldn't contain parameters otherwise following error will be
		// generated:
		//
		// FirebirdSql.Data.Common.IscException : Dynamic SQL Error
		// SQL error code = -804
		//Data type unknown
		protected override bool MergeSupportsParametersInSource => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		protected override string FakeTable => "rdb$database";

		// we need to specify type for string literals
		protected override bool MergeSourceTypesRequired => true;

		protected override void BuildTypedExpression(SqlDataType dataType, ISqlExpression value)
		{
			// without type case firebird will convert string to CHAR(LENGTH_OF_BIGGEST_VALUE_IN_COLUMN) and pad
			// all shorter values with spaces
			var length = 0;
			var typeRequired = false;
			if (value is SqlValue sqlValue && sqlValue.Value is string stringValue)
			{
				typeRequired = true;
				length = Encoding.UTF8.GetByteCount(stringValue);
				if (length == 0)
					length = 1;
			}

			if (typeRequired)
				StringBuilder.Append("CAST(");

			base.BuildTypedExpression(dataType, value);

			if (typeRequired)
				StringBuilder.Append($" AS VARCHAR({length.ToString(CultureInfo.InvariantCulture)}))");
		}
	}
}
