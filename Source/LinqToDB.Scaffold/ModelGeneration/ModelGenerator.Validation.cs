﻿using System;
using System.Globalization;
using System.Linq;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IPropertyValidation : IProperty
	{
		bool CustomValidation { get; set; }
		bool ValidateProperty { get; set; }
	}

	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator
	{
		public void ValidationImplementation<TClass,TMemberGroup,TMethod,TField,TAttribute>()
			where TClass       : Class      <TClass>,       new()
			where TMemberGroup : MemberGroup<TMemberGroup>, new()
			where TMethod      : Method     <TMethod>,      new()
			where TField       : Field      <TField>,       new()
			where TAttribute   : Attribute  <TAttribute>,   new()
		{
			foreach (var cl in GetTreeNodes(Model).OfType<IClass>())
			{
				var validationGroup = new TMemberGroup { Region = "Validation" };
				var props           = GetTreeNodes(cl).OfType<IPropertyValidation>().Where(p => p.CustomValidation).ToList();

				if (props.Count > 0)
				{
					if (Model.Usings.Contains("System.Collections.Generic") == false)
						Model.Usings.Add("System.Collections.Generic");

					var isValid = new TMethod
					{
						TypeBuilder       = static () => "bool",
						Name              = "IsValid",
						ParameterBuilders = { () => cl.Name + " obj" },
						IsStatic          = true
					};

					var validator = new TClass
					{
						Name     = "CustomValidator",
						Members  = { isValid },
						IsStatic = true
					};

					var partialGroup = new TMemberGroup { IsCompact = true };

					validationGroup.Members.Add(new TField { TypeBuilder = static () => "int", Name = "_isValidCounter", Attributes = { new TAttribute { Name = "field : NonSerialized" } } });
					validationGroup.Members.Add(validator);
					validationGroup.Members.Add(partialGroup);

					isValid.BodyBuilders.Add(static () =>
					[
						"try",
						"{",
							"\tobj._isValidCounter++;",
						""
					]);

					var ret = "\treturn ";

					for (var idx = 0; idx < props.Count; idx++)
					{
						var i  = idx;
						var ii = i.ToString(CultureInfo.InvariantCulture);
						var p  = props[i];

						var name  = p.Name!.Trim();
						var mname = "Validate" + name;

						var conditional = p.Conditional;
						if (conditional == null && p is INotifyingPropertyProperty && p.Parent is IMemberGroup mg)
							conditional = mg.Conditional;

						cl.Attributes.Add(
							new TAttribute
							{
								Name = "CustomValidation",
								Parameters =
								{
									$"typeof({cl.Name}.CustomValidator)",
									ToStringLiteral(mname)
								},
								IsSeparated = true
							});

						if (conditional != null)
							isValid.BodyBuilders.Add(() => [$"\t#if {conditional}"]);

						isValid.BodyBuilders.Add(() => [$"\tvar flag{ii} = ValidationResult.Success == {mname}(obj, obj.{name});"]);

						if (conditional != null)
							isValid.BodyBuilders.Add(() =>
							[
								"\t#else",
								$"\tvar flag{ii} = true;",
								"\t#endif"
							]);

						ret += (i == 0 ? "" : " && ") + "flag" + ii;

						var validate = new TMethod
						{
							TypeBuilder       = static () => new ModelType("ValidationResult", true, true).ToTypeName(),
							Name              = mname,
							ParameterBuilders =
							{
								() => $"{cl.Name} obj",
								() => $"{p.BuildType()?.Trim()} value"
							},
							IsStatic          = true,
							Conditional       = conditional
						};

						validate.BodyBuilders.Add(() => new []
						{
							"var list = new List<ValidationResult>();",
							"",
							"Validator.TryValidateProperty(",
								"\tvalue,",
								$"\tnew ValidationContext(obj, null, null) {{ MemberName = NameOf{name} }}, list);",
							"",
							$"obj.{mname}(value, list);",
							"",
							"if (list.Count > 0)",
							"{",
								"\tforeach (var result in list)",
									"\t\tforeach (var name in result.MemberNames)",
										"\t\t\tobj.AddError(name, result.ErrorMessage ?? \"\");",
							"",
								"\treturn list[0];",
							"}",
							"",
							$"obj.RemoveError(NameOf{name});",
							"",
							"return ValidationResult.Success;"
						});

						validator.Members.Add(validate);

						partialGroup.Members.Add(new TMethod
						{
							TypeBuilder       = static () => "void",
							Name              = mname,
							ParameterBuilders =
							{
								() => $"{p.BuildType()?.Trim()} value",
								() => "List<ValidationResult> validationResults",
							},
							AccessModifier    = AccessModifier.Partial,
							Conditional       = conditional
						});
					}

					isValid.BodyBuilders.Add(() => new []
					{
						"",
						$"{ret};",
						"}",
						"finally",
						"{",
							"\tobj._isValidCounter--;",
						"}"
					});
				}

				props = GetTreeNodes(cl).OfType<IPropertyValidation>().Where(p => p.ValidateProperty && p.HasSetter).ToList();

				if (props.Count > 0)
				{
					foreach (var p in props)
					{
						var setBody = p.BuildSetBody().ToList();
						if (setBody.Count > 0)
							setBody.Insert(0, "");

						setBody.Insert(0, "if (_validationLockCounter == 0)");
						setBody.Insert(1, "{");

						if (p.CustomValidation)
						{
							setBody.Insert(2, $"\tvar validationResult = CustomValidator.Validate{p.Name?.Trim()}(this, value);");
							setBody.Insert(3, "\tif (validationResult != ValidationResult.Success)");
							setBody.Insert(4, "\t\tthrow new ValidationException(validationResult, null, null);");
							setBody.Insert(5, "}");
						}
						else
						{
							setBody.Insert(2, "\tValidator.ValidateProperty(");
							setBody.Insert(3, "\t\tvalue,");
							setBody.Insert(4, $"\t\tnew ValidationContext(this, null, null) {{ MemberName = NameOf{p.Name?.Trim()} }});");
							setBody.Insert(5, "}");
						}

						p.SetBodyBuilders.Clear();
						p.SetBodyBuilders.Add(() => setBody);
					}

					validationGroup.Members.Add(new TField
					{
						TypeBuilder    = static () => "int",
						Name           = "_validationLockCounter",
						AccessModifier = AccessModifier.Private,
						InitValue      = "0",
						Attributes     = { new TAttribute { Name = "field : NonSerialized" } }
					});

					validationGroup.Members.Add(new TMethod { TypeBuilder = static () => "void", Name = "LockValidation",   BodyBuilders = { static () => [ "_validationLockCounter++;" ] } });
					validationGroup.Members.Add(new TMethod { TypeBuilder = static () => "void", Name = "UnlockValidation", BodyBuilders = { static () => [ "_validationLockCounter--;" ] } });
				}

				if (validationGroup.Members.Count > 0)
				{
					if (Model.Usings.Contains("System.ComponentModel.DataAnnotations") == false)
						Model.Usings.Add("System.ComponentModel.DataAnnotations");

					cl.Members.Add(validationGroup);
					cl.SetTree();
				}
			}
		}
	}
}
