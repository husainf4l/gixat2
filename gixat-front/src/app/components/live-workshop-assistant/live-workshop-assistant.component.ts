import { Component, Input, Output, EventEmitter, signal, effect, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

interface AudioConfig {
  sampleRate: number;
  channels: number;
}

@Component({
  selector: 'app-live-workshop-assistant',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './live-workshop-assistant.component.html',
  styleUrls: ['./live-workshop-assistant.component.css']
})
export class LiveWorkshopAssistantComponent implements OnDestroy {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  connected = signal<boolean>(false);
  isListening = signal<boolean>(false);
  transcription = signal<string>('');
  botTranscription = signal<string>('');
  connectionStatus = signal<string>('Connecting to Gixat Live...');

  private audioContext: AudioContext | null = null;
  private outputAudioContext: AudioContext | null = null;
  private mediaStream: MediaStream | null = null;
  private scriptProcessor: ScriptProcessorNode | null = null;
  private nextStartTime = 0;
  private audioSources = new Set<AudioBufferSourceNode>();

  constructor() {
    // Watch for isOpen changes
    effect(() => {
      if (this.isOpen) {
        this.startSession();
      } else {
        this.stopSession();
      }
    });
  }

  ngOnDestroy(): void {
    this.stopSession();
  }

  onClose(): void {
    this.close.emit();
  }

  private async startSession(): Promise<void> {
    try {
      this.connectionStatus.set('Connecting to Gixat Live...');
      
      // Initialize audio contexts
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)({ sampleRate: 16000 });
      this.outputAudioContext = new (window.AudioContext || (window as any).webkitAudioContext)({ sampleRate: 24000 });

      // Request microphone access
      this.mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      
      // Simulate connection (replace with actual Google GenAI Live API later)
      await this.simulateConnection();
      
      this.connected.set(true);
      this.isListening.set(true);
      this.connectionStatus.set('Listening for Workshop Commands...');

      // Set up audio processing
      this.setupAudioProcessing();
      
    } catch (err) {
      console.error('Failed to start Live session:', err);
      this.connectionStatus.set('Connection Failed - Check Permissions');
      this.connected.set(false);
    }
  }

  private async simulateConnection(): Promise<void> {
    // Simulate connection delay
    await new Promise(resolve => setTimeout(resolve, 1500));
    
    // Start simulated responses
    this.startSimulatedResponses();
  }

  private setupAudioProcessing(): void {
    if (!this.audioContext || !this.mediaStream) return;

    const source = this.audioContext.createMediaStreamSource(this.mediaStream);
    this.scriptProcessor = this.audioContext.createScriptProcessor(4096, 1, 1);
    
    this.scriptProcessor.onaudioprocess = (e) => {
      const inputData = e.inputBuffer.getChannelData(0);
      // Convert to Int16 PCM
      const int16 = new Int16Array(inputData.length);
      for (let i = 0; i < inputData.length; i++) {
        int16[i] = inputData[i] * 32768;
      }
      
      // In production, send this to Google GenAI Live API
      // For now, just detect audio levels for visual feedback
      const volume = this.calculateVolume(inputData);
      if (volume > 0.02) {
        this.isListening.set(true);
      }
    };

    source.connect(this.scriptProcessor);
    this.scriptProcessor.connect(this.audioContext.destination);
  }

  private calculateVolume(buffer: Float32Array): number {
    let sum = 0;
    for (let i = 0; i < buffer.length; i++) {
      sum += buffer[i] * buffer[i];
    }
    return Math.sqrt(sum / buffer.length);
  }

  private startSimulatedResponses(): void {
    // Simulate mechanic speech detection
    setTimeout(() => {
      this.transcription.set("Check the oil pressure sensor on this Camry");
      setTimeout(() => {
        this.transcription.set('');
        this.botTranscription.set("Oil pressure sensor is located on the engine block near the oil filter. Typical failure symptoms include erratic gauge readings or low oil pressure warning light. Recommended replacement part: OEM 83530-12040 or equivalent. Torque spec: 15 ft-lbs.");
        
        setTimeout(() => {
          this.botTranscription.set('');
        }, 8000);
      }, 2000);
    }, 5000);

    // Simulate another interaction
    setTimeout(() => {
      this.transcription.set("What's the brake fluid type for a 2019 F-150?");
      setTimeout(() => {
        this.transcription.set('');
        this.botTranscription.set("2019 Ford F-150 uses DOT 3 brake fluid. Capacity is approximately 1.5 quarts. Ford specifies Motorcraft PM-1-C or equivalent. Always check master cylinder cap for confirmation.");
        
        setTimeout(() => {
          this.botTranscription.set('');
        }, 7000);
      }, 2000);
    }, 18000);
  }

  private stopSession(): void {
    // Stop all audio sources
    this.audioSources.forEach(source => {
      try {
        source.stop();
      } catch (e) {
        // Source might already be stopped
      }
    });
    this.audioSources.clear();

    // Disconnect audio processing
    if (this.scriptProcessor) {
      this.scriptProcessor.disconnect();
      this.scriptProcessor = null;
    }

    // Stop media stream
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
      this.mediaStream = null;
    }

    // Close audio contexts
    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }

    if (this.outputAudioContext) {
      this.outputAudioContext.close();
      this.outputAudioContext = null;
    }

    // Reset state
    this.connected.set(false);
    this.isListening.set(false);
    this.transcription.set('');
    this.botTranscription.set('');
    this.nextStartTime = 0;
  }

  // Helper method for future Google GenAI integration
  private encode(bytes: Uint8Array): string {
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  private decode(base64: string): Uint8Array {
    const binaryString = atob(base64);
    const len = binaryString.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes;
  }

  private async decodeAudioData(
    data: Uint8Array,
    ctx: AudioContext,
    sampleRate: number,
    numChannels: number
  ): Promise<AudioBuffer> {
    const dataInt16 = new Int16Array(data.buffer);
    const frameCount = dataInt16.length / numChannels;
    const buffer = ctx.createBuffer(numChannels, frameCount, sampleRate);
    
    for (let channel = 0; channel < numChannels; channel++) {
      const channelData = buffer.getChannelData(channel);
      for (let i = 0; i < frameCount; i++) {
        channelData[i] = dataInt16[i * numChannels + channel] / 32768.0;
      }
    }
    
    return buffer;
  }
}
