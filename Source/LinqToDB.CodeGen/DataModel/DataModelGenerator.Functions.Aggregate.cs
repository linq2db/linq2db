using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildAggregateFunction(AggregateFunctionModel aggregate, Func<RegionGroup> functionsGroup)
		{
			var region = functionsGroup().New(aggregate.Method.Name);
			var method = DefineMethod(region.Methods(false), aggregate.Method, false);
			method.Extension();

			var body = method.Body().Append(
					_code.Throw(_code.New(_code.Type(typeof(InvalidOperationException), false), Array.Empty<ICodeExpression>(), Array.Empty<CodeAssignmentStatement>())));

			_metadataBuilder.BuildFunctionMetadata(aggregate.Metadata, method);

			var source = _code.TypeParameter(_code.Identifier("TSource"));
			method.TypeParameter(source);

			method.Returns(aggregate.ReturnType);

			var sourceParam = _code.Parameter(_code.Type(typeof(IEnumerable<>), false, new[] { source }), _code.Identifier("src"), ParameterDirection.In);
			method.Parameter(sourceParam);

			if (aggregate.Parameters.Count > 0)
			{
				var argIndexes = new ICodeExpression[aggregate.Parameters.Count];
				for (var i = 0; i < aggregate.Parameters.Count; i++)
				{
					var param = aggregate.Parameters[i];

					argIndexes[i] = _code.Constant(i + 1, true);

					var parameterType = param.Parameter.Type;

					parameterType = _code.Type(typeof(Func<,>), false, source, parameterType);
					parameterType = _code.Type(typeof(Expression<>), false, parameterType);

					var p = _code.Parameter(parameterType, _code.Identifier(param.Parameter.Name, null/*ctxModel.Parameter*/, i + 1), ParameterDirection.In);
					method.Parameter(p);
					if (param.Parameter.Description != null)
						method.XmlComment().Parameter(p.Name, param.Parameter.Description);
				}
			}
		}
	}
}
