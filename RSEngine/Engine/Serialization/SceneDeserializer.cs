using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Editor.Controls;
using Engine.Logging;

namespace Engine
{
	public class SceneDeserializer
	{
		private readonly string absolutePath;
		private static readonly JsonSerializer serializer;
		private Dictionary<Guid, GameObject> gameObjectLookup;
		private Dictionary<Guid, List<Guid>> parentToChildrenGuids = new();

		static SceneDeserializer()
		{
			serializer = new JsonSerializer
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				Converters = {new GuidEnumerableConverter()}
			};
		}

		public SceneDeserializer(string absolutePath)
		{
			this.absolutePath = absolutePath;
			gameObjectLookup = new Dictionary<Guid, GameObject>();
		}

		public Scene? Deserialize(IProgressUpdater? ProgressUpdater = null)
		{
			try
			{
				string json = File.ReadAllText(absolutePath);
				JObject rootObject = JObject.Parse(json);
				Scene scene = new Scene();

				DeserializeGameObjects(rootObject, scene, ProgressUpdater);

				SetParents();
				try
				{
					scene.Name = Path.GetFileNameWithoutExtension(this.absolutePath);
					scene.Path = absolutePath.MakeProjectRelative();
				}
				catch (Exception e)
				{
					Logger.Warning($"Failed to set scene name from path: {this.absolutePath}");
				}

				return scene;
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to deserialize {absolutePath} - {e}");
				return null;
			}
		}

		private void SetParents()
		{
			foreach (var kvp in parentToChildrenGuids)
			{
				var parentGuid = kvp.Key;
				var childrenGuids = kvp.Value;

				if (gameObjectLookup.TryGetValue(parentGuid, out var parent))
				{
					foreach (var childGuid in childrenGuids)
					{
						if (gameObjectLookup.TryGetValue(childGuid, out var child))
						{
							child.Transform.SetParent(parent.Transform);
						}
					}
				}
			}
		}

		private void DeserializeGameObjects(JObject rootObject, Scene scene, IProgressUpdater? progressUpdater = null)
		{
			int currentIndex = 0;
			int total = rootObject.Count;

			foreach (var goToken in rootObject)
			{
				GameObject go = DeserializeGameObject(goToken.Key, goToken.Value as JObject);
				if (go != null)
				{
					gameObjectLookup.Add(go.Transform.GUID, go);
					scene.AddGameObject(go);
				}

				if (progressUpdater != null) progressUpdater.Value = (float) currentIndex / total;
			}
		}

		private GameObject DeserializeGameObject(string name, JObject gameObjectJObject)
		{
			var go = new GameObject
			{
				Name = name
			};

			var transformObject = gameObjectJObject["transform"] as JObject;
			if (transformObject != null)
			{
				DeserializeProperties(go.Transform, transformObject);
				var childGuids = ExtractChildGuids(transformObject);
				parentToChildrenGuids[go.Transform.GUID] = childGuids;
			}

			var componentsObject = gameObjectJObject["components"] as JObject;
			if (componentsObject != null)
			{
				foreach (var component in componentsObject)
				{
					var componentType = Type.GetType(component.Key);
					if (componentType != null)
					{
						DeserializeComponent(componentType, component.Value as JObject, go);
					}
				}
			}

			return go;
		}

		private List<Guid> ExtractChildGuids(JObject transformObject)
		{
			var childGuids = new List<Guid>();
			var childrenGuidsToken = transformObject["ChildrenGuids"];
			if (childrenGuidsToken is JArray childrenArray)
			{
				foreach (var childToken in childrenArray)
				{
					if (Guid.TryParse(childToken.ToString(), out Guid childGuid))
					{
						childGuids.Add(childGuid);
					}
				}
			}

			return childGuids;
		}

		public static void DeserializeProperties(object obj, JObject parentObject)
		{
			foreach (var prop in parentObject.Properties())
			{
				var propertyInfo = obj.GetType().GetProperty(prop.Name);
				if (propertyInfo != null && propertyInfo.CanWrite)
				{
					if (IsCollection(propertyInfo.PropertyType) && prop.Value is JArray array)
					{
						Type collectionType = propertyInfo.PropertyType;
						Type elementType = collectionType.GetGenericArguments()[0];

						var collectionInstance = Activator.CreateInstance(collectionType);

						foreach (var elementToken in array)
						{
							object elementObj = null;

							if (IsSimpleType(elementType))
							{
								elementObj = elementToken.ToObject(elementType);
							}
							else
							{
								elementObj = Activator.CreateInstance(elementType);
								DeserializeProperties(elementObj, elementToken as JObject);
							}

							if (typeof(ISet<>).MakeGenericType(elementType).IsAssignableFrom(collectionType))
							{
								MethodInfo addMethod = collectionType.GetMethod("Add", new[] {elementType});
								if (addMethod != null)
								{
									addMethod.Invoke(collectionInstance, new[] {elementObj});
								}
								else
								{
									Logger.Warning($"Add method not found for collection type '{collectionType.Name}'");
								}
							}
							else if (collectionInstance is IList list)
							{
								list.Add(elementObj);
							}
						}

						propertyInfo.SetValue(obj, collectionInstance);
					}
					else
					{
						var value = prop.Value.ToObject(propertyInfo.PropertyType, serializer);
						propertyInfo.SetValue(obj, value);
					}
				}
			}
		}

		private static bool IsSimpleType(Type type) =>
			type.IsValueType || type == typeof(string) || type == typeof(Guid);

		private static bool IsCollection(Type type) =>
			type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) &&
			type.IsGenericType;

		private static void DeserializeComponent(Type componentType, JObject componentJObject, GameObject go)
		{
			var constructor = componentType.GetConstructor(new[] {typeof(GameObject)});
			if (constructor == null)
			{
				Logger.Error($"No suitable constructor found for {componentType.Name}");
				return;
			}

			var component = (IComponent) constructor.Invoke(new object[] {go});
			if (component != null)
			{
				DeserializeProperties(component, componentJObject);
				go.AddComponent(component);
			}
		}
	}
}