using System.Runtime.InteropServices;

public class UnmanagedString : IDisposable
{
	public IntPtr Ptr { get; private set; }
	private bool disposed = false;

	public UnmanagedString(string str)
	{
		Ptr = Marshal.StringToHGlobalAnsi(str);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (Ptr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(Ptr);
				Ptr = IntPtr.Zero;
			}

			disposed = true;
		}
	}

	~UnmanagedString()
	{
		Dispose(false);
	}
}