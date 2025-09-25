using Engine.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine;

public static class ObjectSerializer
{
	public static void Serialize(object obj, string absolutePath)
	{
		try
		{
			var data = SceneSerializer.SerializeComponent(obj);
			var wrappedObject = new JObject
			{
				{"Type", obj.GetType().AssemblyQualifiedName},
				{"Data", data}
			};

			string jsonString = wrappedObject.ToString(Formatting.Indented);

			File.WriteAllText(absolutePath, jsonString);
		}
		catch (Exception e)
		{
			Logger.Warning($"Failed to serialize {obj} to {absolutePath}. {e}");
		}
	}

	public static object Deserialize(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				Logger.Warning($"File not found: {path}");
				return null;
			}

			var json = File.ReadAllText(path);
			var jObject = JObject.Parse(json);

			var typeToken = jObject["Type"];
			if (typeToken == null)
			{
				throw new InvalidOperationException("JSON does not contain type information.");
			}

			Type objType = Type.GetType(typeToken.ToString());
			if (objType == null)
			{
				throw new InvalidOperationException($"Type not found: {typeToken}");
			}

			var dataToken = jObject["Data"];
			if (dataToken == null || dataToken.Type != JTokenType.Object)
			{
				throw new InvalidOperationException("JSON does not contain valid data.");
			}

			var objInstance = Activator.CreateInstance(objType);
			if (objInstance == null)
			{
				throw new InvalidOperationException($"Failed to create an instance of type: {typeToken}");
			}

			SceneDeserializer.DeserializeProperties(objInstance, dataToken as JObject);

			return objInstance;
		}
		catch (Exception e)
		{
			Logger.Warning($"Failed to deserialize from {path}. {e}");
			throw;
		}
	}
}