"use client";

import { useState, useEffect, useCallback } from "react";
import {
  Plus,
  Search,
  Loader2,
  Users,
  Building2,
  Workflow,
  ChevronRight,
  Trash2,
  UserPlus,
  Settings2,
  Inbox,
  X,
  Save,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface ReqDepartment {
  id: string;
  name: string;
  code: string;
}

interface ServiceQueue {
  id: string;
  name: string;
  code: string;
  description?: string;
  departmentId?: string;
  managerUserId?: string;
  defaultWorkflowDefinitionId?: string;
  isActive: boolean;
  department?: { name: string; id: string };
  members: QueueMember[];
}

interface QueueMember {
  id: string;
  userId: string;
  userName?: string;
  fullName?: string;
  role: string;
  joinedAt: string;
  isActive: boolean;
}

const MEMBER_ROLES = ["Member", "Lead", "Dispatcher"];

export default function QueuesPage() {
  const [queues, setQueues] = useState<ServiceQueue[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedQueue, setSelectedQueue] = useState<ServiceQueue | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);

  // ── Form state ──
  const [formName, setFormName] = useState("");
  const [formCode, setFormCode] = useState("");
  const [formDescription, setFormDescription] = useState("");
  const [formDepartmentId, setFormDepartmentId] = useState("");
  const [formSaving, setFormSaving] = useState(false);

  // ── Departments ──
  const [departments, setDepartments] = useState<ReqDepartment[]>([]);

  useEffect(() => {
    fetch("/api/req/departments")
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => setDepartments(Array.isArray(data) ? data : []))
      .catch(() => setDepartments([]));
  }, []);

  // ── Add member state ──
  const [showAddMember, setShowAddMember] = useState(false);
  const [newMemberUserId, setNewMemberUserId] = useState("");
  const [newMemberRole, setNewMemberRole] = useState("Member");
  const [addingMember, setAddingMember] = useState(false);

  const fetchQueues = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch("/api/req/queues?activeOnly=false");
      if (res.ok) setQueues(await res.json());
    } catch (err) {
      console.error("Failed to fetch queues:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchQueues(); }, [fetchQueues]);

  const handleCreate = async () => {
    if (!formName || !formCode) return;
    setFormSaving(true);
    try {
      const res = await fetch("/api/req/queues", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formName, code: formCode,
          description: formDescription || null,
          departmentId: formDepartmentId || null,
        }),
      });
      if (res.ok) {
        setShowCreateForm(false);
        setFormName(""); setFormCode(""); setFormDescription(""); setFormDepartmentId("");
        await fetchQueues();
      }
    } finally {
      setFormSaving(false);
    }
  };

  const handleAddMember = async () => {
    if (!selectedQueue || !newMemberUserId) return;
    setAddingMember(true);
    try {
      const res = await fetch(`/api/req/queues/${selectedQueue.id}/members`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: newMemberUserId, role: newMemberRole }),
      });
      if (res.ok) {
        setShowAddMember(false);
        setNewMemberUserId(""); setNewMemberRole("Member");
        await fetchQueues();
        // Refresh selected queue
        const updated = await fetch(`/api/req/queues/${selectedQueue.id}`);
        if (updated.ok) setSelectedQueue(await updated.json());
      }
    } finally {
      setAddingMember(false);
    }
  };

  const handleRemoveMember = async (membershipId: string) => {
    if (!confirm("Bu üyeyi kuyruktan çıkarmak istediğinize emin misiniz?")) return;
    await fetch(`/api/req/queues/members/${membershipId}`, { method: "DELETE" });
    if (selectedQueue) {
      const updated = await fetch(`/api/req/queues/${selectedQueue.id}`);
      if (updated.ok) setSelectedQueue(await updated.json());
    }
    await fetchQueues();
  };

  const filtered = searchTerm
    ? queues.filter((q) => q.name.toLowerCase().includes(searchTerm.toLowerCase()) || q.code.toLowerCase().includes(searchTerm.toLowerCase()))
    : queues;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text)]">Hizmet Kuyrukları</h1>
          <p className="text-sm text-[var(--color-text-muted)] mt-1">
            Taleplerin yönlendirileceği kuyrukları ve üyelerini yönetin.
          </p>
        </div>
        <button
          onClick={() => setShowCreateForm(true)}
          className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-indigo-600 text-white text-sm font-medium hover:bg-indigo-700 shadow-lg shadow-indigo-500/20 transition-all"
        >
          <Plus className="w-4 h-4" />
          Yeni Kuyruk
        </button>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
        <input
          type="text"
          placeholder="Kuyruk ara..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          suppressHydrationWarning
          className="w-full pl-10 pr-4 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
        />
      </div>

      {/* Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Queue List */}
        <div className="lg:col-span-1 space-y-2">
          {loading ? (
            <div className="flex justify-center py-12">
              <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
            </div>
          ) : filtered.length === 0 ? (
            <div className="flex flex-col items-center py-12 text-[var(--color-text-muted)]">
              <Inbox className="w-10 h-10 opacity-30 mb-2" />
              <p className="text-sm">Henüz kuyruk tanımlanmamış</p>
            </div>
          ) : (
            filtered.map((queue) => (
              <button
                key={queue.id}
                onClick={() => setSelectedQueue(queue)}
                className={cn(
                  "w-full text-left rounded-xl p-4 border transition-all duration-200",
                  selectedQueue?.id === queue.id
                    ? "border-indigo-500/40 bg-indigo-500/5 shadow-md"
                    : "border-[var(--color-border)] bg-[var(--color-card-bg)] hover:border-[var(--color-border-hover)] hover:shadow-md"
                )}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className={cn(
                      "w-10 h-10 rounded-xl flex items-center justify-center shadow",
                      queue.isActive
                        ? "bg-gradient-to-br from-indigo-500 to-purple-600"
                        : "bg-slate-600"
                    )}>
                      <Inbox className="w-5 h-5 text-white" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-sm text-[var(--color-text)]">{queue.name}</h3>
                      <p className="text-xs text-[var(--color-text-muted)] font-mono">{queue.code}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-[var(--color-text-muted)]">
                      <Users className="w-3 h-3 inline mr-1" />
                      {queue.members?.length ?? 0}
                    </span>
                    <ChevronRight className="w-4 h-4 text-[var(--color-text-muted)]" />
                  </div>
                </div>
                {queue.department && (
                  <div className="flex items-center gap-1 mt-2 text-xs text-[var(--color-text-muted)]">
                    <Building2 className="w-3 h-3" />
                    {queue.department.name}
                  </div>
                )}
              </button>
            ))
          )}
        </div>

        {/* Queue Detail */}
        <div className="lg:col-span-2">
          {selectedQueue ? (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden">
              {/* Detail Header */}
              <div className="px-6 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-[var(--color-text)]">{selectedQueue.name}</h2>
                  <p className="text-sm text-[var(--color-text-muted)]">{selectedQueue.description || "Açıklama eklenmemiş"}</p>
                </div>
                <span className={cn(
                  "px-2.5 py-1 rounded-full text-xs font-medium",
                  selectedQueue.isActive ? "bg-emerald-500/10 text-emerald-400" : "bg-slate-500/10 text-slate-400"
                )}>
                  {selectedQueue.isActive ? "Aktif" : "Pasif"}
                </span>
              </div>

              {/* Members */}
              <div className="p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
                    <Users className="w-4 h-4 text-indigo-400" />
                    Üyeler ({selectedQueue.members?.length ?? 0})
                  </h3>
                  <button
                    onClick={() => setShowAddMember(true)}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-indigo-500/10 text-indigo-400 hover:bg-indigo-500/20 transition-colors"
                  >
                    <UserPlus className="w-3.5 h-3.5" />
                    Üye Ekle
                  </button>
                </div>

                {/* Add Member Inline Form */}
                {showAddMember && (
                  <div className="flex items-center gap-2 mb-4 p-3 rounded-lg border border-indigo-500/20 bg-indigo-500/5">
                    <input
                      type="text"
                      placeholder="Kullanıcı ID (GUID)"
                      value={newMemberUserId}
                      onChange={(e) => setNewMemberUserId(e.target.value)}
                      className="flex-1 px-3 py-1.5 rounded-md text-sm border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]"
                    />
                    <select
                      value={newMemberRole}
                      onChange={(e) => setNewMemberRole(e.target.value)}
                      className="px-3 py-1.5 rounded-md text-sm border border-[var(--color-border)] bg-[var(--color-input-bg)] text-[var(--color-text)]"
                    >
                      {MEMBER_ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                    </select>
                    <button
                      onClick={handleAddMember}
                      disabled={addingMember || !newMemberUserId}
                      className="px-3 py-1.5 rounded-md text-xs font-medium bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors"
                    >
                      {addingMember ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : "Ekle"}
                    </button>
                    <button onClick={() => setShowAddMember(false)} className="p-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-text)]">
                      <X className="w-4 h-4" />
                    </button>
                  </div>
                )}

                {/* Member List */}
                {(selectedQueue.members?.length ?? 0) === 0 ? (
                  <p className="text-sm text-[var(--color-text-muted)] text-center py-8">Henüz üye eklenmemiş</p>
                ) : (
                  <div className="space-y-2">
                    {selectedQueue.members.map((member) => (
                      <div
                        key={member.id}
                        className="flex items-center justify-between px-4 py-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)]"
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center">
                            <span className="text-xs font-bold text-white">{(member.fullName || member.userName || "U").charAt(0).toUpperCase()}</span>
                          </div>
                          <div>
                            <p className="text-sm font-medium text-[var(--color-text)]">
                              {member.fullName || member.userName || member.userId.substring(0, 8) + "..."}
                            </p>
                            <p className="text-xs text-[var(--color-text-muted)]">
                              {member.userName && <span className="mr-2 font-mono">@{member.userName}</span>}
                              {new Date(member.joinedAt).toLocaleDateString("tr-TR")}
                            </p>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className={cn(
                            "px-2 py-0.5 rounded-full text-xs font-medium",
                            member.role === "Lead" ? "bg-amber-500/10 text-amber-400" :
                            member.role === "Dispatcher" ? "bg-purple-500/10 text-purple-400" :
                            "bg-blue-500/10 text-blue-400"
                          )}>
                            {member.role}
                          </span>
                          <button
                            onClick={() => handleRemoveMember(member.id)}
                            className="p-1.5 rounded hover:bg-red-500/10 text-[var(--color-text-muted)] hover:text-red-400 transition-colors"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center h-64 text-[var(--color-text-muted)]">
              <Settings2 className="w-12 h-12 opacity-20 mb-3" />
              <p className="text-sm">Detay görmek için bir kuyruk seçin</p>
            </div>
          )}
        </div>
      </div>

      {/* Create Queue Modal */}
      {showCreateForm && (
        <>
          <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm" onClick={() => setShowCreateForm(false)} />
          <div className="fixed right-0 top-0 z-50 h-full w-full max-w-md bg-[var(--color-card-bg)] border-l border-[var(--color-border)] shadow-2xl flex flex-col animate-slide-in-right">
            <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--color-border)]">
              <h2 className="text-lg font-semibold text-[var(--color-text)]">Yeni Hizmet Kuyruğu</h2>
              <button onClick={() => setShowCreateForm(false)} className="p-2 rounded-lg hover:bg-[var(--color-border)]">
                <X className="w-5 h-5 text-[var(--color-text-muted)]" />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto px-6 py-5 space-y-4">
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Kuyruk Adı *</label>
                <input
                  type="text" value={formName} onChange={(e) => setFormName(e.target.value)}
                  placeholder="Örn: Network Destek"
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Kod *</label>
                <input
                  type="text" value={formCode} onChange={(e) => setFormCode(e.target.value)}
                  placeholder="Örn: NET-SUPPORT"
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] font-mono focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Açıklama</label>
                <textarea
                  value={formDescription} onChange={(e) => setFormDescription(e.target.value)}
                  rows={3} placeholder="Kuyruğun amacı ve kapsamı..."
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] resize-y focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--color-text)] mb-1">Departman</label>
                <select
                  value={formDepartmentId} onChange={(e) => setFormDepartmentId(e.target.value)}
                  className="w-full px-3 py-2 rounded-lg text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                >
                  <option value="">Departman seçin (opsiyonel)</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id}>{d.name} ({d.code})</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-[var(--color-border)]">
              <button onClick={() => setShowCreateForm(false)} className="px-4 py-2 rounded-lg text-sm border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-border)] transition-colors">
                İptal
              </button>
              <button
                onClick={handleCreate} disabled={formSaving || !formName || !formCode}
                className="flex items-center gap-2 px-5 py-2 rounded-lg text-sm font-medium bg-indigo-600 text-white hover:bg-indigo-700 shadow-md shadow-indigo-500/20 disabled:opacity-50 transition-all"
              >
                {formSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                Oluştur
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
