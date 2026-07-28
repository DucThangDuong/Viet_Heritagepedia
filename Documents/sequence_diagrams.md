# Sequence Diagrams (Sơ Đồ Tuần Tự) Cho Tất Cả Các Endpoints

Tài liệu này chứa sơ đồ tuần tự (Sequence Diagram) mô tả luồng dữ liệu của tất cả 8 Endpoints và WebSocket Hub trong hệ thống **Viet Heritagepedia Backend** bằng cú pháp **Mermaid.js**.

---

## 1. Group Locations (Quản Lý Địa Điểm)

### 1.1. `GET /api/locations/{id}` - Lấy Chi Tiết Địa Điểm theo ID

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant API as GetLocationEndpoint
    participant QS as ILocationQueryService
    participant Dapper as Dapper Query
    participant SQL as SQL Server

    Client->>API: GET /api/locations/{id}
    API->>QS: GetByIdAsync(id, ct)
    QS->>Dapper: QuerySingleOrDefaultAsync<LocationResponseDto>(sql, id)
    Dapper->>SQL: SELECT Id, Name, Category, Region... FROM Locations WHERE Id = @Id
    SQL-->>Dapper: Return Row Data
    Dapper-->>QS: LocationResponseDto
    alt Data Found
        QS-->>API: LocationResponseDto
        API-->>Client: 200 OK (ApiSuccessResponse + Data)
    else Data Not Found
        QS-->>API: null
        API-->>Client: 404 Not Found (ERR_LOCATION_NOT_FOUND)
    end
```

---

### 1.2. `POST /api/locations` - Tạo Địa Điểm Mới (CQRS Write)

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as CreateLocationEndpoint
    participant Validator as CreateLocationCommandValidator
    participant MediatR as IMediator
    participant Handler as CreateLocationCommandHandler
    participant Repo as ILocationRepository (SQL)
    participant UoW as IUnitOfWork (EF Core)

    Client->>API: POST /api/locations
    activate API
    
    Note over API,Validator: FastEndpoints tự động trigger Validator
    API->>Validator: ValidateAsync(Command)
    activate Validator
    Validator->>Repo: IsLocationNameUniqueAsync(Name)
    Repo-->>Validator: bool (isUnique)
    
    alt Validation Fails
        Validator-->>API: Validation Errors
        API-->>Client: 400 Bad Request
    end
    Validator-->>API: Validation Passed
    deactivate Validator
    
    API->>MediatR: Send(CreateLocationCommand)
    activate MediatR
    
    MediatR->>Handler: Handle(Command, ct)
    activate Handler
    
    Note over Handler: Generate Slug (nếu chưa có)<br/>Map sang Entity Location
    
    Handler->>Repo: AddAsync(entity)
    Repo-->>Handler: Task (Tracked in Memory)
    
    Handler->>UoW: SaveChangesAsync()
    Note right of UoW: SQL Transaction: Insert Location
    UoW-->>Handler: Task (Committed)
    
    Note over Handler: Map Entity sang LocationResponseDto
    Handler-->>MediatR: Result.Success(201, Dto)
    deactivate Handler
    
    MediatR-->>API: Result object
    deactivate MediatR
    
    API-->>Client: 201 Created (LocationResponseDto)
    deactivate API
```

---

### 1.3. `GET /api/locations/nearby` - Lấy Danh Sách Địa Điểm Lân Cận GPS

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant API as GetNearbyLocationsEndpoint
    participant Val as GetNearbyLocationsValidator
    participant QS as ILocationQueryService
    participant Dapper as Dapper Query
    participant SQL as SQL Server

    Client->>API: GET /api/locations/nearby?latitude=16.46&longitude=107.57&radiusInKm=10
    API->>Val: Validate(GetNearbyLocationsRequest)
    alt Invalid GPS / Radius
        Val-->>API: Validation Error
        API-->>Client: 400 Bad Request
    else Valid Request
        API->>QS: GetNearbyLocationsAsync(lat, lng, radius, ct)
        QS->>Dapper: QueryAsync<NearbyLocationDto>(HaversineSQL)
        Dapper->>SQL: SELECT Id, Name, Distance, PendingContributionsCount FROM Locations...
        SQL-->>Dapper: Return Nearby List
        Dapper-->>QS: List<NearbyLocationDto>
        QS-->>API: List<NearbyLocationDto>
        API-->>Client: 200 OK (List<NearbyLocationDto>)
    end
