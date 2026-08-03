namespace Domain.Enums;

/// <summary>
/// Trạng thái quy trình làm việc của một Contribution (Bài đóng góp).
/// Dùng để quản lý vòng đời của bài viết từ lúc nháp đến khi được duyệt.
/// </summary>
public enum ContributionWorkflowState
{
    /// <summary>
    /// Bài viết đang ở trạng thái bản nháp (chưa hoàn thiện, chưa gửi để duyệt).
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Bài viết đã được gửi đi và đang chờ Ban quản trị (hoặc Reviewer) kiểm duyệt.
    /// </summary>
    PendingReview = 1,

    /// <summary>
    /// Bài viết đã bị từ chối do không đạt yêu cầu.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Bài viết đã được phê duyệt và hiển thị công khai.
    /// </summary>
    Approved = 3
}
