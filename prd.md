## PRD: Flyball Racing Schedule & Ring Management

## 1. Product overview

### 1.1 Document title and version

- PRD: Flyball Racing Schedule & Ring Management
- Version: 0.1

### 1.2 Product summary

This project enables tournament organizers to upload and manage racing schedules and operate multiple rings in real time. A Tournament Director can import a CSV of races that includes race number, left lane team, right lane team, and division, then configure up to 10 rings with a fixed primary-color palette.

During racing, the Race Director updates each ring’s state: current race, on deck, and in the hole. Viewers can sign in, claim their team, and receive timely push notifications when their team moves into in-the-hole and on-deck states. The system supports multiple concurrent tournaments and retains historical results. State is persisted in Azure Cosmos DB. Authentication is provided by Microsoft Entra ID.

## 2. Goals

### 2.1 Business goals

- Reduce operations overhead for tournament staff by centralizing schedule and ring orchestration.
- Improve participant satisfaction with timely notifications and clear ring status visibility.
- Support multiple tournaments and historical records to drive repeat usage and insights.
- Provide a resilient, observable system aligned with modern cloud-native patterns.

### 2.2 User goals

- Tournament Director: Quickly import schedules and configure rings.
- Race Director: Efficiently manage ring flow and update race states in seconds.
- Viewer/Team member: Claim a team and get reliable, timely notifications and status views.

### 2.3 Non-goals

- Automated results adjudication or scoring beyond race state transitions.
- Advanced scheduling optimization or bracket generation.
- Payment processing or registration workflows.

## 3. User personas

### 3.1 Key user types

- Tournament Director
- Race Director
- Viewer (team member or supporter)

### 3.2 Basic persona details

- Tournament Director: Oversees overall event setup, imports schedules, configures rings.
- Race Director: Operates rings during the tournament and controls live race state.
- Viewer: Follows one or more teams; receives status notifications; reads-only otherwise.

### 3.3 Role-based access

- Tournament Director: Import CSV, create/manage tournaments, configure rings/colors, assign ring scheduling mode, archive/close tournaments.
- Race Director: Update ring status (current/on deck/in the hole), mark race done, switch modes (if allowed), correct mistakes.
- Viewer: Authenticate, claim a team, receive notifications, view live board.

## 4. Functional requirements

- CSV import and validation (Priority: High)
  - Accept CSV with columns: race number, left team, right team, division (only). Validate headers, types, duplicates, and empties.
  - Show errors with line numbers and reasons; allow re-upload.
  - Persist parsed races to tournament scope in Cosmos DB.

- Ring configuration (Priority: High)
  - Configure number of rings (1–10).
  - Select a unique color per ring from a fixed primary-color palette.
  - Persist configuration and allow edits prior to race start; track audit of changes.

- Scheduling modes (Priority: High)
  - For 2 rings: choose manual or odd/even by race number.
  - For >2 rings: choose manual or next-race-up (round-robin style across rings).
  - Allow per-ring overrides in manual mode.

- Live ring operations (Priority: High)
  - For each ring: set current race, on deck, in the hole; mark race done.
  - Prevent assigning the same race to multiple rings at the same time (unless overridden with confirmation).
  - On race done: advance on-deck → current and in-the-hole → on-deck automatically per mode; surface next in-the-hole.
  - Real-time updates propagate to all connected viewers within a few seconds.

- Team claim and notifications (Priority: High)
  - Authenticated viewers can search/claim a team (one or multiple, configurable later; start with one).
  - Notify viewers when their team becomes in the hole and when it becomes on deck.
  - Support web notifications (in-app toasts) and optionally push (web push if enabled in browser).
  - De-duplicate notifications and avoid repeats on page refresh.

- Team GHOST handling (Priority: High)
  - Authorized Director/Race Director can mark a team as GHOST for the current event day.
  - All future races for that day where the team appears should display "GHOST" in place of the team name (left or right lane as applicable).
  - Suppress notifications for ghosted teams for the remainder of the day.
  - Changes are audit-logged and reversible (un-ghost restores original team names on future races for the day).

