"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  listFlowDefinitions,
  createFlowDefinition,
  deleteFlowDefinition,
  type FlowDefinitionDto,
} from "@/lib/api/state-flow";

export default function AdminStateFlowTemplatesPage() {
  const router = useRouter();
  const [templates, setTemplates] = useState<FlowDefinitionDto[]>([]);
  const [loading, setLoading] = useState(true);

  // Create form
  const [showCreate, setShowCreate] = useState(false);
  const [entityType, setEntityType] = useState("Ticket");
  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  const loadTemplates = async () => {
    setLoading(true);
    try {
      const data = await listFlowDefinitions(undefined, true);
      // Admin sadece global şablonları görür
      setTemplates(data.filter((f) => f.isGlobalTemplate));
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTemplates();
  }, []);

  const handleCreate = async () => {
    try {
      const id = await createFlowDefinition({
        entityType,
        key: key || `${entityType.toLowerCase()}-template-${Date.now()}`,
        name,
        description: description || undefined,
        isGlobalTemplate: true,
      });
      router.push(`/admin/state-flows/${id}`);
    } catch (err) {
      console.error(err);
      alert("Şablon oluşturulamadı!");
    }
  };

  const handleDelete = async (flow: FlowDefinitionDto) => {
    if (!confirm(`"${flow.name}" şablonunu silmek istediğinizden emin misiniz?`)) return;
    try {
      await deleteFlowDefinition(flow.id);
      await loadTemplates();
    } catch (err) {
      console.error(err);
      alert("Silme başarısız!");
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">
            Akış Şablonları
          </h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            Tüm tenant&apos;ların kullanabileceği global durum akışı şablonlarını tanımlayın.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={loadTemplates}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-[var(--color-surface-elevated)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all duration-200"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182" />
            </svg>
            Yenile
          </button>
          <button
            onClick={() => setShowCreate(!showCreate)}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-amber-500 to-orange-500 text-white shadow-lg shadow-amber-500/25 hover:shadow-amber-500/40 hover:brightness-110 transition-all duration-200"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Yeni Şablon
          </button>
        </div>
      </div>

      {/* Info Banner */}
      <div className="rounded-xl border border-amber-500/20 bg-amber-500/5 px-4 py-3 text-sm text-amber-300/80 flex items-start gap-3">
        <svg className="w-5 h-5 text-amber-400 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
        </svg>
        <span>
          Burada tanımlanan şablonlar, tenant yönetimi ekranından kopyalanarak özelleştirilebilir.
          Şablonlar doğrudan ticket&apos;lara atanmaz — tenant&apos;lar kendi akışlarını şablondan türetir.
        </span>
      </div>

      {/* Create Form */}
      {showCreate && (
        <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6 shadow-xl shadow-black/10">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-amber-500/20 to-orange-500/10 flex items-center justify-center">
              <svg className="w-5 h-5 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
              </svg>
            </div>
            <div>
              <h3 className="text-sm font-bold text-[var(--color-text)]">Yeni Şablon Oluştur</h3>
              <p className="text-xs text-[var(--color-text-muted)]">Tenant&apos;ların kullanabileceği bir temel akış tanımlayın</p>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Entity Tipi</label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-amber-500/50 focus:ring-2 focus:ring-amber-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                value={entityType}
                onChange={(e) => setEntityType(e.target.value)}
                placeholder="Ticket"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Anahtar (Key)</label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-amber-500/50 focus:ring-2 focus:ring-amber-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all font-mono"
                value={key}
                onChange={(e) => setKey(e.target.value)}
                placeholder="ticket-default-template"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Şablon Adı <span className="text-red-400">*</span></label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-amber-500/50 focus:ring-2 focus:ring-amber-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Destek Talebi Varsayılan Akışı"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Açıklama</label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-amber-500/50 focus:ring-2 focus:ring-amber-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Opsiyonel"
              />
            </div>
          </div>
          <div className="flex items-center justify-end gap-3 mt-4 pt-4 border-t border-[var(--color-border)]">
            <button onClick={() => setShowCreate(false)} className="px-4 py-2 rounded-xl text-sm font-medium text-[var(--color-text-muted)] hover:text-[var(--color-text)] bg-[var(--color-surface-elevated)] border border-[var(--color-border)] transition-all">
              İptal
            </button>
            <button
              onClick={handleCreate}
              disabled={!name}
              className="px-5 py-2 rounded-xl text-sm font-semibold bg-gradient-to-r from-amber-500 to-orange-500 text-white shadow-lg shadow-amber-500/25 hover:shadow-amber-500/40 hover:brightness-110 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Oluştur & Tasarla
            </button>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden shadow-xl shadow-black/10 overflow-x-auto">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="flex items-center gap-3 text-[var(--color-text-muted)]">
              <div className="w-5 h-5 border-2 border-amber-400/30 border-t-amber-400 rounded-full animate-spin" />
              <span className="text-sm">Şablonlar yükleniyor...</span>
            </div>
          </div>
        ) : templates.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 gap-4">
            <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-amber-500/20 to-orange-500/10 flex items-center justify-center">
              <svg className="w-7 h-7 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
              </svg>
            </div>
            <p className="text-sm text-[var(--color-text-muted)]">Henüz şablon tanımlı değil.</p>
            <button
              onClick={() => setShowCreate(true)}
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-amber-500 to-orange-500 text-white shadow-lg shadow-amber-500/25 hover:shadow-amber-500/40 transition-all"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              İlk Şablonu Oluştur
            </button>
          </div>
        ) : (
          <table className="w-full min-w-[600px]">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Şablon</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Entity</th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">State / Geçiş</th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Oluşturulma</th>
                <th className="text-right px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {templates.map((flow) => (
                <tr key={flow.id} className="hover:bg-[var(--color-surface-elevated)] transition-colors">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-amber-500/20 to-orange-500/10 flex items-center justify-center flex-shrink-0">
                        <svg className="w-4 h-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
                        </svg>
                      </div>
                      <div className="min-w-0">
                        <a
                          href={`/admin/state-flows/${flow.id}`}
                          className="block text-sm font-semibold text-amber-400 hover:text-amber-300 truncate transition-colors"
                        >
                          {flow.name}
                        </a>
                        <span className="block text-xs text-[var(--color-text-muted)] font-mono">{flow.key}</span>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-violet-500/10 text-violet-400 border border-violet-500/20">
                      {flow.entityType}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-sm text-center text-[var(--color-text-muted)]">
                    {flow.stateCount} / {flow.transitionCount}
                  </td>
                  <td className="px-4 py-3 text-xs text-center text-[var(--color-text-muted)]">
                    {new Date(flow.createdAt).toLocaleDateString("tr-TR")}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-1.5">
                      <a
                        href={`/admin/state-flows/${flow.id}`}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-amber-400 hover:text-amber-300 hover:bg-amber-500/10 transition-colors"
                      >
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                        </svg>
                        Tasarla
                      </a>
                      <button
                        onClick={() => handleDelete(flow)}
                        className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium text-red-400 hover:text-red-300 hover:bg-red-500/10 transition-colors"
                      >
                        Sil
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
