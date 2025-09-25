using System.Collections;
using System.Diagnostics;

namespace Engine;

[Inspectable(false)]
[Serializable]
public class GameObject : IInspectable
{
	public string Name { get; set; } = "DEFAULT_NAME";

	[Inspectable(true)]
	private HashSet<IComponent> components = new();

	[Serializable(false)]
	public Transform Transform { get; private set; }

	public bool Enabled { get; set; } = true;

	public GameObject()
	{
		Transform = new Transform(this);
	}

	public T? GetComponent<T>() where T : class?, IComponent
	{
		return components?.FirstOrDefault(x => x is T) as T ?? null;
	}

	public bool TryGetComponent<T>(out IComponent? component) where T : class?, IComponent
	{
		component = GetComponent<T>();
		return component != null;
	}

	public T? AddComponent<T>() where T : class, IComponent
	{
		if (GetComponent<T>() != null)
		{
			return null;
		}

		return (T?) AddComponent(typeof(T));
	}

	public IComponent? AddComponent(Type componentType)
	{
		if (!typeof(IComponent).IsAssignableFrom(componentType))
		{
			return null;
		}

		if (components.Any(c => c.GetType() == componentType))
		{
			return null;
		}

		var constructor = componentType.GetConstructor(new[] {typeof(GameObject)});
		if (constructor == null)
		{
			return null;
		}

		var component = (IComponent) constructor.Invoke(new object[] {this});
		if (component != null)
		{
			components.Add(component);
		}

		return component;
	}

	public void RemoveComponent<T>() where T : Component
	{
		var component = components.FirstOrDefault(c => c.GetType() == typeof(T));

		if (component != null)
		{
			components.Remove(component);
		}
	}

	public void RemoveComponent(Type componentType)
	{
		var componentToRemove = components.FirstOrDefault(c => c.GetType() == componentType);
		if (componentToRemove != null)
		{
			components.Remove(componentToRemove);
		}
	}

	public void Update()
	{
		if (!Enabled) return;
		foreach (var component in components)
		{
			component.Update();
		}
	}

	public HashSet<IComponent> GetComponents() => components;

	public void AddComponent(IComponent component)
	{
		components.Add(component);
		component.GameObject = this;
	}
}