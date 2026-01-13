using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public class Property<T> : MemberBase, IProperty
		where T : Property<T>
	{
		public bool    IsAuto     { get; set; } = true;
		public string? InitValue  { get; set; }
		public bool    IsVirtual  { get; set; }
		public bool    IsOverride { get; set; }
		public bool    IsAbstract { get; set; }
		public bool    IsStatic   { get; set; }
		public bool    HasGetter  { get; set; } = true;
		public bool    HasSetter  { get; set; } = true;
		public bool    IsNullable { get; set; }

		public List<Func<IEnumerable<string>>> GetBodyBuilders { get; set; } = [];
		public List<Func<IEnumerable<string>>> SetBodyBuilders { get; set; } = [];

		public int GetterLen { get; set; } = 5;
		public int SetterLen { get; set; } = 5;

		public virtual bool EnforceNotNullable { get; set; }

		public Property()
		{
		}

		public Property(ModelType type, string name, Func<IEnumerable<string>>? getBodyBuilder = null, Func<IEnumerable<string>>? setBodyBuilder = null)
		{
			TypeBuilder = type.ToTypeName;
			Name        = name;

			InitBody(getBodyBuilder, setBodyBuilder);
		}

		public Property(bool enforceNotNullable, Func<string> typeBuilder, string name, Func<IEnumerable<string>>? getBodyBuilder = null, Func<IEnumerable<string>>? setBodyBuilder = null)
			: this(typeBuilder, name, getBodyBuilder, setBodyBuilder)
		{
			EnforceNotNullable = enforceNotNullable;
		}

		public Property(Func<string> typeBuilder, string name, Func<IEnumerable<string>>? getBodyBuilder = null, Func<IEnumerable<string>>? setBodyBuilder = null)
		{
			TypeBuilder = typeBuilder;
			Name        = name;

			InitBody(getBodyBuilder, setBodyBuilder);
		}

		public Property(string type, string name, Func<IEnumerable<string>>? getBodyBuilder = null, Func<IEnumerable<string>>? setBodyBuilder = null)
		{
			TypeBuilder = () => type;
			Name        = name;

			InitBody(getBodyBuilder, setBodyBuilder);
		}

		public override int CalcModifierLen()
		{
			return IsVirtual ? " virtual".Length : 0;
		}

		public override int CalcBodyLen()
		{
			if (IsAuto)
				return 4 + GetterLen + SetterLen; // ' { get; set; }'

			var len = " {".Length;

			if (HasGetter)
			{
				len += " get {".Length;
				foreach (var t in BuildGetBody())
					len += 1 + t.Length;
				len += " }".Length;
			}

			if (HasSetter)
			{
				len += " set {".Length;
				foreach (var t in BuildSetBody())
					len += 1 + t.Length;
				len += " }".Length;
			}

			len += " }".Length;

			return len;
		}

		public override void Render(ModelGenerator tt, bool isCompact)
		{
			if (!IsAuto && HasGetter)
			{
				var getBody = BuildGetBody().ToArray();
				if (getBody.Length == 1)
				{
					var t = getBody[0];

					if (!t.StartsWith("return"))
					{
						t = "return " + t;

						if (!t.EndsWith(";"))
							t += ";";

						GetBodyBuilders.Clear();
						GetBodyBuilders.Add(() => new [] { t });
					}
				}
			}

			tt.WriteProperty(this, isCompact);
		}

		public IProperty InitBody(Func<IEnumerable<string>>? getBodyBuilder = null, Func<IEnumerable<string>>? setBodyBuilder = null)
		{
			IsAuto = getBodyBuilder == null && setBodyBuilder == null;

			if (getBodyBuilder != null) GetBodyBuilders.Add(getBodyBuilder);
			if (setBodyBuilder != null) SetBodyBuilders.Add(setBodyBuilder);

			if (!IsAuto)
			{
				HasGetter = getBodyBuilder != null;
				HasSetter = setBodyBuilder != null;
			}

			return this;
		}

		public IProperty InitGetter(Func<IEnumerable<string>> getBodyBuilder)
		{
			return InitBody(getBodyBuilder);
		}

		public IProperty InitGetter(string getBody)
		{
			return InitBody(() => [getBody]);
		}

		public IEnumerable<string> BuildGetBody()
		{
			return GetBodyBuilders.SelectMany(builder => builder?.Invoke() ?? []);
		}

		public IEnumerable<string> BuildSetBody()
		{
			return SetBodyBuilders.SelectMany(builder => builder?.Invoke() ?? []);
		}
	}
}
