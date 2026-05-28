import { Component, computed, inject, signal } from '@angular/core';

import { toSignal } from '@angular/core/rxjs-interop';

import { SkillsApiService } from '../../../services/skills-api.service';

type SortMode = 'name' | 'count';

@Component({
  selector: 'app-skills-page',
  standalone: true,
  imports: [],
  templateUrl: './skills-page.component.html',
  styleUrl: './skills-page.component.scss'
})
export class SkillsPageComponent {
  private readonly skillsApi = inject(SkillsApiService);

  readonly query = signal('');
  readonly sortMode = signal<SortMode>('count');

  // Backend can return empty for /top if it hasn't been populated yet.
  readonly topSkills = toSignal(this.skillsApi.top(100), { initialValue: [] });
  readonly skills = toSignal(this.skillsApi.list(), { initialValue: [] });

  readonly hasTop = computed(() => this.topSkills().length > 0);

  readonly filteredTop = computed(() => {
    const q = this.query().trim().toLowerCase();
    const rows = this.topSkills();

    const filtered = q
      ? rows.filter((r) => (r.skill ?? '').toLowerCase().includes(q))
      : rows;

    return [...filtered].sort((a, b) => {
      if (this.sortMode() === 'name') {
        return (a.skill ?? '').localeCompare(b.skill ?? '');
      }
      return (b.count ?? 0) - (a.count ?? 0);
    });
  });

  readonly filteredSkills = computed(() => {
    const q = this.query().trim().toLowerCase();
    const rows = this.skills();
    const filtered = q ? rows.filter((s) => s.toLowerCase().includes(q)) : rows;
    return [...filtered].sort((a, b) => a.localeCompare(b));
  });

  readonly totalSkills = computed(() =>
    this.hasTop() ? this.filteredTop().length : this.filteredSkills().length
  );

  readonly totalDemand = computed(() =>
    this.topSkills().reduce((sum, r) => sum + (r.count ?? 0), 0)
  );

  demandShare(count: number) {
    const total = this.totalDemand();
    if (!total) return '—';
    return `${Math.round((count / total) * 1000) / 10}%`;
  }
}
