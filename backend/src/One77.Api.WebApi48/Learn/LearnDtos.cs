using System.Collections.Generic;
using One77.Core.Learn;

namespace One77.Api.WebApi48.Learn
{
    public sealed class PublicLearnCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }

        public static PublicLearnCategory From(LearnCategory c) => new PublicLearnCategory
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description
        };
    }

    public sealed class PublicLearnEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }

        public static PublicLearnEntry From(LearnEntry e) => new PublicLearnEntry
        {
            Id = e.Id,
            Title = e.Title,
            Body = e.Body
        };
    }

    public sealed class PublicLearnCategoryDetail
    {
        public PublicLearnCategory Category { get; set; }
        public List<PublicLearnEntry> Entries { get; set; }
    }

    public sealed class CreateLearnCategoryRequest
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int SortPriority { get; set; }
        public string Status { get; set; } = "Draft";
    }

    public sealed class UpdateLearnCategoryRequest
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int? SortPriority { get; set; }
        public string Status { get; set; }
    }

    public sealed class CreateLearnEntryRequest
    {
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int SortPriority { get; set; }
        public string Status { get; set; } = "Draft";
    }

    public sealed class UpdateLearnEntryRequest
    {
        public int? CategoryId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int? SortPriority { get; set; }
        public string Status { get; set; }
    }
}
