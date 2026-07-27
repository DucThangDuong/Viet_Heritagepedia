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
    actor Client as Client / Frontend
    participant API as CreateLocationEndpoint
    participant Val as CreateLocationValidator
    participant MediatR as MediatR Pipeline
    participant Handler as CreateLocationCommandHandler
    participant Repo as ILocationRepository
    participant UOW as IUnitOfWork
    participant SQL as SQL Server

    Client->>API: POST /api/locations (CreateLocationCommand)
    API->>Val: Validate(CreateLocationCommand)
    alt Validation Failed
        Val-->>API: Validation Errors
        API-->>Client: 400 Bad Request (ERR_VALIDATION_FAILED)
    else Validation Passed
        API->>MediatR: Send(CreateLocationCommand)
        MediatR->>Handler: Handle(CreateLocationCommand)
        Handler->>Repo: AddAsync(LocationEntity)
        Handler->>UOW: SaveChangesAsync()
        UOW->>SQL: INSERT INTO Locations (...) VALUES (...)
        SQL-->>UOW: Success
        UOW-->>Handler: Commit OK
        Handler-->>MediatR: Result<LocationResponseDto>.Success(dto, 201)
        MediatR-->>API: Result
        API-->>Client: 201 Created (LocationResponseDto)
    end
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

### 2.2. `POST /api/heritage-details` - Tạo Mới Chi Tiết Di Sản

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Frontend
    participant API as CreateHeritageDetailEndpoint
    participant Val as CreateHeritageDetailValidator
    participant MediatR as MediatR
    participant Handler as CreateHeritageDetailCommandHandler
    participant MongoRepo as IMongoRepository
    participant ContributionRepo as IContributionRepository
    participant UOW as IUnitOfWork
    participant Mongo as MongoDB
    participant SQL as SQL Server

    Client->>API: POST /api/heritage-details (CreateHeritageDetailCommand)
    API->>Val: Validate(Command)
    alt Validation Failed (Title/Context/LocationId/AuthorId)
        Val-->>API: ERR_TITLE_REQUIRED / ERR_CONTEXT_MIN_LENGTH
        API-->>Client: 400 Bad Request
    else Validation Passed
        API->>MediatR: Send(Command)
        MediatR->>Handler: Handle(Command)
        
        Note over Handler, Mongo: Step 1: Save Rich Document to MongoDB
        Handler->>MongoRepo: InsertAsync(HeritageDetailDocument)
        MongoRepo->>Mongo: InsertOne(Document)
        Mongo-->>MongoRepo: Generated ObjectId

        Note over Handler, SQL: Step 2: Save Metadata Record to SQL Server
        Handler->>ContributionRepo: AddAsync(ContributionEntity with NoSqlDocumentId)
        Handler->>UOW: SaveChangesAsync()
        UOW->>SQL: INSERT INTO Contributions (...) VALUES (...)
        SQL-->>UOW: Success

        Handler-->>MediatR: Result<Guid>.Success(contribution.Id, 201)
        MediatR-->>API: Result
        API-->>Client: 201 Created (Contribution Guid)
    end
```

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
