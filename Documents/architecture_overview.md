# Kiến trúc Hệ thống .NET Core Backend (Clean Architecture & CQRS)
Hệ thống của bạn được thiết kế tuân thủ nghiêm ngặt **Clean Architecture** (Kiến trúc sạch) kết hợp cùng **CQRS** (Command Query Responsibility Segregation). Sự kết hợp này mang lại khả năng mở rộng cao, dễ dàng bảo trì và tối ưu hóa hiệu năng riêng biệt cho cả luồng Đọc (Query) và Ghi (Write).

## 1. Nguyên lý Độc lập (The Dependency Rule)

Nguyên tắc cốt lõi của Clean Architecture là **Dependency Rule**: Các lớp bên trong (Inner Layers) tuyệt đối không được phụ thuộc vào các lớp bên ngoài (Outer Layers).

- Mũi tên phụ thuộc luôn hướng vào trung tâm (Domain Layer).
- Interfaces được định nghĩa ở lớp bên trong (ví dụ: `IRepository`) nhưng được triển khai (implement) ở lớp bên ngoài (Infrastructure). Cơ chế này gọi là **Dependency Inversion**.

## 2. Trách nhiệm của các Lớp (Layers)

### 2.1. Domain Layer (Lõi hệ thống)

Đây là trái tim của hệ thống, không phụ thuộc vào bất kỳ framework hay công nghệ ngoại vi nào.

- **Entities**: Chứa các đối tượng nghiệp vụ cốt lõi (ví dụ: `Location`, `Contribution`).
- **Interfaces**: Định nghĩa các hợp đồng (contracts) cho Repository (`IRepository`, `IUnitOfWork`) mà Infrastructure sẽ phải thực thi.
- **Domain Exceptions & Rules**: Các logic và quy tắc kinh doanh thuần túy.

### 2.2. Application Layer (Nghiệp vụ ứng dụng)

Điều phối các use-cases của ứng dụng. Lớp này chỉ phụ thuộc vào Domain Layer.

- **CQRS với MediatR**: Tách biệt luồng Ghi (`Commands`) và luồng Đọc (`Queries`).
- **FluentValidation**: Hoạt động như một Pipeline Behavior trong MediatR để kiểm tra tính hợp lệ của Request trước khi tiến tới Handler.
- **DTOs**: Chứa các Data Transfer Objects dùng để định dạng dữ liệu trả về.

### 2.3. Infrastructure Layer (Cơ sở hạ tầng)

Chịu trách nhiệm giao tiếp với thế giới bên ngoài (Database, Message Broker, Cache). Phụ thuộc vào Application và Domain.

- **SQL Server (EF Core)**: Triển khai Repository Pattern và UnitOfWork cho luồng Ghi. Áp dụng **Transactional Outbox Pattern** để đảm bảo tính nhất quán của dữ liệu (Eventual Consistency).
- **SQL Server (Dapper)**: Triển khai Query Services, đọc trực tiếp bằng SQL thuần để đạt tốc độ tối đa.
- **MongoDB**: Cơ sở dữ liệu NoSQL lưu trữ cấu trúc dạng Document phức tạp, không định dạng (ví dụ: Bài viết chi tiết).
- **Redis Cache**: Triển khai Cache-Aside pattern giúp giảm tải cho DB trong luồng Đọc.
- **RabbitMQ**: Đóng vai trò là Message Broker. Background Worker sẽ quét bảng Outbox và ném message vào RabbitMQ để đồng bộ hóa dữ liệu.

### 2.4. Presentation Layer (Giao diện API)

Điểm chạm (entry-point) của ứng dụng. Phụ thuộc vào Application Layer.

- **FastEndpoints (REPR Pattern)**: Tổ chức theo kiến trúc Request-Endpoint-Response. Loại bỏ Controllers cồng kềnh, mỗi Endpoint chỉ xử lý một nhiệm vụ duy nhất, gọi trực tiếp đến MediatR.

---

## 3. UML Component Diagram

Sơ đồ dưới đây mô tả cách các thành phần giao tiếp với nhau và chiều hướng phụ thuộc (Dependency Rule):

