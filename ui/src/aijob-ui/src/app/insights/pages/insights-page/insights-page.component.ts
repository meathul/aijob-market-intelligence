import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CareerChatApiService } from '../../../services/career-chat-api.service';

type ChatMessage = {
  role: 'user' | 'bot';
  text: string;
};

@Component({
  selector: 'app-insights-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './insights-page.component.html',
  styleUrl: './insights-page.component.scss'
})
export class InsightsPageComponent {
  private readonly careerChatApi = inject(CareerChatApiService);

  readonly draft = signal('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly messages = signal<ChatMessage[]>([
    {
      role: 'bot',
      text: 'Hi, I am Career Bot. Ask me about resumes, interviews, salary negotiation, career paths, or which skills to build next.'
    }
  ]);

  ask() {
    const question = this.draft().trim();
    if (!question || this.loading()) return;

    this.messages.update((prev) => [...prev, { role: 'user', text: question }]);
    this.draft.set('');
    this.loading.set(true);
    this.error.set(null);

    this.careerChatApi.ask({ message: question }).subscribe({
      next: (res) => {
        this.messages.update((prev) => [
          ...prev,
          { role: 'bot', text: res.answer || 'I could not generate an answer just now.' }
        ]);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set('Career Bot could not answer right now. Please try again.');
        // eslint-disable-next-line no-console
        console.error(e);
      }
    });
  }
}
