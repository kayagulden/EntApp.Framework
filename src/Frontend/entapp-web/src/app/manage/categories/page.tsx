"use client";

import { useState, useEffect, useCallback } from "react";

/** Strongly-typed ID unwrapper: { value: "guid" } → "guid", already string → as-is */
function unwrapId(val: unknown): string {
  if (!val) return "";
  if (typeof val === "string") return val;
  if (typeof val === "object" && val !== null && "value" in val) return String((val as { value: unknown }).value);
  return String(val);
}

// ── Types ────────────────────────────────────────────────────
interface Department {
  id: string;
  name: string;
  code: string;
}

interface SlaDefinition {
  id: string | { value: string };
  name: string;
}



interface Category {
  id: string | { value: string };
  name: string;
  code: string;
  description: string | null;
  departmentId: string | { value: string };
  department?: { name: string };
  slaDefinitionId: string | { value: string } | null;
  slaDefinitionEntity?: { name: string } | null;
  workflowDefinitionId: string | null;
  defaultQueueId: string | { value: string } | null;
  defaultQueue?: { name: string } | null;
  formSchemaJson: string | null;
  autoProjectThreshold: number | null;
  isActive: boolean;
}

interface CategoryForm {
  name: string;
  code: string;
  description: string;
  departmentId: string;
  slaDefinitionId: string;
  defaultQueueId: string;
  workflowDefinitionId: string;
  autoProjectThreshold: string;
  isActive: boolean;
}

interface StateFlowOption {
  id: string;
  name: string;
  status: string;
}

const emptyForm: CategoryForm = {
  name: "",
  code: "",
  description: "",
  departmentId: "",
  slaDefinitionId: "",
  defaultQueueId: "",
  workflowDefinitionId: "",
  autoProjectThreshold: "",
  isActive: true,
};

// ── API ──────────────────────────────────────────────────────
const REQ_API = "/api/req";
const ORG_API = "/api/v1/org";

async function fetchCategories(): Promise<Category[]> {
  const res = await fetch(`${REQ_API}/categories?activeOnly=false`);
  if (!res.ok) throw new Error("Kategoriler yüklenemedi");
  return res.json();
}

async function fetchDepartments(): Promise<Department[]> {
  const res = await fetch(`${ORG_API}/departments`);
  if (!res.ok) return [];
  return res.json();
}

async function fetchSlaDefinitions(): Promise<SlaDefinition[]> {
  const res = await fetch(`${REQ_API}/sla-definitions?activeOnly=true`);
  if (!res.ok) return [];
  return res.json();
}


async function fetchStateFlows(): Promise<StateFlowOption[]> {
  try {
    const res = await fetch(`/api/sf/flows`);
    if (!res.ok) return [];
    const data = await res.json();
    return (data.value || data || []).map((f: { id: string; name: string; status: string }) => ({
      id: f.id,
      name: f.name,
      status: f.status,
    }));
  } catch {
    return [];
  }
}

async function createCategory(form: CategoryForm): Promise<string> {
  const body = {
    name: form.name,
    code: form.code,
    departmentId: form.departmentId,
    description: form.description || null,
    slaDefinitionId: form.slaDefinitionId || null,
    workflowDefinitionId: form.workflowDefinitionId || null,
    defaultQueueId: form.defaultQueueId || null,
    autoProjectThreshold: form.autoProjectThreshold
      ? parseInt(form.autoProjectThreshold)
      : null,
  };
  const res = await fetch(`${REQ_API}/categories`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const err = await res.text();
    throw new Error(err || "Kategori oluşturulamadı");
  }
  const data = await res.json();
  return data.id;
}

