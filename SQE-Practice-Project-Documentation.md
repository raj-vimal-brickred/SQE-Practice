# SQE Practice - Project Documentation & Implementation Guide

**Project Owner:** Raj Vimal  
**Repository:** raj-vimal-brickred/SQE-Practice  
**Language Composition:** C# (96.8%), PowerShell (3.2%)  
**Date:** October 2026  
**Status:** Active Development

---

## Executive Summary

SQE Practice is a comprehensive .NET application designed to reproduce and understand a Standing Query Engine (SQE) telemetry processing workflow. This document provides a complete overview of the project structure, implementation flow, and step-by-step execution guide.

The application demonstrates a production-grade telemetry pipeline that:
- Receives telemetry events via REST API
- Matches events against configured standing queries
- Aggregates matching events in time windows
- Generates metrics with dimensional analysis
- Evaluates alerts based on configured rules
- Persists all data in SQL Server

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture & Project Structure](#architecture--project-structure)
3. [Implementation Steps](#implementation-steps)
4. [Detailed Process Flow](#detailed-process-flow)
5. [Configuration & Setup](#configuration--setup)
6. [API Endpoints](#api-endpoints)
7. [Testing Procedures](#testing-procedures)
8. [Observability & Monitoring](#observability--monitoring)
9. [Quick Reference Guide](#quick-reference-guide)

---

## 1. Project Overview

### Purpose
The SQE Practice application serves as an educational and practical implementation of a Standing Query Engine, commonly used in modern telemetry and monitoring systems.

### Key Components
- **Event Ingestion:** Receives and stores incoming telemetry events
- **Query Matching Engine:** Compares events against standing queries
- **Aggregation Engine:** Aggregates matching events within time windows
- **Alert Engine:** Triggers alerts based on thresholds and conditions
- **SQL Server Persistence:** Stores all events, metrics, and alerts

### Technology Stack
- **Language:** C# (.NET)
- **Database:** SQL Server
- **ORM:** Entity Framework Core
- **Observability:** OpenTelemetry with Console Exporter
- **Deployment Scripts:** PowerShell

---

## 2. Architecture & Project Structure

### Directory Layout

```
SQE_Practice/
│
├── Controllers/
│   ├── EventsController.cs          # Handles event ingestion API
│   ├── MetricsController.cs         # Retrieves generated metrics
│   └── AlertsController.cs          # Retrieves generated alerts
│
├── Models/
│   ├── TelemetryEvent.cs            # Event data model
│   ├── StandingQuery.cs             # Query configuration model
│   ├── Metric.cs                    # Aggregated metric model
│   ├── Alert.cs                     # Alert notification model
│   └── AlertRule.cs                 # Alert evaluation rules
│
├── Services/
│   ├── EventIngestionService.cs     # Event reception and processing
│   ├── QueryLoader.cs               # Loads queries from JSON
│   ├── StandingQueryEngine.cs       # Event-to-query matching logic
│   ├── AggregrationEngine.cs        # Time-windowed aggregation
│   ├── EventStore.cs                # Event persistence layer
│   ├── MetricStore.cs               # Metric persistence layer
│   ├── AlertEngine.cs               # Alert evaluation logic
│   └── AlertStore.cs                # Alert persistence layer
│
├── Storage/
│   ├── SqeDbContext.cs              # Entity Framework context
│   └── Entities/                    # Database entity definitions
│
├── Queries/
│   └── queries.json                 # Standing queries configuration
│
├── Observability/
│   └── SqeTelemetry.cs              # OpenTelemetry instrumentation
│
├── Program.cs                        # Application entry point
├── appsettings.json                 # Configuration settings
└── README.md                         # Project documentation
```

### Component Responsibilities

| Component | Responsibility |
|-----------|-----------------|
| EventsController | REST API endpoint for event submission |
| StandingQueryEngine | Matches incoming events to configured queries |
| AggregrationEngine | Groups events by time window and dimensions |
| AlertEngine | Evaluates metrics against alert thresholds |
| SqeDbContext | Database schema and migrations |
| QueryLoader | Loads and parses standing queries from JSON |
| SqeTelemetry | Provides application-level observability |

---

## 3. Implementation Steps

### Phase 1: Environment Setup

#### Step 1.1: Install Prerequisites
```bash
# Ensure .NET SDK is installed (version 6.0 or higher)
dotnet --version

# Verify SQL Server is running
# For local development, SQL Server Express or Docker can be used
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" `
  -p 1433:1433 `
  -d mcr.microsoft.com/mssql/server:latest
```

#### Step 1.2: Clone Repository
```bash
git clone https://github.com/raj-vimal-brickred/SQE-Practice.git
cd SQE-Practice
```

#### Step 1.3: Restore NuGet Packages
```bash
dotnet restore
```

### Phase 2: Database Configuration

#### Step 2.1: Update Connection String
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "SqeDb": "Server=localhost,1433;Database=SqePractice;User Id=sa;Password=YourPassword123!;"
  }
}
```

#### Step 2.2: Install Entity Framework CLI Tools
```bash
dotnet tool install --global dotnet-ef
```

#### Step 2.3: Add Required NuGet Packages
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package System.Diagnostics.DiagnosticSource
```

#### Step 2.4: Create and Apply Database Migration
```bash
# Create initial migration
dotnet ef migrations add InitialSqlServerCreate

# Apply migration to database
dotnet ef database update
```

This creates three tables:
- **Events:** Stores all incoming telemetry events
- **Metrics:** Stores aggregated metric results
- **Alerts:** Stores triggered alert records

### Phase 3: Configuration

#### Step 3.1: Configure Standing Queries
Edit `Queries/queries.json`:
```json
{
  "queries": [
    {
      "name": "OrderFailureCount",
      "eventName": "OrderFailed",
      "aggregation": "count",
      "windowSeconds": 60,
      "dimensions": ["service", "region"],
      "alert": {
        "operator": "gte",
        "threshold": 3
      }
    },
    {
      "name": "HighLatencyRequests",
      "eventName": "RequestCompleted",
      "aggregation": "avg",
      "windowSeconds": 120,
      "dimensions": ["endpoint"],
      "alert": {
        "operator": "gt",
        "threshold": 1000
      }
    }
  ]
}
```

#### Step 3.2: Configure Application Settings
Edit `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "SqeDb": "Server=localhost,1433;Database=SqePractice;User Id=sa;Password=YourPassword123!;"
  }
}
```

### Phase 4: Build and Deploy

#### Step 4.1: Build the Project
```bash
dotnet build
```

Verify no errors or warnings. Output location:
```
bin/Debug/net6.0/ (or net7.0/net8.0 depending on target)
```

#### Step 4.2: Run the Application
```bash
dotnet run
```

Expected console output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7xxx
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

#### Step 4.3: Verify Application Health
Open browser to: `https://localhost:7xxx/swagger` (if Swagger is configured)
Or test endpoint: `GET https://localhost:7xxx/api/metrics`

---

## 4. Detailed Process Flow

### Complete Application Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                     Client Application                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │  POST /api/events│
                    └────────┬─────────┘
                             │
                             ▼
                    ┌──────────────────────┐
                    │  EventsController    │
                    └────────┬─────────────┘
                             │
                             ▼
                ┌────────────────────────────┐
                │ EventIngestionService      │
                │ - Validate event          │
                │ - Log event metadata      │
                └────────┬───────────────────┘
                         │
                         ▼
                  ┌──────────────┐
                  │  EventStore  │
                  │  (Persist)   │
                  └────┬─────────┘
                       │
                       ▼
          ┌────────────────────────────┐
          │ StandingQueryEngine        │
          │ - Load standing queries    │
          │ - Match event to queries   │
          └────────┬───────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
        ▼                     ▼
    MATCH                  NO MATCH
    │                         │
    │                    (Skip Processing)
    │
    ▼
┌─────────────────────────────────┐
│ Time Window Aggregation         │
│ - Group by windowSeconds        │
│ - Collect matching events       │
└────────┬────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ AggregrationEngine               │
│ - Apply aggregation function:    │
│   • count                        │
│   • sum                          │
│   • avg                          │
│   • min                          │
│   • max                          │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ Dimensional Breakdown            │
│ - Group by dimensions            │
│ - Example:                       │
│   • service: OrderService        │
│   • region: East                 │
└────────┬─────────────────────────┘
         │
         ▼
    ┌─────────────┐
    │ Metric      │
    │ Generated   │
    └────┬────────┘
         │
         ▼
┌──────────────────────┐
│  MetricStore         │
│  (Persist Metric)    │
└────────┬─────────────┘
         │
         ▼
┌────────────────────────────┐
│ AlertEngine                │
│ - Retrieve alert rule      │
│ - Compare: metric vs threshold
│ - Evaluate condition       │
└────────┬───────────────────┘
         │
    ┌────┴─────┐
    │           │
    ▼           ▼
 TRIGGER    NO TRIGGER
   │           │
   │      (Complete)
   │
   ▼
┌──────────────────────┐
│ Alert Generated      │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ AlertStore           │
│ (Persist Alert)      │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ SQL Server Database  │
│ - Events table       │
│ - Metrics table      │
│ - Alerts table       │
└──────────────────────┘
```

### Event Processing Example

**Input Event:**
```json
{
  "eventName": "OrderFailed",
  "service": "OrderService",
  "region": "East",
  "durationMs": 420,
  "properties": {
    "reason": "DatabaseTimeout"
  },
  "timestamp": "2026-10-08T10:30:00Z"
}
```

**Standing Query:**
```json
{
  "name": "OrderFailureCount",
  "eventName": "OrderFailed",
  "aggregation": "count",
  "windowSeconds": 60,
  "dimensions": ["service", "region"],
  "alert": {
    "operator": "gte",
    "threshold": 3
  }
}
```

**Processing Steps:**

| Step | Operation | Result |
|------|-----------|--------|
| 1 | Event received | OrderFailed event stored |
| 2 | Query matching | Event name matches "OrderFailed" ✓ |
| 3 | Time window check | Event within 60-second window ✓ |
| 4 | Aggregation | Count events in window: 3 |
| 5 | Dimensions | OrderService/East: 3 events |
| 6 | Metric generation | OrderFailureCount = 3 |
| 7 | Alert evaluation | 3 >= 3 (threshold) = TRUE |
| 8 | Alert trigger | Alert created and stored |

**Generated Metric:**
```json
{
  "metricName": "OrderFailureCount",
  "value": 3,
  "dimensions": {
    "service": "OrderService",
    "region": "East"
  },
  "timestamp": "2026-10-08T10:31:00Z"
}
```

**Generated Alert:**
```json
{
  "alertName": "OrderFailureCount",
  "triggered": true,
  "value": 3,
  "threshold": 3,
  "operator": "gte",
  "message": "OrderFailureCount exceeded threshold",
  "timestamp": "2026-10-08T10:31:00Z"
}
```

---

## 5. Configuration & Setup

### Standing Queries Configuration

Standing queries are defined in `Queries/queries.json` and specify:

- **name:** Unique identifier for the query
- **eventName:** Event type to match
- **aggregation:** Function to apply (count, sum, avg, min, max)
- **windowSeconds:** Time window for aggregation
- **dimensions:** Fields to group by
- **alert:** Threshold and operator for alert triggering

#### Supported Aggregation Functions

| Function | Description | Example |
|----------|-------------|---------|
| count | Number of events | 5 events → 5 |
| sum | Sum of numeric field | Durations: 100+200+150 → 450 |
| avg | Average of numeric field | Durations: (100+200+150)/3 → 150 |
| min | Minimum value | Durations: 100, 200, 150 → 100 |
| max | Maximum value | Durations: 100, 200, 150 → 200 |

#### Supported Alert Operators

| Operator | Symbol | Meaning |
|----------|--------|---------|
| eq | = | Equal to |
| ne | ≠ | Not equal to |
| lt | < | Less than |
| lte | ≤ | Less than or equal |
| gt | > | Greater than |
| gte | ≥ | Greater than or equal |

### Time Windows

Time windows define the duration over which events are aggregated:

```
windowSeconds = 60

Timeline:
├─ [10:30:00 - 10:31:00] Window 1
├─ [10:31:00 - 10:32:00] Window 2
├─ [10:32:00 - 10:33:00] Window 3
└─ ...
```

Events are grouped and aggregated independently within each window.

### Dimensions

Dimensions enable multi-dimensional analysis of metrics:

```
Query dimensions: ["service", "region"]

Events:
- OrderService/East: 3 failures
- OrderService/West: 1 failure
- PaymentService/East: 2 failures

Generated metrics:
- OrderFailureCount{service="OrderService", region="East"} = 3
- OrderFailureCount{service="OrderService", region="West"} = 1
- OrderFailureCount{service="PaymentService", region="East"} = 2
```

---

## 6. API Endpoints

### Event Ingestion

**Endpoint:** `POST /api/events`

**Request Body:**
```json
{
  "eventName": "OrderFailed",
  "service": "OrderService",
  "region": "East",
  "durationMs": 420,
  "properties": {
    "reason": "DatabaseTimeout",
    "errorCode": "DB_TIMEOUT_001"
  }
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "eventName": "OrderFailed",
  "timestamp": "2026-10-08T10:30:00Z"
}
```

**Response (400 Bad Request):**
```json
{
  "error": "Event validation failed",
  "details": "eventName is required"
}
```

### Retrieve Metrics

**Endpoint:** `GET /api/metrics`

**Query Parameters:**
- `metricName` (optional): Filter by metric name
- `from` (optional): Start timestamp (ISO 8601)
- `to` (optional): End timestamp (ISO 8601)

**Response (200 OK):**
```json
{
  "metrics": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "metricName": "OrderFailureCount",
      "value": 3,
      "dimensions": {
        "service": "OrderService",
        "region": "East"
      },
      "timestamp": "2026-10-08T10:31:00Z"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "metricName": "OrderFailureCount",
      "value": 1,
      "dimensions": {
        "service": "OrderService",
        "region": "West"
      },
      "timestamp": "2026-10-08T10:31:00Z"
    }
  ]
}
```

### Retrieve Alerts

**Endpoint:** `GET /api/alerts`

**Query Parameters:**
- `triggered` (optional): Filter by alert state (true/false)
- `from` (optional): Start timestamp (ISO 8601)
- `to` (optional): End timestamp (ISO 8601)

**Response (200 OK):**
```json
{
  "alerts": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440003",
      "alertName": "OrderFailureCount",
      "triggered": true,
      "value": 3,
      "threshold": 3,
      "operator": "gte",
      "message": "OrderFailureCount exceeded threshold",
      "dimensions": {
        "service": "OrderService",
        "region": "East"
      },
      "timestamp": "2026-10-08T10:31:00Z"
    }
  ]
}
```

### API Response Codes

| Code | Meaning |
|------|---------|
| 200 OK | Request successful, data returned |
| 201 Created | Resource created successfully |
| 400 Bad Request | Invalid request format or parameters |
| 404 Not Found | Resource not found |
| 500 Internal Server Error | Server error occurred |

---

## 7. Testing Procedures

### Test Case 1: Event Ingestion

**Objective:** Verify events are successfully received and stored.

**Steps:**
1. Start the application: `dotnet run`
2. Send event:
```bash
curl -X POST https://localhost:7xxx/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "eventName": "OrderFailed",
    "service": "OrderService",
    "region": "East",
    "durationMs": 420,
    "properties": {"reason": "DatabaseTimeout"}
  }'
```

**Expected Result:**
- HTTP 201 Created response
- Event ID returned
- Event stored in SQL Server Events table

**Verification:**
```sql
SELECT * FROM Events WHERE EventName = 'OrderFailed';
```

### Test Case 2: Query Matching

**Objective:** Verify events are correctly matched to standing queries.

**Steps:**
1. Verify standing queries loaded:
   - Check `queries.json` contains valid queries
   - Application logs should show queries loaded on startup

2. Send 3 matching events in succession:
```bash
for i in {1..3}; do
  curl -X POST https://localhost:7xxx/api/events \
    -H "Content-Type: application/json" \
    -d '{
      "eventName": "OrderFailed",
      "service": "OrderService",
      "region": "East",
      "durationMs": 420,
      "properties": {"reason": "DatabaseTimeout"}
    }'
  sleep 1
