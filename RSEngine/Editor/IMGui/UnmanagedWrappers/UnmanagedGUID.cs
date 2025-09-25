using System.Runtime.InteropServices;

public class UnmanagedGuid : IDisposable
{
	public IntPtr Ptr { get; private set; }
	private bool disposed = false;

	public UnmanagedGuid(Guid guid)
	{
		byte[] guidBytes = guid.ToByteArray();
		Ptr = Marshal.AllocHGlobal(guidBytes.Length);
		Marshal.Copy(guidBytes, 0, Ptr, guidBytes.Length);
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

	~UnmanagedGuid()
	{
		Dispose(false);
	}
}