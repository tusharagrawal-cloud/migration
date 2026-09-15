namespace One77.Api.DevHost.Learn;

// Public shapes are deliberately narrower than the domain model — Draft/Hidden
// bookkeeping fields (SortPriority, Status, timestamps) never leave the admin
// surface.
public sealed class PublicLearnCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class PublicLearnEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class PublicLearnCategoryDetail
{
    public PublicLearnCategory Category { get; set; } = null!;
    public List<PublicLearnEntry> Entries { get; set; } = new();
}

public sealed class CreateLearnCategoryRequest
{
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortPriority { get; set; } = 0;
    public string Status { get; set; } = "Draft";
}

public sealed class UpdateLearnCategoryRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? SortPriority { get; set; }
    public string? Status { get; set; }
}

public sealed class CreateLearnEntryRequest
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public int SortPriority { get; set; } = 0;
    public string Status { get; set; } = "Draft";
}

public sealed class UpdateLearnEntryRequest
{
    public int? CategoryId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public int? SortPriority { get; set; }
    public string? Status { get; set; }
}