async function updateCategory(id: string, form: CategoryForm): Promise<void> {
  const body = {
    name: form.name,
    code: form.code,
    departmentId: form.departmentId,
    description: form.description || null,
    slaDefinitionId: form.slaDefinitionId || null,
    workflowDefinitionId: form.workflowDefinitionId || null,
    defaultQueueId: form.defaultQueueId || null,
    autoProjectThreshold: form.autoProjectThreshold
      ? parseInt(form.autoProjectThreshold)
      : null,
  };
  const res = await fetch(`${REQ_API}/categories/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const err = await res.text();
    throw new Error(err || "Kategori güncellenemedi");
  }
}

// ── Component ────────────────────────────────────────────────
export default function CategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Lookup data
  const [departments, setDepartments] = useState<Department[]>([]);
  const [slaDefinitions, setSlaDefinitions] = useState<SlaDefinition[]>([]);
  const [stateFlows, setStateFlows] = useState<StateFlowOption[]>([]);

  // Modal
  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CategoryForm>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [cats, depts, slas, flows] = await Promise.all([
        fetchCategories(),
        fetchDepartments(),
        fetchSlaDefinitions(),
        fetchStateFlows(),
      ]);
      setCategories(cats);
      setDepartments(depts);
      setSlaDefinitions(slas);
      setStateFlows(flows);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Yükleme hatası");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  function openCreateModal() {
    setEditingId(null);
    setForm(emptyForm);
    setFormError(null);
    setModalOpen(true);
  }

  function openEditModal(cat: Category) {
    setEditingId(unwrapId(cat.id));
    setForm({
      name: cat.name,
      code: cat.code,
      description: cat.description || "",
      departmentId: unwrapId(cat.departmentId),
      slaDefinitionId: unwrapId(cat.slaDefinitionId),
      defaultQueueId: unwrapId(cat.defaultQueueId),
      workflowDefinitionId: cat.workflowDefinitionId || "",
      autoProjectThreshold: cat.autoProjectThreshold?.toString() || "",
      isActive: cat.isActive,
    });
    setFormError(null);
    setModalOpen(true);
  }

  async function handleSave() {
    if (!form.name.trim() || !form.code.trim() || !form.departmentId) {
      setFormError("Ad, Kod ve Departman zorunludur.");
      return;
    }
    setSaving(true);
    setFormError(null);
    try {
      if (editingId) {
        await updateCategory(editingId, form);
      } else {
        await createCategory(form);
      }
      setModalOpen(false);
      await loadAll();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Kaydetme hatası");
    } finally {
      setSaving(false);
    }
  }

  function getFlowName(defId: string | null): string {
    if (!defId) return "—";
    const flow = stateFlows.find((f) => f.id === defId);
    return flow?.name || defId.substring(0, 8) + "…";
  }

  function getDepartmentName(deptId: string): string {
    const dept = departments.find((d) => unwrapId(d.id) === deptId);
    return dept?.name || "—";
  }

  return (
    <div className="space-y-6">
      {/* ── Header ──────────────────────────── */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">
            Talep Kategorileri
          </h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            Talep kategorilerini, departman eşleştirmelerini ve workflow
            bağlantılarını yönetin.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={loadAll}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-[var(--color-surface-elevated)] text-[var(--color-text-muted)] hover:text-[var(--color-text)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all duration-200"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182" />
            </svg>
            Yenile
          </button>
          <button
            onClick={openCreateModal}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 hover:brightness-110 transition-all duration-200"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Yeni Kategori
          </button>
        </div>
      </div>

      {/* ── Error Banner ────────────────────── */}
      {error && (
        <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">
          <span className="font-semibold">Hata:</span> {error}
        </div>
      )}

      {/* ── Table ───────────────────────────── */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden shadow-xl shadow-black/10 overflow-x-auto">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="flex items-center gap-3 text-[var(--color-text-muted)]">
              <div className="w-5 h-5 border-2 border-teal-400/30 border-t-teal-400 rounded-full animate-spin" />
              <span className="text-sm">Kategoriler yükleniyor...</span>
            </div>
          </div>
        ) : categories.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 gap-4">
            <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-teal-500/20 to-cyan-500/10 flex items-center justify-center">
              <svg className="w-7 h-7 text-teal-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581c.699.699 1.78.872 2.607.33a18.095 18.095 0 005.223-5.223c.542-.827.369-1.908-.33-2.607L11.16 3.66A2.25 2.25 0 009.568 3z" />
              </svg>
            </div>
            <p className="text-sm text-[var(--color-text-muted)]">Henüz kategori yok.</p>
            <button
              onClick={openCreateModal}
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 transition-all"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              İlk Kategoriyi Oluştur
            </button>
          </div>
        ) : (
          <table className="w-full min-w-[700px]">
            <thead>
              <tr className="border-b border-[var(--color-border)]">
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Kategori
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Departman
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider hidden lg:table-cell">
                  Durum Akışı
                </th>
                <th className="text-center px-3 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Durum
                </th>
                <th className="text-right px-4 py-3 text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                  Aksiyonlar
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {categories.map((cat) => (
                <tr
                  key={unwrapId(cat.id)}
                  className="hover:bg-[var(--color-surface-elevated)] transition-colors"
                >
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-teal-500/20 to-cyan-500/10 flex items-center justify-center flex-shrink-0">
                        <svg className="w-4 h-4 text-teal-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581c.699.699 1.78.872 2.607.33a18.095 18.095 0 005.223-5.223c.542-.827.369-1.908-.33-2.607L11.16 3.66A2.25 2.25 0 009.568 3z" />
                        </svg>
                      </div>
                      <div className="min-w-0">
                        <span className="block text-sm font-semibold text-[var(--color-text)] truncate">
                          {cat.name}
                        </span>
                        <span className="block text-xs text-[var(--color-text-muted)] font-mono">
                          {cat.code}
                        </span>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-sm text-[var(--color-text-muted)]">
                    {cat.department?.name || getDepartmentName(unwrapId(cat.departmentId))}
                  </td>
                  <td className="px-4 py-3 text-sm hidden lg:table-cell">
                    {cat.workflowDefinitionId ? (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-violet-500/10 text-violet-400 border border-violet-500/20">
                        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z" />
                        </svg>
                        {getFlowName(cat.workflowDefinitionId)}
                      </span>
                    ) : (
                      <span className="text-[var(--color-text-muted)] text-xs">—</span>
                    )}
                  </td>
                  <td className="px-3 py-3 text-center">
                    {cat.isActive ? (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-400">
                        <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                        Aktif
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-red-500/15 text-red-400">
                        <span className="w-1.5 h-1.5 rounded-full bg-red-400" />
                        Pasif
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => openEditModal(cat)}
                      className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-teal-400 hover:text-teal-300 hover:bg-teal-500/10 transition-colors"
                    >
                      <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                      </svg>
                      Düzenle
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* ── Create/Edit Modal ────────────────── */}
      {modalOpen && (
        <>
          <div
            className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
            onClick={() => setModalOpen(false)}
          />
          <div className="fixed inset-0 z-50 flex items-start justify-center p-4 pt-[10vh] overflow-y-auto">
            <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl shadow-2xl shadow-black/30 max-w-lg w-full p-6">
              {/* Header */}
              <div className="flex items-center gap-3 mb-6">
                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-teal-500/20 to-cyan-500/10 flex items-center justify-center">
                  <svg className="w-5 h-5 text-teal-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581c.699.699 1.78.872 2.607.33a18.095 18.095 0 005.223-5.223c.542-.827.369-1.908-.33-2.607L11.16 3.66A2.25 2.25 0 009.568 3z" />
                  </svg>
                </div>
                <div>
                  <h3 className="text-lg font-bold text-[var(--color-text)]">
                    {editingId ? "Kategori Düzenle" : "Yeni Kategori"}
                  </h3>
                  <p className="text-xs text-[var(--color-text-muted)]">
                    {editingId
                      ? "Kategori bilgilerini güncelleyin"
                      : "Yeni bir talep kategorisi oluşturun"}
                  </p>
                </div>
              </div>

              {/* Form Error */}
              {formError && (
                <div className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2.5 text-sm text-red-400 mb-4">
                  {formError}
                </div>
              )}

              {/* Form */}
              <div className="space-y-4">
                {/* Name + Code */}
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                      Kategori Adı <span className="text-red-400">*</span>
                    </label>
                    <input
                      type="text"
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      placeholder="Donanım Talebi"
                      className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-teal-500/50 focus:ring-2 focus:ring-teal-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                      Kod <span className="text-red-400">*</span>
                    </label>
                    <input
                      type="text"
                      value={form.code}
                      onChange={(e) =>
                        setForm({ ...form, code: e.target.value.toUpperCase() })
                      }
                      placeholder="HW-REQ"
                      className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-teal-500/50 focus:ring-2 focus:ring-teal-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all font-mono"
                    />
                  </div>
                </div>

                {/* Description */}
                <div>
                  <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                    Açıklama
                  </label>
                  <textarea
                    value={form.description}
                    onChange={(e) =>
                      setForm({ ...form, description: e.target.value })
                    }
                    placeholder="Kategori açıklaması..."
                    rows={2}
                    className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-teal-500/50 focus:ring-2 focus:ring-teal-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none resize-none transition-all"
                  />
                </div>

                {/* Department */}
                <div>
                  <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                    Departman <span className="text-red-400">*</span>
                  </label>
                  <select
                    value={form.departmentId}
                    onChange={(e) =>
                      setForm({ ...form, departmentId: e.target.value })
                    }
                    className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-teal-500/50 focus:ring-2 focus:ring-teal-500/20 text-sm text-[var(--color-text)] outline-none transition-all [&>option]:bg-[#1e293b] [&>option]:text-white"
                  >
                    <option value="">Departman seçin...</option>
                    {departments.map((d) => (
                      <option key={unwrapId(d.id)} value={unwrapId(d.id)}>
                        {d.name}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Durum Akışı */}
                <div>
                  <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                    <span className="flex items-center gap-1.5">
                      <svg className="w-3.5 h-3.5 text-violet-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
                      </svg>
                      Durum Akışı
                    </span>
                  </label>
                  <select
                    value={form.workflowDefinitionId}
                    onChange={(e) =>
                      setForm({ ...form, workflowDefinitionId: e.target.value })
                    }
                    className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-violet-500/50 focus:ring-2 focus:ring-violet-500/20 text-sm text-[var(--color-text)] outline-none transition-all [&>option]:bg-[#1e293b] [&>option]:text-white"
                  >
                    <option value="">Akış seçin...</option>
                    {stateFlows.map((f) => (
                      <option key={f.id} value={f.id}>
                        {f.name}
                        {f.status === "Published" ? " ✓" : " (Taslak)"}
                      </option>
                    ))}
                  </select>
                  <p className="text-[10px] text-[var(--color-text-muted)] mt-1">
                    Bu kategorideki talepler oluşturulduğunda kullanılacak durum akışı.
                  </p>
                </div>

                {/* SLA */}
                <div>
                  <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                    SLA Tanımı
                  </label>
                  <select
                    value={form.slaDefinitionId}
                    onChange={(e) =>
                      setForm({ ...form, slaDefinitionId: e.target.value })
                    }
                    className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-teal-500/50 focus:ring-2 focus:ring-teal-500/20 text-sm text-[var(--color-text)] outline-none transition-all [&>option]:bg-[#1e293b] [&>option]:text-white"
                  >
                    <option value="">SLA seçin (opsiyonel)...</option>
                    {slaDefinitions.map((s) => (
                      <option key={unwrapId(s.id)} value={unwrapId(s.id)}>
                        {s.name}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Auto Project Threshold */}
                <div>
                  <label className="block text-xs font-semibold text-[var(--color-text-muted)] mb-1.5">
                    Otomatik Proje Eşiği (efor)
                  </label>
                  <input
                    type="number"
                    value={form.autoProjectThreshold}
                    onChange={(e) =>
                      setForm({ ...form, autoProjectThreshold: e.target.value })
                    }
                    placeholder="Örn: 40"
                    min={0}
                    className="w-full px-3 py-2 rounded-xl bg-[var(--color-surface-elevated)] border border-[var(--color-border)] focus:border-teal-500/50 focus:ring-2 focus:ring-teal-500/20 text-sm text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] outline-none transition-all"
                  />
                  <p className="text-[10px] text-[var(--color-text-muted)] mt-1">
                    Bu efor değerinin üzerindeki talepler otomatik proje adayı olur.
                  </p>
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 mt-6 pt-4 border-t border-[var(--color-border)]">
                <button
                  onClick={() => setModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-sm font-medium text-[var(--color-text-muted)] hover:text-[var(--color-text)] bg-[var(--color-surface-elevated)] border border-[var(--color-border)] hover:border-[var(--color-border-strong)] transition-all"
                >
                  Vazgeç
                </button>
                <button
                  onClick={handleSave}
                  disabled={saving}
                  className="px-5 py-2 rounded-xl text-sm font-semibold bg-gradient-to-r from-teal-500 to-cyan-500 text-white shadow-lg shadow-teal-500/25 hover:shadow-teal-500/40 hover:brightness-110 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {saving ? (
                    <span className="flex items-center gap-2">
                      <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                      Kaydediliyor...
                    </span>
                  ) : editingId ? (
                    "Güncelle"
                  ) : (
                    "Oluştur"
                  )}
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
