using System.ComponentModel.DataAnnotations.Schema;

namespace Kaarigar.Models;

/// <summary>Maps to CITY — master list backing the City dropdown on Register / Employee Profile / Employer Profile.</summary>
[Table("CITY")]
public class City
{
    [Column("CITY_ID")]
    public int CityId { get; set; }

    [Column("CITY_NAME")]
    public string CityName { get; set; } = string.Empty;

    [Column("STATE_NAME")]
    public string? StateName { get; set; }

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    [Column("CREATED_BY")]
    public string? CreatedBy { get; set; }

    [Column("CREATED_TS")]
    public DateTime CreatedTs { get; set; } = DateTime.UtcNow;

    [Column("UPDATED_BY")]
    public string? UpdatedBy { get; set; }

    [Column("UPDATED_TS")]
    public DateTime? UpdatedTs { get; set; }
}

/// <summary>Maps to AREA — localities within a CITY, used for the Area type-ahead once a city is selected.</summary>
[Table("AREA")]
public class Area
{
    [Column("AREA_ID")]
    public int AreaId { get; set; }

    [Column("CITY_ID")]
    public int CityId { get; set; }

    [Column("AREA_NAME")]
    public string AreaName { get; set; } = string.Empty;

    [Column("PINCODE_TXT")]
    public string? PincodeTxt { get; set; }

    [Column("IS_ACTIVE_FL")]
    public bool IsActiveFl { get; set; } = true;

    [Column("CREATED_BY")]
    public string? CreatedBy { get; set; }

    [Column("CREATED_TS")]
    public DateTime CreatedTs { get; set; } = DateTime.UtcNow;

    [Column("UPDATED_BY")]
    public string? UpdatedBy { get; set; }

    [Column("UPDATED_TS")]
    public DateTime? UpdatedTs { get; set; }

    public City? City { get; set; }
}
