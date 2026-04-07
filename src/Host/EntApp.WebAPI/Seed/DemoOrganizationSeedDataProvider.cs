using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using IamDb = EntApp.Modules.IAM.Infrastructure.Persistence.IamDbContext;
using IamUser = EntApp.Modules.IAM.Domain.Entities.User;
using ReqDb = EntApp.Modules.RequestManagement.Infrastructure.Persistence.RequestManagementDbContext;
using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;

namespace EntApp.WebAPI.Seed;

/// <summary>
/// Demo tenant için organizasyon, departman, hizmet kuyrukları, SLA, kategoriler,
/// queue routing bağlantıları ve örnek ticket'lar seed'ler.
/// Tek kaynak: org schema → tüm modüller bu departmanları kullanır.
/// </summary>
public sealed class DemoOrganizationSeedDataProvider : ISeedDataProvider
{
    public int Order => 200;
    public string Name => "Demo:OrganizationAndQueues";

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoOrganizationSeedDataProvider>>();
        var orgDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var iamDb = scope.ServiceProvider.GetRequiredService<IamDb>();
        var reqDb = scope.ServiceProvider.GetRequiredService<ReqDb>();

        // ═══════════════════════════════════════════════════════
        //  1. Organization + Departments (Shared — org schema)
        // ═══════════════════════════════════════════════════════
        Organization rootOrg;
        Department deptIt, deptHr, deptFin, deptSales, deptOps, deptLegal;

        if (!await orgDb.Organizations.AnyAsync(ct))
        {
            logger.LogInformation("[SEED] Creating demo organization + departments (org schema)...");

            rootOrg = Organization.Create("EntApp Demo Şirketi", "ENTAPP");
            orgDb.Organizations.Add(rootOrg);

            var ist = Organization.Create("İstanbul Şubesi", "IST", rootOrg.Id);
            var ank = Organization.Create("Ankara Şubesi", "ANK", rootOrg.Id);
            orgDb.Organizations.AddRange(ist, ank);

            // Departmanlar — tek kaynak, tüm modüller tarafından kullanılır
            deptIt = Department.Create("Bilgi Teknolojileri", "IT", "IT hizmet masası ve altyapı yönetimi", rootOrg.Id);
            deptHr = Department.Create("İnsan Kaynakları", "HR", "İK talep ve süreç yönetimi", rootOrg.Id);
            deptFin = Department.Create("Finans", "FIN", "Finans ve muhasebe talepleri", rootOrg.Id);
            deptSales = Department.Create("Satış & Pazarlama", "SALES", "Satış operasyonları", rootOrg.Id);
            deptOps = Department.Create("Operasyon", "OPS", "Operasyon ve lojistik", rootOrg.Id);
            deptLegal = Department.Create("Hukuk", "LEGAL", "Hukuk danışmanlık hizmetleri", rootOrg.Id);
            orgDb.Departments.AddRange(deptIt, deptHr, deptFin, deptSales, deptOps, deptLegal);

            await orgDb.SaveChangesAsync(ct);
            logger.LogInformation("[SEED] Organization + 6 departments created (org schema).");
        }
        else
        {
            rootOrg = (await orgDb.Organizations.FirstAsync(o => o.Code == "ENTAPP", ct))!;
            var depts = await orgDb.Departments.ToListAsync(ct);
            deptIt = depts.First(d => d.Code == "IT");
            deptHr = depts.First(d => d.Code == "HR");
            deptFin = depts.First(d => d.Code == "FIN");
            deptSales = depts.First(d => d.Code == "SALES");
            deptOps = depts.First(d => d.Code == "OPS");
            deptLegal = depts.First(d => d.Code == "LEGAL");
        }

