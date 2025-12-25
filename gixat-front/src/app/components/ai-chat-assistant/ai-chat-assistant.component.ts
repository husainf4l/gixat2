import { Component, signal, effect, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Message {
  role: 'user' | 'bot';
  text: string;
}

@Component({
  selector: 'app-ai-chat-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat-assistant.component.html',
  styleUrls: ['./ai-chat-assistant.component.css']
})
export class AiChatAssistantComponent {
  @ViewChild('scrollContainer') scrollContainer?: ElementRef<HTMLDivElement>;

  isOpen = signal(false);
  messages = signal<Message[]>([
    { role: 'bot', text: 'Hello! I am Gixat AI. How can I assist with your workshop operations or car diagnostics today?' }
  ]);
  input = signal('');
  isTyping = signal(false);

  constructor() {
    // Auto-scroll when messages change
    effect(() => {
      this.messages();
      this.isTyping();
      setTimeout(() => this.scrollToBottom(), 100);
    });
  }

  toggleChat() {
    this.isOpen.set(!this.isOpen());
  }

  scrollToBottom() {
    if (this.scrollContainer) {
      const element = this.scrollContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }

  async handleSendMessage() {
    const userInput = this.input().trim();
    if (!userInput) return;

    // Add user message
    this.messages.update(msgs => [...msgs, { role: 'user', text: userInput }]);
    this.input.set('');
    this.isTyping.set(true);

    try {
      // TODO: Replace with FastAPI LangGraph endpoint integration
      // For now, simulate AI response
      await this.simulateAIResponse(userInput);
    } catch (error) {
      console.error('AI Error:', error);
      this.messages.update(msgs => [
        ...msgs,
        { role: 'bot', text: "I'm having trouble connecting to the network. Please try again later." }
      ]);
    } finally {
      this.isTyping.set(false);
    }
  }

  private async simulateAIResponse(userInput: string): Promise<void> {
    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Sample responses based on keywords
    let response = "I understand you're asking about that. Let me help you with automotive diagnostics and workshop management.";

    if (userInput.toLowerCase().includes('p0') || userInput.toLowerCase().includes('code')) {
      response = "For OBD-II diagnostic codes:\n\n• P0XXX codes are powertrain related\n• Check the specific code in your scanner\n• Common causes include sensor failures or electrical issues\n• Always verify with a proper diagnostic scan\n\nWould you like me to help with a specific fault code?";
    } else if (userInput.toLowerCase().includes('oil') || userInput.toLowerCase().includes('change')) {
      response = "For oil change service:\n\n• Standard labor time: 0.5-1 hour\n• Required items: Oil filter, engine oil (check specification)\n• Don't forget to reset the oil life indicator\n• Document the mileage in the job card\n\nWhat vehicle make and model are you working on?";
    } else if (userInput.toLowerCase().includes('brake')) {
      response = "Brake service considerations:\n\n• Inspect pads, rotors, and calipers\n• Check brake fluid level and condition\n• Standard pad replacement: 1-2 hours labor\n• Always test drive after brake work\n• Document brake pad thickness measurements\n\nIs this for front or rear brakes?";
    } else if (userInput.toLowerCase().includes('inventory') || userInput.toLowerCase().includes('parts')) {
      response = "For inventory management:\n\n• Keep fast-moving items well-stocked (filters, fluids, brake pads)\n• Set reorder points for critical parts\n• Track part usage by job card\n• Consider vehicle-specific common parts\n\nWhat parts are you looking to stock?";
    }

    this.messages.update(msgs => [...msgs, { role: 'bot', text: response }]);
  }

  handleKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.handleSendMessage();
    }
  }
}
