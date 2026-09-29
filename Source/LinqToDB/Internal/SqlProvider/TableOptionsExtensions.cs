using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.SqlProvider
{
	public static partial class TableOptionsExtensions
	{
		public static bool IsSet                        (this TableOptions tableOptions) => tableOptions != TableOptions.NotSet;

		public static bool IsTemporaryOptionSet         (this TableOptions tableOptions) => (tableOptions & TableOptions.IsTemporaryOptionSet) != 0;

		public static bool HasCreateIfNotExists         (this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.CreateIfNotExists);

		public static bool HasDropIfExists              (this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.DropIfExists);

		public static bool HasIsTemporary               (this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.IsTemporary);

		public static bool HasIsLocalTemporaryStructure (this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.IsLocalTemporaryStructure);

		public static bool HasIsGlobalTemporaryStructure(this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.IsGlobalTemporaryStructure);

		public static bool HasIsLocalTemporaryData      (this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.IsLocalTemporaryData);

		public static bool HasIsGlobalTemporaryData     (this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.IsGlobalTemporaryData);

		public static bool HasIsTransactionTemporaryData(this TableOptions tableOptions) => tableOptions.HasFlag(TableOptions.IsTransactionTemporaryData);

		public static TableOptions Or(this TableOptions tableOptions, TableOptions additionalOptions)
		{
			return tableOptions == TableOptions.NotSet ? additionalOptions : tableOptions;
		}

		extension(TableOptions tableOptions)
		{
			public TableOptions TemporaryOptionValue => tableOptions & TableOptions.IsTemporaryOptionSet;
		}
	}
}
