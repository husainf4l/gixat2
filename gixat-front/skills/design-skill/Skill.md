---
name: gixat-design-system
description: Design system for Gixat garage management application. Creates clean, professional Angular components with Tailwind CSS, following established patterns for automotive service centers. Use when building new features, pages, or components for the Gixat application.
---

# Gixat Design System

A professional frontend design skill for building consistent, accessible interfaces in the Gixat garage management application using Angular and Tailwind CSS.

## When to Use This Skill

Activate this skill when users request:
- New pages or components for Gixat
- Consistent UI matching existing patterns
- Tables, lists, forms, or data displays
- Customer, vehicle, or job card interfaces
- Dashboard widgets and analytics views

**Trigger phrases:**
- "Create a new page..."
- "Add a component for..."
- "Build a table showing..."
- "Design a form for..."

---

## Core Design Principles

### The Four Pillars of Gixat Design

1. **Clarity & Purpose**
   - Every element serves the automotive service workflow
   - Clean information hierarchy
   - Easy scanning for busy mechanics and service advisors
   - Clear call-to-action buttons

2. **Professional & Trustworthy**
   - Clean, modern aesthetic builds customer confidence
   - Subtle colors and minimal visual noise
   - Data-first approach with clear labels
   - Professional typography and spacing

3. **Efficiency**
   - Quick access to key information
   - Minimal clicks to complete tasks
   - Smart search and filtering
   - Responsive interactions

4. **Consistency**
   - Unified design language across all pages
   - Predictable component behavior
   - Standardized spacing and colors
   - Reusable patterns

---

## Design System Specifications

### Typography System

**Font Stack:** System sans-serif (Tailwind default)

```css
/* Tailwind Default Sans-Serif */
font-family: ui-sans-serif, system-ui, sans-serif, "Apple Color Emoji", "Segoe UI Emoji";
```

**Font Sizes (Tailwind):**
```html
text-xs    → 12px   /* Labels, metadata */
text-sm    → 14px   /* Secondary text, table content */
text-base  → 16px   /* Body text, descriptions */
text-lg    → 18px   /* Section headings */
text-xl    → 20px   /* Card titles */
text-2xl   → 24px   /* Page subtitles */
text-3xl   → 30px   /* Page titles */
text-4xl   → 36px   /* Hero headings */
text-5xl   → 48px   /* Marketing headlines */
```

**Font Weights:**
```html
font-normal    → 400  /* Body text, descriptions */
font-medium    → 500  /* Labels, emphasized text */
font-semibold  → 600  /* Headings, important labels */
```

**Typography Rules:**
- Use `font-semibold` for headings and page titles
- Use `font-medium` for buttons, labels, and emphasized text
- Use `font-normal` for body text and descriptions
- Maintain readable line-height with `leading-relaxed` or `leading-normal`

### Color System

**Brand Colors:**
```html
Primary Blue:   #1b75bc  /* bg-[#1b75bc], text-[#1b75bc] */
Hover Blue:     #155a92  /* hover:bg-[#155a92] */
```

**Slate Scale (Primary Palette):**
```html
<!-- Text Colors -->
text-slate-900  → #0f172a  /* Primary text, headings */
text-slate-700  → #334155  /* Secondary text */
text-slate-600  → #475569  /* Table content, labels */
text-slate-500  → #64748b  /* Metadata, timestamps */
text-slate-400  → #94a3b8  /* Placeholders, disabled */

<!-- Background Colors -->
bg-white        → #ffffff  /* Cards, inputs, modals */
bg-slate-50     → #f8fafc  /* Page backgrounds */
bg-slate-100    → #f1f5f9  /* Subtle backgrounds, badges */

<!-- Border Colors -->
border-slate-200 → #e2e8f0  /* Default borders */
border-slate-300 → #cbd5e1  /* Emphasized borders */
```

**Semantic Colors:**
```html
<!-- Status Colors -->
bg-emerald-50   text-emerald-700  /* Success states */
bg-red-50       text-red-700      /* Error states */
bg-amber-50     text-amber-700    /* Warning states */
bg-blue-50      text-blue-700     /* Info states */
```

**Usage Guidelines:**
- Use `#1b75bc` for all primary actions and brand elements
- Use slate scale for neutrals (never pure black/white for text)
- White backgrounds for cards and content areas
- Slate-50 for page backgrounds
- Maintain consistent color usage across similar components

