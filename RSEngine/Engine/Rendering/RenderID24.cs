using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// Represents a 24-bit render ID (max value: 16,777,215)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RenderID24 : IEquatable<RenderID24>
{
    private const uint MAX_VALUE = 0xFFFFFF; // 16,777,215
    
    private readonly byte byte0;
    private readonly byte byte1;
    private readonly byte byte2;

    public RenderID24(uint value)
    {
        if (value > MAX_VALUE)
            throw new ArgumentOutOfRangeException(nameof(value), 
                $"Value must be between 0 and {MAX_VALUE} (24-bit max)");

        byte0 = (byte)(value & 0xFF);
        byte1 = (byte)((value >> 8) & 0xFF);
        byte2 = (byte)((value >> 16) & 0xFF);
    }
    
    public uint Value => (uint)(byte0 | (byte1 << 8) | (byte2 << 16));

   
    public static RenderID24 CreateClamped(uint value)
    {
        return new RenderID24(Math.Min(value, MAX_VALUE));
    }
    
    public static bool TryCreate(uint value, out RenderID24 result)
    {
        if (value > MAX_VALUE)
        {
            result = default;
            return false;
        }
        result = new RenderID24(value);
        return true;
    }

    public static implicit operator uint(RenderID24 id) => id.Value;
    public static explicit operator RenderID24(uint value) => new RenderID24(value);

    public bool Equals(RenderID24 other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is RenderID24 other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(RenderID24 left, RenderID24 right) => left.Equals(right);
    public static bool operator !=(RenderID24 left, RenderID24 right) => !left.Equals(right);
    public static RenderID24 operator ++(RenderID24 id)
    {
        return id.Value == MAX_VALUE ? throw new OverflowException($"Cannot increment RenderID24 beyond {MAX_VALUE}") : new RenderID24(id.Value + 1);
    }

    public static RenderID24 operator --(RenderID24 id)
    {
        return id.Value == 0 ? throw new OverflowException("Cannot decrement RenderID24 below 0") : new RenderID24(id.Value - 1);
    }
    public static RenderID24 Zero => new RenderID24(0);
    public static RenderID24 MaxValue => new RenderID24(MAX_VALUE);
}