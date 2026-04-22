"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  listFlowDefinitions,
  createFlowDefinition,
  deleteFlowDefinition,
  publishFlow,
  cloneFromTemplate,
  type FlowDefinitionDto,
} from "@/lib/api/state-flow";

export default function ManageStateFlowsPage() {
  const router = useRouter();
  const [flows, setFlows] = useState<FlowDefinitionDto[]>([]);
  const [templates, setTemplates] = useState<FlowDefinitionDto[]>([]);
  const [loading, setLoading] = useState(true);

  // Tabs
  const [tab, setTab] = useState<"tenant" | "templates">("tenant");

  // Create form
  const [showCreate, setShowCreate] = useState(false);
  const [entityType, setEntityType] = useState("Ticket");
  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await listFlowDefinitions(undefined, false);
      setFlows(data.filter((f) => !f.isGlobalTemplate));
      setTemplates(data.filter((f) => f.isGlobalTemplate));
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreate = async () => {
    try {
      const id = await createFlowDefinition({
        entityType,
        key: key || `${entityType.toLowerCase()}-${Date.now()}`,
        name,
        description: description || undefined,
        isGlobalTemplate: false,
      });
      router.push(`/manage/state-flows/${id}`);
    } catch (err) {
      console.error(err);
      alert("Oluşturma başarısız!");
    }
  };

  const handleCloneTemplate = async (templateId: string, templateName: string) => {
    const customName = prompt("Akış adı:", `${templateName} (Özelleştirilmiş)`);
    if (!customName) return;
    try {
      const newId = await cloneFromTemplate(templateId, customName);
      router.push(`/manage/state-flows/${newId}`);
    } catch (err) {
      console.error(err);
      alert("Şablon kopyalama başarısız!");
    }
  };

  const handlePublish = async (flow: FlowDefinitionDto) => {
    try {
      await publishFlow(flow.id);
      await loadData();
    } catch (err) {
      console.error(err);
      alert("Yayınlama başarısız!");
    }
  };

  const handleDelete = async (flow: FlowDefinitionDto) => {
    if (!confirm(`"${flow.name}" akışını silmek istediğinizden emin misiniz?`)) return;
    try {
      await deleteFlowDefinition(flow.id);
      await loadData();
    } catch (err) {
      console.error(err);
      alert("Silme başarısız!");
    }
  };

  const statusBadge = (status: string) => {
    if (status === "Published") {
      return (
        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-400 border border-emerald-500/20">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
          Aktif
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-400 border border-amber-500/20">
        <span className="w-1.5 h-1.5 rounded-full bg-amber-400" />
        Taslak
      </span>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Durum Akışları</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            Sıfırdan veya global şablonlardan akış oluşturun, tasarlayın ve yayınlayın.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={loadData}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-[var(--color-surface-elevated)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all duration-200"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182" />
            </svg>
            Yenile
          </button>
          <button
            onClick={() => setShowCreate(!showCreate)}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-violet-500 to-purple-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 hover:brightness-110 transition-all duration-200"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Sıfırdan Oluştur
          </button>
        </div>
      </div>

      {/* Create Form */}
      {showCreate && (
        <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] p-6 shadow-xl shadow-black/10">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500/20 to-purple-500/10 flex items-center justify-center">
              <svg className="w-5 h-5 text-violet-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
              </svg>
            </div>
            <div>
              <h3 className="text-sm font-bold text-[var(--color-text)]">Sıfırdan Yeni Akış</h3>
              <p className="text-xs text-[var(--color-text-muted)]">Boş bir akış oluşturun ve designer&apos;da tasarlayın</p>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Entity Tipi</label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-violet-500/50 focus:ring-2 focus:ring-violet-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                value={entityType}
                onChange={(e) => setEntityType(e.target.value)}
                placeholder="Ticket"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Anahtar (Key)</label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-violet-500/50 focus:ring-2 focus:ring-violet-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all font-mono"
                value={key}
                onChange={(e) => setKey(e.target.value)}
                placeholder="ticket-destek-flow"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Ad <span className="text-red-400">*</span></label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-violet-500/50 focus:ring-2 focus:ring-violet-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Destek Talebi Akışı"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">Açıklama</label>
              <input
                className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-violet-500/50 focus:ring-2 focus:ring-violet-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
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
              className="px-5 py-2 rounded-xl text-sm font-semibold bg-gradient-to-r from-violet-500 to-purple-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 hover:brightness-110 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Oluştur & Tasarla
            </button>
          </div>
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-1 p-1 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] w-fit">
        <button
          onClick={() => setTab("tenant")}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
            tab === "tenant"
              ? "bg-gradient-to-r from-violet-500/20 to-purple-500/10 text-violet-400 shadow-sm"
              : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
          }`}
        >
          Akışlarım ({flows.length})
        </button>
        <button
          onClick={() => setTab("templates")}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
            tab === "templates"
              ? "bg-gradient-to-r from-cyan-500/20 to-teal-500/10 text-cyan-400 shadow-sm"
              : "text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
          }`}
        >
          🌐 Şablonlardan Türet ({templates.length})
        </button>
      </div>

      {/* Content */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden shadow-xl shadow-black/10 overflow-x-auto">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="flex items-center gap-3 text-[var(--color-text-muted)]">
              <div className="w-5 h-5 border-2 border-violet-400/30 border-t-violet-400 rounded-full animate-spin" />
              <span className="text-sm">Yükleniyor...</span>
            </div>
          </div>
        ) : tab === "tenant" ? (
          /* ── Tenant Akışları ──────────────────────── */
          flows.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20 gap-4">
              <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-violet-500/20 to-cyan-500/10 flex items-center justify-center">
                <svg className="w-7 h-7 text-violet-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
                </svg>
              </div>
              <p className="text-sm text-[var(--color-text-muted)]">Henüz akış tanımı yok.</p>
              <div className="flex gap-3">
                <button
                  onClick={() => setShowCreate(true)}
                  className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-violet-500 to-purple-500 text-white shadow-lg shadow-violet-500/25 hover:shadow-violet-500/40 transition-all"
                >
                  Sıfırdan Oluştur
                </button>
                {templates.length > 0 && (
                  <button
                    onClick={() => setTab("templates")}
                    className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-cyan-500 to-teal-500 text-white shadow-lg shadow-cyan-500/25 hover:shadow-cyan-500/40 transition-all"
                  >
                    Şablondan Türet
                  </button>
                )}
              </div>
            </div>
          ) : (
            <table className="w-full min-w-[600px]">
              <thead>
                <tr className="border-b border-[var(--color-border)]">
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Akış</th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Entity</th>
                  <th className="text-center px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">State / Geçiş</th>
                  <th className="text-center px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">Durum</th>
                  <th className="text-right px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">İşlemler</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--color-border)]">
                {flows.map((flow) => (
                  <tr key={flow.id} className="hover:bg-[var(--color-surface-elevated)] transition-colors">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-violet-500/20 to-cyan-500/10 flex items-center justify-center flex-shrink-0">
                          <svg className="w-4 h-4 text-violet-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
                          </svg>
                        </div>
                        <div className="min-w-0">
                          <a href={`/manage/state-flows/${flow.id}`} className="block text-sm font-semibold text-violet-400 hover:text-violet-300 truncate transition-colors">
                            {flow.name}
                          </a>
                          <span className="block text-xs text-[var(--color-text-muted)] font-mono">{flow.key}</span>
                          {flow.sourceTemplateId && (
                            <span className="text-[10px] text-cyan-400/60">şablondan türetildi</span>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-violet-500/10 text-violet-400 border border-violet-500/20">
                        {flow.entityType}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm text-center text-[var(--color-text-muted)]">
                      {flow.stateCount} / {flow.transitionCount}
                    </td>
                    <td className="px-4 py-3 text-center">
                      {statusBadge(flow.status)}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <a
                          href={`/manage/state-flows/${flow.id}`}
                          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-violet-400 hover:text-violet-300 hover:bg-violet-500/10 transition-colors"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                          </svg>
                          Tasarla
                        </a>
                        {flow.status === "Draft" && (
                          <button onClick={() => handlePublish(flow)} className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium text-emerald-400 hover:text-emerald-300 hover:bg-emerald-500/10 transition-colors">
                            Yayınla
                          </button>
                        )}
                        <button onClick={() => handleDelete(flow)} className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium text-red-400 hover:text-red-300 hover:bg-red-500/10 transition-colors">
                          Sil
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )
        ) : (
          /* ── Global Şablonlar ────────────────────── */
          templates.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20 gap-4">
              <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-cyan-500/20 to-teal-500/10 flex items-center justify-center">
                <svg className="w-7 h-7 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 017.843 4.582M12 3a8.997 8.997 0 00-7.843 4.582m15.686 0A11.953 11.953 0 0112 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0121 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0112 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 013 12c0-1.605.42-3.113 1.157-4.418" />
                </svg>
              </div>
              <p className="text-sm text-[var(--color-text-muted)]">
                Kullanılabilir global şablon bulunamadı.
              </p>
              <p className="text-xs text-[var(--color-text-muted)]">
                Admin panelinden şablon oluşturulmalıdır.
              </p>
            </div>
          ) : (
            <div className="p-4">
              <p className="text-xs text-[var(--color-text-muted)] mb-4">
                Bir şablonu kopyalayarak tenant&apos;a özel akış oluşturun. Kopyalanan akış bağımsız olarak düzenlenebilir.
              </p>
              <div className="grid gap-3">
                {templates.map((tmpl) => (
                  <div
                    key={tmpl.id}
                    className="flex items-center justify-between px-4 py-3 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] hover:border-cyan-500/30 transition-all group"
                  >
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-cyan-500/20 to-teal-500/10 flex items-center justify-center">
                        <svg className="w-5 h-5 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
                        </svg>
                      </div>
                      <div>
                        <span className="block text-sm font-semibold text-[var(--color-text)]">{tmpl.name}</span>
                        <span className="block text-xs text-[var(--color-text-muted)]">
                          {tmpl.entityType} • {tmpl.stateCount} state, {tmpl.transitionCount} geçiş
                        </span>
                        {tmpl.description && (
                          <span className="block text-xs text-[var(--color-text-muted)] mt-0.5">{tmpl.description}</span>
                        )}
                      </div>
                    </div>
                    <button
                      onClick={() => handleCloneTemplate(tmpl.id, tmpl.name)}
                      className="inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold bg-gradient-to-r from-cyan-500 to-teal-500 text-white shadow-lg shadow-cyan-500/20 hover:shadow-cyan-500/40 hover:brightness-110 opacity-80 group-hover:opacity-100 transition-all"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                      </svg>
                      Kopyala & Özelleştir
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )
        )}
      </div>
    </div>
  );
}