### Spacing System

**Tailwind Spacing Scale:**
```html
<!-- Padding/Margin -->
p-1  m-1   → 4px    /* Tight spacing */
p-2  m-2   → 8px    /* Small spacing */
p-3  m-3   → 12px   /* Compact spacing */
p-4  m-4   → 16px   /* Standard spacing */
p-6  m-6   → 24px   /* Large spacing */
p-8  m-8   → 32px   /* Extra large spacing */
p-12 m-12  → 48px   /* Section spacing */
p-16 m-16  → 64px   /* Page spacing */

<!-- Gap (for flex/grid) -->
gap-2  → 8px    /* Tight elements */
gap-3  → 12px   /* Related items */
gap-4  → 16px   /* Standard separation */
gap-6  → 24px   /* Section separation */
gap-8  → 32px   /* Large separation */
```

**Common Patterns:**
- Buttons: `px-3.5 py-2` or `px-4 py-2`
- Cards: `p-6` or `p-8`
- Page containers: `px-6 py-12` or `px-6 py-16`
- List items: `px-6 py-4`
- Form inputs: `px-4 py-2.5`

### Border Radius

```html
rounded-md   → 6px   /* Small elements, badges */
rounded-lg   → 8px   /* Buttons, inputs, small cards */
rounded-xl   → 12px  /* Cards, modals */
rounded-2xl  → 16px  /* Large cards, images */
rounded-3xl  → 24px  /* Hero images, special cards */
rounded-full → 9999px /* Pills, avatars, icon buttons */
```

**Usage:**
- Use `rounded-lg` as the default for most interactive elements
- Use `rounded-xl` for cards and containers
- Use `rounded-full` for pill-shaped buttons and badges

---

## Component Patterns

### Buttons

**Primary Action Button:**
```html
<button class="inline-flex items-center gap-2 px-3.5 py-2 bg-[#1b75bc] text-white text-sm font-medium rounded-lg hover:bg-[#155a92] transition-all">
  <i class="ri-add-line"></i>
  <span>New Item</span>
</button>
```

**Secondary Button:**
```html
<button class="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 text-sm font-medium rounded-lg hover:bg-slate-50 transition-all">
  <i class="ri-download-line"></i>
  <span>Export</span>
</button>
```

**Icon-Only Button:**
```html
<button class="inline-flex items-center justify-center w-10 h-10 rounded-full bg-white border border-slate-200 hover:bg-slate-50 transition-all">
  <i class="ri-search-line text-slate-700"></i>
</button>
```

**Design Rules:**
- Always use `inline-flex items-center` for consistent alignment
- Include `gap-2` between icon and text
- Use RemixIcon (`ri-*`) for all icons
- Standard padding: `px-3.5 py-2` for compact, `px-4 py-2.5` for larger
- Always include `transition-all` for smooth interactions

### Cards

**Standard Card:**
```html
<div class="bg-white border border-slate-200 rounded-xl shadow-sm p-6">
  <h3 class="text-lg font-semibold text-slate-900 mb-4">Card Title</h3>
  <p class="text-sm text-slate-600">Card content goes here</p>
</div>
```

**Stat Card:**
```html
<div class="bg-white rounded-lg shadow-sm p-5 transition-all duration-200 hover:shadow-md">
  <div class="text-xs font-medium text-slate-500 mb-1">Active projects</div>
  <div class="text-3xl font-semibold text-slate-900">12</div>
  <div class="text-xs text-slate-400 mt-1.5">+2 this week</div>
</div>
```

**Interactive Card:**
```html
<button class="w-full text-left bg-white border border-slate-200 rounded-xl p-6 hover:bg-slate-50 hover:border-slate-300 transition-all">
  <!-- Card content -->
</button>
```

**Design Rules:**
- Use `bg-white` with `border border-slate-200` for definition
- Add `shadow-sm` for subtle depth
- Use `rounded-xl` for cards
- Padding: `p-6` for content cards, `p-5` for stat cards
- Add hover states for interactive cards

### Input Fields

**Text Input:**
```html
<div class="relative">
  <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
    <i class="ri-search-line text-slate-400 text-sm"></i>
  </div>
  <input
    type="text"
    placeholder="Search..."
    class="w-full pl-10 pr-4 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#1b75bc] focus:border-transparent text-sm text-slate-900 placeholder:text-slate-400 transition-all"
  />
</div>
```

