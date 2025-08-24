using System;

namespace Glamourer.Api.Enums;

[Flags]
public enum ApplyFlag : ulong
{
    Once = 0x0001,
    Equipment = 0x0002,
    Customization = 0x0004,
    Accessories = 0x0008,
    Parameters = 0x0010,
    Stains = 0x0020,
    Crests = 0x0040,
    Weapon = 0x0080,
    Hat = 0x0100,
    Top = 0x0200,
    Gloves = 0x0400,
    Legs = 0x0800,
    Feet = 0x1000,
    WeaponOff = 0x2000,
    Visor = 0x4000,
    WeaponVisible = 0x8000,
    HatVisible = 0x10000,

    All = Equipment | Customization | Accessories | Parameters | Stains | Crests | Weapon | Visor | WeaponVisible | HatVisible,

    EquipHead = Hat,
    EquipBody = Top,
    EquipHands = Gloves,
    EquipLegs = Legs,
    EquipFeet = Feet,
    WeaponMainhand = Weapon,
    WeaponOffhand = WeaponOff,

    EquipAll = EquipHead | EquipBody | EquipHands | EquipLegs | EquipFeet | WeaponMainhand | WeaponOffhand,
}
