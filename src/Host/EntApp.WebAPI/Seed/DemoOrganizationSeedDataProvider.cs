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
using EntApp.Modules.RequestManagement.Domain.Ids;

namespace EntApp.WebAPI.Seed;

/// <summary>
/// Demo tenant için organizasyon, departman, hizmet kuyrukları ve üyeler seed'ler.
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
        //  2. RequestManagement Departments + Queues + Members
        // ═══════════════════════════════════════════════════════
        if (await reqDb.Departments.AnyAsync(ct))
        {
            logger.LogInformation("[SEED] RequestManagement data already seeded — skipping.");
            return;
        }

        // Departmanlar
        var reqDeptIt = ReqDept.Create("IT Hizmetleri", "IT-SVC", null);
        var reqDeptHr = ReqDept.Create("İK Hizmetleri", "HR-SVC", null);
        var reqDeptFin = ReqDept.Create("Finans Hizmetleri", "FIN-SVC", null);
        var reqDeptOps = ReqDept.Create("Operasyon Hizmetleri", "OPS-SVC", null);
        reqDb.Departments.AddRange(reqDeptIt, reqDeptHr, reqDeptFin, reqDeptOps);
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 4 RequestManagement departments created.");

        // Kuyruklar — departman bağlantılı
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

        // Demo kullanıcıları bul (IAM tablosundan)
        var users = await iamDb.Users.OrderBy(u => u.CreatedAt).Take(5).ToListAsync(ct);
        if (users.Count > 0)
        {
            logger.LogInformation("[SEED] Found {Count} users for queue membership.", users.Count);

            // İlk kullanıcıyı dispatcher olarak genel kuyruğa
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

        // Kategoriler
        reqDb.Categories.AddRange(
            RequestCategory.Create("Sistem / Network Destek Talebi", "SYS-NET-REQ", reqDeptIt.Id),
            RequestCategory.Create("Uygulama Destek Talebi", "APP-SUPPORT-REQ", reqDeptIt.Id),
            RequestCategory.Create("Yeni Özellik Talebi", "FEATURE-REQ-CAT", reqDeptIt.Id),
            RequestCategory.Create("Ad-hoc Rapor Talebi", "ADHOC-REPORT-CAT", reqDeptIt.Id),
            RequestCategory.Create("Proje Talebi", "PROJECT-REQ-CAT", reqDeptIt.Id),
            RequestCategory.Create("İzin Talebi", "HR-LEAVE-REQ", reqDeptHr.Id),
            RequestCategory.Create("İşe Alım Talebi", "HR-RECRUIT-REQ", reqDeptHr.Id),
            RequestCategory.Create("Ödeme / Fatura Talebi", "FIN-PAYMENT-REQ", reqDeptFin.Id)
        );
        await reqDb.SaveChangesAsync(ct);
        logger.LogInformation("[SEED] 8 request categories created.");

        logger.LogInformation("[SEED] Demo organization seed completed ✓");
    }
}