- Multi-tournament & history (Priority: Medium)
  - Create and manage multiple tournaments concurrently.
  - Archive completed tournaments and retain results and configuration.
  - Allow switching between active tournaments in the UI.

- Authentication & authorization (Priority: High)
  - Microsoft Entra ID sign-in for all roles.
  - Role assignment: Director (Tournament Director), Race Director, Viewer.
  - Enforce least-privilege access on API endpoints.

- Observability & health (Priority: Medium)
  - Health endpoints and basic dashboards for service health.
  - Tracing/metrics for CSV import time, notification send rate, and ring update latency.

## 5. User experience

### 5.1 Entry points & first-time user flow

- Director signs in with Entra ID, creates a new tournament, uploads CSV, configures rings.
- Race Director signs in, selects the tournament, confirms ring modes, opens live board.
- Viewer signs in, claims team, optionally opts in to web notifications, opens live board.

### 5.2 Core experience

- Import schedule: Director uploads CSV, views validation results, confirms import.
  - Clear status of valid/invalid rows with actionable error messages.
- Configure rings: Director sets ring count (1–10) and selects colors from fixed palette.
  - Prevent duplicate colors; show previews.
- Live control: Race Director sees rings with current/on deck/in the hole tiles.
  - One-click mark race done; automatic advancement per scheduling mode.
  - Manual override to swap or replace items.
- Viewer board: Live ring board auto-updates within a few seconds.
  - Claimed team is highlighted; notifications appear when state changes to in the hole or on deck.

### 5.3 Advanced features & edge cases

- Handle CSV re-uploads (replace vs append) before racing begins.
- Conflict detection when the same team appears in multiple races simultaneously.
- Manual corrections: undo last action per ring; audit log of changes.
- Offline viewers: queue notifications for next session; show last-known state.
- Limits: enforce 10-ring max; enforce fixed color palette.
 - GHOST reversals: support un-ghosting if marked in error; restore labels and notifications for future races only (past races remain unchanged).

### 5.4 UI/UX highlights

- Color-coded rings matching configuration for easy field identification.
- Large, glanceable tiles for current/on deck/in the hole.
- Sticky header with tournament selector and role indicator.
- Accessible keyboard shortcuts for Race Director actions (e.g., mark done).

## 6. Narrative

A Tournament Director sets up an event in minutes by uploading a simple CSV and picking ring colors. The Race Director operates rings smoothly, marking races done and letting the system advance the next races based on the selected mode. Viewers sign in, claim their team, and get timely notifications to head to the ring. Everyone sees up-to-date status with minimal friction, and the organization retains historical data for reference.

## 7. Success metrics

### 7.1 User-centric metrics

- 95%+ CSV imports succeed on first attempt (valid files).
- Live updates visible to viewers within ≤3 seconds of change (P95).
- >80% of viewers who claim a team receive correct notifications.

### 7.2 Business metrics

- Reduce ring idle time by 20%+ after adoption.
- Increase repeat event usage by 25% within a season.
- Decrease director/operator intervention tickets by 30%.

### 7.3 Technical metrics

- API 99.9% availability during events.
- <200ms P95 write latency for ring updates; <3s P95 fan-out to clients.
- Error rate for notification delivery <1% (excluding user-disabled notifications).

## 8. Technical considerations

### 8.1 Integration points

- Authentication: Microsoft Entra ID (OpenID Connect/OAuth2 via MSAL).
- Data: Azure Cosmos DB (state, tournaments, races, ring config, claims).
- Real-time: SignalR (server push) with optional Web Push for notifications.
- App architecture: .NET Aspire (AppHost orchestrates Web UI and API service; ServiceDefaults for discovery, resilience, health, telemetry).

### 8.2 Data storage & privacy