```

---

## 2. Group Heritage Details (Chi Tiết Di Sản Polyglot SQL + Mongo)

### 2.1. `GET /api/heritage-details/{slug}` - Lấy Chi Tiết Di Sản Tổng Hợp

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant API as GetHeritageDetailEndpoint
    participant QS as IHeritageQueryService
    participant Dapper as Dapper (SQL)
    participant MongoSvc as MongoDbContext
    participant SQL as SQL Server
    participant Mongo as MongoDB

    Client->>API: GET /api/heritage-details/{slug}
    alt Invalid Slug
        API-->>Client: 400 Bad Request (ERR_INVALID_SLUG)
    else Valid Slug
        API->>QS: GetBySlugAsync(slug, ct)
        par Read SQL Metadata
            QS->>Dapper: Get Location & Contribution Metadata
            Dapper->>SQL: SELECT * FROM Locations WHERE Slug = @slug
            SQL-->>Dapper: Location Metadata
        and Read MongoDB Content
            QS->>MongoSvc: GetCollection<HeritageDetailDocument>("contributions_details")
            MongoSvc->>Mongo: Find(LocationId)
            Mongo-->>MongoSvc: HeritageDetailDocument (JSON)
        end
        Note over QS: Aggregate SQL Metadata + Mongo Rich JSON into HeritageDetailDto
        alt Data Found
            QS-->>API: HeritageDetailDto
            API-->>Client: 200 OK (HeritageDetailDto)
        else Data Not Found
            QS-->>API: null
            API-->>Client: 404 Not Found (ERR_DOCUMENT_NOT_FOUND)
        end
    end
```

---

### 2.2. `POST /api/heritage-details` (DEPRECATED)

> **[LƯU Ý]** Endpoint này và Command `CreateHeritageDetailCommand` tương ứng đã bị xóa bỏ hoàn toàn trong bộ mã nguồn. 
> Logic lưu trữ Polyglot (SQL + MongoDB) hiện tại đã được chuyển giao cho Cụm Tính Năng Document Editor thông qua luồng **Save Draft** và **Publish Contribution** (Xem mục 5).

---

## 3. Group User Articles (Bài Viết Đóng Góp Từ Người Dùng)

### 3.1. `GET /api/user-articles/{id}` - Lấy Chi Tiết Bài Viết Đóng Góp (Dynamic JSON Blocks)

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant API as GetUserArticleEndpoint
    participant QS as IHeritageQueryService
    participant Dapper as Dapper (SQL)
    participant MongoDbContext as MongoDbContext
    participant SQL as SQL Server
    participant Mongo as MongoDB

    Client->>API: GET /api/user-articles/{id}
    API->>QS: GetUserArticleByIdAsync(id, ct)
    QS->>Dapper: JOIN Contributions + Users + Locations
    Dapper->>SQL: SELECT c.Title, u.FullName, c.NoSqlDocumentId... WHERE c.Id = @id AND c.ContributionType = 2
    SQL-->>Dapper: Contribution Metadata
    
    alt Contribution Found in SQL
        QS->>MongoDbContext: Get BSON Document by NoSqlDocumentId
        MongoDbContext->>Mongo: FindOne("_id": ObjectId(NoSqlDocumentId))
        Mongo-->>MongoDbContext: BsonDocument (ContentHtml, Blocks)
        Note over QS: Parse BSON Blocks -> System.Text.Json Dynamic Object
        QS-->>API: UserArticleDto (with dynamic Blocks)
        API-->>Client: 200 OK (UserArticleDto)
    else Contribution Not Found
        QS-->>API: null
        API-->>Client: 404 Not Found (ERR_DOCUMENT_NOT_FOUND)
    end
```

---

## 4. Group Documents & Real-Time Streaming (Xử Lý Tài Liệu Bất Đồng Bộ)

### 4.1. `POST /api/documents/upload` - Upload Tài Liệu PDF/DOCX (Magic Bytes Check + MassTransit)

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant API as UploadDocumentEndpoint
    participant Storage as IFileStorageService
    participant Bus as IPublishEndpoint (MassTransit)
    participant Disk as Shared Volume (./shared_uploads)
    participant RMQ as RabbitMQ

    Client->>API: POST /api/documents/upload (multipart/form-data File)
    alt File Null or Extension Invalid (.exe/.txt)
        API-->>Client: 400 Bad Request (ERR_FILE_REQUIRED / ERR_INVALID_FILE_TYPE)
    else Extension Valid (.pdf / .docx)
        API->>Storage: ValidateMagicBytesAsync(Stream, Extension)
        Note over Storage: Read 4 Bytes Header (%PDF / PK.. signature)
        alt Magic Bytes Spoofed (File giả mạo)
            Storage-->>API: false
            API-->>Client: 400 Bad Request (ERR_INVALID_FILE_SIGNATURE)
        else Magic Bytes Valid
            Storage-->>API: true
            API->>Storage: SaveFileAsync(Stream, {JobId}.pdf)
            Storage->>Disk: Write Stream to ./shared_uploads/{JobId}.pdf
            Disk-->>Storage: File Saved FullPath
            API->>Bus: Publish(ConvertDocumentToJsonCommand)
            Bus->>RMQ: Publish Message to Exchange "convert-document-command"
            RMQ-->>Bus: Ack
            API-->>Client: 202 Accepted (JobId, WebSocketUrl)
        end
    end
```