done
```

**Expected Result:**
- All 3 events successfully stored
- Events matched to "OrderFailureCount" query
- Events grouped in time window

### Test Case 3: Metric Generation

**Objective:** Verify metrics are generated through aggregation.

**Steps:**
1. Send test events (from Test Case 2)
2. Wait for aggregation window to complete (60 seconds)
3. Retrieve metrics:
```bash
curl -X GET https://localhost:7xxx/api/metrics
```

**Expected Result:**
```json
{
  "metricName": "OrderFailureCount",
  "value": 3,
  "dimensions": {
    "service": "OrderService",
    "region": "East"
  }
}
```

**Verification:**
```sql
SELECT * FROM Metrics WHERE MetricName = 'OrderFailureCount';
```

### Test Case 4: Alert Triggering

**Objective:** Verify alerts trigger when thresholds are exceeded.

**Assumptions:**
- Query configured with threshold: 3
- Operator: gte (greater than or equal)

**Steps:**
1. Send 3 matching events (from Test Case 3)
2. Wait for alert evaluation
3. Retrieve alerts:
```bash
curl -X GET https://localhost:7xxx/api/alerts
```

**Expected Result:**
```json
{
  "alertName": "OrderFailureCount",
  "triggered": true,
  "value": 3,
  "threshold": 3,
  "operator": "gte",
  "message": "OrderFailureCount exceeded threshold"
}
```

**Verification:**
```sql
SELECT * FROM Alerts WHERE AlertName = 'OrderFailureCount' AND Triggered = 1;
```

### Test Case 5: Dimensional Analysis

**Objective:** Verify metrics are correctly grouped by dimensions.

**Steps:**
1. Send events with different dimension values:

```bash
# Event 1: OrderService/East
curl -X POST https://localhost:7xxx/api/events \
  -H "Content-Type: application/json" \
  -d '{"eventName":"OrderFailed","service":"OrderService","region":"East","durationMs":420,"properties":{}}'

