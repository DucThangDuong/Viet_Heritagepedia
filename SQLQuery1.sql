CREATE DATABASE VietHeritagePedia;
GO
USE VietHeritagePedia;
GO 

-- 1. Bảng Quản lý Địa điểm Di tích (Locations)
CREATE TABLE Locations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Slug VARCHAR(200) NOT NULL UNIQUE, -- URL slug (/heritage/hue-monuments)
    Name NVARCHAR(200) NOT NULL,
    VietnameseName NVARCHAR(200) NULL,
    Category INT NOT NULL, -- 1: Tangible, 2: Natural, 3: Intangible, 4: Documentary
    Region NVARCHAR(50) NOT NULL DEFAULT N'Trung Bộ', -- 'Bắc Bộ', 'Trung Bộ', 'Nam Bộ'
    Province NVARCHAR(100) NOT NULL DEFAULT N'Thừa Thiên Huế',
    Address NVARCHAR(300) NULL,
    IsPlainRegion BIT NOT NULL DEFAULT 0,
    UnescoYear INT NULL,
    GeoCoordinates GEOGRAPHY NOT NULL,
    CoverImageUrl VARCHAR(500) NULL,
    IsFeatured BIT NOT NULL DEFAULT 0, -- Cho trang chủ Bento Grid
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

CREATE SPATIAL INDEX FIX_SpatialIndex_Locations ON Locations(GeoCoordinates);
CREATE INDEX IX_Locations_Filters ON Locations(Region, Province, IsPlainRegion, Category, IsActive);
CREATE INDEX IX_Locations_Slug ON Locations(Slug);
GO

-- 2. Bảng Người dùng (Users) - Chỉ chứa dữ liệu Profile
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FullName NVARCHAR(100) NOT NULL,
    AvatarUrl VARCHAR(500) NULL,
    Role NVARCHAR(100) NULL DEFAULT N'Thành viên',
    Email VARCHAR(100) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1,
    IsEmailVerified BIT NOT NULL DEFAULT 0,
    IsLocked BIT NOT NULL DEFAULT 0,
    LockedUntil DATETIME2 NULL,
    LastLoginAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- 2.1 Bảng Phương thức Đăng nhập (UserAuthProviders) - Federated Identity
CREATE TABLE UserAuthProviders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ProviderName VARCHAR(50) NOT NULL, -- 'Local', 'Google', v.v.
    ProviderKey VARCHAR(255) NOT NULL, -- Email (Local) hoặc GoogleId (Google)
    PasswordHash NVARCHAR(255) NULL, -- Chỉ dùng cho 'Local'
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    LastUsedAt DATETIME2 NULL,
    CONSTRAINT FK_UserAuthProviders_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Provider_ProviderKey UNIQUE (ProviderName, ProviderKey)
);
CREATE INDEX IX_UserAuthProviders_UserId ON UserAuthProviders(UserId);
GO

-- 3. Bảng Quản lý Phiên Đóng góp & Bài viết Cộng đồng (Contributions)
CREATE TABLE Contributions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    LocationId UNIQUEIDENTIFIER NOT NULL,
    AuthorId UNIQUEIDENTIFIER NOT NULL, 
    
    ContributionType INT NOT NULL DEFAULT 2, -- 1: Official_Heritage_Detail, 2: Community_User_Article
    Title NVARCHAR(255) NOT NULL,
    Summary NVARCHAR(500) NULL,
    LikesCount INT NOT NULL DEFAULT 0,

    WorkflowState INT NOT NULL DEFAULT 0, -- 0: Draft, 1: AI_Processing, 2: Pending, 3: Approved, 4: Rejected
    SourceDocumentUrl VARCHAR(500) NULL,
    NoSqlDocumentId VARCHAR(100) NULL, -- Mongo ID
    Version INT DEFAULT 1,
    
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_Contributions_Locations FOREIGN KEY (LocationId) REFERENCES Locations(Id),
    CONSTRAINT FK_Contributions_Users FOREIGN KEY (AuthorId) REFERENCES Users(Id)
);
GO

