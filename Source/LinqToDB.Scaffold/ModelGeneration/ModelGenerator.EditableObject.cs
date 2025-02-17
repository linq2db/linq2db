using System.Collections.Generic;
using System.Linq;

#pragma warning disable CA1305
#pragma warning disable RS0030

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IEditableObjectProperty : IProperty
	{
		public bool   IsEditable  { get; set; }
		public string IsDirtyText { get; set; }
	}

	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator
	{
		public void EditableObjectImplementation<TMemberGroup,TMethod,TProperty,TField>()
			where TMemberGroup : MemberGroup<TMemberGroup>, new()
			where TMethod      : Method     <TMethod>,      new()
			where TProperty    : Property   <TProperty>,    new()
			where TField       : Field      <TField>,       new()
		{
			foreach (var prop in GetTreeNodes(Model).OfType<IEditableObjectProperty>().Where(p => p.IsEditable).ToList())
			{
				SetPropertyValue(prop, "IsNotifying", true);

				List<IClassMember> parentMembers;

				if (prop.Parent is IClass cl)
				{
					parentMembers = cl.Members;
				}
				else
				{
					var parent = (TMemberGroup)prop.Parent!;
					parentMembers = parent.Members;

					parent.IsCompact = false;
				}

				var name = prop.Name!.Trim();
				var type = prop.BuildType()!.Trim();

				var gr = new TMemberGroup
				{
					Region          = name + " : " + type,
					Members         = { prop },
					IsPropertyGroup = true,
				};

				var index = parentMembers.IndexOf(prop);

				parentMembers.RemoveAt(index);
				parentMembers.Insert  (index, gr);

				var originalField = new TField
				{
					TypeBuilder          = () => type,
					Name                 = $"_original{name}",
					AccessModifier       = AccessModifier.Private,
					InsertBlankLineAfter = false,
				};

				gr.Members.Insert(0, originalField);

				var currentField = new TField
				{
					TypeBuilder          = () => type,
					Name                 = " _current" + name,
					AccessModifier       = AccessModifier.Private,
					InsertBlankLineAfter = false,
				};

				if (prop.InitValue != null)
				{
					originalField.InitValue = prop.InitValue;
					currentField.InitValue  = prop.InitValue;
				}

				gr.Members.Insert(0, currentField);

				prop.Name        = "         " + name;
				prop.TypeBuilder = () => " " + type;
				prop.IsAuto      = false;

				if (prop.HasGetter) prop.GetBodyBuilders.Add(() => new [] { "return " + currentField.Name.Trim() + ";" });
				if (prop.HasSetter) prop.SetBodyBuilders.Add(() => new [] { currentField.Name.Trim() + " = value;" });

				var ac = new TMethod   { TypeBuilder = static () => "void", Name = $"Accept{name}Changes", BodyBuilders = { () => [$"_original{name} = _current{name};"] } };
				var rc = new TMethod   { TypeBuilder = static () => "void", Name = $"Reject{name}Changes", BodyBuilders = { () => [$"{name} = _original{name};"]         } };
				var id = new TProperty { TypeBuilder = static () => "bool", Name = $"Is{name}Dirty" }
					.InitGetter(string.Format(prop.IsDirtyText, $"_current{name}", $"_original{name}"));

				gr.Members.Add(new TMemberGroup
				{
					Region  = "EditableObject support",
					Members = { ac, rc, id },
				});

				prop.Parent!.SetTree();
			}

			foreach (var cl in GetTreeNodes(Model).OfType<IClass>())
			{
				var props = GetTreeNodes(cl).OfType<IEditableObjectProperty>().Where(p => p.IsEditable).ToList();

				if (props.Count > 0)
				{
					if (props.Any(p => p.IsEditable))
					{
						var ctor = GetTreeNodes(cl)
							.OfType<TMethod>()
							.FirstOrDefault(m => m.Name == cl.Name && m.ParameterBuilders.Count == 0);

						if (ctor == null)
						{
							ctor = new TMethod { Name = cl.Name };
							cl.Members.Insert(0, ctor);
						}

						ctor.BodyBuilders.Add(() => ["AcceptChanges();"]);
					}

					var maxLen = props.Max(p => p.Name!.Trim().Length);

					var ac = new TMethod   { TypeBuilder = static () => "void", Name = "AcceptChanges", IsVirtual = true };
					var rc = new TMethod   { TypeBuilder = static () => "void", Name = "RejectChanges", IsVirtual = true };
					var id = new TProperty { TypeBuilder = static () => "bool", Name = "IsDirty",       IsVirtual = true, IsAuto = false, HasSetter = false };

					ac.BodyBuilders.   Add(static () => ["BeforeAcceptChanges();", ""]);
					rc.BodyBuilders.   Add(static () => ["BeforeRejectChanges();", ""]);
					id.GetBodyBuilders.Add(static () => ["return"]);

					foreach (var p in props)
					{
						var name = p.Name!.Trim();

						ac.BodyBuilders.   Add(() => new [] { $"Accept{name}Changes();" });
						rc.BodyBuilders.   Add(() => new [] { $"Reject{name}Changes();" });
						id.GetBodyBuilders.Add(() => new [] { $"\tIs{name}Dirty{LenDiff(maxLen, name)} ||" });
					}

					ac.BodyBuilders.Add(static () => ["", "AfterAcceptChanges();"]);
					rc.BodyBuilders.Add(static () => ["", "AfterRejectChanges();"]);

					var getBody = id.BuildGetBody().ToArray();

					getBody[^1] = getBody[^1].Trim(' ' , '|') + ";";

					id.GetBodyBuilders.Clear();
					id.GetBodyBuilders.Add(() => getBody);

					cl.Members.Add(new TMemberGroup
					{
						Region  = "EditableObject support",
						Members =
						{
							new TMemberGroup
							{
								IsCompact = true,
								Members   =
								{
									new TMethod { TypeBuilder = static () => "void", Name = "BeforeAcceptChanges", AccessModifier = AccessModifier.Partial },
									new TMethod { TypeBuilder = static () => "void", Name = "AfterAcceptChanges",  AccessModifier = AccessModifier.Partial },
								}
							},
							ac,
							new TMemberGroup
							{
								IsCompact = true,
								Members   =
								{
									new TMethod { TypeBuilder = static () => "void", Name = "BeforeRejectChanges", AccessModifier = AccessModifier.Partial },
									new TMethod { TypeBuilder = static () => "void", Name = "AfterRejectChanges",  AccessModifier = AccessModifier.Partial },
								}
							},
							rc,
							id
						},
					});

					if (!cl.Interfaces.Contains("IEditableObject"))
					{
						if (Model.Usings.Contains("System.ComponentModel") == false)
							Model.Usings.Add("System.ComponentModel");

						cl.Interfaces.Add("IEditableObject");

						cl.Members.Add(new TMemberGroup
						{
							Region  = "IEditableObject support",
							Members =
							{
								new TMemberGroup
								{
									IsCompact = true,
									Members   =
									{
										new TField    { TypeBuilder = static () => "bool", Name = "_isEditing", AccessModifier = AccessModifier.Private },
										new TProperty { TypeBuilder = static () => "bool", Name = " IsEditing" }.InitGetter(() => ["_isEditing"]),
									}
								},
								new TMemberGroup
								{
									IsCompact = true,
									Members   =
									{
										new TMethod { TypeBuilder = static () => "void", Name = "BeginEdit",  BodyBuilders = { static () => ["AcceptChanges();",    "_isEditing = true;"] }, IsVirtual = true },
										new TMethod { TypeBuilder = static () => "void", Name = "CancelEdit", BodyBuilders = { static () => ["_isEditing = false;", "RejectChanges();"  ] }, IsVirtual = true },
										new TMethod { TypeBuilder = static () => "void", Name = "EndEdit",    BodyBuilders = { static () => ["_isEditing = false;", "AcceptChanges();"  ] }, IsVirtual = true },
									}
								},
							}
						});
					}
				}

				cl.SetTree();
			}
		}
	}
}