```mermaid
graph TD
    %% Tùy chỉnh CSS
    classDef domain fill:#f9f0ff,stroke:#d4b3ff,stroke-width:2px,color:#000;
    classDef app fill:#e6f7ff,stroke:#91d5ff,stroke-width:2px,color:#000;
    classDef infra fill:#f6ffed,stroke:#b7eb8f,stroke-width:2px,color:#000;
    classDef present fill:#fffb8f,stroke:#ffe58f,stroke-width:2px,color:#000;
    classDef db fill:#f0f0f0,stroke:#d9d9d9,stroke-width:2px,color:#000;

    %% Presentation Layer
    subgraph Presentation_Layer ["Presentation Layer (FastEndpoints)"]
        Endpoints["Endpoints (REPR Pattern)"]
    end
    class Presentation_Layer present

    %% Application Layer
    subgraph Application_Layer ["Application Layer (MediatR & CQRS)"]
        Validation["FluentValidation Pipeline"]
        CommandHandlers["Command Handlers (Write)"]
        QueryHandlers["Query Handlers (Read)"]
      
        Endpoints -->|Send Request| Validation
        Validation -->|If Valid| CommandHandlers
        Validation -->|If Valid| QueryHandlers
    end
    class Application_Layer app

    %% Domain Layer
    subgraph Domain_Layer ["Domain Layer (Core)"]
        Entities["Domain Entities"]
        IRepo["Repository Interfaces"]
        IQuery["Query Interfaces"]
      
        CommandHandlers -.->|Uses| Entities
        CommandHandlers -.->|Injects| IRepo
        QueryHandlers -.->|Injects| IQuery
    end
    class Domain_Layer domain

    %% Infrastructure Layer
    subgraph Infrastructure_Layer ["Infrastructure Layer (Polyglot Persistence)"]
        EFCore["EF Core Repository (SQL Server)"]
        Dapper["Dapper QueryService (SQL Server)"]
        MongoDb["MongoDB Repository"]
        Redis["Redis Cache"]
        Outbox["Outbox Worker (Publisher)"]
      
        EFCore -.->|Implements| IRepo
        Dapper -.->|Implements| IQuery
        MongoDb -.->|Implements| IRepo
    end
    class Infrastructure_Layer infra

    %% External Systems
    subgraph External_Systems ["External Systems"]
        SQLDb[(SQL Server Db)]
        MongoDbStore[(MongoDB Store)]
        RedisStore[(Redis Store)]
        RabbitMQ{{"RabbitMQ (Broker)"}}
    end
    class External_Systems db

    %% Data Flow
    EFCore -->|Write/Tx Outbox| SQLDb
    Dapper -->|Read Raw SQL| SQLDb
    QueryHandlers -->|Cache-Aside| Redis
    Redis -->|Hit/Miss| RedisStore
    Outbox -->|Polls Outbox| SQLDb
    Outbox -->|Publishes Event| RabbitMQ
    RabbitMQ -->|Consumes/Syncs| MongoDbStore
    MongoDb -->|Read/Write Document| MongoDbStore
    QueryHandlers -->|Read Document| MongoDb
```
---

## 4. UML Sequence Diagrams

### 4.1. Luồng Ghi (Write Flow) - Eventual Consistency

Flow: `API -> MediatR -> EF Core -> SQL Server (Outbox) -> Background Worker -> RabbitMQ -> MongoDB`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as FastEndpoints
    participant MediatR as Application (MediatR)
    participant EF as EF Core (Repository)
    participant SQL as SQL Server (Data & Outbox)
    participant Worker as Background Worker
    participant RMQ as RabbitMQ
    participant Mongo as MongoDB

    Client->>API: POST /api/resources (Command)
    API->>MediatR: Send(Command)
    Note over MediatR: FluentValidation checks input
    MediatR->>EF: Create Entity
    MediatR->>EF: Create Outbox Message (Event)
    EF->>SQL: SaveChangesAsync (Transaction)
    SQL-->>EF: Transaction Committed
    EF-->>MediatR: Return Success/Id
    MediatR-->>API: Return Result
    API-->>Client: 201 Created

    Note over Worker, SQL: Background task polls Outbox
    loop Every N seconds
        Worker->>SQL: Fetch Pending Outbox Messages
        SQL-->>Worker: List of Events
        Worker->>RMQ: Publish Event (e.g. ResourceCreated)
        RMQ-->>Worker: Ack
        Worker->>SQL: Mark Message as Processed
    end

    Note over RMQ, Mongo: Sync mechanism
    RMQ->>Mongo: Sync/Create matching JSON Document
```
### 4.2. Luồng Đọc (Read Flow) - Cache-Aside & CQRS

Flow: `API -> MediatR -> Redis -> Dapper / MongoDB`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as FastEndpoints
    participant MediatR as Application (MediatR)
    participant Redis as Redis Cache
    participant Dapper as Dapper (QueryService)
    participant MongoSvc as MongoDB (QueryService)
    participant SQL as SQL Server
    participant Mongo as MongoDB

    Client->>API: GET /api/resources/{slug} (Query)
    API->>MediatR: Send(Query)
    MediatR->>Redis: Check Cache (Key: resource_{slug})  
    alt Cache Hit
        Redis-->>MediatR: Return Cached Data
    else Cache Miss
        Redis-->>MediatR: Null
        par Read Metadata
            MediatR->>Dapper: Get SQL Metadata
            Dapper->>SQL: Execute Raw SQL
            SQL-->>Dapper: Return SQL Data
            Dapper-->>MediatR: Metadata
        and Read Content
            MediatR->>MongoSvc: Get Rich Document
            MongoSvc->>Mongo: Find(ObjectId)
            Mongo-->>MongoSvc: Return JSON Document
            MongoSvc-->>MediatR: Content
        end
        Note over MediatR: Map & Aggregate Data into DTO
        MediatR->>Redis: Set Cache (DTO, Expiry)
    end
    MediatR-->>API: Return Result (DTO)
    API-->>Client: 200 OK
```
