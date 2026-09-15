namespace One77.Api.DevHost.Enrichment;

public sealed class SpecificationDto
{
    public string SpecKey { get; set; } = "";
    public string SpecValue { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class CreateEnrichmentRequest
{
    public string ShopifyProductId { get; set; } = "";
    public string Category { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string? Calibre { get; set; }
    public string? PowerplantType { get; set; }
    public decimal? WeightGrains { get; set; }
    public decimal? RecommendedPelletWeightMin { get; set; }
    public decimal? RecommendedPelletWeightMax { get; set; }
    public List<string>? CompatiblePowerplants { get; set; }
    public List<string>? UseCases { get; set; }
    public List<SpecificationDto>? Specifications { get; set; }
}

public sealed class UpdateEnrichmentRequest
{
    public string ShopifyProductId { get; set; } = "";
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public string? Calibre { get; set; }
    public string? PowerplantType { get; set; }
    public decimal? WeightGrains { get; set; }
    public decimal? RecommendedPelletWeightMin { get; set; }
    public decimal? RecommendedPelletWeightMax { get; set; }
    public List<string>? CompatiblePowerplants { get; set; }
    public List<string>? UseCases { get; set; }
    public List<SpecificationDto>? Specifications { get; set; }
}

// Public/internal read shape — omits Id/IsActive/timestamps (admin-only bookkeeping).
public sealed class PublicEnrichment
{
    public string ShopifyProductId { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Calibre { get; set; }
    public string? PowerplantType { get; set; }
    public decimal? WeightGrains { get; set; }
    public decimal? RecommendedPelletWeightMin { get; set; }
    public decimal? RecommendedPelletWeightMax { get; set; }
    public List<string> CompatiblePowerplants { get; set; } = new();
    public List<string> UseCases { get; set; } = new();
    public List<SpecificationDto> Specifications { get; set; } = new();
}
