namespace Edelstein.Assets.Usm;

public class UsmMetaPage
{
    public UsmMetaElement? First { get; set; }
    public UsmMetaElement? Last { get; set; }

    public UsmMetaElement AddNewElement(string name)
    {
        UsmMetaElement element = new(name, Last, null);

        if (Last is not null)
            Last.Next = element;
        else
            First = element;

        Last = element;

        return element;
    }
}
