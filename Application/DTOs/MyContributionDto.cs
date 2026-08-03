using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class MyContributionListDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int WorkflowState { get; set; } // 0: Draft, 1: Pending, 2: Rejected, 3: Approved
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MyContributionDetailDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int WorkflowState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Rich content from MongoDB
    public string ContentHtml { get; set; } = string.Empty;
    public object? Blocks { get; set; }
}
