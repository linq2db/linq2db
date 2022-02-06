namespace LinqToDB.DataModel
{
	/// <summary>
	/// Descriptor of table function or stored procedure return record.
	/// Either <paramref name="CustomTable"/> or <paramref name="Entity"/> must be specified, but not both.
	/// </summary>
	/// <param name="CustomTable">Custom record class descriptor.</param>
	/// <param name="Entity">Function/procedure returns known entity as record.</param>
	/// <param name="AsyncResult">Stored procedure return class model for async signature.</param>
	public sealed record FunctionResult(ResultTableModel? CustomTable, EntityModel? Entity, AsyncProcedureResult? AsyncResult);
}
