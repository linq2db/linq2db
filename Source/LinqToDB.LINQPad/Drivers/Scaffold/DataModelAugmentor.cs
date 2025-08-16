using System.Linq;

using LinqToDB.CodeModel;

namespace LinqToDB.LINQPad;

/// <summary>
/// AST modification visitor used to add custom scaffold code:
/// <list type="bullet">
/// <item>add custom constructor to generated data context class</item>
/// </list>
/// </summary>
internal sealed class DataModelAugmentor(
	ILanguageProvider languageProvider,
	IType baseContextType,
	int? commandTimeout) : ConvertCodeModelVisitor
{
	private readonly CodeBuilder _ast = languageProvider.ASTBuilder;

	protected override ICodeElement Visit(CodeClass @class)
	{
		// identify context class
		if (@class.Inherits != null && languageProvider.TypeEqualityComparerWithoutNRT.Equals(@class.Inherits.Type, baseContextType))
		{
			var members      = @class.Members.ToList();
			var constructors = new ConstructorGroup(@class);
			members.Add(constructors);

			// context found
			@class = new CodeClass(
				@class.CustomAttributes,
				@class.Attributes,
				@class.XmlDoc,
				@class.Type,
				@class.Name,
				@class.Parent,
				@class.Inherits,
				@class.Implements,
				members,
				@class.TypeInitializer);

			constructors.Class = @class;

			// generate
			// .ctor(string provider, string? assemblyPath, string connectionString)
			var parametrizedCtor      = constructors.New().SetModifiers(Modifiers.Public);
			var providerParam         = _ast.Parameter(WellKnownTypes.System.String, _ast.Name("provider"), CodeParameterDirection.In);
			var assemblyPathParam     = _ast.Parameter(WellKnownTypes.System.String.WithNullability(true), _ast.Name("assemblyPath"), CodeParameterDirection.In);
			var connectionStringParam = _ast.Parameter(WellKnownTypes.System.String, _ast.Name("connectionString"), CodeParameterDirection.In);

			parametrizedCtor
				.Parameter(providerParam)
				.Parameter(assemblyPathParam)
				.Parameter(connectionStringParam)
				.Base(providerParam.Reference, assemblyPathParam.Reference, connectionStringParam.Reference);

			// set default CommandTimeout from LINQPad connection settings
			if (commandTimeout != null)
			{
				parametrizedCtor
					.Body()
					.Append(_ast.Assign(_ast.Member(@class.This, WellKnownTypes.LinqToDB.Data.DataConnection_CommandTimeout), _ast.Constant(commandTimeout.Value, true)));
			}

			return @class;
		}

		return base.Visit(@class);
	}
}
