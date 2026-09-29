using System;
using System.Globalization;

using LinqToDB.CommandLine.Commands.QueryExecution;

using ModelContextProtocol.Protocol;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	static class McpQueryExecutionResult
	{
		public static CallToolResult Create(string output, QueryExecutionResult result, int maxResponseBytes)
		{
			if (!result.Truncated)
			{
				return new CallToolResult
				{
					Content = [new TextContentBlock { Text = output }],
				};
			}

			var warning = result.TruncationReason switch
			{
				QueryTruncationReason.MaxOutputBytes => string.Create(
					CultureInfo.InvariantCulture,
					$"The result was truncated after {result.RowsReturned} row(s) because it reached the MCP response size limit of {maxResponseBytes} bytes. Use provider-appropriate keyset pagination or LIMIT/OFFSET and request smaller pages."),
				QueryTruncationReason.MaxRows => string.Create(
					CultureInfo.InvariantCulture,
					$"The result was truncated after {result.RowsReturned} row(s) because it reached the configured row limit. Use pagination or increase maxRows when a larger result is required."),
				_ => "The result was truncated. Use pagination and request smaller pages.",
			};

			return new CallToolResult
			{
				Content =
				[
					new TextContentBlock { Text = output  },
					new TextContentBlock { Text = warning },
				],
			};
		}
	}
}
