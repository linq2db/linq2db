using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public class Method<T> : MemberBase, IMethod
	where T : IMethod
	{
		public bool                            IsAbstract        { get; set; }
		public bool                            IsVirtual         { get; set; }
		public bool                            IsOverride        { get; set; }
		public bool                            IsStatic          { get; set; }
		public List<string>                    GenericArguments  { get; set; } = [];
		public List<string>                    AfterSignature    { get; set; } = [];
		public List<Func<string>>              ParameterBuilders { get; set; } = [];
		public List<Func<IEnumerable<string>>> BodyBuilders      { get; set; } = [];

		public Method()
		{
		}

		public Method(Func<string> typeBuilder, string name, IEnumerable<Func<string>>? parameterBuilders = null, params Func<IEnumerable<string>>[]? bodyBuilders)
		{
			TypeBuilder = typeBuilder;
			Name        = name;

			if (parameterBuilders  != null) ParameterBuilders.AddRange(parameterBuilders);
			if (bodyBuilders       != null) BodyBuilders.     AddRange(bodyBuilders);
		}

		public Method(string type, string name, IEnumerable<string>? parameters = null, IEnumerable<string>? body = null)
		{
			TypeBuilder = () => type;
			Name        = name;

			if (parameters != null) ParameterBuilders.AddRange(parameters.Select<string,Func<string>>(p => () => p));
			if (body       != null) BodyBuilders.     AddRange(body.Select<string,Func<IEnumerable<string>>>(p => () => new[] { p }).ToArray());
		}

		public IEnumerable<string> BuildBody()
		{
			return BodyBuilders.SelectMany(builder => builder?.Invoke() ?? []);
		}

		public override int CalcModifierLen()
		{
			return
				IsAbstract ? " abstract".Length :
				IsVirtual  ? " virtual".Length  :
				IsStatic   ? " static".Length   : 0;
		}

		public override int CalcBodyLen()
		{
			if (IsAbstract || AccessModifier == AccessModifier.Partial)
				return 1;

			var len = " {".Length;

			foreach (var t in BuildBody())
				len += 1 + t.Length;

			len += " }".Length;

			return len;
		}

		public override int CalcParamLen()
		{
			return ParameterBuilders.Sum(p => p().Length + 2);
		}

		public override void Render(ModelGenerator tt, bool isCompact)
		{
			tt.WriteMethod(this, isCompact);
		}
	}
}
