using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class UserArticleDto
{
    public string Id { get; set; } = string.Empty;
    public string HeritageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ContentHtml { get; set; }
    public object? Blocks { get; set; }
    public List<string> Images { get; set; } = new();
    public int LikesCount { get; set; }
}