**Standard Input (no icon):**
```html
<input
  type="text"
  placeholder="Enter value..."
  class="w-full px-4 py-2.5 bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#1b75bc] focus:border-transparent text-sm text-slate-900 placeholder:text-slate-400 transition-all"
/>
```

**Design Rules:**
- Always use `bg-white` with `border-slate-200`
- Focus state: `focus:ring-2 focus:ring-[#1b75bc] focus:border-transparent`
- Text size: `text-sm` for inputs
- Placeholder color: `text-slate-400`
- Icon spacing: `pl-10` when icon present, `pl-3.5` for icon position
- Standard height: `py-2` or `py-2.5`

### Data Tables

**Table Container:**
```html
<div class="bg-white border border-slate-200 rounded-xl shadow-sm overflow-hidden">
  <!-- Table Header -->
  <div class="border-b border-slate-100 bg-slate-50/50 px-6 py-3">
    <div class="grid grid-cols-12 gap-4">
      <div class="col-span-4 text-xs font-medium text-slate-600">Name</div>
      <div class="col-span-3 text-xs font-medium text-slate-600">Email</div>
      <div class="col-span-2 text-xs font-medium text-slate-600">Status</div>
      <div class="col-span-2 text-xs font-medium text-slate-600">Date</div>
      <div class="col-span-1"></div>
    </div>
  </div>

  <!-- Table Body -->
  <div class="divide-y divide-slate-100">
    @for (item of items; track item.id) {
      <button class="w-full px-6 py-4 grid grid-cols-12 gap-4 items-center hover:bg-slate-50/50 transition-all text-left group">
        <!-- Content columns -->
      </button>
    }
  </div>
</div>
```

**Sortable Column Header:**
```html
<button (click)="toggleSort('name')" class="col-span-4 text-xs font-medium text-slate-600 text-left flex items-center gap-1.5 hover:text-slate-900 transition-colors">
  Name
  @if (sortField() === 'name') {
    <i [class]="sortDirection() === 'asc' ? 'ri-arrow-up-s-line text-sm' : 'ri-arrow-down-s-line text-sm'"></i>
  }
</button>
```

**Table Row with Icon:**
```html
<div class="col-span-4">
  <div class="flex items-center gap-3">
    <div class="w-10 h-10 rounded-lg bg-slate-50 flex items-center justify-center text-slate-400 group-hover:text-[#1b75bc] transition-colors">
      <i class="ri-car-line text-lg"></i>
    </div>
    <div class="min-w-0">
      <p class="text-sm font-semibold text-slate-900 group-hover:text-[#1b75bc] transition-colors truncate">{{ item.name }}</p>
      <p class="text-xs text-slate-500 truncate">{{ item.subtitle }}</p>
    </div>
  </div>
</div>
```

**Design Rules:**
- Container: White background with `border-slate-200`, `rounded-xl`
- Header: `bg-slate-50/50` with `border-b border-slate-100`
- Header text: `text-xs font-medium text-slate-600`
- Rows: `divide-y divide-slate-100` for separators
- Row padding: `px-6 py-4`
- Hover state: `hover:bg-slate-50/50`
- Use grid system: `grid grid-cols-12 gap-4` for responsive columns
- Icons: `w-10 h-10 rounded-lg bg-slate-50` containers

### Search & Filter Bar

**Search with Icon:**
```html
<div class="relative flex-1">
  <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
    <i class="ri-search-line text-slate-400 text-sm"></i>
  </div>
  <input
    type="text"
    placeholder="Search customers..."
    class="w-full pl-10 pr-4 py-2 bg-white border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#1b75bc] focus:border-transparent text-sm text-slate-900 placeholder:text-slate-400 transition-all"
  />
</div>
```

**Filter Button:**
```html
<button class="inline-flex items-center gap-2 px-3.5 py-2 bg-white border text-slate-700 text-sm font-medium rounded-lg hover:bg-slate-50 transition-all"
  [class.border-[#1b75bc]]="showFilters()"
  [class.bg-blue-50]="showFilters()"
  [class.border-slate-200]="!showFilters()">
  <i class="ri-filter-3-line"></i>
  <span>Filters</span>
  @if (activeFilters > 0) {
    <span class="w-1.5 h-1.5 rounded-full bg-[#1b75bc]"></span>
  }
</button>
```

