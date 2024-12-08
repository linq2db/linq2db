using System;
using System.Linq;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public partial class ModelGenerator
	{
		public void NotifyDataErrorInfoImplementation<TMemberGroup,TMethod,TProperty,TField,TEvent,TAttribute>()
			where TMemberGroup : MemberGroup<TMemberGroup>, new()
			where TMethod      : Method     <TMethod>,      new()
			where TProperty    : Property   <TProperty>,    new()
			where TField       : Field      <TField>,       new()
			where TEvent       : Event      <TEvent>,       new()
			where TAttribute   : Attribute  <TAttribute>,   new()
		{
			foreach (var prop in  GetTreeNodes(Model).OfType<IPropertyValidation>().Where(p => p.CustomValidation).ToList())
			{
				var p = prop.Parent!;

				while (p != null && p is not IClass)
					p = p.Parent;

				if (p != null)
				{
					var cl = (IClass)p;

					if (!cl.Interfaces.Contains("INotifyDataErrorInfo"))
					{
						Model.Usings.Add("System.ComponentModel");
						Model.Usings.Add("System.Collections");
						Model.Usings.Add("System.Linq");

						cl.Interfaces.Add("INotifyDataErrorInfo");

						cl.Members.Add(new TMemberGroup
						{
							Region  = "INotifyDataErrorInfo support",
							Members =
							{
								new TEvent
								{
									TypeBuilder = static () => new ModelType("EventHandler<DataErrorsChangedEventArgs>", true, true).ToTypeName(),
									Name        = "ErrorsChanged",
									IsVirtual   = true,
									Attributes  = { new TAttribute { Name = "field : NonSerialized" } }
								},
								new TField
								{
									TypeBuilder    = static () => "Dictionary<string,List<string>>",
									Name           = "_validationErrors",
									InitValue      = "new Dictionary<string,List<string>>()",
									AccessModifier = AccessModifier.Private,
									IsReadonly     = true,
									Attributes     = { new TAttribute { Name = "field : NonSerialized" } }
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "AddError",
									ParameterBuilders = { static () => "string propertyName", static () => "string error" },
									BodyBuilders      =
									{
										() =>
										[
											$"List<string>{(EnableNullableReferenceTypes ? "?" : "")} errors;",
											"",
											"if (!_validationErrors.TryGetValue(propertyName, out errors))",
											"{",
												"\t_validationErrors[propertyName] = new List<string> { error };",
											"}",
											"else if (!errors.Contains(error))",
											"{",
												"\terrors.Add(error);",
											"}",
											"else",
												"\treturn;",
											"",
											"OnErrorsChanged(propertyName);",
										]
									},
									AccessModifier = AccessModifier.Public
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "RemoveError",
									ParameterBuilders = { static () => "string propertyName" },
									BodyBuilders      =
									{
										() =>
										[
											$"List<string>{(EnableNullableReferenceTypes ? "?" : "")} errors;",
											"",
											"if (_validationErrors.TryGetValue(propertyName, out errors) && errors.Count > 0)",
											"{",
												"\t_validationErrors.Clear();",
												"\tOnErrorsChanged(propertyName);",
											"}",
										]
									},
									AccessModifier = AccessModifier.Public
								},
								new TMethod
								{
									TypeBuilder       = static () => "void",
									Name              = "OnErrorsChanged",
									ParameterBuilders = { static () => "string propertyName" },
									BodyBuilders      =
									{
										() =>
										[
											"if (ErrorsChanged != null)",
											"{",
												"\tif (System.Windows.Application.Current.Dispatcher.CheckAccess())",
													"\t\tErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));",
												"\telse",
													"\t\tSystem.Windows.Application.Current.Dispatcher.BeginInvoke(",
														"\t\t\t(Action)(() => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName))));",
											"}",
										]
									},
									AccessModifier = AccessModifier.Protected
								},
								new TMethod
								{
									TypeBuilder       = static () => ModelType.Create<System.Collections.IEnumerable>(true).ToTypeName(),
									Name              = "GetErrors",
									ParameterBuilders = { () => $"{ModelType.Create<string>(true).ToTypeName()} propertyName" },
									BodyBuilders      =
									{
										() =>
										[
											$"List<string>{(EnableNullableReferenceTypes ? "?" : "")} errors;",
											"return propertyName != null && _validationErrors.TryGetValue(propertyName, out errors) ? errors : null;",
										]
									},
									AccessModifier = AccessModifier.Public
								},
								new TProperty
								{
									TypeBuilder = static () => "bool",
									Name        = "HasErrors"
								}
								.InitGetter("_validationErrors.Values.Any(e => e.Count > 0)")
							}
						});
					}
				}
			}
		}
	}
}
