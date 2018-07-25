using System;
using System.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using Extensions;

	class SybaseMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		private bool _hasIdentityInsert;

		public SybaseMergeBuilder(DataConnection connection, IMergeable<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool IsIdentityInsertSupported
		{
			get
			{
				// Sybase supports implicit identify insert
				return true;
			}
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// Doesn't support VALUES(...) syntax in MERGE source
				return false;
			}
		}

		protected override bool EmptySourceSupported
		{
			get
			{
				// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
				return false;
			}
		}

		protected override void BuildTerminator()
		{
			if (_hasIdentityInsert)
				Command.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}

		protected override void OnInsertWithIdentity()
		{
			if (!_hasIdentityInsert)
			{
				_hasIdentityInsert = true;

				// this code should be added before MERGE and command already partially generated at this stage
				Command.Insert(0, string.Format("SET IDENTITY_INSERT {0} ON{1}", TargetTableName, Environment.NewLine));
			}
		}

		public override void Validate()
		{
			base.Validate();

			if (Merge.Operations.All(_ => _.Type == MergeOperationType.Delete))
				throw new LinqToDBException(string.Format("Merge only with Delete operations not supported by {0} provider.", ProviderName));
		}

		protected override void AddSourceValue(
			ValueToSqlConverter valueConverter,
			ColumnDescriptor    column,
			SqlDataType         columnType,
			object              value,
			bool                isFirstRow,
			bool                isLastRow)
		{
			// strange thing, that real type needs explicit typing only on some combinations of columns and values
			// from other side, for Sybase it is not surprising
			if (column.DataType == DataType.Single || (column.DataType == DataType.Undefined && column.MemberType.ToNullableUnderlying() == typeof(float)))
			{
				Command.Append("CAST(");
				base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
				Command.Append(" AS REAL)");
			}
			else if (column.DataType == DataType.DateTime || column.DataType == DataType.DateTime2)
			{
				Command.Append("CAST(");
				base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
				Command.Append(" AS DATETIME)");
			}
			else if (column.DataType == DataType.Date)
			{
				Command.Append("CAST(");
				base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
				Command.Append(" AS DATE)");
			}
			else if (column.DataType == DataType.Time)
			{
				Command.Append("CAST(");
				base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
				Command.Append(" AS TIME)");
			}
			else if (isFirstRow && value == null)
			{
				string type;

				if (column.DbType != null)
				{
					type = column.DbType;
				}
				else
				{
					var dataType = column.DataType != DataType.Undefined ?
						column.DataType :
						DataContext.MappingSchema.GetDataType(column.MemberType).DataType;

					switch (dataType)
					{
						case DataType.Double : type = "FLOAT";    break;
						case DataType.Single : type = "REAL";     break;
						case DataType.SByte  : type = "TINYINT";  break;
						case DataType.UInt16 : type = "INT";      break;
						case DataType.UInt32 : type = "BIGINT";   break;
						case DataType.UInt64 : type = "DECIMAL";  break;
						case DataType.Byte   : type = "TINYINT";  break;
						case DataType.Int16  : type = "SMALLINT"; break;
						case DataType.Int32  : type = "INT";      break;
						case DataType.Int64  : type = "BIGINT";   break;
						case DataType.Boolean: type = "BIT";      break;
						default              : type = null;       break;
					}
				}

				if (type != null)
				{
					Command
						.Append("CAST(NULL AS ")
						.Append(type)
						;

					if (column.Length > 0)
						Command
							.Append('(')
							.Append(column.Length)
							.Append(')')
							;

					if (column.Precision > 0)
						Command
							.Append('(')
							.Append(column.Precision)
							.Append(',')
							.Append(column.Scale)
							.Append(')')
							;

					Command
						.Append(")")
						;
				}
				else
					base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
			}
			else
				base.AddSourceValue(valueConverter, column, columnType, value, isFirstRow, isLastRow);
		}
	}
}
