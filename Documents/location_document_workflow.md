# Location Document Upload & Processing Workflow

This sequence diagram illustrates the complete asynchronous flow when a user uploads a document for a specific location. The process spans across the API, Message Broker (RabbitMQ), Python Worker, SQL Server, MongoDB, and Real-time WebSocket (SignalR).

## End-to-End Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    actor Client as User (Frontend)
    participant API as UploadLocationDocumentEndpoint
    participant FileStorage as IFileStorageService
    participant RMQ_Pub as MassTransit (Publisher)
    participant RMQ as RabbitMQ
    participant Python as Python Worker Service
    participant RMQ_Sub as MassTransit (Consumer)
    participant SQL as SQL Server (Contributions)
    participant Mongo as MongoDB (HeritageDetailDocument)
    participant Hub as DocumentProcessingHub (SignalR)

    Note over Client, API: 1. Upload & Queueing Phase
    Client->>API: POST /api/locations/{LocationId}/documents
    API->>FileStorage: ValidateMagicBytesAsync(File)
    FileStorage-->>API: IsValid = true
    API->>FileStorage: SaveFileAsync(File, JobId)
    FileStorage-->>API: Saved FilePath (/shared_uploads/...)
    API->>RMQ_Pub: Publish(ProcessLocationDocumentCommand)
    RMQ_Pub->>RMQ: Enqueue Message
    API-->>Client: 202 Accepted (JobId)
    Client->>Hub: Connect WebSocket & Join Group(JobId)

    Note over Python, Mongo: 2. Worker Processing Phase
    RMQ->>Python: Receive ProcessLocationDocumentCommand
    Note over Python: Python Worker extracts text,<br/>applies AI/NLP to convert to JSON blocks.
    Python->>Mongo: InsertOne(HeritageDetailDocument)
    Mongo-->>Python: Generated MongoDbId (ObjectId)
    Python->>RMQ: Publish(LocationDocumentProcessedEvent)

    Note over RMQ_Sub, Hub: 3. Backend Consumer & Real-time Update Phase
    RMQ->>RMQ_Sub: Receive LocationDocumentProcessedEvent
    RMQ_Sub->>SQL: Create Contribution (AuthorId, LocationId, NoSqlDocumentId)
    SQL-->>RMQ_Sub: SaveChangesAsync() Success
    RMQ_Sub->>Mongo: GetByIdAsync(MongoDbId)
    Mongo-->>RMQ_Sub: Return Rich HeritageDetailDocument JSON
    RMQ_Sub->>Hub: SendAsync("ReceiveLocationDocumentResult", MongoDB Data)
    Hub-->>Client: WebSocket Broadcast: Extraction Result & Rich Content
```