# Event 2: OrderService/East
curl -X POST https://localhost:7xxx/api/events \
  -H "Content-Type: application/json" \
  -d '{"eventName":"OrderFailed","service":"OrderService","region":"East","durationMs":500,"properties":{}}'

# Event 3: OrderService/West
curl -X POST https://localhost:7xxx/api/events \
  -H "Content-Type: application/json" \
  -d '{"eventName":"OrderFailed","service":"OrderService","region":"West","durationMs":300,"properties":{}}'
```

2. Retrieve metrics:
```bash
curl -X GET https://localhost:7xxx/api/metrics
```

**Expected Result:**
```json
[
  {
    "metricName": "OrderFailureCount",
    "value": 2,
    "dimensions": {"service": "OrderService", "region": "East"}
  },
  {
    "metricName": "OrderFailureCount",
    "value": 1,
    "dimensions": {"service": "OrderService", "region": "West"}
  }
]
```

### Test Case 6: No Match Scenario

**Objective:** Verify unmatched events don't trigger processing.

**Steps:**
1. Send event that doesn't match any standing query:
```bash
curl -X POST https://localhost:7xxx/api/events \
  -H "Content-Type: application/json" \
  -d '{"eventName":"UnknownEvent","service":"Unknown","region":"Unknown","durationMs":0,"properties":{}}'