**Filter Panel:**
```html
<div class="mt-3 p-4 bg-white border border-slate-200 rounded-lg">
  <div class="flex items-center gap-4">
    <span class="text-xs font-medium text-slate-600">Status</span>
    <div class="flex items-center gap-2 flex-wrap">
      <button
        class="px-3 py-1.5 text-xs font-medium rounded-md transition-all"
        [class.bg-[#1b75bc]]="selected"
        [class.text-white]="selected"
        [class.bg-slate-100]="!selected"
        [class.text-slate-700]="!selected"
        [class.hover:bg-slate-200]="!selected">
        Option
      </button>
    </div>
  </div>
</div>
```

### Badges & Status Indicators

**Status Badge:**
```html
<span class="inline-flex px-2.5 py-1 bg-slate-100 text-slate-700 rounded text-xs font-semibold border border-slate-200">
  Active
</span>
```

**Color Variants:**
```html
<!-- Success -->
<span class="inline-flex px-2.5 py-1 bg-emerald-50 text-emerald-700 rounded text-xs font-semibold border border-emerald-200">
  Completed
</span>

<!-- Warning -->
<span class="inline-flex px-2.5 py-1 bg-amber-50 text-amber-700 rounded text-xs font-semibold border border-amber-200">
  Pending
</span>

<!-- Error -->
<span class="inline-flex px-2.5 py-1 bg-red-50 text-red-700 rounded text-xs font-semibold border border-red-200">
  Failed
</span>

<!-- Primary -->
<span class="inline-flex px-2.5 py-1 bg-[#1b75bc] text-white rounded-full text-xs font-medium">
  Featured
</span>
```

**Notification Dot:**
```html
<span class="w-1.5 h-1.5 rounded-full bg-[#1b75bc]"></span>
```

### Page Layouts

**Standard Page Layout:**
```html
<div class="min-h-screen bg-slate-50">
  <div class="max-w-6xl mx-auto px-6 py-16">
    <!-- Page Header -->
    <div class="mb-8">
      <h1 class="text-4xl font-semibold text-slate-900 tracking-tight mb-2">Page Title</h1>
      <p class="text-base text-slate-500 mb-8">Page description</p>
    </div>

    <!-- Page Content -->
  </div>
</div>
```

**Page with Actions Bar:**
```html
<div class="min-h-screen bg-slate-50">
  <div class="max-w-6xl mx-auto px-6 py-16">
    
    <!-- Actions Bar -->
    <div class="mb-8">
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <button class="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 text-sm font-medium rounded-lg hover:bg-slate-50 transition-all">
            <i class="ri-download-line"></i>
            <span>Export</span>
          </button>
          <button class="inline-flex items-center gap-2 px-3.5 py-2 bg-[#1b75bc] text-white text-sm font-medium rounded-lg hover:bg-[#155a92] transition-all">
            <i class="ri-add-line"></i>
            <span>New Item</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Content -->
  </div>
</div>
```

**Dashboard Grid Layout:**
```html
<div class="min-h-screen bg-slate-50">
  <div class="max-w-4xl mx-auto px-6 py-16">
    
    <!-- Stats Grid -->
    <div class="grid grid-cols-3 gap-4 mb-8">
      <!-- Stat cards -->
    </div>

    <!-- Main Content -->
    <div class="bg-white rounded-lg shadow-sm p-7">
      <!-- Content -->
    </div>
  </div>
</div>
```

**Design Rules:**
- Page background: `bg-slate-50`
- Container: `max-w-6xl` or `max-w-4xl` depending on content width
- Page padding: `px-6 py-16` or `px-6 py-12`
- Page titles: `text-4xl font-semibold text-slate-900 tracking-tight`
- Descriptions: `text-base text-slate-500`
- Section spacing: `mb-8` between major sections

### Lists

**Simple List:**
```html
<div class="bg-white border border-slate-200 rounded-xl overflow-hidden divide-y divide-slate-100">
  @for (item of items; track item.id) {
    <button class="w-full px-6 py-4 flex items-center gap-3 hover:bg-slate-50 transition-all text-left">
      <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-500">
        <i class="ri-file-line"></i>
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-sm font-semibold text-slate-900 truncate">{{ item.title }}</p>
        <p class="text-xs text-slate-500 truncate">{{ item.subtitle }}</p>
      </div>
      <i class="ri-arrow-right-s-line text-slate-400"></i>
    </button>
  }
</div>
```

