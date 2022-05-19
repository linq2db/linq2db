using System;
using System.Collections.Generic;
using System.Globalization;
using LinqToDB.CodeModel;

namespace LinqToDB.DataModel;

// contains basic code generation logic, not related to data model.
partial class DataModelGenerator
{
	/// <summary>
	/// Defines method parameter using pre-built parameter model.
	/// </summary>
	/// <param name="method">Method builder.</param>
	/// <param name="model">Parameter model.</param>
	/// <param name="defaultValue">Optional default value for parameter.</param>
	/// <returns>Created parameter AST node.</returns>
	private CodeParameter DefineParameter(MethodBuilder method, ParameterModel model, ICodeExpression? defaultValue = null)
	{
		var parameter = AST.Parameter(model.Type, AST.Name(model.Name), model.Direction, defaultValue);

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
				var nsBuilder = AST.Namespace(model.Namespace);
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
		var @class = classes.New(AST.Name(model.Name));

		@class.SetModifiers(model.Modifiers);

		if (model.BaseType != null)
			@class.Inherits(model.BaseType);

		if (model.Interfaces != null)
			foreach (var iface in model.Interfaces)
				@class.Implements(iface);

		if (model.Summary != null)
			@class.XmlComment().Summary(model.Summary);

		if (model.CustomAttributes != null)
			foreach (var attr in model.CustomAttributes)
				@class.AddAttribute(attr);

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
		var propertyBuilder = propertyGroup.New(AST.Name(property.Name, null, propertyGroup.Members.Count + 1), property.Type!);

		propertyBuilder.SetModifiers(property.Modifiers);

		if (property.IsDefault)
			propertyBuilder.Default(property.HasSetter);

		if (property.Summary != null)
			propertyBuilder.XmlComment().Summary(property.Summary);


		if (property.TrailingComment != null)
			propertyBuilder.TrailingComment(property.TrailingComment);

		if (property.CustomAttributes != null)
			foreach (var attr in property.CustomAttributes)
				propertyBuilder.AddAttribute(attr);

		return propertyBuilder;
	}

	/// <summary>
	/// Defines method using pre-built method model.
	/// </summary>
	/// <param name="methods">Methods group that owns new method.</param>
	/// <param name="model">Method model.</param>
	/// <param name="async">If <c>true</c>, append <see cref="ASYNC_SUFFIX"/> to method name.</param>
	/// <param name="withAwait">If <c>true</c>, method contains <c>await</c> operations and should be marked with <c>async</c> modifier.</param>
	/// <returns>Method builder instance.</returns>
	private MethodBuilder DefineMethod(MethodGroup methods, MethodModel model, bool async = false, bool withAwait = false)
	{
		var builder = methods.New(AST.Name(async ? model.Name + ASYNC_SUFFIX : model.Name));

		if (withAwait)
			builder.SetModifiers(model.Modifiers | Modifiers.Async);
		else
			builder.SetModifiers(model.Modifiers);

		if (model.Summary != null)
			builder.XmlComment().Summary(model.Summary);

		if (model.CustomAttributes != null)
			foreach (var attr in model.CustomAttributes)
				builder.AddAttribute(attr);

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
		var file = AST.File(fileName);
		_files.Add(fileName, (file, new()));

		// add standard `auto-generated` comment
		if (_dataModel.AutoGeneratedHeader != null)
		{
			// note that roslyn compiler disables NRT for files with auto-generated comment for backward compatibility and they should be re-enabled explicitly
			file.AddHeader(AST.Commentary("---------------------------------------------------------------------------------------------------", false));
			file.AddHeader(AST.Commentary("<auto-generated>", false));
			file.AddHeader(AST.Commentary(_dataModel.AutoGeneratedHeader, false));
			file.AddHeader(AST.Commentary("</auto-generated>", false));
			file.AddHeader(AST.Commentary("---------------------------------------------------------------------------------------------------", false));
		}

		// configure compiler options
		//
		// as we don't generate xml-doc comments except cases when we have descriptions for database objects
		// we should disable missing xml-doc warnings in generated code to avoid build errors/warnings in
		// projects with required xml-doc
		if (_dataModel.DisableXmlDocWarnings)
			file.Add(AST.DisableWarnings(_languageProvider.MissingXmlCommentWarnCodes));

		// enable NRT context if we have auto-generated comment which disables it
		if (_dataModel.NRTEnabled && _dataModel.AutoGeneratedHeader != null)
			file.Add(AST.EnableNullableReferenceTypes());

		// add extra line separator between generated header and other file content
		if (_dataModel.DisableXmlDocWarnings || (_dataModel.NRTEnabled && _dataModel.AutoGeneratedHeader != null))
			file.Add(AST.NewLine);
	}
}
