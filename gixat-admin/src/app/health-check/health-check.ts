import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Apollo, gql } from 'apollo-angular';
import { Observable, Subscription, interval } from 'rxjs';
import { map, startWith } from 'rxjs/operators';

interface SystemMetrics {
  workingSetMB: number;
  privateMemoryMB: number;
  gcTotalMemoryMB: number;
  gcHeapSizeMB: number;
  gcFragmentedMB: number;
  gcGen0Collections: number;
  gcGen1Collections: number;
  gcGen2Collections: number;
  gcTotalCollections: number;
  threadCount: number;
  threadPoolWorkerThreads: string;
  threadPoolCompletionPortThreads: string;
  threadPoolMinWorkerThreads: number;
  threadPoolMaxWorkerThreads: number;
  cpuUsagePercent: number;
  processorCount: number;
  totalProcessorTime: string;
  dotNetVersion: string;
  osDescription: string;
  osArchitecture: string;
  processArchitecture: string;
  is64BitProcess: boolean;
  processId: number;
}

interface ComponentHealth {
  isHealthy: boolean;
  service: string;
  message: string;
  responseTime: number;
  details: any;
  checkedAt: string;
}

interface ComprehensiveHealth {
  isHealthy: boolean;
  version: string;
  environment: string;
  uptime: string;
  checkDuration: number;
  checkedAt: string;
  systemMetrics: SystemMetrics;
  redis: ComponentHealth;
  database: ComponentHealth;
  s3: ComponentHealth;
}

@Component({
  selector: 'app-health-check',
  imports: [CommonModule],
  templateUrl: './health-check.html',
  styleUrl: './health-check.css',
})
export class HealthCheckComponent implements OnInit, OnDestroy {
  healthData$: Observable<ComprehensiveHealth>;
  private subscription: Subscription = new Subscription();

  constructor(private apollo: Apollo) {
    // Auto-refresh every 30 seconds
    this.healthData$ = interval(30000).pipe(
      startWith(0),
      map(() => this.getHealthData())
    );
  }

  ngOnInit() {
    // Real GraphQL query implementation
    this.subscription.add(
      this.apollo.watchQuery<{ comprehensiveHealth: ComprehensiveHealth }>({
        query: gql`
          query GetComprehensiveHealth {
            comprehensiveHealth {
              isHealthy
              version
              environment
              uptime
              checkDuration
              checkedAt
              systemMetrics {
                workingSetMB
                privateMemoryMB
                gcTotalMemoryMB
                gcHeapSizeMB
                gcFragmentedMB
                gcGen0Collections
                gcGen1Collections
                gcGen2Collections
                gcTotalCollections
                threadCount
                threadPoolWorkerThreads
                threadPoolCompletionPortThreads
                threadPoolMinWorkerThreads
                threadPoolMaxWorkerThreads
                cpuUsagePercent
                processorCount
                totalProcessorTime
                dotNetVersion
                osDescription
                osArchitecture
                processArchitecture
                is64BitProcess
                processId
              }
              redis {
                isHealthy
                service
                message
                responseTime
                details
                checkedAt
              }
              database {
                isHealthy
                service
                message
                responseTime
                details
                checkedAt
              }
              s3 {
                isHealthy
                service
                message
                responseTime
                details
                checkedAt
              }
            }
          }
        `
      }).valueChanges.subscribe(result => {
        if (result.data?.comprehensiveHealth) {
          this.healthData$ = interval(30000).pipe(
            startWith(0),
            map(() => result.data!.comprehensiveHealth as ComprehensiveHealth)
          );
        }
      })
    );
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  private getHealthData(): ComprehensiveHealth {
    // This will be replaced by the GraphQL query result
    // For now, return a placeholder that will be updated by the subscription
    return {} as ComprehensiveHealth;
  }

  refreshHealth() {
    // Force refresh by re-executing the query
    this.apollo.client.refetchQueries({
      include: ['GetComprehensiveHealth']
    });
  }

  getHealthStatusColor(isHealthy: boolean): string {
    return isHealthy ? 'text-green-600' : 'text-red-600';
  }

  getHealthStatusBgColor(isHealthy: boolean): string {
    return isHealthy ? 'bg-green-100' : 'bg-red-100';
  }

  formatMemory(value: number): string {
    return value.toFixed(2) + ' MB';
  }

  formatCpu(value: number): string {
    return value.toFixed(2) + '%';
  }
}