CREATE INDEX IX_Contributions_Location_Type_State ON Contributions(LocationId, ContributionType, WorkflowState);
GO

-- 4. Bảng Lượt thích bài viết (ContributionLikes)
CREATE TABLE ContributionLikes (
    ContributionId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (ContributionId, UserId),
    CONSTRAINT FK_ContributionLikes_Contributions FOREIGN KEY (ContributionId) REFERENCES Contributions(Id),
    CONSTRAINT FK_ContributionLikes_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- 5. Bảng OutboxMessages (Phục vụ đồng bộ NoSQL)
CREATE TABLE OutboxMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    MessageType VARCHAR(100) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    ProcessedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_OutboxMessages_ProcessedAt ON OutboxMessages(ProcessedAt) WHERE ProcessedAt IS NULL;
GO
USE VietHeritagePedia;
GO

-- ===============================================================================
-- 1. TẠO TÁC GIẢ & HỆ THỐNG (USERS)
-- ===============================================================================
DECLARE @SystemAdminId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @Author1Id     UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @Author2Id     UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @Author3Id     UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';

INSERT INTO Users (Id, FullName, AvatarUrl, Role, Email) VALUES
(@SystemAdminId, N'Ban Quản trị Viet Heritagepedia', 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe', N'Hệ thống', 'admin@heritagepedia.vn'),
(@Author1Id,     N'TS. Nguyễn Văn Hoàng',          'https://images.unsplash.com/photo-1534528741775-53994a69daeb', N'Nhà nghiên cứu Kiến trúc Cung đình', 'hoang.nguyen@heritagepedia.vn'),
(@Author2Id,     N'Kiến trúc sư Trần Minh Anh',   'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d', N'Chuyên gia Quy hoạch Lịch sử', 'minhanh.tran@heritagepedia.vn'),
(@Author3Id,     N'PGS.TS Lương Thu Thảo',        'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2', N'Nhà Địa chất Khảo cổ', 'thao.luong@heritagepedia.vn');

-- ===============================================================================
-- 2. TẠO DANH SÁCH ĐỊA ĐIỂM (LOCATIONS)
-- ===============================================================================
DECLARE @HueId     UNIQUEIDENTIFIER = 'a1111111-1111-1111-1111-111111111111';
DECLARE @HaLongId  UNIQUEIDENTIFIER = 'a2222222-2222-2222-2222-222222222222';
DECLARE @NhaNhacId UNIQUEIDENTIFIER = 'a3333333-3333-3333-3333-333333333333';
DECLARE @HoiAnId   UNIQUEIDENTIFIER = 'a4444444-4444-4444-4444-444444444444';

INSERT INTO Locations 
(Id, Slug, Name, VietnameseName, Category, Region, Province, Address, IsPlainRegion, UnescoYear, GeoCoordinates, CoverImageUrl, IsFeatured, IsActive) 
VALUES
(
    @HueId, 'hue-monuments', 'THE IMPERIAL CITADEL', N'Quần thể Di tích Cố đô Huế', 
    1, N'Trung Bộ', N'Thừa Thiên Huế', N'Phường Thuận Hòa, Thành phố Huế, Tỉnh Thừa Thiên Huế', 
    1, 1993, geography::STGeomFromText('POINT(107.5781 16.4695)', 4326), 
    'https://images.unsplash.com/photo-1569154941061-e231b4725ef1?auto=format&fit=crop&w=1200&q=80', 
    1, 1
),
(
    @HaLongId, 'ha-long-bay', 'HA LONG BAY', N'Vịnh Hạ Long', 
    2, N'Bắc Bộ', N'Quảng Ninh', N'Thành phố Hạ Long, Tỉnh Quảng Ninh', 
    0, 1994, geography::STGeomFromText('POINT(107.1839 20.9101)', 4326), 
    'https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=1200&q=80', 
    1, 1
),
(
    @NhaNhacId, 'nha-nhac-hue', 'ROYAL COURT MUSIC', N'Nhã nhạc Cung đình Huế', 
    3, N'Trung Bộ', N'Thừa Thiên Huế', N'Nhà hát Duyệt Thị Đường, Cố đô Huế', 
    1, 2003, geography::STGeomFromText('POINT(107.5772 16.4682)', 4326), 
    'https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=1200&q=80', 
    1, 1
),
(
    @HoiAnId, 'hoi-an-ancient-town', 'HOI AN ANCIENT TOWN', N'Đô thị cổ Hội An', 
    1, N'Trung Bộ', N'Quảng Nam', N'Thành phố Hội An, Tỉnh Quảng Nam', 
    1, 1999, geography::STGeomFromText('POINT(108.3380 15.8801)', 4326), 
    'https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?auto=format&fit=crop&w=1200&q=80', 
    1, 1
);

-- ===============================================================================
-- 3. TẠO CÁC PHIÊN ĐÓNG GÓP (CONTRIBUTIONS) TRỎ SANG MONGO ID
-- ===============================================================================

-- 3.1 Dữ liệu Chi tiết Di tích Chính (ContributionType = 1)
INSERT INTO Contributions 
(LocationId, AuthorId, ContributionType, Title, Summary, LikesCount, WorkflowState, NoSqlDocumentId) VALUES
(@HueId,     @SystemAdminId, 1, N'Quần thể Di tích Cố đô Huế', N'Kinh đô hoàng gia triều Nguyễn - triều đại phong kiến cuối cùng của Việt Nam.', 520, 3, '60d5ec49f1b2c81234567801'),
(@HaLongId,  @SystemAdminId, 1, N'Vịnh Hạ Long', N'Kỳ quan thiên nhiên thế giới nổi tiếng với hàng nghìn đảo đá vôi kỳ vĩ.', 890, 3, '60d5ec49f1b2c81234567802'),
(@NhaNhacId, @SystemAdminId, 1, N'Nhã nhạc Cung đình Huế', N'Âm nhạc chuẩn mực nghi lễ hoàng gia mang tính bác học cao nhất.', 310, 3, '60d5ec49f1b2c81234567803'),
(@HoiAnId,   @SystemAdminId, 1, N'Đô thị cổ Hội An', N'Thương cảng quốc tế Đông Nam Á cổ xưa được bảo tồn nguyên vẹn.', 640, 3, '60d5ec49f1b2c81234567804');

-- 3.2 Bài viết nghiên cứu Cộng đồng (ContributionType = 2)
INSERT INTO Contributions 
(LocationId, AuthorId, ContributionType, Title, Summary, LikesCount, WorkflowState, NoSqlDocumentId) VALUES
(@HueId,    @Author1Id, 2, N'NGHỆ THUẬT SƠN THIẾP VÀ TRẠM KHẮC GỖ TẠI ĐIỆN THÁI HÒA', N'Nghiên cứu chi tiết về kỹ thuật thếp vàng sơn son trên 80 cột gỗ lim đại điện Thái Hòa.', 142, 3, '60d5ec49f1b2c81234567805'),
(@HueId,    @Author2Id, 2, N'HỆ THỐNG THỦY ĐẠO VÀ PHONG THỦY KINH THÀNH HUẾ', N'Phân tích sự kết hợp giữa dòng sông Hương, núi Ngự Bình và hệ thống hào hồ bao quanh Hoàng thành.', 98,  3, '60d5ec49f1b2c81234567806'),
(@HaLongId, @Author3Id, 2, N'HỆ THỐNG HANG ĐỘNG KARST VÀ DẤU VẾT VĂN HÓA HẠ LONG CỔ', N'Khám phá các công cụ đá búa rìu và di tích cư trú của người Việt cổ trong hang Soi Nhụ.', 115, 3, '60d5ec49f1b2c81234567807');
GO
