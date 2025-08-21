using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

using LinqToDB.Extensions;

namespace LinqToDB.LINQPad.UI;

internal sealed class AboutModel
{
	public static AboutModel Instance { get; } = new AboutModel();

	private AboutModel()
	{
		// avoid Uri constructor crash on pack:// scheme in runtime due to initialization order
		// Application constructor will register schema handler
		if (!UriParser.IsKnownScheme("pack"))
			new Application();

		var assembly = typeof(AboutModel).Assembly;
		Logo         = new BitmapImage(new Uri($"pack://application:,,,/{assembly.FullName};component/resources/logo.png"));
		Project      = $"Linq To DB LINQPad Driver v{assembly.GetName().Version!.ToString(3)}";
		Copyright    = assembly.GetAttribute<AssemblyCopyrightAttribute>()!.Copyright;
	}

	public BitmapImage Logo          { get; }
	public string      Project       { get; }
	public string      Copyright     { get; }
	public Uri         RepositoryUri { get; } = new("https://github.com/linq2db/linq2db");
	public Uri         ReportsUri    { get; } = new("https://github.com/linq2db/linq2db/issues/new");
}
