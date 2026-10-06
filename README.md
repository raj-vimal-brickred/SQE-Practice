# SQE Practice

## Overview

SQE Practice is a .NET application created to reproduce and understand a basic SQE-style telemetry processing flow.

The application receives telemetry events, matches them against configured standing queries, generates metrics, creates alerts, and stores the results in SQL Server.

## Project Flow

```text
Application
    |
    v
POST /api/events
    |
    v
Event Ingestion
    |
    v
Standing Query Engine
    |
    v
Query Match
    |
    v
Time Window
    |
    v
Aggregation
    |
    v
Metric
    |
    v
Alert Evaluation
    |
    v
SQL Server
```

## Project Structure

```text
SQE_Practice
|
|-- Controllers
|   |-- EventsController.cs
|   |-- MetricsController.cs
|   `-- AlertsController.cs
|
|-- Models
|   |-- TelemetryEvent.cs
|   |-- StandingQuery.cs
|   |-- Metric.cs
|   |-- Alert.cs
|   `-- AlertRule.cs
|
|-- Services
|   |-- EventIngestionService.cs
|   |-- QueryLoader.cs
|   |-- StandingQueryEngine.cs
|   |-- AggregationEngine.cs
|   |-- EventStore.cs
|   |-- MetricStore.cs
|   |-- AlertEngine.cs
|   `-- AlertStore.cs
|
|-- Storage
|   |-- SqeDbContext.cs
|   `-- Entities
|
|-- Queries
|   `-- queries.json
|
|-- Observability
|   `-- SqeTelemetry.cs
|
|-- Program.cs
`-- appsettings.json
```

## How It Works

### 1. Send a Telemetry Event

Send an event to:

```http
POST /api/events
```

Example:

```json
{
  "eventName": "OrderFailed",
  "service": "OrderService",
  "region": "East",
  "durationMs": 420,
  "properties": {
    "reason": "DatabaseTimeout"
  }
}
```

### 2. Event Ingestion

The API receives the event and passes it to the event ingestion service.

```text
EventsController
    |
    v
EventIngestionService
```

### 3. Load Standing Queries

The application loads standing queries from:

```text
Queries/queries.json
```

Example:

```json
{
  "queries": [
    {
      "name": "OrderFailureCount",
      "eventName": "OrderFailed",
      "aggregation": "count",
      "windowSeconds": 60,
      "dimensions": [
        "service",
        "region"
      ],
      "alert": {
        "operator": "gte",
        "threshold": 3
      }
    }
  ]
}
```

### 4. Query Matching

The Standing Query Engine compares the incoming event with the configured queries.

Example:

```text
Incoming Event: OrderFailed
Query Event:    OrderFailed

Result: Match
```

If the event does not match a query, that query does not process the event.

### 5. Time Window

Each standing query has a configured time window.

Example:

```text
windowSeconds = 60
```

The aggregation uses matching events that belong to the configured window.

### 6. Aggregation

Matching events are processed by the Aggregation Engine.

Example:

```text
OrderFailed
OrderFailed
OrderFailed

Count = 3
```

The generated metric is:

```text
OrderFailureCount = 3
```

### 7. Dimensions

Metrics can be grouped by dimensions.

Example dimensions:

```text
service
region
```

Example:

```text
OrderService / East = 2
OrderService / West = 1
```

### 8. Metric Generation

The generated metric is stored by the Metric Store.

Metrics can be viewed using:

```http
GET /api/metrics
```

### 9. Alert Evaluation

The Alert Engine checks the generated metric against the configured alert rule.

Example:

```text
Metric Value = 3
Threshold    = 3
Operator     = gte
```

Condition:

```text
3 >= 3
```

Result:

```text
Alert Triggered
```

Alerts can be viewed using:

```http
GET /api/alerts
```

## SQL Server

SQL Server is used to persist application data.

The database contains:

```text
Events
Metrics
Alerts
```

The processing flow is:

```text
Telemetry Event
    |
    v
Events Table

Generated Metric
    |
    v
Metrics Table

Generated Alert
    |
    v
Alerts Table
```

## Database Setup

Install the required EF Core packages:

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Create the migration:

```bash
dotnet ef migrations add InitialSqlServerCreate
```

Apply the migration:

```bash
dotnet ef database update
```

## Running the Project

Restore the packages:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the project:

```bash
dotnet run
```

Use the application URL displayed in the console.

## Testing

### Test Event Ingestion

Send:

```http
POST /api/events
```

Example:

```json
{
  "eventName": "OrderFailed",
  "service": "OrderService",
  "region": "East",
  "durationMs": 420,
  "properties": {
    "reason": "DatabaseTimeout"
  }
}
```

### Test Metrics

Send multiple matching events and then call:

```http
GET /api/metrics
```

Expected flow:

```text
OrderFailed
OrderFailed
OrderFailed
    |
    v
OrderFailureCount = 3
```

### Test Alerts

When the metric reaches the configured threshold:

```text
OrderFailureCount = 3
Threshold = 3
```

The Alert Engine creates an alert.

Check it using:

```http
GET /api/alerts
```

## OpenTelemetry

OpenTelemetry is used to observe the SQE application itself.

The application will use:

```text
ActivitySource for tracing
Meter for metrics
Console Exporter for local output
```

Example operational metrics:

```text
sqe.events.received
sqe.events.matched
sqe.events.unmatched
sqe.metrics.generated
sqe.alerts.triggered
sqe.processing.duration
```

The OpenTelemetry flow is:

```text
SQE Application
    |
    |-- ActivitySource
    |
    `-- Meter
          |
          v
    OpenTelemetry
          |
          v
    Console Exporter
```

## OpenTelemetry Test Scenarios

The following scenarios are used to understand OpenTelemetry configuration.

### Remove AddMeter

Remove the Meter registration and run the application.

Record the observed metric behavior.

### Remove AddSource

Remove the ActivitySource registration and run the application.

Record the observed tracing behavior.

### Remove Console Exporter

Remove the console exporter and run the application.

Record the observed console behavior.

Restore each configuration after completing the test.

## API Endpoints

```text
POST /api/events
GET  /api/metrics
GET  /api/alerts
```

## Complete Application Flow

```text
Client Application
    |
    v
POST /api/events
    |
    v
EventsController
    |
    v
EventIngestionService
    |
    v
EventStore
    |
    v
StandingQueryEngine
    |
    v
QueryLoader
    |
    v
queries.json
    |
    v
Query Match
    |
    v
Time Window
    |
    v
Dimensions
    |
    v
AggregationEngine
    |
    v
Metric
    |
    v
MetricStore
    |
    v
AlertEngine
    |
    v
AlertStore
    |
    v
SQL Server
```

## Summary

SQE Practice is a .NET telemetry processing application used to understand a standing-query monitoring workflow.

The main application flow is:

```text
Event
|
v
Standing Query
|
v
Match
|
v
Aggregate
|
v
Metric
|
v
Alert
|
v
SQL Server
```

OpenTelemetry is used separately to observe the processing behavior of the SQE Practice application.