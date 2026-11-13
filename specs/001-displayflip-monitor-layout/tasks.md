---
description: "Task list template for feature implementation"
---

# Tasks: DisplayFlip multi-monitor layout

**Input**: Design documents from `/specs/001-displayflip-monitor-layout/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: xUnit pure-logic tests included per plan.md Testing section (DisplayFlip.Core is independently testable without physical monitors; CCD end-to-end validated via quickstart.md).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project** per plan.md: `src/DisplayFlip.Core/`, `src/DisplayFlip.Windows/`, `src/DisplayFlip/`, `src/DisplayFlip.Tests/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution and single-project structure per plan (`DisplayFlip.sln`, `src/DisplayFlip.Core/DisplayFlip.Core.csproj`, `src/DisplayFlip.Windows/DisplayFlip.Windows.csproj`, `src/DisplayFlip/DisplayFlip.csproj`, `src/DisplayFlip.Tests/DisplayFlip.Tests.csproj`)
- [X] T002 [P] Configure .editorconfig, .gitignore, and Directory.Build.props (net8.0-windows, LangVersion) in repo root
- [X] T003 [P] Add xUnit + FluentAssertions references to `src/DisplayFlip.Tests/DisplayFlip.Tests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**âš ï¸ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Create Monitor, Position, and LayoutProfile models in `src/DisplayFlip.Core/Models.cs` (Monitor StableKey, FriendlyName, Position, LayoutProfile schemaVersion)
- [X] T005 [P] Create DisplayTopology pure math scaffold (Mirror, SetPrimary, Legalizer signatures) in `src/DisplayFlip.Core/DisplayTopology.cs` (delegates to US1/US2 for full implementations)
- [X] T006 [P] Create ProfileStore JSON serialize/deserialize + atomic write in `src/DisplayFlip.Core/ProfileStore.cs`
- [X] T007 [P] Create CCD P/Invoke declarations (QueryDisplayConfig, SetDisplayConfig, DisplayConfigGetDeviceInfo) in `src/DisplayFlip.Windows/NativeMethods.cs`
- [X] T008 Create CcdTopologyAdapter skeleton (ReadTopology via QDC_ONLY_ACTIVE_PATHS, Validate, Apply) in `src/DisplayFlip.Windows/CcdTopologyAdapter.cs`
- [X] T009 Create tray host skeleton with NotifyIcon and WM_DISPLAYCHANGE refresh in `src/DisplayFlip/TrayHost.cs`
- [X] T010 Create CLI arg parser skeleton mapping `--mirror` / `--apply <name>` to LayoutOperation in `src/DisplayFlip/Program.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Cerminkan posisi kiri-kanan sekali klik (Priority: P1) ðŸŽ¯ MVP

**Goal**: Satu klik dari tray atau `--mirror` membalik susunan cermin dengan primary tetap pada identitas yang sama dan layout tetap legal.

**Independent Test**: Atur 2 monitor aktif berdamping, jalankan tray "Tukar kiri-kanan" atau `DisplayFlip.exe --mirror`, dan verifikasi urutan terbalik, primary tetap pada monitor yang sama, settled <3s.

