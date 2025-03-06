namespace WPFNode.Demo.Models;

[Flags]
public enum ItemAttributes {
    None = 0,
    Stackable = 1 << 0,
    Usable = 1 << 1,
}
public class ItemInfo {
    public Guid Guid { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ItemAttributes Attributes { get; set; }
    public int MaxStack { get; set; }
}

public class ItemSimple {
    public Guid ItemGuid  { get; set; }
    public int  Count { get; set; }
}

public class ShopitemInfo {
    public Guid Guid { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    public List<ItemSimple> Items  { get; set; }
    public List<ItemSimple> Prices { get; set; }
}