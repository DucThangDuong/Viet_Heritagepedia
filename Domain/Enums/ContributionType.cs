namespace Domain.Enums;

/// <summary>
/// Loại nội dung của một bài đóng góp.
/// </summary>
public enum ContributionType
{
    /// <summary>
    /// Nội dung chính thức (thường do chuyên gia hoặc ban biên tập soạn thảo).
    /// </summary>
    MainContent = 1,

    /// <summary>
    /// Bài viết hoặc đóng góp từ cộng đồng.
    /// </summary>
    CommunityArticle = 2
}
