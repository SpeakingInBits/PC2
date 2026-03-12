using System.ComponentModel.DataAnnotations;

namespace PC2.Models.ViewModels;

public class PersonViewModel
{
    public int ID { get; set; }

    [Required]
    public PersonType Type { get; set; }

    // ---- Common (People base) ----

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Title { get; set; }

    [Required]
    [Display(Name = "Sort Priority")]
    public byte PriorityOrder { get; set; } = 10;

    // ---- Photo ----

    public string? CurrentImageUrl { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? PhotoFile { get; set; }

    [Display(Name = "Remove current photo")]
    public bool RemovePhoto { get; set; }

    // ---- Staff-specific ----

    public string? Phone { get; set; }

    public int? Extension { get; set; }

    [DataType(DataType.EmailAddress)]
    [EmailAddress]
    public string? Email { get; set; }

    // ---- Board-specific ----

    [Display(Name = "Membership Start Year")]
    public string? MembershipStart { get; set; }

    // ---- Factory methods ----

    public static PersonViewModel FromStaff(Staff s) => new()
    {
        ID = s.ID,
        Type = PersonType.Staff,
        Name = s.Name,
        Title = s.Title,
        PriorityOrder = s.PriorityOrder,
        CurrentImageUrl = s.ImageUrl,
        Phone = s.Phone,
        Extension = s.Extension,
        Email = s.Email
    };

    public static PersonViewModel FromBoard(Board b) => new()
    {
        ID = b.ID,
        Type = PersonType.Board,
        Name = b.Name,
        Title = b.Title,
        PriorityOrder = b.PriorityOrder,
        CurrentImageUrl = b.ImageUrl,
        MembershipStart = b.MembershipStart
    };

    public static PersonViewModel FromSteeringCommittee(SteeringCommittee sc) => new()
    {
        ID = sc.ID,
        Type = PersonType.SteeringCommittee,
        Name = sc.Name,
        Title = sc.Title,
        PriorityOrder = sc.PriorityOrder,
        CurrentImageUrl = sc.ImageUrl
    };
}
