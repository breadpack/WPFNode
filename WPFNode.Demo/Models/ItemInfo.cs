namespace WPFNode.Demo.Models;

[Flags]
public enum ItemAttributes {
    None      = 0,
    Stackable = 1 << 0,
    Usable    = 1 << 1,
}

[Flags]
public enum ItemFlags {
    HideFromInventory     = 1 << 0,
    HideLevel             = 1 << 1,
    DontTakeInventorySlot = 1 << 2,
    DestroyByUse          = 1 << 3,
    UndestructableByCount = 1 << 4,
    AutoUse               = 1 << 5,
    Stackable             = 1 << 6,
    IgnoreLevelToStack    = 1 << 7,
    IgnoreGradeToStack    = 1 << 8,
    Seal                  = 1 << 9,
    SpecialSkill          = 1 << 10,
    AutoLevelUp           = 1 << 11,
    NotifyOthers          = 1 << 12,
    Lockable              = 1 << 13,
    AddRelatedItemOnce    = 1 << 14,
    AutoEquip             = 1 << 15,
    AutoEquipEveryTime    = 1 << 16,
    AutoEquipEmptySlot    = 1 << 17,
    ShareLevelWithGroup   = 1 << 18,
    UseFixedGrade         = 1 << 19,
    UseFixedLevel         = 1 << 20,
    SellWithEquipment     = 1 << 21,
    VirtualItem           = 1 << 22,
    ExpireByDay           = 1 << 23,
    ExtendPeriod          = 1 << 24,
    PopupAlram            = 1 << 25,
    AutoTierUp            = 1 << 26,
    HasTier               = 1 << 27,
    HasItemTable          = 1 << 28,
    HasStat               = 1 << 29,
    ExceptOfflineReward   = 1 << 30,
    ZeroCountStart        = 1 << 31
}

public class ItemInfo {
    public Guid           Guid        { get; set; }
    public string         Id          { get; set; }
    public string         Name        { get; set; }
    public string         Description { get; set; }
    public ItemAttributes Attributes  { get; set; }
    public ItemFlags      Flags       { get; set; }
    public int            MaxStack    { get; set; }
}

public class ItemSimple {
    public Guid ItemGuid { get; set; }
    public int  Count    { get; set; }
}

public class ShopitemInfo {
    public Guid   Guid        { get; set; }
    public string Id          { get; set; }
    public string Name        { get; set; }
    public string Description { get; set; }

    public List<ItemSimple> Items  { get; set; }
    public List<ItemSimple> Prices { get; set; }
}