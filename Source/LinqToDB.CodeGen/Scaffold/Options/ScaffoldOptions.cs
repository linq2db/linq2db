namespace LinqToDB.Scaffold
{
	public class ScaffoldOptions
	{
		private ScaffoldOptions()
		{
		}

		public SchemaOptions         Schema         { get; } = new ();
		public DataModelOptions      DataModel      { get; } = new ();
		public CodeGenerationOptions CodeGeneration { get; } = new ();

		/// <summary>
		/// Gets default scaffold options.
		/// </summary>
		/// <returns>Options object.</returns>
		public static ScaffoldOptions Default()
		{
			// no need to configure, default options already set
			return new ScaffoldOptions();
		}

		/// <summary>
		/// Gets options that correspond to default settings, used by T4 templates.
		/// </summary>
		/// <returns>Options object.</returns>
		public static ScaffoldOptions T4()
		{
			var options = new ScaffoldOptions();
			// TODO: configure
			return options;
		}
	}
}