        // ═══════════════════════════════════════════════════════
        //  2. IAM — Demo Users
        // ═══════════════════════════════════════════════════════
        if (!await iamDb.Users.AnyAsync(ct))
        {
            logger.LogInformation("[SEED] Creating demo users...");

            var u1 = IamUser.Create("kc-demo-001", "ahmet.yilmaz", "ahmet.yilmaz@entapp.demo", "Ahmet", "Yılmaz", "+905551001001");
            u1.AssignToOrganization(rootOrg.Id, deptIt.Id);
            var u2 = IamUser.Create("kc-demo-002", "elif.demir", "elif.demir@entapp.demo", "Elif", "Demir", "+905551001002");
            u2.AssignToOrganization(rootOrg.Id, deptIt.Id);
            var u3 = IamUser.Create("kc-demo-003", "mehmet.kaya", "mehmet.kaya@entapp.demo", "Mehmet", "Kaya", "+905551001003");
            u3.AssignToOrganization(rootOrg.Id, deptHr.Id);
            var u4 = IamUser.Create("kc-demo-004", "ayse.celik", "ayse.celik@entapp.demo", "Ayşe", "Çelik", "+905551001004");
            u4.AssignToOrganization(rootOrg.Id, deptFin.Id);
            var u5 = IamUser.Create("kc-demo-005", "can.ozturk", "can.ozturk@entapp.demo", "Can", "Öztürk", "+905551001005");
            u5.AssignToOrganization(rootOrg.Id, deptOps.Id);

            iamDb.Users.AddRange(u1, u2, u3, u4, u5);
            await iamDb.SaveChangesAsync(ct);
            logger.LogInformation("[SEED] 5 demo users created.");
        }

        // ═══════════════════════════════════════════════════════
        //  3. RequestManagement — SLA, Queue, Category, Tickets
        // ═══════════════════════════════════════════════════════
        if (await reqDb.SlaDefinitions.AnyAsync(ct))
        {
            logger.LogInformation("[SEED] RequestManagement data already seeded — skipping.");
            return;
        }

        // ─── 3a. SLA Tanımları ────────────────────────────────
        var slaStandard = SlaDefinition.Create(
            "Standart SLA",
            "Genel talepler için standart yanıt ve çözüm süreleri",
            """{"Low":480,"Medium":240,"High":120,"Critical":60,"Urgent":30}""",
            """{"Low":2880,"Medium":1440,"High":480,"Critical":240,"Urgent":120}""");

        var slaPremium = SlaDefinition.Create(
            "Premium SLA",
            "Kritik sistemler ve VIP kullanıcılar için hızlandırılmış SLA",
            """{"Low":240,"Medium":120,"High":60,"Critical":30,"Urgent":15}""",
            """{"Low":1440,"Medium":480,"High":240,"Critical":120,"Urgent":60}""");

        var slaHr = SlaDefinition.Create(
            "İK SLA",
            "İnsan kaynakları talepleri için özel SLA süreleri",
            """{"Low":1440,"Medium":480,"High":240,"Critical":120,"Urgent":60}""",
            """{"Low":10080,"Medium":4320,"High":2880,"Critical":1440,"Urgent":480}""");

        reqDb.SlaDefinitions.AddRange(slaStandard, slaPremium, slaHr);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 3 SLA definitions created.");

        // ─── 3b. Service Queue'lar ────────────────────────────
        // Departmanlar artık org schema'sında — DepartmentId ile referans veriyoruz
        var qGeneral = ServiceQueue.Create("Genel Destek", "GENERAL-SUPPORT",
            "Tüm gelen taleplerin ilk düştüğü genel destek kuyruğu", null, null, null);
        var qSysNet = ServiceQueue.Create("Sistem / Network Destek", "SYS-NET",
            "Sunucu, ağ, altyapı ve sistem yönetimi talepleri", deptIt.Id, null, null);
        var qAppSupport = ServiceQueue.Create("Uygulama Destek", "APP-SUPPORT",
            "Mevcut uygulamalardaki sorunlar ve kullanıcı destek talepleri", deptIt.Id, null, null);
        var qFeature = ServiceQueue.Create("Yeni Özellik / Geliştirme", "FEATURE-REQ",
            "Yeni özellik, iyileştirme ve uygulama geliştirme talepleri", deptIt.Id, null, null);
        var qReport = ServiceQueue.Create("Ad-hoc Rapor", "ADHOC-REPORT",
            "Anlık rapor, veri çekme ve analiz talepleri", deptIt.Id, null, null);
        var qProject = ServiceQueue.Create("Proje Talebi", "PROJECT-REQ",
            "Yeni proje başlatma, proje değerlendirme ve PMO talepleri", null, null, null);
        var qHr = ServiceQueue.Create("İK Talepleri", "HR-REQUESTS",
            "İzin, özlük, işe alım ve diğer İK talepleri", deptHr.Id, null, null);
        var qFin = ServiceQueue.Create("Finans Talepleri", "FIN-REQUESTS",
            "Ödeme, fatura, masraf ve bütçe talepleri", deptFin.Id, null, null);