```

2. Retrieve metrics and verify no new metrics created for this event

**Expected Result:**
- Event stored in database
- No metrics generated
- No alerts triggered
- Application logs show "unmatched" notification

---

## 8. Observability & Monitoring

### OpenTelemetry Integration

The SQE Practice application uses OpenTelemetry for comprehensive observability.

### Instrumentation Points

#### Activity (Tracing)

```
sqe.processing
├── sqe.processing.event_ingestion
├── sqe.processing.query_matching
├── sqe.processing.aggregation
├── sqe.processing.alert_evaluation
└── sqe.processing.storage
```

**Example trace:**
```
Activity: sqe.processing
├─ Start: 2026-10-08T10:30:00Z
├─ Tags:
│  ├─ event.name: OrderFailed
│  ├─ event.id: 550e8400-...
│  └─ processing.duration: 45ms
└─ Events:
   ├─ query_matched (OrderFailureCount)
   ├─ events_aggregated (count: 3)
   └─ alert_triggered
```

#### Meter (Metrics)

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| sqe.events.received | Counter | count | Total events received |
| sqe.events.matched | Counter | count | Events matched to queries |
| sqe.events.unmatched | Counter | count | Events not matched |
| sqe.metrics.generated | Counter | count | Total metrics generated |
| sqe.alerts.triggered | Counter | count | Total alerts triggered |
| sqe.processing.duration | Histogram | ms | Processing time per event |
| sqe.aggregation.size | Gauge | count | Current aggregation buffer size |

### Observability Configuration

**Program.cs setup:**
```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry
var tracingOleProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("SqeProcessing")
    .AddConsoleExporter()
    .Build();

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("SqeMetrics")
    .AddConsoleExporter()
    .Build();
