using System;

using LinqToDB.Mapping;

namespace Tests.Wasm
{
	[Table]
	public sealed class WasmPerson
	{
		[Column, PrimaryKey] public int      Id        { get; set; }
		[Column, NotNull   ] public string   Name      { get; set; } = null!;
		[Column            ] public DateTime BirthDate { get; set; }
	}
}
