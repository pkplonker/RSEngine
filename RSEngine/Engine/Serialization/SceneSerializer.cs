using System.Collections;
using System.Reflection;
using Engine.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Editor.Controls;
using Editor.Properties;

namespace Engine
{
	public class SceneSerializer
	{
		private readonly IScene scene;
		private readonly string absolutePath;
		private static JObject rootObject;
		private BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		private static JsonSerializer serializer;

		static SceneSerializer()
		{
			serializer = new JsonSerializer
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				Converters = {new GuidEnumerableConverter()}
			};
			rootObject = new JObject();
		}

		public SceneSerializer(IScene scene, string location)
		{
			if (scene == null) return;
			this.scene = scene;

			this.absolutePath =
				Path.Join(!string.IsNullOrEmpty(location) ? location : ProjectManager.ActiveProject.Directory,
					scene.Name) + IScene.Extension;
			rootObject = new JObject();
		}

		public bool Serialize(IProgressUpdater progress)
		{
			bool result = true;
			try
			{
				var gos = scene?.ChildrenAsGameObjectsRecursive;
				float total = gos.Count();
				for (int i = 0; i < total; i++)
				{
					SerializeGameObject(scene?.ChildrenAsGameObjectsRecursive.ElementAt(i));
					progress.Value = i / total;
				}

				File.WriteAllText(absolutePath, rootObject.ToString());
			}
			catch (Exception e)
			{
				Logger.Error(e);
				result = false;
			}

			if (result)
			{
				Logger.Log($"Serialized scene to {absolutePath}");
			}
			else
			{
				Logger.Warning($"Failed serializing scene to {absolutePath}");
			}

			return result;
		}

		private void SerializeGameObject(GameObject? go)
		{
			if (go == null) return;
			var gameObjectJObject = new JObject();
			var componentsJObject = new JObject();
			SerializeTransform(go.Transform, gameObjectJObject);
			SerializeProperties(go, gameObjectJObject);

			var components = go.GetComponents();
			foreach (var component in components)
			{
				componentsJObject.Add(component.GetType().AssemblyQualifiedName,
					SerializeComponent(component));
			}

			if (componentsJObject.Count > 0)
			{
				gameObjectJObject.Add("components", componentsJObject);
			}

			rootObject.Add(go.Name, gameObjectJObject);
		}

		private void SerializeTransform(Transform transform, JObject parentObject)
		{
			var transformJObject = new JObject();
			SerializeProperties(transform, transformJObject);
			parentObject.Add("transform", transformJObject);
		}

		public static JObject SerializeComponent(object component)
		{
			var componentJObject = new JObject();
			SerializeProperties(component, componentJObject);
			return componentJObject;
		}

		private static void SerializeProperties(object obj, JObject parentObject)
		{
			try
			{
				var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

				var properties = obj.GetType().GetProperties(flags)
					.Where(prop =>
					{
						var attr = prop.GetCustomAttribute<SerializableAttribute>();
						if (attr != null)
						{
							return attr.Show;
						}

						return prop.CanRead && prop.CanWrite;
					});

				foreach (var property in properties)
				{
					CreateObjectFromProperty(obj, parentObject, new PropertyAdapter(property));
				}

				var fields = obj.GetType().GetFields(flags)
					.Where(field => field.IsDefined(typeof(SerializableAttribute), true) &&
					                field.GetCustomAttribute<SerializableAttribute>().Show);

				foreach (var field in fields)
				{
					CreateObjectFromProperty(obj, parentObject, new FieldAdapter(field));
				}
			}
			catch (Exception e)
			{
				Logger.Warning($"Err {obj.GetType()}");
			}
		}

		private static void CreateObjectFromProperty(object obj, JObject parentObject, IMemberAdapter member)
		{
			var value = member.GetValue(obj);

			var attribute = value?.GetType().GetCustomAttribute<SerializableAttribute>();
			if (value != null && attribute != null && attribute.Show)
			{
				var customObjectJObject = new JObject();
				SerializeProperties(value, customObjectJObject);
				parentObject.Add(member.Name, customObjectJObject);
			}
			else if (IsCollection(member.MemberType))
			{
				if (value is IEnumerable collection)
				{
					var array = new JArray();
					foreach (var element in collection)
					{
						if (IsSimpleType(element.GetType()))
						{
							array.Add(JToken.FromObject(element, serializer));
						}
						else
						{
							var elementObject = new JObject();
							SerializeProperties(element, elementObject);
							array.Add(elementObject);
						}
					}

					parentObject.Add(member.Name, array);
				}
			}
			else
			{
				parentObject.Add(member.Name, value == null ? null : JToken.FromObject(value, serializer));
			}
		}

		private static bool IsSimpleType(Type type) =>
			type.IsValueType || type == typeof(string) || type == typeof(Guid);

		private static bool IsCollection(Type type) =>
			type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
	}
}