```

### Monitoring Dashboard Example

```
┌─────────────────────────────────────────────────┐
│         SQE Practice - Live Dashboard           │
├─────────────────────────────────────────────────┤
│                                                 │
│  Events Received:      1,247                    │
│  Events Matched:       1,089 (87.3%)            │
│  Events Unmatched:       158 (12.7%)            │
│                                                 │
│  Metrics Generated:      892                    │
│  Alerts Triggered:        34                    │
│                                                 │
│  Avg Processing Time:    45ms                   │
│  P95 Processing Time:   120ms                   │
│  P99 Processing Time:   250ms                   │
│                                                 │
│  Database Storage:                              │
│  ├─ Events Table:      1,247 rows              │
│  ├─ Metrics Table:       892 rows              │
│  └─ Alerts Table:         34 rows              │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Test Scenarios

#### Scenario 1: Remove Console Exporter

**Objective:** Understand OpenTelemetry behavior without exporter

**Step:** Comment out `.AddConsoleExporter()` in Program.cs
```csharp
// var tracingOleProvider = Sdk.CreateTracerProviderBuilder()
//     .AddSource("SqeProcessing")
//     .AddConsoleExporter()  // ← Disabled
//     .Build();
```

**Observation:** Console output of tracing/metrics stops, but application continues functioning normally

