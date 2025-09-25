using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Engine;

public class GuidEnumerableConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		var guids = (IEnumerable<Guid>) value;
		JArray array = new JArray();
		foreach (var guid in guids)
		{
			array.Add(guid.ToString());
		}

		array.WriteTo(writer);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JArray array = JArray.Load(reader);
		var guids = new List<Guid>();
		foreach (var item in array)
		{
			if (Guid.TryParse(item.ToString(), out Guid guid))
			{
				guids.Add(guid);
			}
			else
			{
				throw new JsonSerializationException("Invalid GUID format.");
			}
		}

		return guids;
	}

	public override bool CanConvert(Type objectType) => objectType == typeof(IEnumerable<Guid>);
}