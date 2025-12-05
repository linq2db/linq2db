using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using LinqToDB.Internal.Common;

namespace LinqToDB.LINQPad.Json;

internal sealed class IReadOnlySetConverter<T> : JsonConverter<IReadOnlySet<T>>
{
	private readonly JsonConverter<T> _elementConverter;
	private readonly Type _elementType = typeof(T);

	private static IReadOnlySetConverter<T>? _instance;

	public static readonly JsonConverterFactory Factory = new IReadOnlySetConverterFactory();

	private static JsonConverter GetInstance(JsonConverter<T> elementConverter)
	{
		return _instance ??= new IReadOnlySetConverter<T>(elementConverter);
	}

	private IReadOnlySetConverter(JsonConverter<T> elementConverter)
	{
		_elementConverter = elementConverter;
	}

	public override IReadOnlySet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var hashSet = new HashSet<T>();

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray)
			{
				break;
			}

			var item = _elementConverter.Read(ref reader, _elementType, options);
			hashSet.Add(item!);
		}

		return hashSet.AsReadOnly();
	}

	public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (var item in value)
		{
			_elementConverter.Write(writer, item, options);
		}

		writer.WriteEndArray();
	}

	private sealed class IReadOnlySetConverterFactory : JsonConverterFactory
	{
		public override bool CanConvert(Type typeToConvert)
		{
			if (!typeToConvert.IsGenericType)
			{
				return false;
			}

			return typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);
		}

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			var elementType = typeToConvert.GetGenericArguments()[0];

			var converterType = typeof(IReadOnlySetConverter<>).MakeGenericType(elementType);
			return (JsonConverter)converterType
				.GetMethod(nameof(IReadOnlySetConverter<>.GetInstance), BindingFlags.NonPublic | BindingFlags.Static)!
				.InvokeExt(null, [options.GetConverter(elementType)])!;
		}
	}
}
