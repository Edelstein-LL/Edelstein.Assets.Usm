namespace Edelstein.Assets.Usm;

public class UsmMetaElement
{
    public string Name { get; set; }
    public UsmMetaElementType Type { get; set; }
    public UsmMetaElement? Previous { get; set; }
    public UsmMetaElement? Next { get; set; }

    private object? _value;

    public UsmMetaElement(string name, UsmMetaElement? previous, UsmMetaElement? next)
    {
        Name = name;
        Previous = previous;
        Next = next;
    }

    public T? GetValue<T>() =>
        (T?)_value;

    public void SetValue<T>(T? value)
    {
        _value = value;

        Type = value switch
        {
            null => UsmMetaElementType.Null,
            sbyte => UsmMetaElementType.SByte,
            byte => UsmMetaElementType.Byte,
            short => UsmMetaElementType.Int16,
            ushort => UsmMetaElementType.UInt16,
            int => UsmMetaElementType.Int32,
            uint => UsmMetaElementType.UInt32,
            long => UsmMetaElementType.Int64,
            ulong => UsmMetaElementType.UInt64,
            float => UsmMetaElementType.Single,
            string => UsmMetaElementType.String,
            byte[] => UsmMetaElementType.ByteArray,
            _ => UsmMetaElementType.Null
        };
    }

    public void CopyValueTo(UsmMetaElement element)
    {
        element._value = _value;
        element.Type = Type;
    }

    public void CopyValueFrom(UsmMetaElement element)
    {
        _value = element._value;
        Type = element.Type;
    }
}
