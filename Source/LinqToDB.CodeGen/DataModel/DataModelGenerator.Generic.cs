using System;
using System.Collections.Generic;
using System.Globalization;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	// contains basic code generation logic, not related to data model.
	partial class DataModelGenerator
	{
		/// <summary>
		/// Defines method parameter using pre-built parameter model.
		/// </summary>
		/// <param name="method">Method builder.</param>
		/// <param name="model">Parameter model.</param>
		/// <returns>Created parameter AST node.</returns>
		private CodeParameter DefineParameter(MethodBuilder method, ParameterModel model)
		{
			var parameter = _code.Parameter(model.Type, _code.Name(model.Name), model.Direction);

			method.Parameter(parameter);

			// add paramer's xml doc to method xml doc definition
			if (model.Description != null)
				method.XmlComment().Parameter(parameter.Name, model.Description);

			return parameter;
		}

		/// <summary>
		/// Defines class in file, specified in pre-built class model.
		/// If file doesn't exists yet, also will add this file.
		/// </summary>
		/// <param name="model">Class model.</param>
		/// <returns>Class builder instance.</returns>
		private ClassBuilder DefineFileClass(ClassModel model)
		{
			if (model.FileName == null)
				throw new InvalidOperationException($"{nameof(DefineFileClass)} called for class without {nameof(model.FileName)} set.");

			// get or create class file
			if (!_files.TryGetValue(model.FileName, out var file))
			{
				DefineFile(model.FileName);
				file = _files[model.FileName];
			}

			// for top-level types without namespace we use empty string internally for lookup collections key
			var nsKey = model.Namespace ?? string.Empty;

			// create or use existing per-namespace class group to add new class to it
			if (!file.classesPerNamespace.TryGetValue(nsKey, out var classes))
			{
				// group not found - create new one
				if (model.Namespace != null)
				{
					// for namespaced class - define namespace
					var nsBuilder = _code.Namespace(model.Namespace);
					file.file.Add(nsBuilder.Namespace);
					file.classesPerNamespace.Add(nsKey, classes = nsBuilder.Classes());
				}
				else
				{
					// for top-level class - add class group to file directly
					file.classesPerNamespace.Add(nsKey, classes = new ClassGroup(null));
					file.file.Add(classes);
				}
			}

			return DefineClass(classes, model);
		}

		/// <summary>
		/// Defines class using pre-built class model.
		/// </summary>
		/// <param name="classes">Class group that owns new class.</param>
		/// <param name="model">Class model.</param>
		/// <returns>Class builder instance.</returns>
		private ClassBuilder DefineClass(ClassGroup classes, ClassModel model)
		{
			var @class = classes.New(_code.Name(model.Name));

			if (model.IsPublic ) @class.Public ();
			if (model.IsStatic ) @class.Static ();
			if (model.IsPartial) @class.Partial();

			if (model.BaseType != null)
				@class.Inherits(model.BaseType);

			if (model.Summary != null)
				@class.XmlComment().Summary(model.Summary);

			return @class;
		}

		/// <summary>
		/// Defines property using pre-built property model.
		/// </summary>
		/// <param name="propertyGroup">Property group that owns new property.</param>
		/// <param name="property">Property model.</param>
		/// <returns>Property builder instance.</returns>
		private PropertyBuilder DefineProperty(PropertyGroup propertyGroup, PropertyModel property)
		{
			var propertyBuilder = propertyGroup.New(_code.Name(property.Name, null, propertyGroup.Members.Count + 1), property.Type!);

			if (property.IsPublic)
				propertyBuilder.Public();

			if (property.IsDefault)
				propertyBuilder.Default(property.HasSetter);

			if (property.Summary != null)
				propertyBuilder.XmlComment().Summary(property.Summary);


			if (property.TrailingComment != null)
				propertyBuilder.TrailingComment(property.TrailingComment);

			return propertyBuilder;
		}

		/// <summary>
		/// Defines method using pre-built method model.
		/// </summary>
		/// <param name="methods">Methods group that owns new method.</param>
		/// <param name="model">Method model.</param>
		/// <returns>Method builder instance.</returns>
		private MethodBuilder DefineMethod(MethodGroup methods, MethodModel model)
		{
			var builder = methods.New(_code.Name(model.Name));

			if (model.Public   ) builder.Public   ();
			if (model.Static   ) builder.Static   ();
			if (model.Partial  ) builder.Partial  ();
			if (model.Extension) builder.Extension();

			if (model.Summary != null)
				builder.XmlComment().Summary(model.Summary);

			return builder;
		}

		/// <summary>
		/// Deduplicate generate file names to be unique within current model by adding counter to duplicates.
		/// </summary>
		private void DeduplicateFileNames()
		{
			var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var (file, _) in _files.Values)
			{
				var fileName = file.FileName;
				var cnt      = 0;

				while (!fileNames.Add(fileName))
				{
					cnt++;
					fileName = file.FileName + cnt.ToString(NumberFormatInfo.InvariantInfo);
				}

				file.FileName = fileName;
			}
		}

		/// <summary>
		/// Define new file and fill it with generic functionality like header comments and pragmas.
		/// </summary>
		/// <param name="fileName">Name for new file.</param>
		private void DefineFile(string fileName)
		{
			var file = _code.File(fileName);
			_files.Add(fileName, (file, new()));

			// add standard `auto-generated` comment
			if (_dataModel.AutoGeneratedComment != null)
			{
				// note that roslyn compiler disables NRT for files with auto-generated comment for backward compatibility and they should be re-enabled explicitly
				file.Header.Add(_code.Commentary("---------------------------------------------------------------------------------------------------", false));
				file.Header.Add(_code.Commentary("<auto-generated>", false));
				file.Header.Add(_code.Commentary(_dataModel.AutoGeneratedComment, false));
				file.Header.Add(_code.Commentary("</auto-generated>", false));
				file.Header.Add(_code.Commentary("---------------------------------------------------------------------------------------------------", false));
			}

			// configure compiler options
			//
			// as we don't generate xml-doc comments except cases when we have descriptions for database objects
			// we should disable missing xml-doc warnings in generated code to avoid build errors/warnings in
			// projects with required xml-doc
			if (_dataModel.DisableXmlDocWarnings)
				file.Add(_code.DisableWarnings(_languageProvider.MissingXmlCommentWarnCodes));

			// enable NRT context if we have auto-generated comment which disables it
			if (_dataModel.NRTEnabled && _dataModel.AutoGeneratedComment != null)
				file.Add(_code.EnableNullableReferenceTypes());

			// add extra line separator between generated header and other file content
			if (_dataModel.DisableXmlDocWarnings || (_dataModel.NRTEnabled && _dataModel.AutoGeneratedComment != null))
				file.Add(_code.NewLine);
		}
	}
}