- Partition Cosmos DB data by tournament to support concurrency and performance.
- PII minimization: store team identifiers, not personal data, for claims when possible.
- Retention policy configurable per tournament; archived data read-only.

### 8.3 Scalability & performance

- Horizontal scale-out for Web/API; cache frequently accessed tournament state.
- Efficient fan-out to viewers via SignalR groups per team and per ring.

### 8.4 Potential challenges

- Conflicting updates from multiple operators; require idempotent transitions and optimistic concurrency.
- Notification reliability across browsers and user permissions.
- CSV inconsistencies; robust validation and user guidance needed.

## 9. Milestones & sequencing

### 9.1 Project estimate

- Medium: 6–8 weeks for MVP; 10–12 weeks for full feature set.

### 9.2 Team size & composition

- 3–5: Full-stack engineer(s), frontend/UI, backend/API, QA, DevOps (shared).

### 9.3 Suggested phases

- Phase 1: Foundations (2 weeks)
  - Entra ID auth, Cosmos DB wiring, Aspire app skeleton, health/telemetry.
- Phase 2: CSV import & validation, ring configuration (2 weeks)
  - CSV parser/validation UI, ring config UI, persistence.
- Phase 3: Live ring operations & scheduling modes (2 weeks)
  - Current/on deck/in the hole flows, mark done, odd/even and next-up modes.
- Phase 4: Viewer claim & notifications (2–3 weeks)
  - Team claim UX, SignalR groups, in-app toasts, optional web push.
- Phase 5: Multi-tournament & history; polish (1–2 weeks)
  - Tournament switcher, archive, audits, accessibility and performance.

## 10. User stories

### 10.1 upload schedule CSV

- ID: GH-001
- Description: As a Tournament Director, I upload a CSV with race number, left team, right team, and division.
- Acceptance criteria:
  - CSV must contain required headers and valid rows.
  - Invalid rows are reported with row numbers and reasons; import blocked until resolved or removed.
  - Successful import stores races in Cosmos DB under the chosen tournament.

### 10.2 validate and re-upload CSV

- ID: GH-002
- Description: As a Tournament Director, I see validation errors and can re-upload before racing begins.
- Acceptance criteria:
  - Show a summarized error list and downloadable error report.
  - Re-upload replaces prior data if selected; confirmation required.

### 10.3 configure rings and colors

- ID: GH-003
- Description: As a Tournament Director, I set 1–10 rings and choose distinct colors from a fixed primary-color palette.
- Acceptance criteria:
  - Prevent selecting more than 10 rings.
  - Disallow duplicate colors; provide visual previews.
  - Persist configuration, editable until racing starts.

### 10.4 choose scheduling mode

- ID: GH-004
- Description: As a Race Director, I select scheduling mode (2 rings: manual or odd/even; >2 rings: manual or next-up).
- Acceptance criteria:
  - UI reflects allowed modes based on ring count.
  - Saved mode is applied when advancing races.

### 10.5 assign races to rings manually

- ID: GH-005
- Description: As a Race Director, I manually assign races to rings and set current, on deck, in the hole.
- Acceptance criteria:
  - Prevent the same race from being current in two rings without override.
  - Changes reflect in UI in ≤3 seconds for viewers.

### 10.6 automatic odd/even assignment (2 rings)

- ID: GH-006
- Description: As a Race Director, I use odd/even mode to distribute races across 2 rings by race number.
- Acceptance criteria:
  - Odd race numbers go to Ring A; even to Ring B (or configured equivalently).
  - Marking a race done advances the next appropriate race number.

### 10.7 automatic next-up assignment (>2 rings)

- ID: GH-007
- Description: As a Race Director, I use next-up mode to rotate assignments across >2 rings.
- Acceptance criteria:
  - After race completion, the next unrun race is placed on the next ring in rotation.
  - Rotation order persists across the session.

### 10.8 mark race done and advance

