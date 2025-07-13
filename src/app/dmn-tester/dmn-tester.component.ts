import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService, GenerateTestRequest, EvaluateRequest, BatchTestRequest } from '../services/api.service';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

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

  generatePage: number = 1;
  generatePageSize: number = 100;
  generateTotal: number = 0;

  // DMN Viewer properties
  dmnFiles: any[] = [];
  selectedDmnFile: string = '';
  dmnFileContent: any = null;
  dmnPage: number = 1;
  dmnPageSize: number = 100;
  jumpToPage: number = 1;

  // File Analysis properties
  fileAnalysis: any = null;
  private analyzeSubject = new Subject<string>();

  // Decision-level properties
  decisionSummaries: any[] = [];
  selectedDecision: any = null;
  decisionContent: any = null;
  decisionEvaluateForm: FormGroup;
  
  // Chunking properties
  decisionChunks: any = null;
  selectedChunk: any = null;
  chunkSize: number = 50;
  currentChunkIndex: number = 0;
  chunkEvaluateForm: FormGroup;

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

  get generateTotalPages(): number {
    return Math.ceil(this.generateTotal / this.generatePageSize) || 1;
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

    this.decisionEvaluateForm = this.fb.group({
      inputs: ['{}', Validators.required]
    });

    this.chunkEvaluateForm = this.fb.group({
      inputs: ['{}', Validators.required]
    });
  }

  ngOnInit(): void {
    // Setup debounced file analysis
    this.analyzeSubject.pipe(
      debounceTime(500), // Đợi 500ms sau khi user ngừng nhập
      distinctUntilChanged() // Chỉ gọi khi giá trị thay đổi
    ).subscribe(fileName => {
      this.performFileAnalysis(fileName);
    });
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
      const request: any = {
        dmnFileName: this.generateForm.value.dmnFileName,
        page: this.generatePage,
        pageSize: this.generatePageSize
      };
      this.apiService.generateTest(request).subscribe({
        next: (result) => {
          this.generatedTests = result.data || [];
          this.generateTotal = result.total || 0;
          this.generatePage = result.page || 1;
          this.generatePageSize = result.pageSize || 100;
          if (this.generateTotal > 200) {
            this.timeoutWarning = `Kết quả có ${this.generateTotal} test case, bạn nên sử dụng phân trang hoặc tìm kiếm để xem chi tiết.`;
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

  nextGeneratePage() {
    if (this.generatePage * this.generatePageSize < this.generateTotal) {
      this.generatePage++;
      this.generateTests();
    }
  }

  prevGeneratePage() {
    if (this.generatePage > 1) {
      this.generatePage--;
      this.generateTests();
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

  // DMN Viewer methods
  onTabChange(event: any): void {
    if (event.index === 2) { // DMN Viewer tab
      this.loadDmnFiles();
    }
  }

  loadDmnFiles(): void {
    this.loading = true;
    this.error = '';
    this.apiService.listDmnFiles().subscribe({
      next: (result) => {
        this.dmnFiles = result.files || [];
        this.loading = false;
      },
      error: (error) => {
        this.handleError(error, 'loading DMN files');
      }
    });
  }

  onFileSelect(): void {
    if (this.selectedDmnFile) {
      this.dmnPage = 1;
      this.loadDmnFileContent();
    }
  }

  loadDmnFileContent(): void {
    if (!this.selectedDmnFile) return;
    
    this.loading = true;
    this.error = '';
    this.apiService.readDmnFile(this.selectedDmnFile, this.dmnPage, this.dmnPageSize).subscribe({
      next: (result) => {
        this.dmnFileContent = result;
        this.loading = false;
      },
      error: (error) => {
        this.handleError(error, 'reading DMN file');
      }
    });
  }

  prevDmnPage(): void {
    if (this.dmnFileContent && this.dmnFileContent.hasPreviousPage) {
      this.dmnPage--;
      this.loadDmnFileContent();
    }
  }

  nextDmnPage(): void {
    if (this.dmnFileContent && this.dmnFileContent.hasNextPage) {
      this.dmnPage++;
      this.loadDmnFileContent();
    }
  }

  onPageSizeChange(): void {
    this.dmnPage = 1;
    this.loadDmnFileContent();
  }

  goToFirstPage(): void {
    this.dmnPage = 1;
    this.loadDmnFileContent();
  }

  goToLastPage(): void {
    if (this.dmnFileContent) {
      this.dmnPage = this.dmnFileContent.totalPages;
      this.loadDmnFileContent();
    }
  }

  jumpToPageNumber(): void {
    if (this.dmnFileContent && this.jumpToPage >= 1 && this.jumpToPage <= this.dmnFileContent.totalPages) {
      this.dmnPage = this.jumpToPage;
      this.loadDmnFileContent();
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (!this.dmnFileContent) return;
    
    switch (event.key) {
      case 'ArrowLeft':
        if (this.dmnFileContent.hasPreviousPage) {
          this.prevDmnPage();
        }
        break;
      case 'ArrowRight':
        if (this.dmnFileContent.hasNextPage) {
          this.nextDmnPage();
        }
        break;
      case 'Home':
        if (this.dmnFileContent.hasPreviousPage) {
          this.goToFirstPage();
        }
        break;
      case 'End':
        if (this.dmnFileContent.hasNextPage) {
          this.goToLastPage();
        }
        break;
    }
  }

  highlightXml(line: string): string {
    // Simple XML highlighting
    return line
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/&lt;(\/?[^>]+?)&gt;/g, '<span class="xml-tag">&lt;$1&gt;</span>')
      .replace(/(\w+)=["'][^"']*["']/g, '<span class="xml-attr">$1</span>');
  }

  // File Analysis methods
  analyzeFile(): void {
    const fileName = this.generateForm.value.dmnFileName;
    if (!fileName) {
      this.fileAnalysis = null;
      return;
    }
    
    // Gửi tên file vào subject để debounce
    this.analyzeSubject.next(fileName);
  }

  private performFileAnalysis(fileName: string): void {
    this.loading = true;
    this.error = '';
    this.apiService.analyzeDmnFile(fileName).subscribe({
      next: (result) => {
        this.fileAnalysis = result;
        this.loading = false;
        
        // Hiển thị cảnh báo nếu file quá lớn
        if (result.complexity === 'High') {
          this.timeoutWarning = `File này có ${result.ruleCount} rules và ước tính sẽ tạo ra ${result.estimatedTestCases} test cases. Có thể gây timeout.`;
        } else if (result.complexity === 'Medium') {
          this.timeoutWarning = `File này có ${result.ruleCount} rules. Có thể mất thời gian để xử lý.`;
        } else {
          this.timeoutWarning = '';
        }
      },
      error: (error) => {
        this.fileAnalysis = null;
        this.loading = false;
        // Không hiển thị error cho việc phân tích file
        console.log('File analysis failed:', error);
      }
    });
  }

  // Decision-level methods
  loadDecisionSummaries(): void {
    if (!this.selectedDmnFile) return;
    
    this.loading = true;
    this.apiService.getDecisionSummaries(this.selectedDmnFile).subscribe({
      next: (result) => {
        this.decisionSummaries = result.decisions || [];
        this.loading = false;
      },
      error: (error) => {
        this.handleError(error, 'loading decision summaries');
      }
    });
  }

  selectDecision(decision: any): void {
    this.selectedDecision = decision;
    this.loadDecisionContent();
  }

  loadDecisionContent(): void {
    if (!this.selectedDmnFile || !this.selectedDecision) return;
    
    this.loading = true;
    this.apiService.extractDecision(this.selectedDmnFile, this.selectedDecision.id).subscribe({
      next: (result) => {
        this.decisionContent = result;
        this.loading = false;
      },
      error: (error) => {
        this.handleError(error, 'loading decision content');
      }
    });
  }

  evaluateSelectedDecision(): void {
    if (!this.decisionEvaluateForm.valid || !this.selectedDmnFile || !this.selectedDecision) return;
    
    this.loading = true;
    this.error = '';
    this.timeoutWarning = '';
    
    try {
      const inputs = JSON.parse(this.decisionEvaluateForm.value.inputs);
      const request = {
        fileName: this.selectedDmnFile,
        decisionId: this.selectedDecision.id,
        inputs: inputs
      };
      
      this.apiService.evaluateDecision(request).subscribe({
        next: (result) => {
          this.evaluationResult = result;
          this.loading = false;
        },
        error: (error) => {
          this.handleError(error, 'evaluating decision');
        }
      });
    } catch (error) {
      this.handleError(error, 'parsing inputs');
    }
  }

  getDecisionSizeClass(decision: any): string {
    if (decision.isLarge) return 'text-danger';
    if (decision.ruleCount > 100) return 'text-warning';
    return 'text-success';
  }

  formatDecisionSize(size: number): string {
    if (size >= 1024 * 1024) {
      return `${(size / (1024 * 1024)).toFixed(1)} MB`;
    } else if (size >= 1024) {
      return `${(size / 1024).toFixed(1)} KB`;
    }
    return `${size} B`;
  }

  // Chunking methods
  loadDecisionChunks(): void {
    if (!this.selectedDmnFile || !this.selectedDecision) return;
    
    this.loading = true;
    this.apiService.getDecisionChunks(this.selectedDmnFile, this.selectedDecision.id, this.chunkSize).subscribe({
      next: (result) => {
        this.decisionChunks = result;
        this.currentChunkIndex = 0;
        this.selectChunk(0);
        this.loading = false;
      },
      error: (error) => {
        this.handleError(error, 'loading decision chunks');
      }
    });
  }

  selectChunk(chunkIndex: number): void {
    if (!this.decisionChunks || chunkIndex < 0 || chunkIndex >= this.decisionChunks.totalChunks) return;
    
    this.currentChunkIndex = chunkIndex;
    this.selectedChunk = this.decisionChunks.chunks[chunkIndex];
  }

  nextChunk(): void {
    if (this.decisionChunks && this.currentChunkIndex < this.decisionChunks.totalChunks - 1) {
      this.selectChunk(this.currentChunkIndex + 1);
    }
  }

  prevChunk(): void {
    if (this.currentChunkIndex > 0) {
      this.selectChunk(this.currentChunkIndex - 1);
    }
  }

  onChunkSizeChange(): void {
    if (this.selectedDecision) {
      this.loadDecisionChunks();
    }
  }

  evaluateSelectedChunk(): void {
    if (!this.chunkEvaluateForm.valid || !this.selectedDmnFile || !this.selectedDecision || !this.selectedChunk) return;
    
    this.loading = true;
    this.error = '';
    this.timeoutWarning = '';
    
    try {
      const inputs = JSON.parse(this.chunkEvaluateForm.value.inputs);
      const request = {
        fileName: this.selectedDmnFile,
        decisionId: this.selectedDecision.id,
        chunkIndex: this.currentChunkIndex,
        chunkSize: this.chunkSize,
        inputs: inputs
      };
      
      this.apiService.evaluateDecisionChunk(request).subscribe({
        next: (result) => {
          this.evaluationResult = result;
          this.loading = false;
        },
        error: (error) => {
          this.handleError(error, 'evaluating decision chunk');
        }
      });
    } catch (error) {
      this.handleError(error, 'parsing inputs');
    }
  }

  getChunkProgress(): number {
    if (!this.decisionChunks) return 0;
    return ((this.currentChunkIndex + 1) / this.decisionChunks.totalChunks) * 100;
  }

  getChunkStatusClass(chunk: any): string {
    if (chunk.isLastChunk) return 'text-success';
    return 'text-primary';
  }
} 