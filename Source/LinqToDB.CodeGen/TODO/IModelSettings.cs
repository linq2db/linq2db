using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.CodeGen.Configuration
{
	public interface IModelSettings
	{
		string Provider { get; }
		string ConnectionString { get; }

		bool IncludeSchemas { get; }
		ISet<string> Schemas { get; }

		//string? DefaultSchema { get; }
		//string[] ExcludedCatalogs { get; }
		//string[] ExcludedSchemas { get; }
		//bool Char1AsString { get; }
	}
}
