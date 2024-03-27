using System;

namespace LinqToDB.Tools.ModelGenerator
{
	public interface IField : IMemberBase
	{
		bool    IsStatic   { get; set; }
		bool    IsReadonly { get; set; }
		string? InitValue  { get; set; }
	}

	public class Field<T> : MemberBase, IField
	where T : Field<T>
	{
		public bool    IsStatic   { get; set; }
		public bool    IsReadonly { get; set; }
		public string? InitValue  { get; set; }

		public Field()
		{
		}

		public Field(ModelType type, string name)
		{
			TypeBuilder = type.ToTypeName;
			Name        = name;
		}

		public Field(Func<string> typeBuilder, string name)
		{
			TypeBuilder = typeBuilder;
			Name        = name;
		}

		public Field(string type, string name)
		{
			TypeBuilder = () => type;
			Name        = name;
		}

		public override int CalcModifierLen()
		{
			return
				(IsStatic   ? " static".  Length : 0) +
				(IsReadonly ? " readonly".Length : 0) ;
		}

		public override int CalcBodyLen() { return InitValue == null ? 1 : 4 + InitValue.Length; }

		public override void Render(CodeTemplateGenerator tt, bool isCompact)
		{
			tt.WriteField(this);
		}
	}
}
