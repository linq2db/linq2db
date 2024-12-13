namespace LinqToDB.CodeModel
{
	partial class TableLayoutBuilder
	{
		/// <summary>
		/// Column group data builder.
		/// </summary>
		public interface IGroupDataBuilder
		{
			/// <summary>
			/// Add new group instance.
			/// </summary>
			/// <returns>COlumn builder for new group instance.</returns>
			IGroupColumnsDataBuilder NewGroup();
		}

		/// <summary>
		/// Child columns data builder for column group.
		/// </summary>
		public interface IGroupColumnsDataBuilder
		{
			/// <summary>
			/// Gets nested column group data builder with specified group name.
			/// </summary>
			/// <param name="name">Name of child group.</param>
			/// <returns>Group data builder.</returns>
			IGroupDataBuilder Group(string name);

			/// <summary>
			/// Sets value for simple column with specified name within current group.
			/// </summary>
			/// <param name="name">Simple column name.</param>
			/// <param name="value">Column value.</param>
			/// <returns></returns>
			IGroupColumnsDataBuilder ColumnValue(string name, string value);
		}
	}
}
