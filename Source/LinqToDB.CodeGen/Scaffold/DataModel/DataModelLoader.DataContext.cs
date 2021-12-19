using System.Text;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;

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
			var className = _namingServices.NormalizeIdentifier(
				_contextSettings.DataContextClassNameNormalization,
				_contextSettings.ContextClassName ?? _schemaProvider.DatabaseName ?? "MyDataContext");

			var dataContextClass = new ClassModel(className, className)
			{
				IsPartial = true,
				IsPublic  = true,
				Namespace = _codegenSettings.Namespace
			};

			if (_codegenSettings.BaseContextClass != null)
				dataContextClass.BaseType = _languageProvider.TypeParser.Parse(_codegenSettings.BaseContextClass, false);
			else
				dataContextClass.BaseType = WellKnownTypes.LinqToDB.Data.DataConnection;

			if (_codegenSettings.IncludeDatabaseInfo)
			{
				var summary = new StringBuilder();
				if (_schemaProvider.DatabaseName != null)
					summary.AppendFormat("Database       : {0}", _schemaProvider.DatabaseName ).AppendLine();
				if (_schemaProvider.DataSource != null)
					summary.AppendFormat("Data Source    : {0}", _schemaProvider.DataSource   ).AppendLine();
				if (_schemaProvider.ServerVersion != null)
					summary.AppendFormat("Server Version : {0}", _schemaProvider.ServerVersion).AppendLine();

				if (summary.Length > 0)
					dataContextClass.Summary = summary.ToString();
			}

			var dataContext = new DataContextModel(dataContextClass);

			dataContext.HasDefaultConstructor        = _contextSettings.HasDefaultConstructor;
			dataContext.HasConfigurationConstructor  = _contextSettings.HasConfigurationConstructor;
			dataContext.HasUntypedOptionsConstructor = _contextSettings.HasUntypedOptionsConstructor;
			dataContext.HasTypedOptionsConstructor   = _contextSettings.HasTypedOptionsConstructor;

			return dataContext;
		}
	}
}
