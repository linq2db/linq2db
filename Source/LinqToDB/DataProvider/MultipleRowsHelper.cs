using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using System.Threading.Tasks;

	public class MultipleRowsHelper<T> : MultipleRowsHelper
	{
		public MultipleRowsHelper(ITable<T> table, BulkCopyOptions options)
			: base((DataConnection)table.DataContext, options, typeof(T))
		{
			TableName = BasicBulkCopy.GetTableName(SqlBuilder, options, table);
		}
	}

	public abstract class MultipleRowsHelper
	{
		protected MultipleRowsHelper(DataConnection dataConnection, BulkCopyOptions options, Type entityType)
		{
			DataConnection = dataConnection;
			Options        = options;
			SqlBuilder     = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
			ValueConverter = dataConnection.MappingSchema.ValueToSqlConverter;
			Descriptor     = dataConnection.MappingSchema.GetEntityDescriptor(entityType);
			Columns        = Descriptor.Columns
				.Where(c => !c.SkipOnInsert || c.IsIdentity && options.KeepIdentity == true)
				.ToArray();
			ColumnTypes    = Columns.Select(c => new SqlDataType(c)).ToArray();
			ParameterName  = SqlBuilder.ConvertInline("p", ConvertType.NameToQueryParameter);
			BatchSize      = Math.Max(10, Options.MaxBatchSize ?? 1000);
		}

		public readonly ISqlBuilder         SqlBuilder;
		public readonly DataConnection      DataConnection;
		public readonly BulkCopyOptions     Options;
		public readonly ValueToSqlConverter ValueConverter;
		public readonly EntityDescriptor    Descriptor;
		public readonly ColumnDescriptor[]  Columns;
		public readonly SqlDataType[]       ColumnTypes;
		public          string?             TableName;
		public readonly string              ParameterName;

		public readonly List<DataParameter> Parameters    = new List<DataParameter>();
		public readonly StringBuilder       StringBuilder = new StringBuilder();
		public readonly BulkCopyRowsCopied  RowsCopied    = new BulkCopyRowsCopied();

		public int CurrentCount;
		public int ParameterIndex;
		public int HeaderSize;
		public int BatchSize;

		public void SetHeader()
		{
			HeaderSize = StringBuilder.Length;
		}

		public virtual void BuildColumns(object item, Func<ColumnDescriptor, bool>? skipConvert = null)
		{
			skipConvert = skipConvert ?? (_ => false);

			for (var i = 0; i < Columns.Length; i++)
			{
				var column = Columns[i];
				var value  = column.GetValue(item);

				if (skipConvert(column) || !ValueConverter.TryConvert(StringBuilder, ColumnTypes[i], value))
				{
					var name = ParameterName == "?" ? ParameterName : ParameterName + ++ParameterIndex;

					StringBuilder.Append(name);

					if (value is DataParameter dataParameter)
						value = dataParameter.Value;

					Parameters.Add(new DataParameter(ParameterName == "?" ? ParameterName : "p" + ParameterIndex, value,
						column.DataType, column.DbType)
					{
						Size      = column.Length,
						Precision = column.Precision,
						Scale     = column.Scale
					});
				}

				StringBuilder.Append(",");
			}

			StringBuilder.Length--;
		}

		public bool Execute()
		{
			DataConnection.Execute(StringBuilder.AppendLine().ToString(), Parameters.ToArray());

			if (Options.RowsCopiedCallback != null)
			{
				Options.RowsCopiedCallback(RowsCopied);

				if (RowsCopied.Abort)
					return false;
			}

			Parameters.Clear();
			ParameterIndex       = 0;
			CurrentCount         = 0;
			StringBuilder.Length = HeaderSize;

			return true;
		}

		public async Task<bool> ExecuteAsync()
		{
			await DataConnection.ExecuteAsync(StringBuilder.AppendLine().ToString(), Parameters.ToArray())
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (Options.RowsCopiedCallback != null)
			{
				Options.RowsCopiedCallback(RowsCopied);

				if (RowsCopied.Abort)
					return false;
			}

			Parameters.Clear();
			ParameterIndex = 0;
			CurrentCount = 0;
			StringBuilder.Length = HeaderSize;

			return true;
		}
	}
}