**List with Sections:**
```html
<div class="space-y-8">
  @for (section of sections; track section.id) {
    <div>
      <h3 class="text-xs font-medium text-slate-500 uppercase tracking-wider mb-3 px-6">{{ section.title }}</h3>
      <div class="bg-white border border-slate-200 rounded-xl overflow-hidden divide-y divide-slate-100">
        @for (item of section.items; track item.id) {
          <div class="px-6 py-4 flex items-center justify-between">
            <span class="text-sm text-slate-900">{{ item.label }}</span>
            <span class="text-sm text-slate-500">{{ item.value }}</span>
          </div>
        }
      </div>
    </div>
  }
</div>
```

**Design Rules:**
- Container: `bg-white border border-slate-200 rounded-xl`
- Use `divide-y divide-slate-100` for separators
- List items: `px-6 py-4` padding
- Hover state: `hover:bg-slate-50`
- Section headers: `text-xs font-medium text-slate-500 uppercase`

---

## Animation & Transitions

### Standard Transitions

```html
<!-- Use transition-all for smooth state changes -->
<button class="... transition-all duration-150">
<!-- Use transition-colors for color-only changes -->
<button class="... transition-colors">
<!-- Use transition-transform for movement -->
<button class="... transition-transform">
```

### Tailwind Durations

```html
duration-75   → 75ms    /* Instant feedback */
duration-150  → 150ms   /* Hover, focus states (default) */
duration-200  → 200ms   /* Standard transitions */
duration-300  → 300ms   /* Smooth animations */
duration-500  → 500ms   /* Complex animations */
```

### Custom Animations (styles.css)

```css
@keyframes fade-in {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slide-up {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.animate-fade-in {
  animation: fade-in 0.2s ease-out;
}

.animate-slide-up {
  animation: slide-up 0.3s ease-out;
}
```

### Usage Guidelines

- Use `transition-all` for most interactive elements
- Default duration: `duration-150` (already applied by transition-all)
- Add `hover:` and `focus:` states for better UX
- Use `group-hover:` for child element transitions

---

## Icon System

### RemixIcon Integration

Gixat uses **RemixIcon** for all interface icons. Icons are loaded via CDN in the main HTML.

**Icon Sizes:**
```html
<!-- Small icons (16px) -->
<i class="ri-icon-name text-sm"></i>

<!-- Regular icons (20px) - Default -->
<i class="ri-icon-name"></i>

<!-- Large icons (24px) -->
<i class="ri-icon-name text-lg"></i>

<!-- Extra large icons (32px) -->
<i class="ri-icon-name text-2xl"></i>
```

### Common Icons

```html
<!-- Actions -->
<i class="ri-add-line"></i>           <!-- Add/Create -->
<i class="ri-edit-line"></i>          <!-- Edit -->
<i class="ri-delete-bin-line"></i>    <!-- Delete -->
<i class="ri-save-line"></i>          <!-- Save -->
<i class="ri-close-line"></i>         <!-- Close -->

<!-- Navigation -->
<i class="ri-arrow-right-s-line"></i> <!-- Forward -->
<i class="ri-arrow-left-s-line"></i>  <!-- Back -->
<i class="ri-arrow-down-s-line"></i>  <!-- Down/Sort -->
<i class="ri-arrow-up-s-line"></i>    <!-- Up/Sort -->

<!-- UI Elements -->
<i class="ri-search-line"></i>        <!-- Search -->
<i class="ri-filter-3-line"></i>      <!-- Filter -->
<i class="ri-download-line"></i>      <!-- Download/Export -->
<i class="ri-upload-line"></i>        <!-- Upload/Import -->
<i class="ri-settings-line"></i>      <!-- Settings -->

<!-- Content -->
<i class="ri-car-line"></i>           <!-- Vehicle -->
<i class="ri-user-line"></i>          <!-- User/Customer -->
<i class="ri-file-line"></i>          <!-- Document/Job Card -->
<i class="ri-calendar-line"></i>      <!-- Date/Schedule -->
```

### Usage Guidelines

- Always use `-line` variant (outline style)
- Color icons with text color utilities: `text-slate-400`, `text-[#1b75bc]`
- Add `transition-colors` to icons that change color on hover
- Include descriptive aria-labels for accessibility

---

## Accessibility Requirements

### Color Contrast