        reqDb.ServiceQueues.AddRange(qGeneral, qSysNet, qAppSupport, qFeature, qReport, qProject, qHr, qFin);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 8 service queues created.");

        // ─── 3c. Departman → DefaultQueue bağlantısı ──────────
        // org schema'daki departmanları güncelle — DefaultQueueId set et
        deptIt.Update(deptIt.Name, deptIt.Code, deptIt.Description, deptIt.ManagerUserId,
            deptIt.ParentDepartmentId, deptIt.OrganizationId, qGeneral.Id.Value);
        deptHr.Update(deptHr.Name, deptHr.Code, deptHr.Description, deptHr.ManagerUserId,
            deptHr.ParentDepartmentId, deptHr.OrganizationId, qHr.Id.Value);
        deptFin.Update(deptFin.Name, deptFin.Code, deptFin.Description, deptFin.ManagerUserId,
            deptFin.ParentDepartmentId, deptFin.OrganizationId, qFin.Id.Value);
        await orgDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] Department → DefaultQueue links set.");

        // ─── 3d. Queue Membership ─────────────────────────────
        var users = await iamDb.Users.OrderBy(u => u.CreatedAt).Take(5).ToListAsync(ct);
        if (users.Count > 0)
        {
            logger.LogInformation("[SEED] Found {Count} users for queue membership.", users.Count);

            reqDb.QueueMemberships.Add(QueueMembership.Create(qGeneral.Id, users[0].Id, "Dispatcher"));
            reqDb.QueueMemberships.Add(QueueMembership.Create(qSysNet.Id, users[0].Id, "Lead"));
            reqDb.QueueMemberships.Add(QueueMembership.Create(qAppSupport.Id, users[0].Id, "Lead"));

            if (users.Count > 1)
            {
                reqDb.QueueMemberships.Add(QueueMembership.Create(qGeneral.Id, users[1].Id, "Member"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qSysNet.Id, users[1].Id, "Member"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qAppSupport.Id, users[1].Id, "Member"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qFeature.Id, users[1].Id, "Member"));
            }
            if (users.Count > 2)
            {
                reqDb.QueueMemberships.Add(QueueMembership.Create(qReport.Id, users[2].Id, "Lead"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qHr.Id, users[2].Id, "Member"));
            }
            if (users.Count > 3)
            {
                reqDb.QueueMemberships.Add(QueueMembership.Create(qHr.Id, users[3].Id, "Lead"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qFin.Id, users[3].Id, "Lead"));
            }
            if (users.Count > 4)
            {
                reqDb.QueueMemberships.Add(QueueMembership.Create(qProject.Id, users[4].Id, "Lead"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qFin.Id, users[4].Id, "Member"));
            }

            await reqDb.SaveChangesAsync(ct);
            logger.LogInformation("[SEED] Queue memberships created.");
        }
        else
        {
            logger.LogWarning("[SEED] No IAM users found — skipping queue membership seed.");
        }

        // ─── 3e. Kategoriler (SLA + DefaultQueue bağlantılı) ──
        var catSysNet = RequestCategory.Create("Sistem / Network Destek Talebi", "SYS-NET-REQ", deptIt.Id,
            "Sunucu, ağ, VPN, firewall ve altyapı sorunları",
            slaStandard.Id, defaultQueueId: qSysNet.Id);

        var catAppSupport = RequestCategory.Create("Uygulama Destek Talebi", "APP-SUPPORT-REQ", deptIt.Id,
            "Mevcut uygulamalardaki hatalar, erişim sorunları ve kullanıcı destek",
            slaStandard.Id, defaultQueueId: qAppSupport.Id);

        var catFeature = RequestCategory.Create("Yeni Özellik Talebi", "FEATURE-REQ-CAT", deptIt.Id,
            "Yeni fonksiyon, iyileştirme ve değişiklik talepleri",
            slaStandard.Id, defaultQueueId: qFeature.Id);

        var catReport = RequestCategory.Create("Ad-hoc Rapor Talebi", "ADHOC-REPORT-CAT", deptIt.Id,
            "Anlık rapor, veri export ve analiz talepleri",
            slaStandard.Id, defaultQueueId: qReport.Id);

        var catProject = RequestCategory.Create("Proje Talebi", "PROJECT-REQ-CAT", deptIt.Id,
            "Yeni proje başlatma, fizibilite ve PMO talepleri",
            slaStandard.Id, autoProjectThreshold: 40, defaultQueueId: qProject.Id);

        var catLeave = RequestCategory.Create("İzin Talebi", "HR-LEAVE-REQ", deptHr.Id,
            "Yıllık izin, mazeret izni, ücretsiz izin talepleri",
            slaHr.Id, defaultQueueId: qHr.Id);

        var catRecruit = RequestCategory.Create("İşe Alım Talebi", "HR-RECRUIT-REQ", deptHr.Id,
            "Yeni pozisyon açma, işe alım süreci başlatma",
            slaHr.Id, defaultQueueId: qHr.Id);

        var catPayment = RequestCategory.Create("Ödeme / Fatura Talebi", "FIN-PAYMENT-REQ", deptFin.Id,
            "Tedarikçi ödemesi, fatura onayı, masraf talepleri",
            slaStandard.Id, defaultQueueId: qFin.Id);

        var catAccess = RequestCategory.Create("Erişim / Yetki Talebi", "ACCESS-REQ", deptIt.Id,
            "Sistem erişimi, rol değişikliği, yetki talebi",
            slaPremium.Id, defaultQueueId: qAppSupport.Id);

        var catHardware = RequestCategory.Create("Donanım Talebi", "HARDWARE-REQ", deptIt.Id,
            "Bilgisayar, monitör, yazıcı ve diğer donanım talepleri",
            slaStandard.Id, defaultQueueId: qGeneral.Id);

        reqDb.Categories.AddRange(catSysNet, catAppSupport, catFeature, catReport, catProject,
            catLeave, catRecruit, catPayment, catAccess, catHardware);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 10 request categories created (with SLA + DefaultQueue links).");

        // ─── 3f. Demo Ticket'lar ──────────────────────────────
        if (users.Count < 2)
        {
            logger.LogWarning("[SEED] Not enough users for demo tickets — skipping.");
            return;
        }

        var demoTickets = new List<Ticket>();

        var t1 = Ticket.Create("REQ-0001", "VPN bağlantısı kurulamıyor",
            catSysNet.Id, deptIt.Id, users[0].Id,
            "Uzaktan çalışma sırasında VPN bağlantısı 'Authentication failed' hatası veriyor.",
            TicketPriority.High, TicketChannel.Portal,
            serviceQueueId: qSysNet.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t1.Assign(users[1].Id);
        t1.RecordFirstResponse();
        t1.SetSlaDeadlines(DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(6));
        t1.ChangeStatus(TicketStatus.Resolved, users[1].Id, "VPN profili yeniden oluşturuldu, sorun giderildi.");
        demoTickets.Add(t1);

        var t2 = Ticket.Create("REQ-0002", "ERP raporları yüklenmiyor",
            catAppSupport.Id, deptIt.Id, users[2].Id,
            "Finans modülündeki aylık rapor sayfası 'timeout' hatası veriyor.",
            TicketPriority.Critical, TicketChannel.Email,
            serviceQueueId: qAppSupport.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t2.Assign(users[0].Id);
        t2.RecordFirstResponse();
        t2.SetSlaDeadlines(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(3));
        t2.ChangeStatus(TicketStatus.InProgress, users[0].Id, "Veritabanı sorgu optimizasyonu yapılıyor.");
        demoTickets.Add(t2);

        var t3 = Ticket.Create("REQ-0003", "Dashboard'a gerçek zamanlı grafik eklenmesi",
            catFeature.Id, deptIt.Id, users[3].Id,
            "Yönetim dashboard'unda satış verilerinin gerçek zamanlı gösterilmesi isteniyor.",
            TicketPriority.Low, TicketChannel.Portal,
            serviceQueueId: qFeature.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t3.SetSlaDeadlines(DateTime.UtcNow.AddHours(8), DateTime.UtcNow.AddDays(2));
        demoTickets.Add(t3);

        var t4 = Ticket.Create("REQ-0004", "Müşteri bazlı satış analiz raporu",
            catReport.Id, deptIt.Id, users[3].Id,
            "Son 6 aylık müşteri bazlı satış dağılımı raporu isteniyor.",
            TicketPriority.Medium, TicketChannel.Internal,
            serviceQueueId: qReport.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t4.Assign(users[2].Id);
        t4.RecordFirstResponse();
        t4.ChangeStatus(TicketStatus.WaitingForInfo, users[2].Id, "Hangi müşteri segmentleri dahil edilecek?");
        demoTickets.Add(t4);

        var t5 = Ticket.Create("REQ-0005", "Yıllık izin talebi — 14-18 Nisan",
            catLeave.Id, deptHr.Id, users[0].Id,
            "14-18 Nisan tarihleri arasında 5 günlük yıllık izin talep ediyorum.",
            TicketPriority.Low, TicketChannel.Portal,
            serviceQueueId: qHr.Id, routingSource: TicketRoutingSource.CategoryDefault);
        if (users.Count > 3)
        {
            t5.Assign(users[3].Id);
            t5.RecordFirstResponse();
            t5.ChangeStatus(TicketStatus.Resolved, users[3].Id, "İzin onaylandı.");
            t5.ChangeStatus(TicketStatus.Closed, users[3].Id);
        }
        demoTickets.Add(t5);

        var t6 = Ticket.Create("REQ-0006", "Tedarikçi fatura ödemesi — ABC Ltd.",
            catPayment.Id, deptFin.Id, users[4].Id,
            "ABC Ltd. fatura ödemesi: 15.000 TL. Fatura no: ABC-2026-0142.",
            TicketPriority.Medium, TicketChannel.Portal,
            serviceQueueId: qFin.Id, routingSource: TicketRoutingSource.CategoryDefault);
        demoTickets.Add(t6);

        var t7 = Ticket.Create("REQ-0007", "Üretim veritabanına salt okunur erişim",
            catAccess.Id, deptIt.Id, users[2].Id,
            "İK raporları için üretim veritabanına salt okunur erişim gerekiyor.",
            TicketPriority.High, TicketChannel.Portal,
            serviceQueueId: qAppSupport.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t7.Assign(users[0].Id);
        t7.RecordFirstResponse();
        t7.ChangeStatus(TicketStatus.InProgress, users[0].Id, "DBA onayı bekleniyor.");
        demoTickets.Add(t7);

        var t8 = Ticket.Create("REQ-0008", "Yeni monitör talebi — 27\" 4K",
            catHardware.Id, deptIt.Id, users[1].Id,
            "27\" 4K monitör talep ediyorum.",
            TicketPriority.Low, TicketChannel.Portal,
            serviceQueueId: qGeneral.Id, routingSource: TicketRoutingSource.CategoryDefault);
        demoTickets.Add(t8);

        var t9 = Ticket.Create("REQ-0009", "E-posta sunucusu performans sorunu",
            catSysNet.Id, deptIt.Id, users[3].Id,
            "Exchange sunucusu yavaş yanıt veriyor.",
            TicketPriority.Urgent, TicketChannel.Phone,
            serviceQueueId: qSysNet.Id, routingSource: TicketRoutingSource.Manual);
        t9.Assign(users[0].Id);
        t9.RecordFirstResponse();
        t9.SetSlaDeadlines(DateTime.UtcNow.AddMinutes(30), DateTime.UtcNow.AddHours(2));
        t9.ChangeStatus(TicketStatus.Escalated, users[0].Id, "Microsoft desteğine eskalasyon yapıldı.");
        demoTickets.Add(t9);

        var t10 = Ticket.Create("REQ-0010", "Ofis klima bakım talebi",
            catSysNet.Id, deptOps.Id, users[4].Id,
            "3. kat toplantı odasının kliması çalışmıyor, bakım gerekiyor.",
            TicketPriority.Low, TicketChannel.Chat);
        demoTickets.Add(t10);

        reqDb.Tickets.AddRange(demoTickets);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 10 demo tickets created (various statuses, priorities, routing sources).");

        logger.LogInformation("[SEED] Demo organization seed completed ✓");
    }
}
