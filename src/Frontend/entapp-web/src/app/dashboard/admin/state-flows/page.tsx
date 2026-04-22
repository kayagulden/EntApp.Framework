"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  listFlowDefinitions,
  createFlowDefinition,
  deleteFlowDefinition,
  publishFlow,
  archiveFlow,
  createNewVersion,
  type FlowDefinitionDto,
} from "@/lib/api/state-flow";

export default function StateFlowsListPage() {
  const router = useRouter();
  const [flows, setFlows] = useState<FlowDefinitionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [includeArchived, setIncludeArchived] = useState(false);

  // Create form
  const [entityType, setEntityType] = useState("Ticket");
  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isGlobalTemplate, setIsGlobalTemplate] = useState(false);

  const loadFlows = async () => {
    setLoading(true);
    try {
      const data = await listFlowDefinitions(undefined, includeArchived);
      setFlows(data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadFlows();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [includeArchived]);

  const handleCreate = async () => {
    try {
      const id = await createFlowDefinition({
        entityType,
        key: key || `${entityType.toLowerCase()}-${Date.now()}`,
        name,
        description: description || undefined,
        isGlobalTemplate,
      });
      router.push(`/dashboard/admin/state-flows/${id}`);
    } catch (err) {
      console.error(err);
      alert("Oluşturma başarısız!");
    }
  };

  const handleAction = async (action: string, flow: FlowDefinitionDto) => {
    try {
      switch (action) {
        case "publish":
          await publishFlow(flow.id);
          break;
        case "archive":
          await archiveFlow(flow.id);
          break;
        case "new-version": {
          const newId = await createNewVersion(flow.id);
          router.push(`/dashboard/admin/state-flows/${newId}`);
          return;
        }
        case "delete":
          if (!confirm("Bu akışı silmek istediğinizden emin misiniz?")) return;
          await deleteFlowDefinition(flow.id);
          break;
      }
      await loadFlows();
    } catch (err) {
      console.error(err);
      alert(`İşlem başarısız: ${err instanceof Error ? err.message : "Bilinmeyen hata"}`);
    }
  };

  return (
    <div className="sf-list-page">
      <style jsx>{`
        .sf-list-page {
          padding: 32px;
          max-width: 1200px;
          margin: 0 auto;
          font-family: 'Inter', sans-serif;
        }
        .sf-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 24px;
        }
        .sf-header h1 {
          font-size: 24px;
          font-weight: 700;
          background: linear-gradient(135deg, #8b5cf6, #06b6d4);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }
        .sf-actions {
          display: flex;
          gap: 8px;
          align-items: center;
        }
        .sf-btn {
          padding: 8px 16px;
          border-radius: 8px;
          border: 1px solid rgba(255,255,255,0.1);
          background: rgba(255,255,255,0.05);
          color: #e0e0e0;
          cursor: pointer;
          font-size: 13px;
          font-weight: 500;
          transition: all 0.2s;
        }
        .sf-btn:hover { background: rgba(255,255,255,0.1); }
        .sf-btn--primary {
          background: linear-gradient(135deg, #8b5cf6, #7c3aed);
          border-color: transparent;
          color: #fff;
        }
        .sf-btn--primary:hover { opacity: 0.9; }
        .sf-btn--sm { padding: 4px 10px; font-size: 12px; }
        .sf-btn--danger { color: #ef4444; border-color: #ef4444; }
        .sf-btn--danger:hover { background: rgba(239,68,68,0.1); }
        .sf-checkbox {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 13px;
          color: #888;
          cursor: pointer;
        }
        .sf-table {
          width: 100%;
          border-collapse: separate;
          border-spacing: 0;
          background: rgba(255,255,255,0.02);
          border: 1px solid rgba(255,255,255,0.06);
          border-radius: 12px;
          overflow: hidden;
        }
        .sf-table th {
          text-align: left;
          padding: 12px 16px;
          font-size: 11px;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.08em;
          color: #888;
          border-bottom: 1px solid rgba(255,255,255,0.06);
          background: rgba(255,255,255,0.02);
        }
        .sf-table td {
          padding: 12px 16px;
          font-size: 13px;
          border-bottom: 1px solid rgba(255,255,255,0.04);
          color: #ccc;
        }
        .sf-table tr:last-child td { border-bottom: none; }
        .sf-table tr:hover td { background: rgba(255,255,255,0.03); }
        .sf-table a {
          color: #8b5cf6;
          text-decoration: none;
          font-weight: 600;
        }
        .sf-table a:hover { text-decoration: underline; }
        .sf-status {
          display: inline-block;
          padding: 2px 8px;
          border-radius: 999px;
          font-size: 11px;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }
        .sf-status--Draft { background: rgba(245,158,11,0.15); color: #f59e0b; }
        .sf-status--Published { background: rgba(34,197,94,0.15); color: #22c55e; }
        .sf-status--Archived { background: rgba(107,114,128,0.15); color: #6b7280; }
        .sf-empty {
          text-align: center;
          padding: 48px;
          color: #666;
          font-size: 14px;
        }
        .sf-create-dialog {
          margin-bottom: 24px;
          padding: 20px;
          background: rgba(255,255,255,0.03);
          border: 1px solid rgba(255,255,255,0.08);
          border-radius: 12px;
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 12px;
        }
        .sf-create-dialog .full { grid-column: 1 / -1; }
        .sf-input {
          width: 100%;
          padding: 8px 12px;
          border-radius: 8px;
          border: 1px solid rgba(255,255,255,0.1);
          background: rgba(0,0,0,0.3);
          color: #e0e0e0;
          font-size: 13px;
          outline: none;
        }
        .sf-input:focus { border-color: #8b5cf6; }
        .sf-input-label {
          font-size: 11px;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          color: #888;
          margin-bottom: 4px;
        }
        .td-actions {
          display: flex;
          gap: 4px;
        }
        .loading {
          text-align: center;
          padding: 48px;
          color: #888;
        }
      `}</style>

      <div className="sf-header">
        <h1>⚡ State Flow Tanımları</h1>
        <div className="sf-actions">
          <label className="sf-checkbox">
            <input
              type="checkbox"
              checked={includeArchived}
              onChange={(e) => setIncludeArchived(e.target.checked)}
            />
            Arşivlenenleri göster
          </label>
          <button className="sf-btn sf-btn--primary" onClick={() => setShowCreate(!showCreate)}>
            ＋ Yeni Akış
          </button>
        </div>
      </div>

      {showCreate && (
        <div className="sf-create-dialog">
          <div>
            <div className="sf-input-label">Entity Tipi</div>
            <input className="sf-input" value={entityType} onChange={(e) => setEntityType(e.target.value)} placeholder="Ticket" />
          </div>
          <div>
            <div className="sf-input-label">Anahtar (Key)</div>
            <input className="sf-input" value={key} onChange={(e) => setKey(e.target.value)} placeholder="ticket-default-flow" />
          </div>
          <div>
            <div className="sf-input-label">Ad</div>
            <input className="sf-input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Destek Talebi Akışı" />
          </div>
          <div>
            <div className="sf-input-label">Açıklama</div>
            <input className="sf-input" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Opsiyonel" />
          </div>
          <div className="full" style={{ display: "flex", alignItems: "center", gap: 16 }}>
            <label className="sf-checkbox">
              <input type="checkbox" checked={isGlobalTemplate} onChange={(e) => setIsGlobalTemplate(e.target.checked)} />
              Global Şablon
            </label>
            <button className="sf-btn sf-btn--primary" onClick={handleCreate} disabled={!name}>
              Oluştur
            </button>
            <button className="sf-btn" onClick={() => setShowCreate(false)}>
              İptal
            </button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="loading">Yükleniyor...</div>
      ) : flows.length === 0 ? (
        <div className="sf-empty">Henüz akış tanımı yok. &quot;Yeni Akış&quot; butonuna tıklayarak başlayın.</div>
      ) : (
        <table className="sf-table">
          <thead>
            <tr>
              <th>Ad</th>
              <th>Entity</th>
              <th>Versiyon</th>
              <th>Durum</th>
              <th>State / Geçiş</th>
              <th>Şablon</th>
              <th>İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {flows.map((flow) => (
              <tr key={flow.id}>
                <td>
                  <a href={`/dashboard/admin/state-flows/${flow.id}`}>{flow.name}</a>
                </td>
                <td>{flow.entityType}</td>
                <td>v{flow.version}</td>
                <td>
                  <span className={`sf-status sf-status--${flow.status}`}>{flow.status}</span>
                </td>
                <td>
                  {flow.stateCount} / {flow.transitionCount}
                </td>
                <td>{flow.isGlobalTemplate ? "🌐" : "—"}</td>
                <td>
                  <div className="td-actions">
                    {flow.status === "Draft" && (
                      <>
                        <button className="sf-btn sf-btn--sm" onClick={() => handleAction("publish", flow)}>Yayınla</button>
                        <button className="sf-btn sf-btn--sm sf-btn--danger" onClick={() => handleAction("delete", flow)}>Sil</button>
                      </>
                    )}
                    {flow.status === "Published" && (
                      <>
                        <button className="sf-btn sf-btn--sm" onClick={() => handleAction("new-version", flow)}>Yeni Versiyon</button>
                        <button className="sf-btn sf-btn--sm" onClick={() => handleAction("archive", flow)}>Arşivle</button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
