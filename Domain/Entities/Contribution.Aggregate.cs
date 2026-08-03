using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;
public partial class Contribution
{
    [NotMapped]
    public ContributionWorkflowState StateEnum
    {
        get => (ContributionWorkflowState)WorkflowState;
        private set => WorkflowState = (int)value;
    }

    [NotMapped]
    public ContributionType TypeEnum
    {
        get => (ContributionType)ContributionType;
        private set => ContributionType = (int)value;
    }

    public static Contribution CreateDraft(
        Guid locationId, 
        Guid authorId, 
        string title, 
        string? summary, 
        string? sourceDocumentUrl, 
        string? noSqlDocumentId,
        ContributionType type)
    {
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            AuthorId = authorId,
            Title = title,
            Summary = summary,
            SourceDocumentUrl = sourceDocumentUrl,
            NoSqlDocumentId = noSqlDocumentId,
            LikesCount = 0,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        contribution.StateEnum = ContributionWorkflowState.Draft;
        contribution.TypeEnum = type;

        return contribution;
    }

    public void UpdateContent(string title, string? summary, string? noSqlDocumentId)
    {
        if (StateEnum != ContributionWorkflowState.Draft && StateEnum != ContributionWorkflowState.Rejected)
        {
            throw new InvalidOperationException("Chỉ có thể cập nhật bài viết khi đang ở trạng thái Nháp hoặc Bị Từ Chối.");
        }

        Title = title;
        Summary = summary;
        if (!string.IsNullOrEmpty(noSqlDocumentId))
        {
            NoSqlDocumentId = noSqlDocumentId;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Chuyển trạng thái sang chờ duyệt.
    /// </summary>
    public void SubmitForReview()
    {
        if (StateEnum != ContributionWorkflowState.Draft && StateEnum != ContributionWorkflowState.Rejected)
        {
            throw new InvalidOperationException("Chỉ có thể gửi duyệt từ trạng thái Nháp hoặc Bị Từ Chối.");
        }

        StateEnum = ContributionWorkflowState.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Phê duyệt bài đóng góp.
    /// </summary>
    public void Approve()
    {
        if (StateEnum != ContributionWorkflowState.PendingReview)
        {
            throw new InvalidOperationException("Chỉ có thể duyệt bài viết đang chờ duyệt.");
        }

        StateEnum = ContributionWorkflowState.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Từ chối bài đóng góp.
    /// </summary>
    public void Reject()
    {
        if (StateEnum != ContributionWorkflowState.PendingReview)
        {
            throw new InvalidOperationException("Chỉ có thể từ chối bài viết đang chờ duyệt.");
        }

        StateEnum = ContributionWorkflowState.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }
}
