using EntApp.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Aliases to avoid ambiguity
using IamDb = EntApp.Modules.IAM.Infrastructure.Persistence.IamDbContext;
using IamOrg = EntApp.Modules.IAM.Domain.Entities.Organization;
using IamDept = EntApp.Modules.IAM.Domain.Entities.Department;
using IamUser = EntApp.Modules.IAM.Domain.Entities.User;
using ReqDb = EntApp.Modules.RequestManagement.Infrastructure.Persistence.RequestManagementDbContext;
using ReqDept = EntApp.Modules.RequestManagement.Domain.Entities.Department;
using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;

namespace EntApp.WebAPI.Seed;

/// <summary>
/// Demo tenant için organizasyon, departman, hizmet kuyrukları, SLA, kategoriler,
/// queue routing bağlantıları ve örnek ticket'lar seed'ler.
/// </summary>
public sealed class DemoOrganizationSeedDataProvider : ISeedDataProvider
{
    public int Order => 200;
    public string Name => "Demo:OrganizationAndQueues";

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DemoOrganizationSeedDataProvider>>();
        var iamDb = scope.ServiceProvider.GetRequiredService<IamDb>();
        var reqDb = scope.ServiceProvider.GetRequiredService<ReqDb>();

        // ═══════════════════════════════════════════════════════
        //  1. Organization + IAM Departments + Demo Users
        // ═══════════════════════════════════════════════════════
        if (!await iamDb.Organizations.AnyAsync(ct))
        {
            logger.LogInformation("[SEED] Creating demo organization structure...");

            var rootOrg = IamOrg.Create("EntApp Demo Şirketi", "ENTAPP");
            iamDb.Organizations.Add(rootOrg);

            var ist = IamOrg.Create("İstanbul Şubesi", "IST", rootOrg.Id);
            var ank = IamOrg.Create("Ankara Şubesi", "ANK", rootOrg.Id);
            iamDb.Organizations.AddRange(ist, ank);

            var deptIt = IamDept.Create("Bilgi Teknolojileri", "IT", rootOrg.Id);
            var deptHr = IamDept.Create("İnsan Kaynakları", "HR", rootOrg.Id);
            var deptFin = IamDept.Create("Finans", "FIN", rootOrg.Id);
            var deptSales = IamDept.Create("Satış & Pazarlama", "SALES", rootOrg.Id);
            var deptOps = IamDept.Create("Operasyon", "OPS", rootOrg.Id);
            var deptLegal = IamDept.Create("Hukuk", "LEGAL", rootOrg.Id);
            iamDb.Departments.AddRange(deptIt, deptHr, deptFin, deptSales, deptOps, deptLegal);

            await iamDb.SaveChangesAsync(ct);
            logger.LogInformation("[SEED] Organization + 6 departments created.");

            // Demo kullanıcılar
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
        //  2. RequestManagement — Full Seed
        // ═══════════════════════════════════════════════════════
        if (await reqDb.Departments.AnyAsync(ct))
        {
            logger.LogInformation("[SEED] RequestManagement data already seeded — skipping.");
            return;
        }

        // ─── 2a. SLA Tanımları ────────────────────────────────
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

        // ─── 2b. Departmanlar ─────────────────────────────────
        // Not: DefaultQueueId sonradan set edilecek (queue oluşturulduktan sonra)
        var reqDeptIt = ReqDept.Create("IT Hizmetleri", "IT-SVC", "Bilgi teknolojileri hizmet masası");
        var reqDeptHr = ReqDept.Create("İK Hizmetleri", "HR-SVC", "İnsan kaynakları talep yönetimi");
        var reqDeptFin = ReqDept.Create("Finans Hizmetleri", "FIN-SVC", "Finans ve muhasebe talepleri");
        var reqDeptOps = ReqDept.Create("Operasyon Hizmetleri", "OPS-SVC", "Operasyon ve lojistik talepleri");
        reqDb.Departments.AddRange(reqDeptIt, reqDeptHr, reqDeptFin, reqDeptOps);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 4 RequestManagement departments created.");

        // ─── 2c. Service Queue'lar ────────────────────────────
        var qGeneral = ServiceQueue.Create("Genel Destek", "GENERAL-SUPPORT",
            "Tüm gelen taleplerin ilk düştüğü genel destek kuyruğu", null, null, null);
        var qSysNet = ServiceQueue.Create("Sistem / Network Destek", "SYS-NET",
            "Sunucu, ağ, altyapı ve sistem yönetimi talepleri", reqDeptIt.Id, null, null);
        var qAppSupport = ServiceQueue.Create("Uygulama Destek", "APP-SUPPORT",
            "Mevcut uygulamalardaki sorunlar ve kullanıcı destek talepleri", reqDeptIt.Id, null, null);
        var qFeature = ServiceQueue.Create("Yeni Özellik / Geliştirme", "FEATURE-REQ",
            "Yeni özellik, iyileştirme ve uygulama geliştirme talepleri", reqDeptIt.Id, null, null);
        var qReport = ServiceQueue.Create("Ad-hoc Rapor", "ADHOC-REPORT",
            "Anlık rapor, veri çekme ve analiz talepleri", reqDeptIt.Id, null, null);
        var qProject = ServiceQueue.Create("Proje Talebi", "PROJECT-REQ",
            "Yeni proje başlatma, proje değerlendirme ve PMO talepleri", null, null, null);
        var qHr = ServiceQueue.Create("İK Talepleri", "HR-REQUESTS",
            "İzin, özlük, işe alım ve diğer İK talepleri", reqDeptHr.Id, null, null);
        var qFin = ServiceQueue.Create("Finans Talepleri", "FIN-REQUESTS",
            "Ödeme, fatura, masraf ve bütçe talepleri", reqDeptFin.Id, null, null);

        reqDb.ServiceQueues.AddRange(qGeneral, qSysNet, qAppSupport, qFeature, qReport, qProject, qHr, qFin);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 8 service queues created.");

        // ─── 2d. Departman → DefaultQueue bağlantısı ──────────
        reqDeptIt.Update(reqDeptIt.Name, reqDeptIt.Code, reqDeptIt.Description,
            reqDeptIt.ManagerUserId, reqDeptIt.ParentDepartmentId, qGeneral.Id);
        reqDeptHr.Update(reqDeptHr.Name, reqDeptHr.Code, reqDeptHr.Description,
            reqDeptHr.ManagerUserId, reqDeptHr.ParentDepartmentId, qHr.Id);
        reqDeptFin.Update(reqDeptFin.Name, reqDeptFin.Code, reqDeptFin.Description,
            reqDeptFin.ManagerUserId, reqDeptFin.ParentDepartmentId, qFin.Id);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] Department → DefaultQueue links set.");

