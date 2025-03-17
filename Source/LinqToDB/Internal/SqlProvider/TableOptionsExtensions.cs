using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.SqlProvider
{
	public static partial class TableOptionsExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSet                        (this TableOptions tableOptions) => tableOptions != TableOptions.NotSet;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTemporaryOptionSet         (this TableOptions tableOptions) => (tableOptions & TableOptions.IsTemporaryOptionSet) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasCreateIfNotExists         (this TableOptions tableOptions) => (tableOptions & TableOptions.CreateIfNotExists) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasDropIfExists              (this TableOptions tableOptions) => (tableOptions & TableOptions.DropIfExists) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsTemporary               (this TableOptions tableOptions) => (tableOptions & TableOptions.IsTemporary) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsLocalTemporaryStructure (this TableOptions tableOptions) => (tableOptions & TableOptions.IsLocalTemporaryStructure) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsGlobalTemporaryStructure(this TableOptions tableOptions) => (tableOptions & TableOptions.IsGlobalTemporaryStructure) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsLocalTemporaryData      (this TableOptions tableOptions) => (tableOptions & TableOptions.IsLocalTemporaryData) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsGlobalTemporaryData     (this TableOptions tableOptions) => (tableOptions & TableOptions.IsGlobalTemporaryData) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasIsTransactionTemporaryData(this TableOptions tableOptions) => (tableOptions & TableOptions.IsTransactionTemporaryData) != 0;

		public static TableOptions Or(this TableOptions tableOptions, TableOptions additionalOptions)
		{
			return tableOptions == TableOptions.NotSet ? additionalOptions : tableOptions;
		}
	}
}