Our color palette meets WCAG AA standards:
- `text-slate-900` on `bg-white`: ✓ Excellent contrast
- `text-slate-600` on `bg-white`: ✓ Good contrast
- `text-slate-400` on `bg-white`: Use only for non-essential text
- `text-white` on `bg-[#1b75bc]`: ✓ Excellent contrast

### Focus States

```html
<!-- Always include focus states for keyboard navigation -->
<button class="... focus:outline-none focus:ring-2 focus:ring-[#1b75bc] focus:ring-offset-2">

<!-- For inputs -->
<input class="... focus:outline-none focus:ring-2 focus:ring-[#1b75bc] focus:border-transparent" />
```

### Semantic HTML

```html
<!-- Use semantic elements -->
<button type="button">Action</button>
<nav aria-label="Main navigation">...</nav>
<main>...</main>
<header>...</header>
<footer>...</footer>

<!-- Add ARIA labels for icons -->
<button aria-label="Close modal">
  <i class="ri-close-line" aria-hidden="true"></i>
</button>

<!-- Use proper heading hierarchy -->
<h1>Page Title</h1>
<h2>Section Title</h2>
<h3>Subsection Title</h3>
```

### Screen Reader Support

```html
<!-- Hide decorative icons from screen readers -->
<i class="ri-icon-name" aria-hidden="true"></i>

<!-- Provide text alternatives -->
<span class="sr-only">Text for screen readers</span>

<!-- Use proper button labels -->
<button aria-label="Add new customer">
  <i class="ri-add-line" aria-hidden="true"></i>
  <span>Add</span>
</button>
```

---

## Output Format

When generating Gixat UI code, always include:

1. **Complete Angular components** with TypeScript
2. **Tailwind CSS classes** (no custom CSS unless necessary)
3. **Angular signals** for reactive state management
4. **Responsive design** using Tailwind breakpoints
5. **Accessibility attributes** (aria-*, proper semantic HTML)

### Component Structure

```typescript
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-slate-50">
      <div class="max-w-6xl mx-auto px-6 py-16">
        <!-- Component content -->
      </div>
    </div>
  `
})
export class ExampleComponent {
  // Use signals for reactive state
  items = signal<Item[]>([]);
  isLoading = signal(false);
  searchQuery = signal('');
  
  // Methods
  loadItems() {
    // Implementation
  }
}
```

### Template Patterns

```html
<!-- Use @for for lists -->
@for (item of items(); track item.id) {
  <div>{{ item.name }}</div>
}

<!-- Use @if for conditionals -->
@if (isLoading()) {
  <div>Loading...</div>
} @else {
  <div>Content</div>
}

<!-- Use @empty for empty states -->
@for (item of items(); track item.id) {
  <div>{{ item.name }}</div>
} @empty {
  <div>No items found</div>
}
```

---

## Best Practices Checklist

Before finalizing any component, verify:

- [ ] **Typography:** Using consistent text sizes and weights
- [ ] **Colors:** Primary `#1b75bc`, slate scale for neutrals
- [ ] **Spacing:** Consistent padding/margin (px-6, py-4, etc.)
- [ ] **Buttons:** Standard sizing `px-3.5 py-2` with icons
- [ ] **Border radius:** `rounded-lg` for buttons, `rounded-xl` for cards
- [ ] **Transitions:** All interactive elements have `transition-all`
- [ ] **Accessibility:** Proper focus states, aria-labels, semantic HTML
- [ ] **Icons:** RemixIcon `-line` variant with proper sizing
- [ ] **Responsive:** Works on mobile, tablet, and desktop
- [ ] **Signals:** Using Angular signals for reactive state

---

## Responsive Design

### Tailwind Breakpoints

```html
<!-- Mobile first approach -->
<div class="px-4 sm:px-6 lg:px-8">        <!-- Responsive padding -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">  <!-- Responsive grid -->
<h1 class="text-2xl sm:text-3xl lg:text-4xl"> <!-- Responsive text -->
```

**Breakpoints:**
- `sm:` - 640px (Small tablets)
- `md:` - 768px (Tablets)
- `lg:` - 1024px (Laptops)
- `xl:` - 1280px (Desktops)
- `2xl:` - 1536px (Large screens)

---

## Resources

- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [RemixIcon](https://remixicon.com/)
- [Angular Documentation](https://angular.dev/)
- [Angular Signals](https://angular.dev/guide/signals)

---

*This design system ensures consistent, professional, and accessible interfaces across the Gixat garage management application.*
