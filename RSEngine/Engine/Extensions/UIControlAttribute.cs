namespace Engine;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public abstract class UIControlAttribute : Attribute
{
    public string? Label { get; set; }
    public string? Tooltip { get; set; }
    public bool ReadOnly { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RangeAttribute : UIControlAttribute
{
    public float Min { get; set; } = float.MinValue;
    public float Max { get; set; } = float.MaxValue;
    public float Step { get; set; } = 0.1f;
    public float Speed { get; set; } = 1.0f;
    
    public RangeAttribute() { }
    public RangeAttribute(float min, float max, float step = 0.1f, float speed = 1.0f)
    {
        Min = min;
        Max = max;
        Step = step;
        Speed = speed;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SliderAttribute : UIControlAttribute
{
    public float Min { get; set; } = 0f;
    public float Max { get; set; } = 1f;
    public string? Format { get; set; } = "%.3f";
    
    public SliderAttribute(float min = 0f, float max = 1f)
    {
        Min = min;
        Max = max;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ColorAttribute : UIControlAttribute
{
    public bool ShowAlpha { get; set; } = true;
    public bool HDR { get; set; } = false;
    
    public ColorAttribute(bool showAlpha = true, bool hdr = false)
    {
        ShowAlpha = showAlpha;
        HDR = hdr;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DropdownAttribute : UIControlAttribute
{
    public string[] Options { get; set; }
    
    public DropdownAttribute(params string[] options)
    {
        Options = options;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TextInputAttribute : UIControlAttribute
{
    public int MaxLength { get; set; } = 256;
    public bool Multiline { get; set; } = false;
    public string? Placeholder { get; set; }
    
    public TextInputAttribute(int maxLength = 256, bool multiline = false)
    {
        MaxLength = maxLength;
        Multiline = multiline;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CheckboxAttribute : UIControlAttribute
{
    public CheckboxAttribute() { }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class VectorAttribute : UIControlAttribute
{
    public float Min { get; set; } = float.MinValue;
    public float Max { get; set; } = float.MaxValue;
    public float Step { get; set; } = 0.1f;
    public float Speed { get; set; } = 1.0f;
    
    public VectorAttribute(float min = float.MinValue, float max = float.MaxValue, float step = 0.1f, float speed = 1.0f)
    {
        Min = min;
        Max = max;
        Step = step;
        Speed = speed;
    }
}
