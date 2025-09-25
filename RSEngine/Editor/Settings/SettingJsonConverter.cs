using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Editor;

public class SettingJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(Setting);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		var jsonObject = JObject.Load(reader);
		var typeToken = jsonObject["Type"];
		var type = Type.GetType(typeToken.Value<string>());

		var setting = new Setting
		{
			Name = jsonObject["Name"]?.Value<string>(),
			Category = jsonObject["Category"]?.Value<string>(),
			Exposed = jsonObject["Exposed"]?.Value<bool>() ?? false,
			Type = type
		};

		if (jsonObject["Prop"] != null)
		{
			setting.Prop = jsonObject["Prop"].ToObject(type, serializer);
		}

		return setting;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		serializer.Serialize(writer, value);
	}
}