---

### 4.2. `SignalR Hub` & `MassTransit Consumer` - Real-Time WebSocket Chunk Streaming

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant Hub as DocumentProcessingHub (WebSocket)
    participant Consumer as DocumentChunkProcessedConsumer
    participant RMQ as RabbitMQ
    participant Python as Python Worker Service

    Note over Client, Hub: Step 1: Client connects WebSocket & Joins Job Group
    Client->>Hub: Connect /hubs/document-processing
    Client->>Hub: JoinJobGroup(jobId)
    Hub-->>Client: Joined Group {jobId}

    Note over Python, RMQ: Step 2: Python Worker processes PDF page-by-page
    Python->>RMQ: Publish(DocumentChunkProcessedEvent: JobId, ChunkIndex, DataJson, IsCompleted)
    RMQ->>Consumer: Consume(DocumentChunkProcessedEvent)
    Consumer->>Hub: Clients.Group(jobId).SendAsync("ReceiveDocumentChunk", data)
    Hub-->>Client: Push WebSocket Event "ReceiveDocumentChunk" (JSON Batch Data)

    alt Processing Completed (IsCompleted = true)
        Hub-->>Client: Final Chunk Event (IsCompleted: true)
        Client->>Hub: LeaveJobGroup(jobId)
    end
```

---

## 5. Group Contributions (Document Editor)

### 5.1. `POST /api/contributions/drafts` - Auto-Save Draft (Mỗi lần user thao tác Editor)

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as SaveDraftEndpoint
    participant MediatR as IMediator
    participant Handler as SaveDraftCommandHandler
    participant MongoRepo as IMongoRepository
    participant SQLRepo as IContributionRepository
    participant UoW as IUnitOfWork
    
    Client->>API: POST /api/contributions/drafts
    API->>MediatR: Send(SaveDraftCommand)
    MediatR->>Handler: Handle(Command)
    
    alt is New Draft (ContributionId == null)
        Handler->>MongoRepo: InsertAsync(HeritageDetailDocument)
        Handler->>SQLRepo: AddAsync(Contribution [State=0])
        Handler->>UoW: SaveChangesAsync() (No Outbox Message)
        Handler-->>API: Result (ContributionId, MongoId)
    else is Update Draft
        Handler->>SQLRepo: GetByIdAsync(ContributionId)
        alt Not Owner
            Handler-->>API: 403 Forbidden (ERR_UNAUTHORIZED_DRAFT_ACCESS)
        else Is Owner
            Handler->>MongoRepo: ReplaceOneAsync / UpdateAsync (MongoId)
            Handler->>SQLRepo: Update(Contribution.UpdatedAt)
            Handler->>UoW: SaveChangesAsync() (No Outbox Message)
            Handler-->>API: Result (ContributionId, MongoId)
        end
    end
    API-->>Client: 200/201 OK (SaveDraftResponse)
```

---

### 5.2. `POST /api/contributions/{id}/publish` - Publish Bài Viết Từ Draft

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as PublishContributionEndpoint
    participant Validator as PublishContributionCommandValidator
    participant MediatR as IMediator
    participant Handler as PublishContributionCommandHandler
    participant MongoRepo as IMongoRepository
    participant SQLRepo as IContributionRepository
    participant OutboxRepo as IRepository<OutboxMessage>
    participant UoW as IUnitOfWork
    
    Client->>API: POST /api/contributions/{id}/publish
    
    Note over API,Validator: Pipeline bọc MustAsync validate DB
    API->>Validator: ValidateAsync(Command)
    Validator->>SQLRepo: GetByIdAsync(id)
    Validator->>MongoRepo: GetByIdAsync(NoSqlDocumentId)
    Note over Validator: Check State == 0, Owner, Title Length, ContentHtml Length
    alt Validation Fails
        Validator-->>API: Errors (ERR_CONTRIBUTION_NOT_DRAFT, v.v...)
        API-->>Client: 400 Bad Request
    end
    
    Validator-->>API: Validation Passed
    API->>MediatR: Send(Command)
    MediatR->>Handler: Handle(Command)
    
    Handler->>SQLRepo: GetByIdAsync(id)
    Note over Handler: Change State: 0 -> 1 (Pending Review)
    Handler->>SQLRepo: Update(Contribution)
    
    Note over Handler: Prepare Integration Event
    Handler->>OutboxRepo: AddAsync(OutboxMessage: ContributionSubmittedEvent)
    
    Handler->>UoW: SaveChangesAsync()
    Note right of UoW: Atomic Transaction:<br/>Update State + Insert Outbox
    
    Handler-->>API: Result.Success(200)
    API-->>Client: 200 OK
```
