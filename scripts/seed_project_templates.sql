-- Create project_templates table
CREATE TABLE IF NOT EXISTS pm.project_templates (
    "Id" uuid NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Description" varchar(2000),
    "Icon" varchar(10),
    "Methodology" varchar(20) NOT NULL DEFAULT 'Kanban',
    "Category" varchar(30) NOT NULL DEFAULT 'General',
    "EstimationMode" varchar(20) NOT NULL DEFAULT 'StoryPoints',
    "IsBuiltIn" boolean NOT NULL DEFAULT false,
    "IsActive" boolean NOT NULL DEFAULT true,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "BoardColumnsJson" text NOT NULL DEFAULT '[]',
    "MilestonesJson" text,
    "WorkItemsJson" text,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid,
    "UpdatedAt" timestamptz,
    "UpdatedBy" uuid,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "RowVersion" bigint NOT NULL DEFAULT 0,
    CONSTRAINT pk_project_templates PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS ix_project_templates_name ON pm.project_templates ("Name");

-- Seed 5 built-in templates
INSERT INTO pm.project_templates ("Id", "Name", "Description", "Icon", "Methodology", "Category", "EstimationMode", "IsBuiltIn", "IsActive", "SortOrder", "BoardColumnsJson", "MilestonesJson", "WorkItemsJson", "TenantId")
VALUES
-- 1. Scrum Yazılım Projesi
('11111111-1111-1111-1111-111111111101', 'Scrum Yazılım Projesi', 'Sprint tabanlı yazılım geliştirme projesi. Code review ve test aşamaları dahil.', '🏃', 'Scrum', 'SoftwareDevelopment', 'StoryPoints', true, true, 1,
 '[{"name":"Backlog","order":0,"mappedStatus":"Backlog"},{"name":"Sprint Backlog","order":1,"mappedStatus":"Todo"},{"name":"Geliştirme","order":2,"mappedStatus":"InProgress","wipLimit":3},{"name":"Code Review","order":3,"mappedStatus":"InReview","wipLimit":2},{"name":"Test","order":4,"mappedStatus":"InReview"},{"name":"Tamamlandı","order":5,"mappedStatus":"Done"},{"name":"İptal","order":6,"mappedStatus":"Cancelled"}]',
 '[{"name":"Proje Kickoff","dayOffset":0,"description":"Proje başlangıç toplantısı ve takım oluşturma"},{"name":"MVP Ready","dayOffset":30,"description":"Minimum uygulanabilir ürün hazır"},{"name":"UAT Başlangıcı","dayOffset":45,"description":"Kullanıcı kabul testleri başlangıcı"},{"name":"Go-Live","dayOffset":60,"description":"Canlıya geçiş"}]',
 '[{"title":"Proje ortamını hazırla (repo, CI/CD, ortamlar)","type":"Task","priority":"High"},{"title":"Teknik tasarım dokümanı","type":"Task","priority":"High"},{"title":"Sprint 0 - Mimari spike","type":"Spike","priority":"High"}]',
 '00000000-0000-0000-0000-000000000000'),

-- 2. Kanban Yazılım Projesi
('11111111-1111-1111-1111-111111111102', 'Kanban Yazılım Projesi', 'Sürekli akış tabanlı yazılım geliştirme. WIP limitleri ile iş akışı yönetimi.', '📋', 'Kanban', 'SoftwareDevelopment', 'StoryPoints', true, true, 2,
 '[{"name":"Backlog","order":0,"mappedStatus":"Backlog"},{"name":"Yapılacak","order":1,"mappedStatus":"Todo"},{"name":"İşlemde","order":2,"mappedStatus":"InProgress","wipLimit":5},{"name":"İnceleme","order":3,"mappedStatus":"InReview","wipLimit":3},{"name":"Tamamlandı","order":4,"mappedStatus":"Done"},{"name":"İptal","order":5,"mappedStatus":"Cancelled"}]',
 '[{"name":"Proje Başlangıcı","dayOffset":0,"description":"Proje hedefleri ve kapsamın netleştirilmesi"},{"name":"İlk Release","dayOffset":21,"description":"İlk sürüm yayını"}]',
 '[{"title":"Backlog grooming ve önceliklendirme","type":"Task","priority":"High"},{"title":"Board ve WIP limitlerini yapılandır","type":"Task","priority":"Medium"}]',
 '00000000-0000-0000-0000-000000000000'),

-- 3. Altyapı Projesi
('11111111-1111-1111-1111-111111111103', 'Altyapı Projesi', 'Sistem, network veya altyapı kurulum/güncelleme projesi. Aşamalı ilerleme ile.', '⚙️', 'Waterfall', 'Infrastructure', 'Hours', true, true, 3,
 '[{"name":"Planlama","order":0,"mappedStatus":"Backlog"},{"name":"Tedarik","order":1,"mappedStatus":"Todo"},{"name":"Kurulum","order":2,"mappedStatus":"InProgress"},{"name":"Test","order":3,"mappedStatus":"InReview"},{"name":"Devreye Alma","order":4,"mappedStatus":"Done"},{"name":"Kapalı","order":5,"mappedStatus":"Cancelled"}]',
 '[{"name":"İhtiyaç Analizi Tamamlandı","dayOffset":7,"description":"Gereksinimler netleştirildi"},{"name":"Tedarik Onayı","dayOffset":14,"description":"Donanım/yazılım siparişi verildi"},{"name":"Kurulum Tamamlandı","dayOffset":30,"description":"Fiziksel/sanal kurulum bitti"},{"name":"Kabul Testi Geçildi","dayOffset":40,"description":"Fonksiyonel testler başarılı"},{"name":"Devreye Alma","dayOffset":45,"description":"Prodüksiyona geçiş"}]',
 '[{"title":"İhtiyaç analizi ve kapsam belgesi","type":"Task","priority":"High"},{"title":"Risk değerlendirmesi","type":"Task","priority":"High"},{"title":"Rollback planı hazırla","type":"Task","priority":"Medium"}]',
 '00000000-0000-0000-0000-000000000000'),

-- 4. İş / Organizasyonel Proje
('11111111-1111-1111-1111-111111111104', 'İş / Organizasyonel Proje', 'İş süreçleri, organizasyonel değişim veya strateji projesi.', '🏢', 'Kanban', 'Business', 'None', true, true, 4,
 '[{"name":"Fikir","order":0,"mappedStatus":"Backlog"},{"name":"Analiz","order":1,"mappedStatus":"Todo"},{"name":"Uygulama","order":2,"mappedStatus":"InProgress"},{"name":"Değerlendirme","order":3,"mappedStatus":"InReview"},{"name":"Tamamlandı","order":4,"mappedStatus":"Done"}]',
 '[{"name":"Proje Onayı","dayOffset":0,"description":"Yönetim onayı alındı"},{"name":"Analiz Raporu","dayOffset":14,"description":"Mevcut durum analizi tamamlandı"},{"name":"Pilot Uygulama","dayOffset":30,"description":"Pilot süreç başlatıldı"},{"name":"Yaygınlaştırma","dayOffset":60,"description":"Tüm organizasyona yayılım"}]',
 '[{"title":"Paydaş analizi","type":"Task","priority":"High"},{"title":"Mevcut durum değerlendirmesi","type":"Task","priority":"High"},{"title":"Hedef süreç tasarımı","type":"Task","priority":"Medium"}]',
 '00000000-0000-0000-0000-000000000000'),

-- 5. Boş Proje
('11111111-1111-1111-1111-111111111105', 'Boş Proje', 'Sıfırdan başlayın — varsayılan board kolonları ile minimal yapı.', '📄', 'Kanban', 'General', 'StoryPoints', true, true, 5,
 '[{"name":"Bekleyenler","order":0,"mappedStatus":"Backlog"},{"name":"Yapılacak","order":1,"mappedStatus":"Todo"},{"name":"İşlemde","order":2,"mappedStatus":"InProgress"},{"name":"İnceleme","order":3,"mappedStatus":"InReview"},{"name":"Tamamlandı","order":4,"mappedStatus":"Done"},{"name":"İptal","order":5,"mappedStatus":"Cancelled"}]',
 NULL, NULL,
 '00000000-0000-0000-0000-000000000000')
ON CONFLICT ON CONSTRAINT pk_project_templates DO NOTHING;
