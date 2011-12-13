using System;

namespace LinqToDB.Reflection.Extension
{
	public class ValueCollection : System.Collections.Generic.Dictionary<string,object>
	{
		private object _value;
		public  object  Value
		{
			get { return _value; }
		}

		public new object this[string name]
		{
			get
			{
				object value;
				return TryGetValue(name, out value) ? value : null;
			}
		}

		public void Add(string name, string value)
		{
			if (this != _null)
			{
				var isType = name.EndsWith(TypeExtension.ValueName.TypePostfix);

				if (isType)
				{
					var type      = Type.GetType(value, true);
					var valueName = name.Substring(0, name.Length - TypeExtension.ValueName.TypePostfix.Length);

					Add(name, type);

					object val;

					TryGetValue(valueName, out val);

					if (val != null && val.GetType() != type)
					{
						base[valueName] = val = TypeExtension.ChangeType(val.ToString(), type);

						if (valueName == TypeExtension.ValueName.Value)
							_value = val;
					}
				}
				else
				{
					object val;

					var type = TryGetValue(name + TypeExtension.ValueName.TypePostfix, out val) ? (Type)val : null;

					val  = value;

					if (type != null && type != _value.GetType())
						val = TypeExtension.ChangeType(value, type);

					if (ContainsKey(name))
						base[name] = val;
					else
						Add(name, val);

					if (name == TypeExtension.ValueName.Value)
						_value = val;
				}
			}
		}

		private static readonly Extension.ValueCollection _null = new Extension.ValueCollection();
		public  static          Extension.ValueCollection  Null
		{
			get { return _null;  }
		}
	}
}
