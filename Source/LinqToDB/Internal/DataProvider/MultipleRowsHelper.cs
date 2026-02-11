using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.SqlQuery;
using LinqToDB.Mapping;
using System.Globalization;

namespace LinqToDB.Internal.DataProvider
{
	public class MultipleRowsHelper<T> : MultipleRowsHelper
		where T : notnull
	{
		public MultipleRowsHelper(ITable<T> table, DataOptions options)
			: base(table.DataContext, options, typeof(T))
		{
			TableName = BasicBulkCopy.GetTableName(SqlBuilder, options.BulkCopyOptions, table);
		}
	}

	public abstract class MultipleRowsHelper
	{
		protected MultipleRowsHelper(IDataContext dataConnection, DataOptions options, Type entityType)
		{
			OriginalContext = dataConnection;
			DataConnection  = dataConnection is DataConnection dc
				? dc
				: dataConnection is DataContext dx
					? dx.GetDataConnection()
					: throw new ArgumentException($"Must be of {nameof(DataConnection)} or {nameof(DataContext)} type but was {dataConnection.GetType()}", nameof(dataConnection));

			MappingSchema   = dataConnection.MappingSchema;
			Options         = options;
			SqlBuilder      = DataConnection.DataProvider.CreateSqlBuilder(MappingSchema, DataConnection.Options);
			Descriptor      = MappingSchema.GetEntityDescriptor(entityType, options.ConnectionOptions.OnEntityDescriptorCreated);
			Columns         = Descriptor.Columns
				.Where(c => !c.SkipOnInsert || (c.IsIdentity && options.BulkCopyOptions.KeepIdentity == true))
				.ToArray();
			//TODO: check how to remove SqlDataType here
			ColumnTypes   = Columns.Select(c => new SqlDataType(c).Type).ToArray();
			ParameterName = "p";
			BatchSize     = Math.Max(10, Options.BulkCopyOptions.MaxBatchSize ?? 1000);
		}

		public readonly ISqlBuilder         SqlBuilder;
		public readonly IDataContext        OriginalContext;
		public readonly DataConnection      DataConnection;
		public readonly MappingSchema       MappingSchema;
		public readonly DataOptions         Options;
		public readonly EntityDescriptor    Descriptor;
		public readonly ColumnDescriptor[]  Columns;
		public readonly DbDataType[]        ColumnTypes;
		public          string?             TableName;
		public readonly string              ParameterName;

		public readonly List<DataParameter> Parameters    = new ();
		public readonly StringBuilder       StringBuilder = new ();
		public readonly BulkCopyRowsCopied  RowsCopied    = new ();

		public int CurrentCount;
		public int ParameterIndex;
		public int HeaderSize;
		public int BatchSize;
		public int LastRowStringIndex;
		public int LastRowParameterIndex;

		private static readonly Func<DataOptions, ColumnDescriptor, object?, bool> _defaultConvertToParameter = (o, _, _) => o.BulkCopyOptions.UseParameters;

		public bool SuppressCloseAfterUse { get; set; }

		public Func<DataOptions, ColumnDescriptor, object?, bool> ConvertToParameter { get; set; } = _defaultConvertToParameter;

		public void SetHeader()
		{
			HeaderSize = StringBuilder.Length;
		}

		public virtual void BuildColumns(
			object                                 item,
			bool                                   castParameters                = false,
			bool                                   castAllRows                   = false,
			bool                                   castFirstRowLiteralOnUnionAll = false,
			Func<ColumnDescriptor, bool>?          castLiteral                   = null)
		{
			for (var i = 0; i < Columns.Length; i++)
			{
				var column = Columns[i];
				var type   = ColumnTypes[i];
				var value  = column.GetProviderValue(item);

				var position = StringBuilder.Length;

				if (ConvertToParameter(Options, column, value) || !MappingSchema.TryConvertToSql(StringBuilder, type, Options, value))
				{
					var name = SqlBuilder.ConvertInline(string.Equals(ParameterName, "?", StringComparison.Ordinal) ? ParameterName : string.Create(CultureInfo.InvariantCulture, $"{ParameterName}{++ParameterIndex}"), ConvertType.NameToQueryParameter);

					if (castParameters && (CurrentCount == 0 || castAllRows))
					{
						AddValueCasted(name, type);
					}
					else
					{
						StringBuilder.Append(name);
					}

					if (value is DataParameter dataParameter)
						value = dataParameter.Value;

					Parameters.Add(new DataParameter(
						SqlBuilder.ConvertInline(string.Equals(ParameterName, "?", StringComparison.Ordinal) ? ParameterName : string.Create(CultureInfo.InvariantCulture, $"p{ParameterIndex}"), ConvertType.NameToQueryParameter),
						value, type.DataType, type.DbType)
					{
						Size      = type.Length,
						Precision = type.Precision,
						Scale     = type.Scale,
					});
				}
				else if (castFirstRowLiteralOnUnionAll && CurrentCount == 0 && castLiteral?.Invoke(Columns[i]) != false)
				{
					var literal          = StringBuilder.ToString(position, StringBuilder.Length - position);
					StringBuilder.Length = position;
					AddValueCasted(literal, type);
				}

				StringBuilder.Append(',');
			}

			StringBuilder.Length--;
		}

		private void AddValueCasted(string sql, DbDataType type)
		{
			StringBuilder.Append("CAST(");
			StringBuilder.Append(sql);
			StringBuilder.Append(" AS ");
			SqlBuilder.BuildDataType(StringBuilder, type);
			StringBuilder.Append(')');
		}

		public bool Execute()
		{
			var commandSql = StringBuilder.AppendLine().ToString();
			var parameters = Parameters.ToArray();

			DataConnection.Execute(commandSql, parameters);

			if (Options.BulkCopyOptions.RowsCopiedCallback != null)
			{
				Options.BulkCopyOptions.RowsCopiedCallback(RowsCopied);

				if (RowsCopied.Abort)
					return false;
			}

			Parameters.Clear();
			ParameterIndex        = 0;
			CurrentCount          = 0;
			LastRowParameterIndex = 0;
			LastRowStringIndex    = HeaderSize;
			StringBuilder.Length  = HeaderSize;

			return true;
		}

		internal bool ExecuteCustom(Func<DataConnection, string, DataParameter[], int> customExecute)
		{
			var commandSql = StringBuilder.AppendLine().ToString();
			var parameters = Parameters.ToArray();

			customExecute(DataConnection, commandSql, parameters);

			if (Options.BulkCopyOptions.RowsCopiedCallback != null)
			{
				Options.BulkCopyOptions.RowsCopiedCallback(RowsCopied);

				if (RowsCopied.Abort)
					return false;
			}

			Parameters.Clear();
			ParameterIndex        = 0;
			CurrentCount          = 0;
			LastRowParameterIndex = 0;
			LastRowStringIndex    = HeaderSize;
			StringBuilder.Length  = HeaderSize;

			return true;
		}

		public async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			await DataConnection.ExecuteAsync(StringBuilder.AppendLine().ToString(), cancellationToken, Parameters.ToArray())
				.ConfigureAwait(false);

			if (Options.BulkCopyOptions.RowsCopiedCallback != null)
			{
				Options.BulkCopyOptions.RowsCopiedCallback(RowsCopied);

				if (RowsCopied.Abort)
					return false;
			}

			Parameters.Clear();

			ParameterIndex        = 0;
			CurrentCount          = 0;
			LastRowParameterIndex = 0;
			LastRowStringIndex    = HeaderSize;
			StringBuilder.Length  = HeaderSize;

			return true;
		}
	}
}
