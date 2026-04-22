-- State Flow Engine — Ticket Varsayılan Akış Seed Data
-- Tarih: 2026-04-22
-- Bu script idempotent: tekrar çalıştırılabilir (ON CONFLICT DO NOTHING)

-- ═══════════════════════════════════════════════════════════════
--  1. Flow Definition: Destek Talebi Varsayılan Akışı
-- ═══════════════════════════════════════════════════════════════

-- Sabit GUID'ler (tekrarlanabilirlik için)
-- Flow:  00000000-0000-0000-0000-000000000001
-- States: 00000000-0000-0000-0001-0000000000XX
-- Transitions: 00000000-0000-0000-0002-0000000000XX

INSERT INTO sf.state_flow_definitions (
    "Id", "EntityType", "Key", "Name", "Description",
    "Version", "Status", "PublishedAt", "IsGlobalTemplate",
    "TenantId", "CreatedAt", "IsDeleted"
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Ticket',
    'ticket-default-flow',
    'Destek Talebi Varsayılan Akışı',
    'Ticket (destek talebi) için standart durum akışı. New → WaitForAssignment → InProgress → Resolved → Closed.',
    1,
    'Published',
    now(),
    true,
    '00000000-0000-0000-0000-000000000000',
    now(),
    false
) ON CONFLICT ("Id") DO NOTHING;


-- ═══════════════════════════════════════════════════════════════
--  2. State Definitions (10 state)
-- ═══════════════════════════════════════════════════════════════

INSERT INTO sf.state_definitions (
    "Id", "FlowDefinitionId", "Name", "Label", "Color", "Icon",
    "IsInitial", "IsTerminal", "IsPaused", "Category",
    "PositionX", "PositionY", "SortOrder", "CreatedAt", "IsDeleted"
) VALUES
    -- New (▶ Initial)
    ('00000000-0000-0000-0001-000000000001', '00000000-0000-0000-0000-000000000001',
     'New', 'Yeni', '#3b82f6', 'plus-circle',
     true, false, false, 'Active',
     0, 200, 0, now(), false),

    -- WaitForAssignment
    ('00000000-0000-0000-0001-000000000002', '00000000-0000-0000-0000-000000000001',
     'WaitForAssignment', 'Atama Bekliyor', '#8b5cf6', 'user-plus',
     false, false, false, 'Waiting',
     220, 200, 1, now(), false),

    -- Open
    ('00000000-0000-0000-0001-000000000003', '00000000-0000-0000-0000-000000000001',
     'Open', 'Açık', '#06b6d4', 'folder-open',
     false, false, false, 'Active',
     440, 200, 2, now(), false),

    -- InProgress
    ('00000000-0000-0000-0001-000000000004', '00000000-0000-0000-0000-000000000001',
     'InProgress', 'İşlemde', '#f59e0b', 'play-circle',
     false, false, false, 'Active',
     660, 200, 3, now(), false),

    -- WaitingForInfo (⏸ Paused)
    ('00000000-0000-0000-0001-000000000005', '00000000-0000-0000-0000-000000000001',
     'WaitingForInfo', 'Bilgi Bekliyor', '#a855f7', 'clock',
     false, false, true, 'Waiting',
     660, 50, 4, now(), false),

    -- Escalated
    ('00000000-0000-0000-0001-000000000006', '00000000-0000-0000-0000-000000000001',
     'Escalated', 'Eskalasyon', '#ef4444', 'arrow-up-circle',
     false, false, false, 'Active',
     660, 350, 5, now(), false),

    -- AllTasksDone
    ('00000000-0000-0000-0001-000000000007', '00000000-0000-0000-0000-000000000001',
     'AllTasksDone', 'Görevler Tamamlandı', '#14b8a6', 'check-square',
     false, false, false, 'Active',
     880, 200, 6, now(), false),

    -- Resolved
    ('00000000-0000-0000-0001-000000000008', '00000000-0000-0000-0000-000000000001',
     'Resolved', 'Çözüldü', '#22c55e', 'check-circle',
     false, false, false, 'Active',
     1100, 200, 7, now(), false),

    -- Closed (■ Terminal)
    ('00000000-0000-0000-0001-000000000009', '00000000-0000-0000-0000-000000000001',
     'Closed', 'Kapatıldı', '#6b7280', 'x-circle',
     false, true, false, 'Closed',
     1320, 200, 8, now(), false),

    -- Cancelled (■ Terminal)
    ('00000000-0000-0000-0001-000000000010', '00000000-0000-0000-0000-000000000001',
     'Cancelled', 'İptal Edildi', '#991b1b', 'slash',
     false, true, false, 'Closed',
     660, 500, 9, now(), false)

ON CONFLICT ("Id") DO NOTHING;


-- ═══════════════════════════════════════════════════════════════
--  3. Transition Definitions (16 transition)
-- ═══════════════════════════════════════════════════════════════