- [X] T011 [US1] Implement Mirror primary-preserving math (`x' = totalWidth âˆ’ (x+width)`) in `src/DisplayFlip.Core/DisplayTopology.cs`
- [X] T012 [US1] Implement Legalizer (sort by x' and pack from 0 without gap/overlap) in `src/DisplayFlip.Core/DisplayTopology.cs`
- [X] T013 [US1] Wire tray menu item "ðŸ”„ Tukar kiri-kanan" to Mirror via Validateâ†’Apply in `src/DisplayFlip/TrayHost.cs`
- [X] T014 [US1] Wire CLI `--mirror` to same Mirror path in `src/DisplayFlip/Program.cs`
- [X] T015 [US1] Add guard for <2 active monitors (disable menu + balloon message) in `src/DisplayFlip/TrayHost.cs`
- [X] T016 [US1] Add pure-logic tests for Mirror and Legalizer in `src/DisplayFlip.Tests/DisplayTopologyTests.cs`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Jadikan monitor tertentu sebagai primary (Priority: P1)

**Goal**: Pilih monitor dari submenu tray untuk dijadikan primary; posisi (0,0) pindah ke target dan susunan tetap legal.

**Independent Test**: Pilih monitor kedua via tray "Jadikan primary", dan verifikasi monitor itu menjadi primary OS (taskbar pindah) dengan posisi legal.

- [X] T017 [US2] Implement SetPrimary math (shift target to 0,0 + legalize others) in `src/DisplayFlip.Core/DisplayTopology.cs`
- [X] T018 [US2] Build dynamic submenu "Jadikan primary > <Monitor>" per active monitor in `src/DisplayFlip/TrayHost.cs`
- [X] T019 [US2] Add already-primary no-op path (balloon "Sudah primary") in `src/DisplayFlip/TrayHost.cs`
- [X] T020 [US2] Add pure-logic tests for SetPrimary + Legalizer interaction in `src/DisplayFlip.Tests/DisplayTopologyTests.cs`

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Simpan dan terapkan profil layout (Priority: P2)

**Goal**: Simpan susunan saat ini sebagai profil bernama dan terapkan kembali dengan satu klik, mengikuti missing-monitor opt-in policy.

**Independent Test**: Simpan layout sebagai "Primary Kanan", ubah posisi manual di Settings, lalu Apply dari tray dan verifikasi persis membalik, termasuk pesan bila monitor hilang.

- [X] T021 [US3] Implement Monitor identity resolution via DisplayConfigGetDeviceInfo + SetupAPI DeviceID in `src/DisplayFlip.Windows/MonitorIdentity.cs`
- [X] T022 [US3] Implement Profile save (CaptureTopology â†’ LayoutProfile) in `src/DisplayFlip.Windows/ProfileService.cs`
- [X] T023 [US3] Implement Profile apply with identity lookup + MissingMonitor opt-in dialog in `src/DisplayFlip/TrayHost.cs`
- [X] T024 [US3] Implement tray menus "Simpan layout sekarang" dialog and "Terapkan profil > <Name>" in `src/DisplayFlip/TrayHost.cs`
- [X] T025 [US3] Add pure-logic tests for Profile JSON round-trip and missing-monitor policy in `src/DisplayFlip.Tests/ProfileStoreTests.cs`

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: User Story 4 - Panggil lewat CLI untuk shortcut/hotkey (Priority: P3)

**Goal**: CLI `--mirror` / `--apply "<name>"` / `--list` menghasilkan efek yang sama dengan tray (SC-004), dengan exit codes sesuai contracts/cli.md.

**Independent Test**: Jalankan `--mirror`, `--apply "Primary Kanan"`, `--apply "NoSuchProfile"` (expect exit 3) dari PowerShell dan cocokkan layout dengan tray.

- [X] T026 [US4] Implement full CLI semantics in `src/DisplayFlip/Program.cs` (exit codes 0/2/3/4/5 per contracts/cli.md)
- [X] T027 [US4] Implement `--list [--json]` diagnostics in `src/DisplayFlip/Program.cs`
- [X] T028 [US4] Add CLI-to-Core parity test (same Mirror path invoked via Program in `src/DisplayFlip.Tests/CliParityTests.cs`)

---

## Phase 7: User Story 5 - Jalan otomatis saat Windows start (Priority: P3)

**Goal**: Toggle startup menyinkronkan registry Run key; login berikutnya DisplayFlip sudah di tray.

**Independent Test**: Aktifkan toggle, logoff/login, dan verifikasi ikon tray muncul tanpa klik manual; nonaktifkan dan ulangi login.

- [X] T029 [US5] Implement registry Run key sync for AutoStartEnabled in `src/DisplayFlip.Windows/StartupRegistry.cs`
- [X] T030 [US5] Implement tray checkbox "Jalan otomatis" reflecting and toggling the setting in `src/DisplayFlip/TrayHost.cs`

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T031 [P] Refresh monitor/profile lists on WM_DISPLAYCHANGE and after Apply in `src/DisplayFlip/TrayHost.cs`
- [X] T032 [P] Add SDC_VALIDATE-before-APPLY + ERROR_BAD_CONFIGURATION handling in `src/DisplayFlip.Windows/CcdTopologyAdapter.cs`
- [X] T033 [P] Harden ProfileStore corrupt-file handling and atomic tmp-rename in `src/DisplayFlip.Core/ProfileStore.cs`
- [X] T034 Run quickstart.md validation (S1â€“S8 incl. 3-monitor legality, reboot stability) and record results in `/tmp/displayflip-validation.md`
- [X] T035 Verify portability boundary: DisplayFlip.Core references no Win32 imports in `src/DisplayFlip.Core/DisplayFlip.Core.csproj`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (US1 â†’ US2 â†’ US3 â†’ US4 â†’ US5)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories; MVP slice
- **User Story 2 (P1)**: Can start after Foundational (Phase 2), reuses Legalizer from US1 but independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2), reuses ReadTopology; needs MonitorIdentity (T021)
- **User Story 4 (P3)**: Can start after US1/US3 (CLI maps to same operations)
- **User Story 5 (P3)**: Can start after Foundational (Phase 2), independent of topology logic

### Within Each User Story

- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- T011 and T021 are parallelizable (different files, both depend on T004 only)
- T018 and T023 are parallelizable (tray submenu vs profile service, different files)

---

## Parallel Example: User Story 1

```bash
# Launch pure-logic pieces for User Story 1 together:
Task: "Implement Mirror primary-preserving math in src/DisplayFlip.Core/DisplayTopology.cs (T011)"
Task: "Implement Legalizer in src/DisplayFlip.Core/DisplayTopology.cs (T012)"

# Launch wiring pieces together after core is done:
Task: "Wire tray menu in src/DisplayFlip/TrayHost.cs (T013)"
Task: "Wire CLI --mirror in src/DisplayFlip/Program.cs (T014)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently (quickstart S1)
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational â†’ Foundation ready
2. Add User Story 1 â†’ Test independently â†’ Deploy/Demo (MVP!)
3. Add User Story 2 â†’ Test independently â†’ Deploy/Demo
4. Add User Story 3 â†’ Test independently â†’ Deploy/Demo
5. Add User Story 4/5 â†’ Test independently â†’ Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2 (legalizer share from A when ready)
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