- ID: GH-008
- Description: As a Race Director, I mark the current race done and the system advances queues.
- Acceptance criteria:
  - On deck → current; in the hole → on deck; next race surfaced to in the hole per mode.
  - Operation is idempotent; accidental double-click doesn’t double-complete.

### 10.9 correct ring state mistakes

- ID: GH-009
- Description: As a Race Director, I undo or edit ring state when a mistake occurs.
- Acceptance criteria:
  - Provide undo of last action and manual edit for any of the three slots.
  - Audit log records who changed what and when.

### 10.10 view live board

- ID: GH-010
- Description: As a Viewer, I see all rings with current/on deck/in the hole and colors.
- Acceptance criteria:
  - Board updates within ≤3 seconds after changes.
  - Colors match configured ring colors; accessible contrast.

### 10.11 authenticate with Entra ID

- ID: GH-011
- Description: As any user, I sign in using Microsoft Entra ID.
- Acceptance criteria:
  - Successful sign-in creates/updates a user profile and role mapping.
  - Unauthorized access to director functions is blocked.

### 10.12 claim a team

- ID: GH-012
- Description: As a Viewer, I claim my team to receive notifications and highlights.
- Acceptance criteria:
  - Team search/autocomplete against imported schedule.
  - Claimed team highlights on the board.

### 10.13 receive notifications for team

- ID: GH-013
- Description: As a Viewer, I receive notifications when my team becomes in the hole or on deck.
- Acceptance criteria:
  - In-app notification appears reliably with the state change.
  - Optional browser push if user grants permission; gracefully degrades if denied.

### 10.14 manage multiple tournaments

- ID: GH-014
- Description: As a Director, I manage multiple concurrent tournaments and switch between them.
- Acceptance criteria:
  - Tournament switcher lists active and archived tournaments.
  - Data isolation across tournaments is enforced.

### 10.15 archive tournament and view history

- ID: GH-015
- Description: As a Director, I archive a completed tournament and view historical data later.
- Acceptance criteria:
  - Archived tournaments are read-only.
  - Historical ring states and race results are retrievable.

### 10.16 error handling for CSV issues

- ID: GH-016
- Description: As a Director, I see clear errors for missing headers, wrong columns, empty values, or duplicates.
- Acceptance criteria:
  - Report includes row number and actionable guidance.
  - Import blocked until fixed; partial imports allowed only with explicit confirmation.

### 10.17 handle offline viewers

- ID: GH-017
- Description: As a Viewer, if I’m offline when a notification occurs, I see the latest state when I return.
- Acceptance criteria:
  - On reconnect, claimed team state is refreshed.
  - No duplicate notifications for the same state.

### 10.18 authorization and roles

- ID: GH-018
- Description: As an Admin/Director, I enforce roles (Director, Race Director, Viewer) across the app.
- Acceptance criteria:
  - API routes require correct roles; attempts are logged and denied with 403.
  - UI hides actions the user cannot perform.

### 10.19 real-time performance

- ID: GH-019
- Description: As a Viewer, I see updates within a few seconds of any change.
- Acceptance criteria:
  - P95 end-to-end latency from ring update to client render ≤3 seconds under typical event load.

### 10.20 health and observability

- ID: GH-020
- Description: As an Operator, I can monitor system health and event flow.
- Acceptance criteria:
  - Health endpoints respond during development; basic metrics/traces collected for imports, notifications, and ring updates.

### 10.21 mark team as GHOST

- ID: GH-021
- Description: As a Director or Race Director, I mark a team as GHOST for the current event day so all of their remaining races show GHOST and notifications are suppressed.
- Acceptance criteria:
  - Selecting a team and choosing "Mark as GHOST" immediately updates the schedule so future races for that day display "GHOST" in that team’s lane.
  - Prior races are not modified; only future races on the same day are affected.
  - Viewers who claimed the ghosted team no longer receive in-the-hole/on-deck notifications for the remainder of the day.
  - Action is audit-logged (who/when) and is reversible via "Un-ghost"; reversing restores team names and notifications for future races only.
