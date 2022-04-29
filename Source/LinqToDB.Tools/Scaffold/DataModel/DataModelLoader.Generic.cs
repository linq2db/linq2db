﻿using System.Collections.Generic;
using LinqToDB.Schema;

namespace LinqToDB.Scaffold
{
	partial class DataModelLoader
	{
		/// <summary>
		/// Preprocess database object name according to current settings.
		/// </summary>
		/// <param name="originalName">Original (full) database object name.</param>
		/// <param name="defaultSchemas">Collection of default schemas.</param>
		/// <returns>New database object name for use in data model metadata and non-default schema flag.</returns>
		private (ObjectName name, bool isNonDefaultSchema) ProcessObjectName(ObjectName originalName, ISet<string> defaultSchemas)
		{
			var isNonDefaultSchema = originalName.Schema != null && !defaultSchemas.Contains(originalName.Schema);
			var result             = originalName;

			// remove default schema from name if disabled
			if (!_options.DataModel.GenerateDefaultSchema && !isNonDefaultSchema && originalName.Schema != null)
				result = result with { Schema = null };

			// remove database name if disabled
			if (!_options.DataModel.IncludeDatabaseName)
				result = result with { Database = null };

			// currently we don't load schema data from linked servers, so there is no need to process server name

			return (result, isNonDefaultSchema);
		}
	}
}
