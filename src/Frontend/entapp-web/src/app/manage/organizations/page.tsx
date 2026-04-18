"use client";

import { useState, useEffect, useCallback } from "react";
import {
  Building2,
  Plus,
  ChevronRight,
  ChevronDown,
  Pencil,
  Loader2,
  FolderTree,
  Users,
  X,
  Save,
  Trash2,
  UserCircle,
  Inbox,
  AlertTriangle,
} from "lucide-react";
import { cn } from "@/lib/utils";

// ── Types ──────────────────────────────────────────────

interface OrgNode {
  id: string;
  name: string;
  code: string;
  parentId: string | null;
  isActive: boolean;
  children: OrgNode[];
}

interface DeptData {
  id: string;
  name: string;
  code: string;
  description: string | null;
  organizationId: string | null;
  managerUserId: string | null;
  parentDepartmentId: string | null;
  defaultQueueId: string | null;
  isActive: boolean;
  subDepartments?: { id: string; name: string; code: string; isActive: boolean }[];
}

interface UserOption {
  id: string;
  fullName: string;
  email: string;
}

interface QueueOption {
  queueId: string;
  name: string;
  code: string;
}

// ── Helpers ────────────────────────────────────────────

function extractId(id: { value: string } | string | undefined): string {
  if (!id) return "";
  if (typeof id === "string") return id;
  return id.value;
}

// ── Main Component ─────────────────────────────────────

