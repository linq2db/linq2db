using System.Collections.Generic;

using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Stores code model file node with containing namespace groups.
	/// </summary>
	/// <param name="File">File-level AST note.</param>
	/// <param name="ClassesPerNamespace">Map of namespace name to AST group withing file.</param>
	public sealed record class FileData(CodeFile File, Dictionary<string, ClassGroup> ClassesPerNamespace);
}