INSERT INTO sf.transition_definitions (
    "Id", "FlowDefinitionId",
    "FromStateName", "ToStateName", "TriggerName", "Label",
    "RequiredRole", "SortOrder", "CreatedAt", "IsDeleted"
) VALUES
    -- New → WaitForAssignment
    ('00000000-0000-0000-0002-000000000001', '00000000-0000-0000-0000-000000000001',
     'New', 'WaitForAssignment', 'Submit', 'Gönder',
     NULL, 0, now(), false),

    -- WaitForAssignment → Open
    ('00000000-0000-0000-0002-000000000002', '00000000-0000-0000-0000-000000000001',
     'WaitForAssignment', 'Open', 'Assign', 'Atama Yap',
     'Agent', 1, now(), false),

    -- Open → InProgress
    ('00000000-0000-0000-0002-000000000003', '00000000-0000-0000-0000-000000000001',
     'Open', 'InProgress', 'StartWork', 'İşleme Al',
     'Agent', 2, now(), false),

    -- InProgress → WaitingForInfo
    ('00000000-0000-0000-0002-000000000004', '00000000-0000-0000-0000-000000000001',
     'InProgress', 'WaitingForInfo', 'RequestInfo', 'Bilgi İste',
     'Agent', 3, now(), false),

    -- WaitingForInfo → InProgress
    ('00000000-0000-0000-0002-000000000005', '00000000-0000-0000-0000-000000000001',
     'WaitingForInfo', 'InProgress', 'ProvideInfo', 'Bilgi Sağla',
     NULL, 4, now(), false),

    -- InProgress → Escalated
    ('00000000-0000-0000-0002-000000000006', '00000000-0000-0000-0000-000000000001',
     'InProgress', 'Escalated', 'Escalate', 'Eskalasyon',
     'Agent', 5, now(), false),

    -- Escalated → InProgress
    ('00000000-0000-0000-0002-000000000007', '00000000-0000-0000-0000-000000000001',
     'Escalated', 'InProgress', 'DeEscalate', 'Eskalasyonu Kaldır',
     'Manager', 6, now(), false),

    -- InProgress → AllTasksDone
    ('00000000-0000-0000-0002-000000000008', '00000000-0000-0000-0000-000000000001',
     'InProgress', 'AllTasksDone', 'CompleteAllTasks', 'Tüm Görevleri Tamamla',
     NULL, 7, now(), false),

    -- AllTasksDone → Resolved
    ('00000000-0000-0000-0002-000000000009', '00000000-0000-0000-0000-000000000001',
     'AllTasksDone', 'Resolved', 'Resolve', 'Çöz',
     'Agent', 8, now(), false),

    -- InProgress → Resolved
    ('00000000-0000-0000-0002-000000000010', '00000000-0000-0000-0000-000000000001',
     'InProgress', 'Resolved', 'Resolve', 'Çöz',
     'Agent', 9, now(), false),

    -- Resolved → Closed
    ('00000000-0000-0000-0002-000000000011', '00000000-0000-0000-0000-000000000001',
     'Resolved', 'Closed', 'Close', 'Kapat',
     NULL, 10, now(), false),

    -- Resolved → Open (Reopen)
    ('00000000-0000-0000-0002-000000000012', '00000000-0000-0000-0000-000000000001',
     'Resolved', 'Open', 'Reopen', 'Yeniden Aç',
     NULL, 11, now(), false),

    -- Closed → Open (Reopen)
    ('00000000-0000-0000-0002-000000000013', '00000000-0000-0000-0000-000000000001',
     'Closed', 'Open', 'Reopen', 'Yeniden Aç',
     NULL, 12, now(), false),

    -- Cancel transitions: New, Open, InProgress, WaitingForInfo, Escalated → Cancelled
    ('00000000-0000-0000-0002-000000000014', '00000000-0000-0000-0000-000000000001',
     'New', 'Cancelled', 'Cancel', 'İptal Et',
     NULL, 13, now(), false),

    ('00000000-0000-0000-0002-000000000015', '00000000-0000-0000-0000-000000000001',
     'Open', 'Cancelled', 'Cancel', 'İptal Et',
     NULL, 14, now(), false),

    ('00000000-0000-0000-0002-000000000016', '00000000-0000-0000-0000-000000000001',
     'InProgress', 'Cancelled', 'Cancel', 'İptal Et',
     'Manager', 15, now(), false),

    ('00000000-0000-0000-0002-000000000017', '00000000-0000-0000-0000-000000000001',
     'WaitingForInfo', 'Cancelled', 'Cancel', 'İptal Et',
     'Manager', 16, now(), false),

    ('00000000-0000-0000-0002-000000000018', '00000000-0000-0000-0000-000000000001',
     'Escalated', 'Cancelled', 'Cancel', 'İptal Et',
     'Manager', 17, now(), false)

ON CONFLICT ("Id") DO NOTHING;
