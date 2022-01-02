using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CLI
{
	internal abstract class CliCommand
	{
		protected CliCommand(string name, string template, string help, CommandExample[] examples)
		{
			Name = name;
			Template = template;
			Help = help;
			Examples = examples;
		}

		public string Name { get; }
		public string Template { get; }
		public string Help { get; }
		public CommandExample[] Examples { get; }

		private readonly Dictionary<OptionCategory, List<CliOption>> _optionsByCategory = new ();
		private readonly Dictionary<string, CliOption> _optionsByName = new ();
		private readonly Dictionary<char, CliOption> _optionsByShortName = new ();

		public IEnumerable<OptionCategory> Categories => _optionsByCategory.Keys.OrderBy(_ => _.Order);
		public IReadOnlyCollection<CliOption> GetCategoryOptions(OptionCategory category) => _optionsByCategory[category];

		protected void AddOption(OptionCategory category, CliOption option)
		{
			if (!_optionsByCategory.TryGetValue(category, out var options))
			{
				_optionsByCategory.Add(category, options = new ());
			}

			options.Add(option);

			_optionsByName.Add(option.Name, option);
			if (option.ShortName != null)
			{
				_optionsByShortName.Add(option.ShortName.Value, option);
			}
		}

		public abstract int Execute(
			CLIController controller,
			string[] rawArgs,
			IReadOnlyCollection<CliOption> options,
			IReadOnlyCollection<string> unknownArgs);
	}
}
