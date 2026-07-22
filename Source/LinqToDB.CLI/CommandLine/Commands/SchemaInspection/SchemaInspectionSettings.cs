using System;

using LinqToDB.CommandLine.Commands.Connection;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Fully resolved schema inspection settings.
	/// </summary>
	internal sealed record SchemaInspectionSettings(
		ConnectionSettings               Connection,
		SchemaInspectionEffectiveOptions Options,
		string                           Output,
		string?                          OutputFile,
		bool                             Overwrite);
}
