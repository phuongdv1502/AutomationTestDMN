# Timeout Features in DMN Tester

## Overview
The DMN Tester now includes comprehensive timeout handling to prevent long-running operations from hanging the application and to provide better user experience.

## Configuration

### Backend Timeout Settings
Timeout settings are configured in `appsettings.json` and `appsettings.Development.json`:

```json
{
  "TimeoutSettings": {
    "RequestTimeout": 30000,        // 30 seconds - General API requests
    "DmnEvaluationTimeout": 10000,  // 10 seconds - DMN evaluation operations
    "BatchTestTimeout": 60000,      // 60 seconds - Batch test operations
    "FileReadTimeout": 5000         // 5 seconds - File reading operations
  }
}
```

### Frontend Timeout Settings
Angular frontend has corresponding timeout settings in `api.service.ts`:

```typescript
private readonly requestTimeout = 30000;     // 30 seconds
private readonly evaluationTimeout = 10000;  // 10 seconds
private readonly batchTimeout = 60000;      // 60 seconds
```

## Features

### 1. Backend Timeout Handling

#### DMN Engine Timeouts
- **File Reading**: Timeout for reading DMN and config files
- **DMN Evaluation**: Timeout for evaluating decision tables
- **Rule Processing**: Cancellation token checks during rule evaluation

#### API Controller Timeouts
- **Request Timeout**: General API request timeout
- **Evaluation Timeout**: Specific timeout for DMN evaluation endpoints
- **Batch Test Timeout**: Extended timeout for batch operations
- **HTTP 408 Status**: Returns timeout status code with descriptive messages

### 2. Frontend Timeout Handling

#### HTTP Request Timeouts
- **RxJS Timeout**: Automatic timeout for all HTTP requests
- **Error Handling**: Specific handling for timeout errors (HTTP 408)
- **User-Friendly Messages**: Clear timeout error messages with suggestions

#### UI Improvements
- **Timeout Warnings**: Performance tips when timeouts occur
- **Loading Indicators**: Enhanced loading states with spinners
- **Error Dismissal**: Ability to dismiss error and warning messages

## Error Handling

### Backend Error Responses
```json
{
  "error": "DMN evaluation timed out after 10000ms"
}
```

### Frontend Error Handling
- Detects timeout errors (HTTP 408)
- Provides user-friendly error messages
- Suggests performance improvements
- Shows timeout warnings with tips

## Performance Tips

When timeouts occur, the system suggests:

1. **Break down large DMN files** into smaller components
2. **Reduce the number of test cases** in batch operations
3. **Check network connectivity** for slow connections
4. **Use smaller DMN files** for testing complex scenarios

## Configuration Examples

### For Large DMN Files
```json
{
  "TimeoutSettings": {
    "DmnEvaluationTimeout": 30000,  // Increase to 30 seconds
    "BatchTestTimeout": 120000      // Increase to 2 minutes
  }
}
```

### For Development
```json
{
  "TimeoutSettings": {
    "RequestTimeout": 10000,        // Shorter timeouts for faster feedback
    "DmnEvaluationTimeout": 5000,
    "BatchTestTimeout": 30000
  }
}
```

## Monitoring

### Console Logging
The backend logs timeout events:
```
DMN evaluation timed out after 10000ms
Request timed out after 30000ms
```

### Frontend Monitoring
- Timeout errors are displayed in the UI
- Performance warnings provide actionable advice
- Loading states indicate ongoing operations

## Best Practices

1. **Start with smaller timeouts** and increase as needed
2. **Monitor timeout frequency** to identify performance issues
3. **Use batch operations sparingly** for large datasets
4. **Break complex DMN files** into smaller, focused decision tables
5. **Test with representative data sizes** during development 