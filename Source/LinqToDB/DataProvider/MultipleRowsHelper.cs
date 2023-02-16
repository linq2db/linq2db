﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using Extensions;

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
			DataConnection = dataConnection is DataConnection dc
				? dc
				: dataConnection is DataContext dx
					? dx.GetDataConnection()
					: throw new ArgumentException($"Must be of {nameof(DataConnection)} or {nameof(DataContext)} type but was {dataConnection.GetType()}", nameof(dataConnection));

			MappingSchema  = dataConnection.MappingSchema;
			Options        = options;
			SqlBuilder     = DataConnection.DataProvider.CreateSqlBuilder(MappingSchema, DataConnection.Options);
			Descriptor     = MappingSchema.GetEntityDescriptor(entityType, options.ConnectionOptions.OnEntityDescriptorCreated);
			Columns        = Descriptor.Columns
				.Where(c => !c.SkipOnInsert || c.IsIdentity && options.BulkCopyOptions.KeepIdentity == true)
				.ToArray();
			ColumnTypes    = Columns.Select(c => new SqlDataType(c)).ToArray();
			ParameterName  = SqlBuilder.ConvertInline("p", ConvertType.NameToQueryParameter);
			BatchSize      = Math.Max(10, Options.BulkCopyOptions.MaxBatchSize ?? 1000);
		}

		public readonly ISqlBuilder         SqlBuilder;
		public readonly DataConnection      DataConnection;
		public readonly MappingSchema       MappingSchema;
		public readonly DataOptions         Options;
		public readonly EntityDescriptor    Descriptor;
		public readonly ColumnDescriptor[]  Columns;
		public readonly SqlDataType[]       ColumnTypes;
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

		public void SetHeader()
		{
			HeaderSize = StringBuilder.Length;
		}

		static readonly Func<ColumnDescriptor, bool> _defaultSkipConvert = _ => false;

		public virtual void BuildColumns(
			object                        item,
			Func<ColumnDescriptor, bool>? skipConvert                   = null,
			bool                          castParameters                = false,
			bool                          castAllRows                   = false,
			bool                          castFirstRowLiteralOnUnionAll = false,
			Func<ColumnDescriptor, bool>? castLiteral                   = null)
		{
			skipConvert ??= _defaultSkipConvert;

			for (var i = 0; i < Columns.Length; i++)
			{
				var column = Columns[i];
				var value  = column.GetProviderValue(item);

				var position = StringBuilder.Length;

				if (Options.BulkCopyOptions.UseParameters || skipConvert(column) || !MappingSchema.TryConvertToSql(StringBuilder, ColumnTypes[i], Options, value))
				{
					var name = ParameterName == "?" ? ParameterName : ParameterName + ++ParameterIndex;

					if (castParameters && (CurrentCount == 0 || castAllRows))
					{
						AddValueCasted(name, ColumnTypes[i]);
					}
					else
					{
						StringBuilder.Append(name);
					}


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
				else if (castFirstRowLiteralOnUnionAll && CurrentCount == 0 && castLiteral?.Invoke(Columns[i]) != false)
				{
					var literal          = StringBuilder.ToString(position, StringBuilder.Length - position);
					StringBuilder.Length = position;
					AddValueCasted(literal, ColumnTypes[i]);
				}

				StringBuilder.Append(',');
			}

			StringBuilder.Length--;
		}

		private void AddValueCasted(string sql, SqlDataType type)
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
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
