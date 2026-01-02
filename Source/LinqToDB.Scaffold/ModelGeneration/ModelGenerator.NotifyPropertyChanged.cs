using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.Common;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator
	{
		public bool ImplementNotifyPropertyChanging         { get; set; }
		public bool SkipNotifyPropertyChangedImplementation { get; set; }

		public void NotifyPropertyChangedImplementation<TMemberGroup,TMethod,TField,TEvent,TAttribute>()
			where TMemberGroup : MemberGroup<TMemberGroup>, new()
			where TMethod      : Method     <TMethod>,      new()
			where TField       : Field      <TField>,       new()
			where TEvent       : Event      <TEvent>,       new()
			where TAttribute   : Attribute  <TAttribute>,   new()
		{
			foreach (var prop in GetTreeNodes(Model).OfType<INotifyingPropertyProperty>().Where(p => p.IsNotifying).ToList())
			{
				List<IClassMember> parentMembers;

				IMemberGroup? gr = null;

				if (prop.Parent is IClass c)
				{
					parentMembers = c.Members;
				}
				else
				{
					var parent = (IMemberGroup)prop.Parent!;

					parent.IsCompact = false;

					parentMembers = parent.Members;

					if (parent.IsPropertyGroup)
						gr = parent;
				}

				var name = prop.Name!.Trim();
				var type = prop.BuildType()!.Trim();

				if (gr == null)
				{
					gr = new TMemberGroup
					{
						Region          = $"{name} : {type}",
						Members         = { prop },
						IsPropertyGroup = true,
					};

					var index = parentMembers.IndexOf(prop);

					parentMembers.RemoveAt(index);
					parentMembers.Insert  (index, gr);
				}

				gr.Conditional   = prop.Conditional;
				prop.Conditional = null;

				if (prop.IsAuto)
				{
					var field = new TField
					{
						TypeBuilder          = () => type,
						Name                 = "_" + ToCamelCase(name),
						AccessModifier       = AccessModifier.Private,
						InsertBlankLineAfter = false
					};

					if (prop.InitValue != null)
						field.InitValue = prop.InitValue;
					else if (prop.EnforceNotNullable)
						field.InitValue = "null!";

					gr.Members.Insert(0, field);

					prop.Name        = " " + name;
					prop.TypeBuilder = () => " " + type;
					prop.IsAuto      = false;

					if (prop.HasGetter) prop.GetBodyBuilders.Add(() => [$"return {field.Name};"]);
					if (prop.HasSetter) prop.SetBodyBuilders.Add(() => [$"{field.Name} = value;"]);
				}

				var methods = new TMemberGroup
				{
					Region  = "INotifyPropertyChanged support",
					Members =
					{
						new TField
						{
							TypeBuilder    = static () => "const string",
							Name           = $"NameOf{name}",
							InitValue      = ToStringLiteral(name),
							AccessModifier = AccessModifier.Public
						},
						new TField
						{
							TypeBuilder    = static () => "PropertyChangedEventArgs",
							Name           = $"_{ToCamelCase(name)}ChangedEventArgs",
							InitValue      = $"new PropertyChangedEventArgs(NameOf{name})",
							AccessModifier = AccessModifier.Private,
							IsStatic       = true,
							IsReadonly     = true
						},
						new TMethod
						{
							TypeBuilder    = static () => "void",
							Name           = $"On{name}Changed",
							BodyBuilders   = { () => [$"OnPropertyChanged(_{ToCamelCase(name)}ChangedEventArgs);"] },
							AccessModifier = AccessModifier.Private
						}
					}
				};

				gr.Members.Add(methods);

				if (prop.Dependents.Count == 0)
					prop.Dependents.Add(name);

				if (ImplementNotifyPropertyChanging)
				{
					gr.Members.Add(new TMemberGroup
					{
						Region  = "INotifyPropertyChanging support",
						Members =
						{
							new TField
							{
								TypeBuilder    = static () => "PropertyChangingEventArgs",
								Name           = $"_{ToCamelCase(name)}ChangingEventArgs",
								InitValue      = $"new PropertyChangingEventArgs(NameOf{name})",
								AccessModifier = AccessModifier.Private,
								IsStatic       = true,
								IsReadonly     = true
							},
							new TMethod
							{
								TypeBuilder    = static () => "void",
								Name           = $"On{name}Changing",
								BodyBuilders   = { () => [$"OnPropertyChanging(_{ToCamelCase(name)}ChangingEventArgs);"] },
								AccessModifier = AccessModifier.Private
							}
						}
					});
				}

				if (prop.HasSetter)
				{
					var setBody = prop.BuildSetBody().Select(s => $"\t{s}").ToArray();

					prop.SetBodyBuilders.Clear();
					prop.SetBodyBuilders.Add(() => setBody);

					string getValue;

					var getBody = prop.BuildGetBody().ToArray();

					if (getBody.Length == 1 && getBody[0].StartsWith("return"))
					{
						getValue = getBody[0]["return".Length..].Trim(' ', '\t', ';');
					}
					else
					{
						getValue = name;
					}

					var insSpaces = setBody.Length > 1;
					var n         = 0;

					prop.SetBodyBuilders.Insert(n++, () =>
						prop.IsNullable
							? [$"if (!object.Equals({getValue}, value))", "{"]
							: [$"if ({getValue} != value)", "{"]);

					if (ImplementNotifyPropertyChanging)
					{
						foreach (var dp in prop.Dependents)
							prop.SetBodyBuilders.Insert(n++, () => [$"\tOn{dp}Changing();"]);
						prop.SetBodyBuilders.Insert(n++, static () => [""]);
					}

					prop.SetBodyBuilders.Insert(n++, () => [$"\tBefore{name}Changed(value);"]);

					if (insSpaces)
					{
						prop.SetBodyBuilders.Insert(3, static () => [""]);
						prop.SetBodyBuilders.Add(static () => [""]);
					}

					prop.SetBodyBuilders.Add(() => [$"\tAfter{name}Changed();"]);
					prop.SetBodyBuilders.Add(() => [""]);

					foreach (var dp in prop.Dependents)
						prop.SetBodyBuilders.Add(() => [$"\tOn{dp}Changed();"]);

					prop.SetBodyBuilders.Add(static () => ["}"]);

					methods.Members.Insert(0, new TMemberGroup
					{
						IsCompact = true,
						Members   =
						{
							new TMethod { TypeBuilder = static () => "void", Name = $"Before{name}Changed", ParameterBuilders = { () => $"{type} newValue" }, AccessModifier = AccessModifier.Partial },
							new TMethod { TypeBuilder = static () => "void", Name = $"After{name}Changed",  AccessModifier = AccessModifier.Partial },
						}
					});
				}

				prop.Parent!.SetTree();

				var p = prop.Parent;

				while (p != null && p is not IClass)
					p = p.Parent;

				if (p != null)
				{
					var cl = (IClass)p;

					if (!SkipNotifyPropertyChangedImplementation && !cl.Interfaces.Contains("INotifyPropertyChanged"))
					{
						if (Model.Usings.Contains("System.ComponentModel") == false)
							Model.Usings.Add("System.ComponentModel");

						cl.Interfaces.Add("INotifyPropertyChanged");

						cl.Members.Add(new TMemberGroup
						{
							Region  = "INotifyPropertyChanged support",
							Members =
							{
								new TEvent
								{
									TypeBuilder       = static () => new ModelType("PropertyChangedEventHandler", true, true).ToTypeName(),
									Name              = "PropertyChanged",
									IsVirtual         = true,
									Attributes        = { new TAttribute { Name = "field : NonSerialized"} }
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "OnPropertyChanged",
									ParameterBuilders = { () => "string propertyName" },
									BodyBuilders      = { () => OnPropertyChangedBody },
									AccessModifier    = AccessModifier.Protected
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "OnPropertyChanged",
									ParameterBuilders = { () => "PropertyChangedEventArgs arg" },
									BodyBuilders      = { () => OnPropertyChangedArgBody },
									AccessModifier    = AccessModifier.Protected
								},
							}
						});
					}

					if (ImplementNotifyPropertyChanging && !cl.Interfaces.Contains("INotifyPropertyChanging"))
					{
						if (Model.Usings.Contains("System.ComponentModel") == false)
							Model.Usings.Add("System.ComponentModel");

						cl.Interfaces.Add("INotifyPropertyChanging");

						cl.Members.Add(new TMemberGroup
						{
							Region  = "INotifyPropertyChanging support",
							Members =
							{
								new TEvent
								{
									TypeBuilder = static () => new ModelType("PropertyChangingEventHandler", true, true).ToTypeName(),
									Name        = "PropertyChanging",
									IsVirtual   = true,
									Attributes  = { new TAttribute { Name = "field : NonSerialized" } }
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "OnPropertyChanging",
									ParameterBuilders = { () => "string propertyName" },
									BodyBuilders      = { () => OnPropertyChangingBody },
									AccessModifier    = AccessModifier.Protected
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "OnPropertyChanging",
									ParameterBuilders = { () => "PropertyChangingEventArgs arg" },
									BodyBuilders      = { () => OnPropertyChangingArgBody },
									AccessModifier    = AccessModifier.Protected
								},
							}
						});
					}
				}
			}
		}

		public string[] OnPropertyChangedBody =
		[
			"var propertyChanged = PropertyChanged;",
			"",
			"if (propertyChanged != null)",
			"{",
				"\tpropertyChanged(this, new PropertyChangedEventArgs(propertyName));",
			"}",
		];

		public string[] OnPropertyChangedArgBody =
		[
			"var propertyChanged = PropertyChanged;",
			"",
			"if (propertyChanged != null)",
			"{",
				"\tpropertyChanged(this, arg);",
			"}",
		];

		public string[] OnPropertyChangingBody =
		[
			"var propertyChanging = PropertyChanging;",
			"",
			"if (propertyChanging != null)",
			"{",
				"\tpropertyChanging(this, new PropertyChangingEventArgs(propertyName));",
			"}",
		];

		public string[] OnPropertyChangingArgBody =
		[
			"var propertyChanging = PropertyChanging;",
			"",
			"if (propertyChanging != null)",
			"{",
				"\tpropertyChanging(this, arg);",
			"}",
		];
	}
}
