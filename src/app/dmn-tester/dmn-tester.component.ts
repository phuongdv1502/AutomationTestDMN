import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService, GenerateTestRequest, EvaluateRequest, BatchTestRequest } from '../services/api.service';

@Component({
  selector: 'app-dmn-tester',
  templateUrl: './dmn-tester.component.html',
  styleUrls: ['./dmn-tester.component.scss']
})
export class DmnTesterComponent implements OnInit {
  generateForm: FormGroup;
  evaluateForm: FormGroup;
  batchForm: FormGroup;
  
  generatedTests: any[] = [];
  evaluationResult: any = null;
  batchResults: any[] = [];
  loading = false;
  error = '';
  timeoutWarning = '';
  batchPage: number = 1;
  batchPageSize: number = 20;

  // Phân trang cho từng nhóm test case trong batchResults
  batchTestPages: { [decisionId: string]: number } = {};
  batchTestPageSize: number = 20;

  testPage: number = 0;
  testPageSize: number = 20;

  pagedGeneratedTests() {
    const start = this.testPage * this.testPageSize;
    return this.generatedTests.slice(start, start + this.testPageSize);
  }

  onTestPageChange(event: any) {
    this.testPage = event.pageIndex;
    this.testPageSize = event.pageSize;
  }

  pagedBatchResults() {
    const start = (this.batchPage - 1) * this.batchPageSize;
    return this.batchResults.slice(start, start + this.batchPageSize);
  }

  totalBatchPages() {
    return Math.ceil(this.batchResults.length / this.batchPageSize);
  }

  nextBatchPage() {
    if (this.batchPage < this.totalBatchPages()) this.batchPage++;
  }

  prevBatchPage() {
    if (this.batchPage > 1) this.batchPage--;
  }

  pagedTestResults(result: any) {
    const page = this.batchTestPages[result.decisionId] || 1;
    const start = (page - 1) * this.batchTestPageSize;
    return result.results.slice(start, start + this.batchTestPageSize);
  }

  totalTestPages(result: any) {
    return Math.ceil(result.results.length / this.batchTestPageSize);
  }

  nextTestPage(result: any) {
    const page = this.batchTestPages[result.decisionId] || 1;
    if (page < this.totalTestPages(result)) {
      this.batchTestPages[result.decisionId] = page + 1;
    }
  }

  prevTestPage(result: any) {
    const page = this.batchTestPages[result.decisionId] || 1;
    if (page > 1) {
      this.batchTestPages[result.decisionId] = page - 1;
    }
  }

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService
  ) {
    this.generateForm = this.fb.group({
      dmnFileName: ['dinnerDecisions.dmn', Validators.required]
    });

    this.evaluateForm = this.fb.group({
      dmnPath: ['dinnerDecisions.dmn', Validators.required],
      decisionId: ['beverages', Validators.required],
      inputs: ['{"desiredDish": "Stew", "guestsWithChildren": "true"}', Validators.required]
    });

    this.batchForm = this.fb.group({
      batchData: ['', Validators.required]
    });
  }

  ngOnInit(): void {
  }

  private handleError(error: any, operation: string): void {
    this.loading = false;
    
    if (error.message && error.message.includes('timed out')) {
      this.error = `${operation} timed out. This might be due to a large DMN file or complex rules. Try with a smaller file or fewer test cases.`;
      this.timeoutWarning = 'Consider breaking down large DMN files into smaller components for better performance.';
    } else {
      this.error = `Error ${operation.toLowerCase()}: ${error.message}`;
      this.timeoutWarning = '';
    }
  }

  generateTests(): void {
    if (this.generateForm.valid) {
      this.loading = true;
      this.error = '';
      this.timeoutWarning = '';
      
      const request: GenerateTestRequest = {
        dmnFileName: this.generateForm.value.dmnFileName
      };

      this.apiService.generateTest(request).subscribe({
        next: (result) => {
          this.generatedTests = result;
          if (result.length > 200) {
            this.timeoutWarning = `Kết quả có ${result.length} test case, bạn nên sử dụng phân trang hoặc tìm kiếm để xem chi tiết.`;
          } else {
            this.timeoutWarning = '';
          }
          this.loading = false;
        },
        error: (error) => {
          this.handleError(error, 'generating tests');
        }
      });
    }
  }

  evaluateDecision(): void {
    if (this.evaluateForm.valid) {
      this.loading = true;
      this.error = '';
      this.timeoutWarning = '';
      
      try {
        const inputs = JSON.parse(this.evaluateForm.value.inputs);
        const request: EvaluateRequest = {
          dmnPath: this.evaluateForm.value.dmnPath,
          decisionId: this.evaluateForm.value.decisionId,
          inputs: inputs
        };

        this.apiService.evaluate(request).subscribe({
          next: (result) => {
            this.evaluationResult = result;
            this.loading = false;
            this.timeoutWarning = '';
          },
          error: (error) => {
            this.handleError(error, 'evaluating decision');
          }
        });
      } catch (e) {
        this.error = 'Invalid JSON in inputs field';
        this.loading = false;
      }
    }
  }

  runBatchTest(): void {
    if (this.batchForm.valid) {
      this.loading = true;
      this.error = '';
      this.timeoutWarning = '';
      
      try {
        const batchData = JSON.parse(this.batchForm.value.batchData);
        const request: BatchTestRequest = batchData;

        this.apiService.batchTest(request).subscribe({
          next: (result) => {
            this.batchResults = result;
            this.batchPage = 1;
            // Reset phân trang cho từng nhóm
            this.batchTestPages = {};
            for (const group of result) {
              this.batchTestPages[group.decisionId] = 1;
            }
            this.loading = false;
            this.timeoutWarning = '';
          },
          error: (error) => {
            this.handleError(error, 'running batch test');
          }
        });
      } catch (e) {
        this.error = 'Invalid JSON in batch data field';
        this.loading = false;
      }
    }
  }

  convertToBatch(): void {
    if (this.generatedTests.length > 0) {
      this.loading = true;
      this.error = '';
      this.timeoutWarning = '';
      
      const request = {
        items: this.generatedTests
      };

      this.apiService.convertGenerateToBatch(request).subscribe({
        next: (result) => {
          this.batchForm.patchValue({
            batchData: JSON.stringify(result, null, 2)
          });
          this.loading = false;
          this.timeoutWarning = '';
        },
        error: (error) => {
          this.handleError(error, 'converting to batch');
        }
      });
    }
  }

  getPassCount(results: any[]): number {
    return results.filter(r => r.pass).length;
  }

  getFailCount(results: any[]): number {
    return results.filter(r => !r.pass).length;
  }

  getDiffKeys(diff: any): string[] {
    return Object.keys(diff || {});
  }

  clearError(): void {
    this.error = '';
    this.timeoutWarning = '';
  }
} 