        // ─── 2e. Queue Membership ─────────────────────────────
        var users = await iamDb.Users.OrderBy(u => u.CreatedAt).Take(5).ToListAsync(ct);
        if (users.Count > 0)
        {
            logger.LogInformation("[SEED] Found {Count} users for queue membership.", users.Count);

            // Ahmet Yılmaz — IT Lead, Genel Destek dispatcher
            reqDb.QueueMemberships.Add(QueueMembership.Create(qGeneral.Id, users[0].Id, "Dispatcher"));
            reqDb.QueueMemberships.Add(QueueMembership.Create(qSysNet.Id, users[0].Id, "Lead"));
            reqDb.QueueMemberships.Add(QueueMembership.Create(qAppSupport.Id, users[0].Id, "Lead"));

            if (users.Count > 1)
            {
                // Elif Demir — IT Member
                reqDb.QueueMemberships.Add(QueueMembership.Create(qGeneral.Id, users[1].Id, "Member"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qSysNet.Id, users[1].Id, "Member"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qAppSupport.Id, users[1].Id, "Member"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qFeature.Id, users[1].Id, "Member"));
            }
            if (users.Count > 2)
            {
                // Mehmet Kaya — İK, Rapor Lead
                reqDb.QueueMemberships.Add(QueueMembership.Create(qReport.Id, users[2].Id, "Lead"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qHr.Id, users[2].Id, "Member"));
            }
            if (users.Count > 3)
            {
                // Ayşe Çelik — İK Lead, Finans Lead
                reqDb.QueueMemberships.Add(QueueMembership.Create(qHr.Id, users[3].Id, "Lead"));
                reqDb.QueueMemberships.Add(QueueMembership.Create(qFin.Id, users[3].Id, "Lead"));
            }
            if (users.Count > 4)
            {
                // Can Öztürk — Proje Lead, Finans Member
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

        // ─── 2f. Kategoriler (SLA + DefaultQueue bağlantılı) ──
        var catSysNet = RequestCategory.Create("Sistem / Network Destek Talebi", "SYS-NET-REQ", reqDeptIt.Id,
            "Sunucu, ağ, VPN, firewall ve altyapı sorunları",
            slaStandard.Id, defaultQueueId: qSysNet.Id);

        var catAppSupport = RequestCategory.Create("Uygulama Destek Talebi", "APP-SUPPORT-REQ", reqDeptIt.Id,
            "Mevcut uygulamalardaki hatalar, erişim sorunları ve kullanıcı destek",
            slaStandard.Id, defaultQueueId: qAppSupport.Id);

        var catFeature = RequestCategory.Create("Yeni Özellik Talebi", "FEATURE-REQ-CAT", reqDeptIt.Id,
            "Yeni fonksiyon, iyileştirme ve değişiklik talepleri", 
            slaStandard.Id, defaultQueueId: qFeature.Id);

        var catReport = RequestCategory.Create("Ad-hoc Rapor Talebi", "ADHOC-REPORT-CAT", reqDeptIt.Id,
            "Anlık rapor, veri export ve analiz talepleri",
            slaStandard.Id, defaultQueueId: qReport.Id);

        var catProject = RequestCategory.Create("Proje Talebi", "PROJECT-REQ-CAT", reqDeptIt.Id,
            "Yeni proje başlatma, fizibilite ve PMO talepleri",
            slaStandard.Id, autoProjectThreshold: 40, defaultQueueId: qProject.Id);

        var catLeave = RequestCategory.Create("İzin Talebi", "HR-LEAVE-REQ", reqDeptHr.Id,
            "Yıllık izin, mazeret izni, ücretsiz izin talepleri",
            slaHr.Id, defaultQueueId: qHr.Id);

        var catRecruit = RequestCategory.Create("İşe Alım Talebi", "HR-RECRUIT-REQ", reqDeptHr.Id,
            "Yeni pozisyon açma, işe alım süreci başlatma",
            slaHr.Id, defaultQueueId: qHr.Id);

        var catPayment = RequestCategory.Create("Ödeme / Fatura Talebi", "FIN-PAYMENT-REQ", reqDeptFin.Id,
            "Tedarikçi ödemesi, fatura onayı, masraf talepleri",
            slaStandard.Id, defaultQueueId: qFin.Id);

        var catAccess = RequestCategory.Create("Erişim / Yetki Talebi", "ACCESS-REQ", reqDeptIt.Id,
            "Sistem erişimi, rol değişikliği, yetki talebi",
            slaPremium.Id, defaultQueueId: qAppSupport.Id);

        var catHardware = RequestCategory.Create("Donanım Talebi", "HARDWARE-REQ", reqDeptIt.Id,
            "Bilgisayar, monitör, yazıcı ve diğer donanım talepleri",
            slaStandard.Id, defaultQueueId: qGeneral.Id);

        reqDb.Categories.AddRange(catSysNet, catAppSupport, catFeature, catReport, catProject,
            catLeave, catRecruit, catPayment, catAccess, catHardware);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 10 request categories created (with SLA + DefaultQueue links).");

        // ─── 2g. Demo Ticket'lar ──────────────────────────────
        if (users.Count < 2)
        {
            logger.LogWarning("[SEED] Not enough users for demo tickets — skipping.");
            return;
        }

        var demoTickets = new List<Ticket>();

        // Ticket 1: Çözülmüş — VPN Sorunu (CategoryDefault routing)
        var t1 = Ticket.Create("REQ-0001", "VPN bağlantısı kurulamıyor",
            catSysNet.Id, reqDeptIt.Id, users[0].Id,
            "Uzaktan çalışma sırasında VPN bağlantısı 'Authentication failed' hatası veriyor. İşletim sistemi: Windows 11.",
            TicketPriority.High, TicketChannel.Portal,
            serviceQueueId: qSysNet.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t1.Assign(users[1].Id);
        t1.RecordFirstResponse();
        t1.SetSlaDeadlines(DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(6));
        t1.ChangeStatus(TicketStatus.Resolved, users[1].Id, "VPN profili yeniden oluşturuldu, sorun giderildi.");
        demoTickets.Add(t1);

        // Ticket 2: Açık — Uygulama Hatası (CategoryDefault routing)
        var t2 = Ticket.Create("REQ-0002", "ERP raporları yüklenmiyor",
            catAppSupport.Id, reqDeptIt.Id, users[2].Id,
            "Finans modülündeki aylık rapor sayfası 'timeout' hatası veriyor. Saat 09:00-10:00 arası yoğunlukta oluşuyor.",
            TicketPriority.Critical, TicketChannel.Email,
            serviceQueueId: qAppSupport.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t2.Assign(users[0].Id);
        t2.RecordFirstResponse();
        t2.SetSlaDeadlines(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(3));
        t2.ChangeStatus(TicketStatus.InProgress, users[0].Id, "Veritabanı sorgu optimizasyonu yapılıyor.");
        demoTickets.Add(t2);

        // Ticket 3: Yeni — Özellik talebi (CategoryDefault routing)
        var t3 = Ticket.Create("REQ-0003", "Dashboard'a gerçek zamanlı grafik eklenmesi",
            catFeature.Id, reqDeptIt.Id, users[3].Id,
            "Yönetim dashboard'unda satış verilerinin gerçek zamanlı olarak güncellenen grafiklerde gösterilmesi isteniyor.",
            TicketPriority.Low, TicketChannel.Portal,
            serviceQueueId: qFeature.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t3.SetSlaDeadlines(DateTime.UtcNow.AddHours(8), DateTime.UtcNow.AddDays(2));
        demoTickets.Add(t3);

        // Ticket 4: Bilgi bekleniyor — Rapor (CategoryDefault routing)
        var t4 = Ticket.Create("REQ-0004", "Müşteri bazlı satış analiz raporu",
            catReport.Id, reqDeptIt.Id, users[3].Id,
            "Son 6 aylık müşteri bazlı satış dağılımı, ürün kategorilerine göre kırılımlı rapor isteniyor.",
            TicketPriority.Medium, TicketChannel.Internal,
            serviceQueueId: qReport.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t4.Assign(users[2].Id);
        t4.RecordFirstResponse();
        t4.ChangeStatus(TicketStatus.WaitingForInfo, users[2].Id, "Hangi müşteri segmentleri dahil edilecek? CRM'deki segment tanımlarını belirtir misiniz?");
        demoTickets.Add(t4);

        // Ticket 5: İzin talebi — İK (CategoryDefault routing)
        var t5 = Ticket.Create("REQ-0005", "Yıllık izin talebi — 14-18 Nisan",
            catLeave.Id, reqDeptHr.Id, users[0].Id,
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

        // Ticket 6: Ödeme talebi — Finans (CategoryDefault routing)
        var t6 = Ticket.Create("REQ-0006", "Tedarikçi fatura ödemesi — ABC Ltd.",
            catPayment.Id, reqDeptFin.Id, users[4].Id,
            "ABC Ltd. Şti. tarafından kesilen 15.000 TL tutarındaki fatura için ödeme talebi. Fatura no: ABC-2026-0142.",
            TicketPriority.Medium, TicketChannel.Portal,
            serviceQueueId: qFin.Id, routingSource: TicketRoutingSource.CategoryDefault);
        demoTickets.Add(t6);

        // Ticket 7: Erişim talebi — Premium SLA (CategoryDefault routing)
        var t7 = Ticket.Create("REQ-0007", "Üretim veritabanına salt okunur erişim",
            catAccess.Id, reqDeptIt.Id, users[2].Id,
            "İK raporları için üretim veritabanına salt okunur erişim gerekiyor. Kapsam: hr schema.",
            TicketPriority.High, TicketChannel.Portal,
            serviceQueueId: qAppSupport.Id, routingSource: TicketRoutingSource.CategoryDefault);
        t7.Assign(users[0].Id);
        t7.RecordFirstResponse();
        t7.ChangeStatus(TicketStatus.InProgress, users[0].Id, "DBA onayı bekleniyor.");
        demoTickets.Add(t7);

        // Ticket 8: Donanım talebi — DepartmentDefault routing (queue: general)
        var t8 = Ticket.Create("REQ-0008", "Yeni monitör talebi — 27\" 4K",
            catHardware.Id, reqDeptIt.Id, users[1].Id,
            "Mevcut 22\" monitör yerine 27\" 4K monitör talep ediyorum. Geliştirme çalışmaları için gerekli.",
            TicketPriority.Low, TicketChannel.Portal,
            serviceQueueId: qGeneral.Id, routingSource: TicketRoutingSource.CategoryDefault);
        demoTickets.Add(t8);

        // Ticket 9: Manuel route edilmiş — dispatcher tarafından
        var t9 = Ticket.Create("REQ-0009", "E-posta sunucusu performans sorunu",
            catSysNet.Id, reqDeptIt.Id, users[3].Id,
            "Exchange sunucusu yavaş yanıt veriyor, e-posta gönderimi 30+ saniye sürüyor.",
            TicketPriority.Urgent, TicketChannel.Phone,
            serviceQueueId: qSysNet.Id, routingSource: TicketRoutingSource.Manual);
        t9.Assign(users[0].Id);
        t9.RecordFirstResponse();
        t9.SetSlaDeadlines(DateTime.UtcNow.AddMinutes(30), DateTime.UtcNow.AddHours(2));
        t9.ChangeStatus(TicketStatus.Escalated, users[0].Id, "Microsoft desteğine eskalasyon yapıldı.");
        demoTickets.Add(t9);

        // Ticket 10: Unrouted — queue bağlantısı olmayan senaryo
        var t10 = Ticket.Create("REQ-0010", "Ofis klima bakım talebi",
            catSysNet.Id, reqDeptOps.Id, users[4].Id,
            "3. kat toplantı odasının kliması çalışmıyor, bakım gerekiyor.",
            TicketPriority.Low, TicketChannel.Chat);
        // Bilinçli olarak queue atanmadı — Unrouted durumda
        demoTickets.Add(t10);

        reqDb.Tickets.AddRange(demoTickets);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 10 demo tickets created (various statuses, priorities, routing sources).");

        logger.LogInformation("[SEED] Demo organization seed completed ✓");
    }
}
