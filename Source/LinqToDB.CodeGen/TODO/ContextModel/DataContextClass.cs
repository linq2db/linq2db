namespace LinqToDB.CodeGen.ContextModel
{
	public class DataContextClass
	{
		public DataContextClass(string className, string baseClassName)
		{
			ClassName = className;
			BaseClassName = baseClassName;
		}

		public string ClassName { get; }
		public string BaseClassName { get; }
	}
}
