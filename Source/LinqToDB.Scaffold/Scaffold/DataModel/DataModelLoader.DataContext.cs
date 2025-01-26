using System;
using System.Text;
using System.Globalization;

using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Naming;

namespace LinqToDB.Scaffold
{
	partial class DataModelLoader
	{
		/// <summary>
		/// Creates empty data context class model for current schema and initialize it with basic properties.
		/// </summary>
		/// <returns>Data context model instance.</returns>
		private DataContextModel BuildDataContext()
		{
			string className;
			if (_options.DataModel.ContextClassName != null)
			{
				// name provided by user and shouldn't be modified except cases when it is invalid
				className = _namingServices.NormalizeIdentifier(
					NormalizationOptions.None,
					_options.DataModel.ContextClassName);
			}
			else
			{
				className = _namingServices.NormalizeIdentifier(
					_options.DataModel.DataContextClassNameOptions,
					_schemaProvider.DatabaseName != null ? (_schemaProvider.DatabaseName + "DB") : "MyDataContext");
			}

			var dataContextClass = new ClassModel(className, className)
			{
				Modifiers = (_options.DataModel.ContextClassModifier ?? Modifiers.Public) | Modifiers.Partial,
				Namespace = _options.CodeGeneration.Namespace
			};

			if (_options.DataModel.BaseContextClass != null)
				dataContextClass.BaseType = _languageProvider.TypeParser.Parse(_options.DataModel.BaseContextClass, false);
			else
				dataContextClass.BaseType = WellKnownTypes.LinqToDB.Data.DataConnection;

			if (_options.DataModel.IncludeDatabaseInfo)
			{
				var summary = new StringBuilder();
				if (_schemaProvider.DatabaseName != null)
					summary.Append(CultureInfo.InvariantCulture, $"Database       : {_schemaProvider.DatabaseName}" ).AppendLine();
				if (_schemaProvider.DataSource != null)
					summary.Append(CultureInfo.InvariantCulture, $"Data Source    : {_schemaProvider.DataSource}"   ).AppendLine();
				if (_schemaProvider.ServerVersion != null)
					summary.Append(CultureInfo.InvariantCulture, $"Server Version : {_schemaProvider.ServerVersion}").AppendLine();

				if (summary.Length > 0)
					dataContextClass.Summary = summary.ToString();
			}

			var dataContext = new DataContextModel(dataContextClass);

			dataContext.HasDefaultConstructor        = _options.DataModel.HasDefaultConstructor;
			dataContext.HasConfigurationConstructor  = _options.DataModel.HasConfigurationConstructor;
			dataContext.HasUntypedOptionsConstructor = _options.DataModel.HasUntypedOptionsConstructor;
			dataContext.HasTypedOptionsConstructor   = _options.DataModel.HasTypedOptionsConstructor;

			return dataContext;
		}
	}
}
