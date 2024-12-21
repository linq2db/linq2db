using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	// contains basic code generation logic, not related to data model.
	internal static class CodeGenerationExtensions
	{
		/// <summary>
		/// Defines method parameter using pre-built parameter model.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="method">Method builder.</param>
		/// <param name="model">Parameter model.</param>
		/// <param name="defaultValue">Optional default value for parameter.</param>
		/// <returns>Created parameter AST node.</returns>
		public static CodeParameter DefineParameter(this IDataModelGenerationContext context, MethodBuilder method, ParameterModel model, ICodeExpression? defaultValue = null)
		{
			var parameter = context.AST.Parameter(model.Type, context.AST.Name(model.Name), model.Direction, defaultValue);

			method.Parameter(parameter);

			// add paramer's xml doc to method xml doc definition
			if (model.Description != null)
				method.XmlComment().Parameter(parameter.Name, model.Description);

			parameter.ChangeHandler += p =>
			{
				model.Name = p.Name.Name;
				model.Type = p.Type.Type;
			};

			return parameter;
		}

		/// <summary>
		/// Defines class in file, specified in pre-built class model.
		/// If file doesn't exists yet, also will add this file.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="model">Class model.</param>
		/// <returns>Class builder instance.</returns>
		public static ClassBuilder DefineFileClass(this IDataModelGenerationContext context, ClassModel model)
		{
			if (model.FileName == null)
				throw new InvalidOperationException($"{nameof(DefineFileClass)} called for class without {nameof(model.FileName)} set.");

			var setClassNameToFileName = false;

			// get or create class file
			if (!context.TryGetFile(model.FileName, out var file))
			{
				setClassNameToFileName = true;
				file = DefineFile(context, model.FileName);
			}

			// for top-level types without namespace we use empty string internally for lookup collections key
			var nsKey = model.Namespace ?? string.Empty;

			// create or use existing per-namespace class group to add new class to it
			if (!file.ClassesPerNamespace.TryGetValue(nsKey, out var classes))
			{
				// group not found - create new one
				if (model.Namespace != null)
				{
					// for namespaced class - define namespace
					var nsBuilder = context.AST.Namespace(model.Namespace);
					file.File.Add(nsBuilder.Namespace);
					file.ClassesPerNamespace.Add(nsKey, classes = nsBuilder.Classes());
				}
				else
				{
					// for top-level class - add class group to file directly
					file.ClassesPerNamespace.Add(nsKey, classes = new ClassGroup(null));
					file.File.Add(classes);
				}
			}

			var builder = DefineClass(context, classes, model);

			if (setClassNameToFileName)
				file.File.NameSource = builder.Type.Name;

			return builder;
		}

		/// <summary>
		/// Defines class using pre-built class model.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="classes">Class group that owns new class.</param>
		/// <param name="model">Class model.</param>
		/// <returns>Class builder instance.</returns>
		public static ClassBuilder DefineClass(this IDataModelGenerationContext context, ClassGroup classes, ClassModel model)
		{
			var @class = classes.New(context.AST.Name(model.Name));

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

			@class.Type.ChangeHandler += t =>
			{
				model.Name      = t.Type.Name!.Name;
				model.Namespace = t.Type.Namespace != null && t.Type.Namespace.Count > 0 ? string.Join(".", t.Type.Namespace.Select(p => p.Name)) : null;
			};

			return @class;
		}

		/// <summary>
		/// Defines property using pre-built property model.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="propertyGroup">Property group that owns new property.</param>
		/// <param name="property">Property model.</param>
		/// <returns>Property builder instance.</returns>
		public static PropertyBuilder DefineProperty(this IDataModelGenerationContext context, PropertyGroup propertyGroup, PropertyModel property)
		{
			var propertyBuilder = propertyGroup.New(context.AST.Name(property.Name, null, propertyGroup.Members.Count + 1), property.Type!);

			propertyBuilder.SetModifiers(property.Modifiers);

			if (property.IsDefault)
				propertyBuilder.Default(property.HasSetter, property.SetterModifiers);

			if (property.Summary != null)
				propertyBuilder.XmlComment().Summary(property.Summary);

			if (property.TrailingComment != null)
				propertyBuilder.TrailingComment(property.TrailingComment);

			if (property.CustomAttributes != null)
				foreach (var attr in property.CustomAttributes)
					propertyBuilder.AddAttribute(attr);

			propertyBuilder.Property.ChangeHandler += p =>
			{
				property.Name = p.Name.Name;
				property.Type = p.Type.Type;
			};

			return propertyBuilder;
		}

		/// <summary>
		/// Defines method using pre-built method model.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="methods">Methods group that owns new method.</param>
		/// <param name="model">Method model.</param>
		/// <param name="async">If <c>true</c>, append <see cref="DataModelConstants.ASYNC_SUFFIX"/> to method name.</param>
		/// <param name="withAwait">If <c>true</c>, method contains <c>await</c> operations and should be marked with <c>async</c> modifier.</param>
		/// <returns>Method builder instance.</returns>
		public static MethodBuilder DefineMethod(this IDataModelGenerationContext context, MethodGroup methods, MethodModel model, bool async = false, bool withAwait = false)
		{
			var builder = methods.New(context.AST.Name(async ? model.Name + DataModelConstants.ASYNC_SUFFIX : model.Name));

			if (withAwait)
				builder.SetModifiers(model.Modifiers | Modifiers.Async);
			else
				builder.SetModifiers(model.Modifiers);

			if (model.Summary != null)
				builder.XmlComment().Summary(model.Summary);

			if (model.CustomAttributes != null)
				foreach (var attr in model.CustomAttributes)
					builder.AddAttribute(attr);

			builder.Method.ChangeHandler += m =>
			{
				model.Name = m.Name.Name;
			};

			return builder;
		}

		/// <summary>
		/// Deduplicate generate file names to be unique within current model by adding counter to duplicates.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		public static void DeduplicateFileNames(this IDataModelGenerationContext context)
		{
			var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var file in context.Files)
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
		/// <param name="context">Model generation context.</param>
		/// <param name="fileName">Name for new file.</param>
		public static FileData DefineFile(this IDataModelGenerationContext context, string fileName)
		{
			var fileData = context.AddFile(fileName);
			var file     = fileData.File;

			// add standard `auto-generated` comment
			if (context.Model.AutoGeneratedHeader != null)
			{
				// note that roslyn compiler disables NRT for files with auto-generated comment for backward compatibility and they should be re-enabled explicitly
				file.AddHeader(context.AST.Commentary("---------------------------------------------------------------------------------------------------", false));
				file.AddHeader(context.AST.Commentary("<auto-generated>", false));
				file.AddHeader(context.AST.Commentary(context.Model.AutoGeneratedHeader, false));
				file.AddHeader(context.AST.Commentary("</auto-generated>", false));
				file.AddHeader(context.AST.Commentary("---------------------------------------------------------------------------------------------------", false));
			}

			// configure compiler options
			//
			// as we don't generate xml-doc comments except cases when we have descriptions for database objects
			// we should disable missing xml-doc warnings in generated code to avoid build errors/warnings in
			// projects with required xml-doc
			if (context.Model.DisableXmlDocWarnings)
				file.Add(context.AST.DisableWarnings(context.LanguageProvider.MissingXmlCommentWarnCodes));

			// enable NRT context if we have auto-generated comment which disables it
			if (context.Model.NRTEnabled && context.Model.AutoGeneratedHeader != null)
				file.Add(context.AST.EnableNullableReferenceTypes());

			// add extra line separator between generated header and other file content
			if (context.Model.DisableXmlDocWarnings || (context.Model.NRTEnabled && context.Model.AutoGeneratedHeader != null))
				file.Add(CodeEmptyLine.Instance);

			return fileData;
		}
	}
}