#### Scenario 2: Remove AddMeter

**Objective:** Understand metrics-only collection

**Step:** Comment out meter registration
```csharp
// var meterProvider = Sdk.CreateMeterProviderBuilder()
//     .AddMeter("SqeMetrics")
//     .AddConsoleExporter()
//     .Build();
```

**Observation:** Metric events not exported, but tracing continues

#### Scenario 3: Remove AddSource

**Objective:** Understand tracing-only collection

**Step:** Comment out trace source
```csharp
// var tracingOleProvider = Sdk.CreateTracerProviderBuilder()
//     .AddSource("SqeProcessing")  // ← Disabled
//     .AddConsoleExporter()
//     .Build();
```

**Observation:** Trace events not exported, but metrics continue

---

## 9. Quick Reference Guide

### Common Commands

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run application
dotnet run

# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# View database
sqlcmd -S localhost,1433 -U sa -P YourPassword
> SELECT * FROM Events;
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| Connection string error | Verify SQL Server running, check credentials in appsettings.json |
| Migration fails | Ensure SQL Server database exists, correct connection string |
| Events not aggregating | Check queries.json format, verify time window hasn't expired |
| Alerts not triggering | Verify alert thresholds and operators in queries.json |
| OpenTelemetry not showing output | Ensure console exporter configured, check log level |

### Performance Tuning

| Aspect | Optimization |
|--------|--------------|
| Query Matching | Use hash map for O(1) event name lookups |
| Aggregation | Implement sliding window buffers |
| Database | Add indexes on EventName, Timestamp columns |
| Memory | Implement retention policies for old events |

### Next Steps & Enhancements

1. **Distributed Tracing:** Integrate with Jaeger/Zipkin
2. **Custom Exporters:** Add Prometheus, New Relic exporters
3. **Advanced Queries:** Support regex and JSON path matching
4. **Machine Learning:** Anomaly detection for dynamic thresholds
5. **Horizontal Scaling:** Implement event streaming (Kafka/Pub-Sub)
6. **UI Dashboard:** Create real-time metrics visualization
7. **Alerting Integration:** Connect to PagerDuty, Slack, Teams

---

## Appendix: Project Metadata

| Item | Value |
|------|-------|
| Repository | https://github.com/raj-vimal-brickred/SQE-Practice |
| Owner | Raj Vimal |
| Language | C# (96.8%), PowerShell (3.2%) |
| Repository Size | 70 KB |
| Default Branch | main |
| Visibility | Public |
| License | Not specified |
| Created | 2 days ago |
| Last Updated | 20 hours ago |

---

## Document Changelog

| Date | Version | Changes |
|------|---------|---------|
| 2026-10-08 | 1.0 | Initial comprehensive documentation |

---

**End of Document**

*This document provides a complete guide to the SQE Practice project, including architecture, implementation steps, API specifications, and testing procedures. For questions or contributions, please refer to the GitHub repository.*
