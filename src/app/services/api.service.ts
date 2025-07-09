import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, timeout, catchError, of, TimeoutError } from 'rxjs';
import { retry, delay } from 'rxjs/operators';

export interface GenerateTestRequest {
  dmnFileName: string;
}

export interface EvaluateRequest {
  dmnPath: string;
  decisionId: string;
  inputs: { [key: string]: any };
}

export interface BatchTestRequest {
  items: BatchTestItem[];
}

export interface BatchTestItem {
  dmnFileName: string;
  decisionId: string;
  testCases: TestCase[];
}

export interface TestCase {
  name?: string;
  inputs: { [key: string]: any };
  expectedOutputs: { [key: string]: any };
}

export interface TestResult {
  name: string;
  pass: boolean;
  actualOutputs: { [key: string]: any };
  expectedOutputs: { [key: string]: any };
  diff: { [key: string]: any };
}

export interface TimeoutConfig {
  requestTimeout: number;
  evaluationTimeout: number;
  batchTimeout: number;
  retryAttempts: number;
  retryDelay: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'https://localhost:5002/api/dmn';
  
  // Default timeout configuration
  private timeoutConfig: TimeoutConfig = {
    requestTimeout: 30000,      // 30 seconds for general requests
    evaluationTimeout: 15000,    // 15 seconds for DMN evaluation
    batchTimeout: 120000,       // 2 minutes for batch operations
    retryAttempts: 2,           // Retry failed requests
    retryDelay: 1000            // 1 second delay between retries
  };

  constructor(private http: HttpClient) { }

  // Method to update timeout configuration
  updateTimeoutConfig(config: Partial<TimeoutConfig>) {
    this.timeoutConfig = { ...this.timeoutConfig, ...config };
  }

  private handleError(error: any, operation: string = 'API call') {
    let errorMessage = 'An error occurred';
    let isTimeout = false;
    
    if (error.status === 408 || error instanceof TimeoutError || (error.message && error.message.toLowerCase().includes('timeout'))) {
      errorMessage = `Request timed out during ${operation}. The operation may be too complex or the server is busy. Please try again with a smaller dataset.`;
      isTimeout = true;
    } else if (error.status === 0) {
      errorMessage = 'Network error. Please check your connection and try again.';
    } else if (error.status === 500) {
      errorMessage = 'Server error. Please try again later.';
    } else if (error.status === 404) {
      errorMessage = 'Resource not found. Please check the file name and try again.';
    } else if (error.error && error.error.error) {
      errorMessage = error.error.error;
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    console.error(`${operation} failed:`, error);
    
    return throwError(() => ({
      message: errorMessage,
      isTimeout: isTimeout,
      originalError: error
    }));
  }

  private createRequestWithTimeout<T>(url: string, data: any, timeoutMs: number, operation: string): Observable<T> {
    return this.http.post<T>(url, data)
      .pipe(
        timeout(timeoutMs),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying ${operation} (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, operation))
      );
  }

  generateTest(request: GenerateTestRequest): Observable<any> {
    console.log(`Generating test cases for ${request.dmnFileName} with timeout: ${this.timeoutConfig.requestTimeout}ms`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/generate-test`, 
      request, 
      this.timeoutConfig.requestTimeout,
      'test generation'
    );
  }

  evaluate(request: EvaluateRequest): Observable<any> {
    console.log(`Evaluating decision ${request.decisionId} with timeout: ${this.timeoutConfig.evaluationTimeout}ms`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/evaluate`, 
      request, 
      this.timeoutConfig.evaluationTimeout,
      'DMN evaluation'
    );
  }

  batchTest(request: BatchTestRequest): Observable<any> {
    const totalTestCases = request.items.reduce((sum, item) => sum + item.testCases.length, 0);
    console.log(`Running batch test with ${totalTestCases} test cases, timeout: ${this.timeoutConfig.batchTimeout}ms`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/batch-test`, 
      request, 
      this.timeoutConfig.batchTimeout,
      'batch testing'
    );
  }

  convertGenerateToBatch(request: any): Observable<any> {
    console.log(`Converting generate to batch with timeout: ${this.timeoutConfig.requestTimeout}ms`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/convert-generate-to-batch`, 
      request, 
      this.timeoutConfig.requestTimeout,
      'convert to batch'
    );
  }

  // Method to get current timeout configuration
  getTimeoutConfig(): TimeoutConfig {
    return { ...this.timeoutConfig };
  }

  // Method to check if operation might timeout based on file size
  estimateTimeoutRisk(dmnFileName: string): 'low' | 'medium' | 'high' {
    // Simple heuristic based on file name patterns
    if (dmnFileName.includes('tem-code') || dmnFileName.includes('large')) {
      return 'high';
    }
    if (dmnFileName.includes('test') || dmnFileName.includes('demo')) {
      return 'medium';
    }
    return 'low';
  }
} 