export default function OrganizationsPage() {
  // Data
  const [orgs, setOrgs] = useState<OrgNode[]>([]);
  const [depts, setDepts] = useState<DeptData[]>([]);
  const [users, setUsers] = useState<UserOption[]>([]);
  const [queues, setQueues] = useState<QueueOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Selection
  const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null);
  const [selectedDeptId, setSelectedDeptId] = useState<string | null>(null);

  // Tree expand state
  const [expandedOrgs, setExpandedOrgs] = useState<Set<string>>(new Set());

  // Create/Edit modals
  const [showOrgForm, setShowOrgForm] = useState(false);
  const [editingOrg, setEditingOrg] = useState<OrgNode | null>(null);
  const [showDeptForm, setShowDeptForm] = useState(false);
  const [editingDept, setEditingDept] = useState<DeptData | null>(null);

  // Form state
  const [formName, setFormName] = useState("");
  const [formCode, setFormCode] = useState("");
  const [formDesc, setFormDesc] = useState("");
  const [formManagerUserId, setFormManagerUserId] = useState("");
  const [formParentDeptId, setFormParentDeptId] = useState("");
  const [formDefaultQueueId, setFormDefaultQueueId] = useState("");
  const [formSaving, setFormSaving] = useState(false);
  const [formError, setFormError] = useState("");

  // ── Fetch all data ───────────────────────────────────

  const fetchAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [orgRes, deptRes] = await Promise.all([
        fetch("/api/v1/org/organizations/tree"),
        fetch("/api/v1/org/departments?activeOnly=false"),
      ]);

      if (orgRes.ok) {
        const orgData = await orgRes.json();
        setOrgs(Array.isArray(orgData) ? orgData : []);
      }
      if (deptRes.ok) {
        const deptData = await deptRes.json();
        setDepts(Array.isArray(deptData) ? deptData : []);
      }
    } catch (err) {
      setError("Veriler yüklenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch users & queues for dropdowns (lazy)
  const fetchDropdownData = useCallback(async () => {
    try {
      const [userRes, queueRes] = await Promise.all([
        fetch("/api/v1/iam/users"),
        fetch("/api/req/my-queues/00000000-0000-0000-0000-000000000000").catch(() => null),
      ]);
      if (userRes.ok) {
        const userData = await userRes.json();
        const items = Array.isArray(userData) ? userData : userData?.items ?? [];
        setUsers(items.map((u: any) => ({
          id: extractId(u.id),
          fullName: u.fullName || `${u.firstName} ${u.lastName}`,
          email: u.email,
        })));
      }
      // Queues — try listing all
      const qRes = await fetch("/api/req/queues");
      if (qRes?.ok) {
        const qData = await qRes.json();
        const items = Array.isArray(qData) ? qData : qData?.items ?? [];
        setQueues(items.map((q: any) => ({
          queueId: extractId(q.id),
          name: q.name,
          code: q.code,
        })));
      }
    } catch { /* silent */ }
  }, []);

  useEffect(() => {
    fetchAll();
    fetchDropdownData();
  }, [fetchAll, fetchDropdownData]);

  // ── Tree toggle ──────────────────────────────────────

  const toggleOrg = (id: string) => {
    setExpandedOrgs((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  // ── Org selection ────────────────────────────────────

  const handleSelectOrg = (id: string) => {
    setSelectedOrgId(id);
    setSelectedDeptId(null);
  };

  // ── Org CRUD ─────────────────────────────────────────

  const openCreateOrg = () => {
    setEditingOrg(null);
    setFormName("");
    setFormCode("");
    setFormError("");
    setShowOrgForm(true);
  };

  const openEditOrg = (org: OrgNode) => {
    setEditingOrg(org);
    setFormName(org.name);
    setFormCode(org.code);
    setFormError("");
    setShowOrgForm(true);
  };

  const handleSaveOrg = async () => {
    if (!formName.trim() || !formCode.trim()) {
      setFormError("Ad ve kod zorunludur.");
      return;
    }
    setFormSaving(true);
    setFormError("");
    try {
      if (editingOrg) {
        // Update — API yok, org entity'de Update metodu var ama endpoint yok
        // Şimdilik skip
        setFormError("Organizasyon güncelleme API'si henüz mevcut değil.");
        setFormSaving(false);
        return;
      }

      const res = await fetch("/api/v1/org/organizations", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formName.trim(),
          code: formCode.trim(),
          parentId: selectedOrgId || null,
        }),
      });
      if (res.ok) {
        setShowOrgForm(false);
        await fetchAll();
      } else {
        const err = await res.text();
        setFormError(err.substring(0, 200));
      }
    } catch {
      setFormError("Bağlantı hatası.");
    } finally {
      setFormSaving(false);
    }
  };

  // ── Dept CRUD ────────────────────────────────────────

  const openCreateDept = () => {
    setEditingDept(null);
    setFormName("");
    setFormCode("");
    setFormDesc("");
    setFormManagerUserId("");
    setFormParentDeptId("");
    setFormDefaultQueueId("");
    setFormError("");
    setShowDeptForm(true);
  };

  const openEditDept = (dept: DeptData) => {
    setEditingDept(dept);
    setFormName(dept.name);
    setFormCode(dept.code);
    setFormDesc(dept.description ?? "");
    setFormManagerUserId(dept.managerUserId ?? "");
    setFormParentDeptId(dept.parentDepartmentId ?? "");
    setFormDefaultQueueId(dept.defaultQueueId ?? "");
    setFormError("");
    setShowDeptForm(true);
  };

  const handleSaveDept = async () => {
    if (!formName.trim() || !formCode.trim()) {
      setFormError("Ad ve kod zorunludur.");
      return;
    }
    setFormSaving(true);
    setFormError("");
    try {
      const payload = {
        name: formName.trim(),
        code: formCode.trim(),
        description: formDesc.trim() || null,
        organizationId: selectedOrgId || null,
        managerUserId: formManagerUserId || null,
        parentDepartmentId: formParentDeptId || null,
        defaultQueueId: formDefaultQueueId || null,
      };

      let res: Response;
      if (editingDept) {
        res = await fetch(`/api/v1/org/departments/${editingDept.id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        });
      } else {
        res = await fetch("/api/v1/org/departments", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        });
      }

      if (res.ok || res.status === 204) {
        setShowDeptForm(false);
        await fetchAll();
      } else {
        const err = await res.text();
        setFormError(err.substring(0, 200));
      }
    } catch {
      setFormError("Bağlantı hatası.");
    } finally {
      setFormSaving(false);
    }
  };

  // ── Derived data ─────────────────────────────────────

  const orgDepts = selectedOrgId
    ? depts.filter((d) => d.organizationId === selectedOrgId)
    : [];

  const selectedDept = selectedDeptId
    ? depts.find((d) => d.id === selectedDeptId)
    : null;

  const findUserName = (userId: string | null) => {
    if (!userId) return "—";
    const u = users.find((u) => u.id === userId);
    return u?.fullName ?? userId.slice(0, 8) + "...";
  };

  const findQueueName = (queueId: string | null) => {
    if (!queueId) return "—";
    const q = queues.find((q) => q.queueId === queueId);
    return q?.name ?? queueId.slice(0, 8) + "...";
  };

  const selectedOrgName = (() => {
    const findOrg = (nodes: OrgNode[], id: string): OrgNode | null => {
      for (const n of nodes) {
        if (n.id === id) return n;
        const found = findOrg(n.children, id);
        if (found) return found;
      }
      return null;
    };
    return selectedOrgId ? findOrg(orgs, selectedOrgId) : null;
  })();

  // ── Org Tree Renderer ───────────────────────────────

  const renderOrgTree = (nodes: OrgNode[], depth = 0) =>
    nodes.map((org) => {
      const isExpanded = expandedOrgs.has(org.id);
      const isSelected = selectedOrgId === org.id;
      const hasChildren = org.children && org.children.length > 0;

      return (
        <div key={org.id}>
          <div
            className={cn(
              "flex items-center gap-2 px-3 py-2.5 rounded-xl cursor-pointer transition-all duration-200 group",
              isSelected
                ? "bg-gradient-to-r from-amber-500/15 to-orange-500/10 text-amber-300 shadow-sm"
                : "text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-card-bg)]"
            )}
            style={{ paddingLeft: `${depth * 20 + 12}px` }}
            onClick={() => handleSelectOrg(org.id)}
          >
            {/* Expand/collapse toggle */}
            <button
              onClick={(e) => { e.stopPropagation(); if (hasChildren) toggleOrg(org.id); }}
              className={cn("p-0.5 rounded", hasChildren ? "hover:bg-white/10" : "invisible")}
            >
              {isExpanded ? (
                <ChevronDown className="w-3.5 h-3.5" />
              ) : (
                <ChevronRight className="w-3.5 h-3.5" />
              )}
            </button>

            {/* Icon */}
            <div className={cn(
              "w-7 h-7 rounded-lg flex items-center justify-center shrink-0",
              isSelected
                ? "bg-gradient-to-br from-amber-500 to-orange-500 shadow-md shadow-amber-500/20"
                : "bg-[var(--color-border)]"
            )}>
              <Building2 className={cn("w-3.5 h-3.5", isSelected ? "text-white" : "text-[var(--color-text-muted)]")} />
            </div>

            {/* Text */}
            <div className="flex-1 min-w-0">
              <p className={cn("text-sm font-medium truncate", isSelected && "text-amber-300")}>{org.name}</p>
              <p className="text-[10px] opacity-60 font-mono">{org.code}</p>
            </div>

            {/* Edit button */}
            <button
              onClick={(e) => { e.stopPropagation(); openEditOrg(org); }}
              className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-white/10 transition-all"
            >
              <Pencil className="w-3 h-3" />
            </button>
          </div>

          {/* Children */}
          {hasChildren && isExpanded && (
            <div className="animate-fade-in">
              {renderOrgTree(org.children, depth + 1)}
            </div>
          )}
        </div>
      );
    });

  // ── Main Render ──────────────────────────────────────

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="w-8 h-8 text-amber-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Organizasyon Yönetimi</h1>
          <p className="mt-1 text-sm text-[var(--color-text-muted)]">
            Organizasyon ağacı, departmanlar ve yönetici atamaları
          </p>
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-3 p-4 rounded-xl bg-red-500/10 border border-red-500/20">
          <AlertTriangle className="w-5 h-5 text-red-400 shrink-0" />
          <p className="text-sm text-red-400">{error}</p>
        </div>
      )}

      {/* 3-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

        {/* ── Column 1: Org Tree ── */}
        <div className="lg:col-span-3">
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
            <div className="px-4 py-3 border-b border-[var(--color-border)] flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FolderTree className="w-4 h-4 text-amber-400" />
                <h3 className="text-sm font-semibold text-[var(--color-text)]">Organizasyonlar</h3>
              </div>
              <button
                onClick={openCreateOrg}
                className="p-1.5 rounded-lg bg-amber-500/10 text-amber-400 hover:bg-amber-500/20 transition-colors"
              >
                <Plus className="w-3.5 h-3.5" />
              </button>
            </div>

            <div className="p-2 max-h-[70vh] overflow-y-auto">
              {orgs.length === 0 ? (
                <div className="py-8 text-center text-sm text-[var(--color-text-muted)]">
                  <Building2 className="w-8 h-8 mx-auto mb-2 opacity-20" />
                  Henüz organizasyon yok
                </div>
              ) : (
                renderOrgTree(orgs)
              )}
            </div>
          </div>
        </div>

        {/* ── Column 2: Departments ── */}
        <div className="lg:col-span-4">
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
            <div className="px-4 py-3 border-b border-[var(--color-border)] flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Users className="w-4 h-4 text-cyan-400" />
                <h3 className="text-sm font-semibold text-[var(--color-text)]">
                  {selectedOrgName ? `${selectedOrgName.name} — Departmanlar` : "Departmanlar"}
                </h3>
                {orgDepts.length > 0 && (
                  <span className="text-xs text-[var(--color-text-muted)]">({orgDepts.length})</span>
                )}
              </div>
              {selectedOrgId && (
                <button
                  onClick={openCreateDept}
                  className="p-1.5 rounded-lg bg-cyan-500/10 text-cyan-400 hover:bg-cyan-500/20 transition-colors"
                >
                  <Plus className="w-3.5 h-3.5" />
                </button>
              )}
            </div>

            <div className="max-h-[70vh] overflow-y-auto">
              {!selectedOrgId ? (
                <div className="py-12 text-center text-sm text-[var(--color-text-muted)]">
                  <Building2 className="w-8 h-8 mx-auto mb-2 opacity-20" />
                  Sol panelden bir organizasyon seçin
                </div>
              ) : orgDepts.length === 0 ? (
                <div className="py-12 text-center text-sm text-[var(--color-text-muted)]">
                  <Users className="w-8 h-8 mx-auto mb-2 opacity-20" />
                  Bu organizasyonda departman yok
                  <button
                    onClick={openCreateDept}
                    className="block mx-auto mt-3 text-xs text-cyan-400 hover:text-cyan-300 transition-colors"
                  >
                    + Departman Ekle
                  </button>
                </div>
              ) : (
                <div className="divide-y divide-[var(--color-border)]">
                  {orgDepts.map((dept) => {
                    const isSelected = selectedDeptId === dept.id;
                    return (
                      <div
                        key={dept.id}
                        onClick={() => setSelectedDeptId(dept.id)}
                        className={cn(
                          "px-4 py-3 cursor-pointer transition-all duration-200 group flex items-center gap-3",
                          isSelected
                            ? "bg-cyan-500/10 border-l-2 border-l-cyan-400"
                            : "hover:bg-[var(--color-border)]/30 border-l-2 border-l-transparent"
                        )}
                      >
                        <div className={cn(
                          "w-8 h-8 rounded-lg flex items-center justify-center shrink-0",
                          isSelected ? "bg-cyan-500/20" : "bg-[var(--color-border)]"
                        )}>
                          <Users className={cn("w-4 h-4", isSelected ? "text-cyan-400" : "text-[var(--color-text-muted)]")} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className={cn("text-sm font-medium truncate", isSelected && "text-cyan-300")}>
                            {dept.name}
                          </p>
                          <div className="flex items-center gap-2 mt-0.5">
                            <span className="text-[10px] font-mono text-[var(--color-text-muted)]">{dept.code}</span>
                            {dept.managerUserId && (
                              <span className="text-[10px] text-purple-400 flex items-center gap-0.5">
                                <UserCircle className="w-2.5 h-2.5" />
                                {findUserName(dept.managerUserId).split(" ")[0]}
                              </span>
                            )}
                          </div>
                        </div>
                        {!dept.isActive && (
                          <span className="text-[9px] px-1.5 py-0.5 rounded bg-red-500/10 text-red-400 font-medium">
                            Pasif
                          </span>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ── Column 3: Department Detail ── */}
        <div className="lg:col-span-5">
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
            <div className="px-4 py-3 border-b border-[var(--color-border)] flex items-center justify-between">
              <h3 className="text-sm font-semibold text-[var(--color-text)]">Departman Detayı</h3>
              {selectedDept && (
                <button
                  onClick={() => openEditDept(selectedDept)}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-cyan-500/10 text-cyan-400 hover:bg-cyan-500/20 transition-colors"
                >
                  <Pencil className="w-3 h-3" />
                  Düzenle
                </button>
              )}
            </div>

            {!selectedDept ? (
              <div className="py-16 text-center text-sm text-[var(--color-text-muted)]">
                <Inbox className="w-10 h-10 mx-auto mb-3 opacity-20" />
                <p>Detaylarını görmek için bir departman seçin</p>
              </div>
            ) : (
              <div className="p-5 space-y-5">
                {/* Name & Code */}
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-cyan-500 to-teal-500 flex items-center justify-center shadow-lg shadow-cyan-500/20">
                    <Users className="w-6 h-6 text-white" />
                  </div>
                  <div>
                    <h2 className="text-lg font-bold text-[var(--color-text)]">{selectedDept.name}</h2>
                    <p className="text-xs font-mono text-[var(--color-text-muted)]">{selectedDept.code}</p>
                  </div>
                </div>

                {/* Description */}
                {selectedDept.description && (
                  <p className="text-sm text-[var(--color-text-muted)] leading-relaxed">
                    {selectedDept.description}
                  </p>
                )}

                {/* Detail fields */}
                <div className="space-y-3">
                  {[
                    {
                      label: "Yönetici",
                      value: findUserName(selectedDept.managerUserId),
                      icon: <UserCircle className="w-4 h-4" />,
                      accent: selectedDept.managerUserId ? "text-purple-400" : "text-[var(--color-text-muted)]",
                      highlight: !!selectedDept.managerUserId,
                    },
                    {
                      label: "Varsayılan Kuyruk",
                      value: findQueueName(selectedDept.defaultQueueId),
                      icon: <Inbox className="w-4 h-4" />,
                      accent: selectedDept.defaultQueueId ? "text-teal-400" : "text-[var(--color-text-muted)]",
                      highlight: !!selectedDept.defaultQueueId,
                    },
                    {
                      label: "Durum",
                      value: selectedDept.isActive ? "Aktif" : "Pasif",
                      icon: selectedDept.isActive
                        ? <div className="w-2 h-2 rounded-full bg-emerald-400" />
                        : <div className="w-2 h-2 rounded-full bg-red-400" />,
                      accent: selectedDept.isActive ? "text-emerald-400" : "text-red-400",
                      highlight: false,
                    },
                  ].map((field) => (
                    <div key={field.label} className={cn(
                      "flex items-center gap-3 px-4 py-3 rounded-xl border transition-colors",
                      field.highlight
                        ? "border-[var(--color-border)] bg-[var(--color-bg)]"
                        : "border-transparent bg-[var(--color-bg)]/50"
                    )}>
                      <span className={cn("shrink-0", field.accent)}>{field.icon}</span>
                      <span className="text-xs text-[var(--color-text-muted)] w-28 shrink-0">{field.label}</span>
                      <span className={cn("text-sm font-medium", field.accent)}>{field.value}</span>
                    </div>
                  ))}
                </div>

                {/* Sub departments */}
                {selectedDept.subDepartments && selectedDept.subDepartments.length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">
                      Alt Departmanlar
                    </h4>
                    <div className="space-y-1">
                      {selectedDept.subDepartments.map((sub) => (
                        <div
                          key={sub.id}
                          onClick={() => setSelectedDeptId(sub.id)}
                          className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-[var(--color-border)]/30 cursor-pointer transition-colors"
                        >
                          <ChevronRight className="w-3 h-3 text-[var(--color-text-muted)]" />
                          <span className="text-sm text-[var(--color-text)]">{sub.name}</span>
                          <span className="text-[10px] font-mono text-[var(--color-text-muted)]">{sub.code}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ══════════ Org Create/Edit Modal ══════════ */}
      {showOrgForm && (
        <>
          <div
            className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
            onClick={() => setShowOrgForm(false)}
          />
          <div className="fixed z-50 top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-full max-w-md">
            <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-card-bg)] shadow-2xl">
              <div className="px-6 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center">
                    <Building2 className="w-4 h-4 text-white" />
                  </div>
                  <h2 className="text-base font-semibold text-[var(--color-text)]">
                    {editingOrg ? "Organizasyon Düzenle" : "Yeni Organizasyon"}
                  </h2>
                </div>
                <button onClick={() => setShowOrgForm(false)} className="p-1.5 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                  <X className="w-4 h-4 text-[var(--color-text-muted)]" />
                </button>
              </div>

              <div className="px-6 py-5 space-y-4">
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                    Ad <span className="text-red-400">*</span>
                  </label>
                  <input
                    type="text"
                    value={formName}
                    onChange={(e) => setFormName(e.target.value)}
                    placeholder="Organizasyon adı..."
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                    Kod <span className="text-red-400">*</span>
                  </label>
                  <input
                    type="text"
                    value={formCode}
                    onChange={(e) => setFormCode(e.target.value.toUpperCase())}
                    placeholder="ÖR: HQ, TR-IST"
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-amber-500/40 font-mono"
                  />
                </div>
                {selectedOrgId && !editingOrg && (
                  <p className="text-xs text-amber-400">
                    ⤷ "{selectedOrgName?.name}" altında oluşturulacak
                  </p>
                )}
                {formError && (
                  <p className="text-xs text-red-400">{formError}</p>
                )}
              </div>

              <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
                <button
                  onClick={() => setShowOrgForm(false)}
                  className="px-4 py-2 rounded-lg text-xs font-medium text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors"
                >
                  İptal
                </button>
                <button
                  onClick={handleSaveOrg}
                  disabled={formSaving}
                  className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-medium bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-40 transition-colors"
                >
                  {formSaving ? <Loader2 className="w-3 h-3 animate-spin" /> : <Save className="w-3 h-3" />}
                  Kaydet
                </button>
              </div>
            </div>
          </div>
        </>
      )}

      {/* ══════════ Dept Create/Edit Modal ══════════ */}
      {showDeptForm && (
        <>
          <div
            className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm"
            onClick={() => setShowDeptForm(false)}
          />
          <div className="fixed z-50 top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-full max-w-lg">
            <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-card-bg)] shadow-2xl">
              <div className="px-6 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-cyan-500 to-teal-500 flex items-center justify-center">
                    <Users className="w-4 h-4 text-white" />
                  </div>
                  <h2 className="text-base font-semibold text-[var(--color-text)]">
                    {editingDept ? "Departman Düzenle" : "Yeni Departman"}
                  </h2>
                </div>
                <button onClick={() => setShowDeptForm(false)} className="p-1.5 rounded-lg hover:bg-[var(--color-border)] transition-colors">
                  <X className="w-4 h-4 text-[var(--color-text-muted)]" />
                </button>
              </div>

              <div className="px-6 py-5 space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                      Ad <span className="text-red-400">*</span>
                    </label>
                    <input
                      type="text"
                      value={formName}
                      onChange={(e) => setFormName(e.target.value)}
                      placeholder="Departman adı..."
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                      Kod <span className="text-red-400">*</span>
                    </label>
                    <input
                      type="text"
                      value={formCode}
                      onChange={(e) => setFormCode(e.target.value.toUpperCase())}
                      placeholder="ÖR: IT, HR"
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40 font-mono"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">Açıklama</label>
                  <textarea
                    value={formDesc}
                    onChange={(e) => setFormDesc(e.target.value)}
                    placeholder="Departman açıklaması (opsiyonel)..."
                    rows={2}
                    className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] resize-none focus:outline-none focus:ring-2 focus:ring-cyan-500/40"
                  />
                </div>

                <div>
                  <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                    Departman Yöneticisi
                  </label>
                  <select
                    value={formManagerUserId}
                    onChange={(e) => setFormManagerUserId(e.target.value)}
                    className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                  >
                    <option value="">Yönetici seçin (opsiyonel)</option>
                    {users.map((u) => (
                      <option key={u.id} value={u.id}>{u.fullName} — {u.email}</option>
                    ))}
                  </select>
                  <p className="text-[10px] text-[var(--color-text-muted)] mt-1">
                    Onay akışında bu kişi departman üyelerinin talepleri için onaylayıcı olarak atanacak.
                  </p>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                      Üst Departman
                    </label>
                    <select
                      value={formParentDeptId}
                      onChange={(e) => setFormParentDeptId(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40"
                    >
                      <option value="">Yok (Kök departman)</option>
                      {orgDepts
                        .filter((d) => d.id !== editingDept?.id)
                        .map((d) => (
                          <option key={d.id} value={d.id}>{d.name}</option>
                        ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-[var(--color-text)] mb-1.5">
                      Varsayılan Kuyruk
                    </label>
                    <select
                      value={formDefaultQueueId}
                      onChange={(e) => setFormDefaultQueueId(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-cyan-500/40"
                    >
                      <option value="">Yok</option>
                      {queues.map((q) => (
                        <option key={q.queueId} value={q.queueId}>{q.name} ({q.code})</option>
                      ))}
                    </select>
                  </div>
                </div>

                {formError && (
                  <p className="text-xs text-red-400">{formError}</p>
                )}
              </div>

              <div className="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-2">
                <button
                  onClick={() => setShowDeptForm(false)}
                  className="px-4 py-2 rounded-lg text-xs font-medium text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors"
                >
                  İptal
                </button>
                <button
                  onClick={handleSaveDept}
                  disabled={formSaving}
                  className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-medium bg-cyan-600 text-white hover:bg-cyan-700 disabled:opacity-40 transition-colors"
                >
                  {formSaving ? <Loader2 className="w-3 h-3 animate-spin" /> : <Save className="w-3 h-3" />}
                  Kaydet
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
