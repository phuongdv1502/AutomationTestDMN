import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, timeout, catchError, of, TimeoutError } from 'rxjs';
import { retry, delay } from 'rxjs/operators';
import { environment } from '../../environments/environment';

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
  private baseUrl = environment.apiUrl;
  
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
      `${this.baseUrl}/dmn/generate-test`, 
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
      `${this.baseUrl}/dmn/batch-test`, 
      request, 
      this.timeoutConfig.batchTimeout,
      'batch testing'
    );
  }

  convertGenerateToBatch(request: any): Observable<any> {
    console.log(`Converting generate to batch with timeout: ${this.timeoutConfig.requestTimeout}ms`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/dmn/convert-generate-to-batch`, 
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

  // DMN File Viewer methods
  listDmnFiles(): Observable<any> {
    console.log('Loading DMN files list');
    
    return this.http.get<any>(`${this.baseUrl}/dmn/list-files`)
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying list files (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'loading DMN files'))
      );
  }

  readDmnFile(fileName: string, page: number = 1, pageSize: number = 100): Observable<any> {
    console.log(`Reading DMN file ${fileName}, page ${page}, pageSize ${pageSize}`);
    
    const params = {
      fileName: fileName,
      page: page.toString(),
      pageSize: pageSize.toString()
    };
    
    return this.http.get<any>(`${this.baseUrl}/dmn/read-file`, { params })
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying read file (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'reading DMN file'))
      );
  }

  analyzeDmnFile(fileName: string): Observable<any> {
    console.log(`Analyzing DMN file ${fileName}`);
    
    const params = { fileName: fileName };
    
    return this.http.get<any>(`${this.baseUrl}/dmn/analyze-dmn`, { params })
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying analyze file (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'analyzing DMN file'))
      );
  }

  // Decision-level methods
  getDecisionSummaries(fileName: string): Observable<any> {
    console.log(`Getting decision summaries for ${fileName}`);
    
    const params = { fileName: fileName };
    
    return this.http.get<any>(`${this.baseUrl}/dmn/decision-summaries`, { params })
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying get decision summaries (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'getting decision summaries'))
      );
  }

  extractDecision(fileName: string, decisionId: string): Observable<any> {
    console.log(`Extracting decision ${decisionId} from ${fileName}`);
    
    const params = { 
      fileName: fileName,
      decisionId: decisionId
    };
    
    return this.http.get<any>(`${this.baseUrl}/dmn/extract-decision`, { params })
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying extract decision (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'extracting decision'))
      );
  }

  evaluateDecision(request: any): Observable<any> {
    console.log(`Evaluating decision ${request.decisionId} from ${request.fileName}`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/dmn/evaluate-decision`, 
      request, 
      this.timeoutConfig.evaluationTimeout,
      'decision evaluation'
    );
  }

  // Decision chunking methods
  getDecisionChunks(fileName: string, decisionId: string, chunkSize: number = 50): Observable<any> {
    console.log(`Getting decision chunks for ${decisionId} from ${fileName}, chunk size: ${chunkSize}`);
    
    const params = { 
      fileName: fileName,
      decisionId: decisionId,
      chunkSize: chunkSize.toString()
    };
    
    return this.http.get<any>(`${this.baseUrl}/dmn/decision-chunks`, { params })
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying get decision chunks (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'getting decision chunks'))
      );
  }

  getDecisionChunk(fileName: string, decisionId: string, chunkIndex: number, chunkSize: number = 50): Observable<any> {
    console.log(`Getting decision chunk ${chunkIndex} for ${decisionId} from ${fileName}`);
    
    const params = { 
      fileName: fileName,
      decisionId: decisionId,
      chunkIndex: chunkIndex.toString(),
      chunkSize: chunkSize.toString()
    };
    
    return this.http.get<any>(`${this.baseUrl}/dmn/decision-chunk`, { params })
      .pipe(
        timeout(this.timeoutConfig.requestTimeout),
        retry({
          count: this.timeoutConfig.retryAttempts,
          delay: (error, retryCount) => {
            console.log(`Retrying get decision chunk (attempt ${retryCount + 1}/${this.timeoutConfig.retryAttempts + 1})`);
            return of(null).pipe(delay(this.timeoutConfig.retryDelay));
          }
        }),
        catchError((error) => this.handleError(error, 'getting decision chunk'))
      );
  }

  evaluateDecisionChunk(request: any): Observable<any> {
    console.log(`Evaluating decision chunk ${request.chunkIndex} for ${request.decisionId} from ${request.fileName}`);
    
    return this.createRequestWithTimeout(
      `${this.baseUrl}/dmn/evaluate-chunk`, 
      request, 
      this.timeoutConfig.evaluationTimeout,
      'decision chunk evaluation'
    );
  }
} 