using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider.Firebird
{
	class FirebirdMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public FirebirdMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// VALUES (...) syntax not supported by firebird
				return false;
			}
		}

		protected override string FakeSourceTable
		{
			get
			{
				// table with exactly one record to replace VALUES for enumerable source
				return "rdb$database";
			}
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// Firebird doesn't support INSERT FROM
				return true;
			}
		}

		protected override bool SupportsParametersInSource
		{
			get
			{
				// source subquery select list shouldn't contain parameters otherwise following error will be
				// generated:
				//
				// FirebirdSql.Data.Common.IscException : Dynamic SQL Error
				// SQL error code = -804
				//Data type unknown
				return false;
			}
		}

		protected override void AddSourceValue(
			ValueToSqlConverter valueConverter,
			ColumnDescriptor    column,
			SqlDataType         columnType,
			object              value,
			bool                isFirstRow)
		{
			if (value is string)
			{
				// without it firebird will convert it to CHAR(LENGTH_OF_BIGGEST_VALUE) and pad all values with spaces
				Command.Append("CAST(");
				valueConverter.TryConvert(Command, columnType, value);

				var stringValue = (string)value;
				var length = Encoding.UTF8.GetByteCount(stringValue);
				if (length == 0)
					length = 1;

				Command.AppendFormat(" AS VARCHAR({0}))", length.ToString(CultureInfo.InvariantCulture));
			}
			else
				base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow);
		}

		protected override void BuildMatch()
		{
			var old = FirebirdConfiguration.DisableConvertInnerJoinsToLeftJoins.Value;

			try
			{
				FirebirdConfiguration.DisableConvertInnerJoinsToLeftJoins.Value = true;

				base.BuildMatch();
			}
			finally
			{
				FirebirdConfiguration.DisableConvertInnerJoinsToLeftJoins.Value = old;
			}
		}

		protected override void BuildPredicateByTargetAndSource(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var old = FirebirdConfiguration.DisableConvertInnerJoinsToLeftJoins.Value;

			try
			{
				FirebirdConfiguration.DisableConvertInnerJoinsToLeftJoins.Value = true;

				base.BuildPredicateByTargetAndSource(predicate);
			}
			finally
			{
				FirebirdConfiguration.DisableConvertInnerJoinsToLeftJoins.Value = old;
			}
		}
	}
}
