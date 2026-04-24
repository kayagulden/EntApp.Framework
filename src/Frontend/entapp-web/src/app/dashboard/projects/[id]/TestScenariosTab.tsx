"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Trash2, Edit3, Save, X, Search, FlaskConical, ChevronDown, ChevronRight, GripVertical } from "lucide-react";
import { cn } from "@/lib/utils";

const API = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5212";

interface TestScenario {
  id: string; key: string; title: string; type: string; priority: string; status: string;
  requirementId?: string; requirementKey?: string; stepCount: number; executionCount: number;
  tags?: string; estimatedDurationMinutes?: number; sortOrder: number; createdAt: string;
}
interface TestStep { id: string; stepNumber: number; action: string; expectedResult: string; testData?: string; notes?: string; }
interface ScenarioDetail extends TestScenario {
  description?: string; preconditions?: string; steps?: TestStep[];
}

const TYPE_COLORS: Record<string,string> = { Functional:"bg-blue-500/20 text-blue-400", Regression:"bg-orange-500/20 text-orange-400", Smoke:"bg-green-500/20 text-green-400", Integration:"bg-purple-500/20 text-purple-400", UAT:"bg-yellow-500/20 text-yellow-400", Performance:"bg-red-500/20 text-red-400" };
const PRIORITY_COLORS: Record<string,string> = { Critical:"bg-red-500/20 text-red-400", High:"bg-orange-500/20 text-orange-400", Medium:"bg-yellow-500/20 text-yellow-400", Low:"bg-green-500/20 text-green-400" };
const STATUS_COLORS: Record<string,string> = { Draft:"bg-gray-500/20 text-gray-400", Active:"bg-green-500/20 text-green-400", Deprecated:"bg-orange-500/20 text-orange-400", Archived:"bg-red-500/20 text-red-400" };

export default function TestScenariosTab({ projectId }: { projectId: string }) {
  const [scenarios, setScenarios] = useState<TestScenario[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string|null>(null);
  const [selectedId, setSelectedId] = useState<string|null>(null);
  const [detail, setDetail] = useState<ScenarioDetail|null>(null);
  const [form, setForm] = useState({ title:"", type:"Functional", priority:"Medium", description:"", preconditions:"", tags:"" });
  const [steps, setSteps] = useState<{action:string;expectedResult:string;testData:string;notes:string}[]>([]);
  const [filterType, setFilterType] = useState("");
  const [filterStatus, setFilterStatus] = useState("");

  const fetchScenarios = useCallback(async () => {
    setLoading(true);
    const params = new URLSearchParams();
    if (filterType) params.set("type", filterType);
    if (filterStatus) params.set("status", filterStatus);
    const res = await fetch(`${API}/api/pm/projects/${projectId}/test-scenarios?${params}`);
    if (res.ok) setScenarios(await res.json());
    setLoading(false);
  }, [projectId, filterType, filterStatus]);

  useEffect(() => { fetchScenarios(); }, [fetchScenarios]);

  const fetchDetail = async (id: string) => {
    const res = await fetch(`${API}/api/pm/test-scenarios/${id}`);
    if (res.ok) { const d = await res.json(); setDetail(d); setSteps(d.steps?.map((s:TestStep)=>({action:s.action,expectedResult:s.expectedResult,testData:s.testData||"",notes:s.notes||""})) || []); }
  };

  useEffect(() => { if (selectedId) fetchDetail(selectedId); }, [selectedId]);

  const handleSave = async () => {
    const body = { ...form, estimatedDurationMinutes: undefined };
    if (editingId) {
      await fetch(`${API}/api/pm/test-scenarios/${editingId}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
    } else {
      const res = await fetch(`${API}/api/pm/projects/${projectId}/test-scenarios`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
      if (res.ok) { const r = await res.json(); setSelectedId(r.id); }
    }
    setShowForm(false); setEditingId(null); setForm({title:"",type:"Functional",priority:"Medium",description:"",preconditions:"",tags:""});
    fetchScenarios();
  };

  const handleDelete = async (id: string) => {
    await fetch(`${API}/api/pm/test-scenarios/${id}`, { method:"DELETE" });
    if (selectedId === id) { setSelectedId(null); setDetail(null); }
    fetchScenarios();
  };

  const handleSaveSteps = async () => {
    if (!selectedId) return;
    const stepsPayload = steps.map((s, i) => ({ action: s.action, expectedResult: s.expectedResult, stepNumber: i + 1, testData: s.testData || null, notes: s.notes || null }));
    await fetch(`${API}/api/pm/test-scenarios/${selectedId}/steps`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ steps: stepsPayload }) });
    fetchDetail(selectedId);
  };

  const handleEdit = (s: TestScenario) => {
    setEditingId(s.id); setForm({ title:s.title, type:s.type, priority:s.priority, description:"", preconditions:"", tags:s.tags||"" });
    setShowForm(true);
  };

  const filtered = scenarios.filter(s => !search || s.title.toLowerCase().includes(search.toLowerCase()) || s.key.toLowerCase().includes(search.toLowerCase()));

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold flex items-center gap-2"><FlaskConical className="w-5 h-5 text-blue-400" /> Test Senaryoları</h3>
        <button onClick={() => { setShowForm(true); setEditingId(null); setForm({title:"",type:"Functional",priority:"Medium",description:"",preconditions:"",tags:""}); }}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-sm transition-colors"><Plus className="w-4 h-4"/>Yeni Senaryo</button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]"/>
          <input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Ara..." className="w-full pl-9 pr-3 py-1.5 rounded-lg bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-sm"/>
        </div>
        <select value={filterType} onChange={e=>setFilterType(e.target.value)} className="px-3 py-1.5 rounded-lg bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-sm">
          <option value="">Tüm Tipler</option>
          {["Functional","Regression","Smoke","Integration","UAT","Performance"].map(t=><option key={t} value={t}>{t}</option>)}
        </select>
        <select value={filterStatus} onChange={e=>setFilterStatus(e.target.value)} className="px-3 py-1.5 rounded-lg bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-sm">
          <option value="">Tüm Durumlar</option>
          {["Draft","Active","Deprecated","Archived"].map(s=><option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {/* Form */}
      {showForm && (
        <div className="p-4 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)] space-y-3">
          <h4 className="text-sm font-semibold">{editingId ? "Senaryo Düzenle" : "Yeni Test Senaryosu"}</h4>
          <input value={form.title} onChange={e=>setForm({...form,title:e.target.value})} placeholder="Başlık *" className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          <div className="grid grid-cols-3 gap-3">
            <select value={form.type} onChange={e=>setForm({...form,type:e.target.value})} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm">
              {["Functional","Regression","Smoke","Integration","UAT","Performance"].map(t=><option key={t}>{t}</option>)}
            </select>
            <select value={form.priority} onChange={e=>setForm({...form,priority:e.target.value})} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm">
              {["Low","Medium","High","Critical"].map(p=><option key={p}>{p}</option>)}
            </select>
            <input value={form.tags} onChange={e=>setForm({...form,tags:e.target.value})} placeholder="Etiketler (virgülle)" className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          </div>
          <textarea value={form.description} onChange={e=>setForm({...form,description:e.target.value})} placeholder="Açıklama (Markdown)" rows={3} className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          <textarea value={form.preconditions} onChange={e=>setForm({...form,preconditions:e.target.value})} placeholder="Ön Koşullar" rows={2} className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          <div className="flex gap-2 justify-end">
            <button onClick={()=>{setShowForm(false);setEditingId(null);}} className="px-3 py-1.5 rounded-lg text-sm hover:bg-[var(--color-bg-tertiary)]"><X className="w-4 h-4 inline mr-1"/>İptal</button>
            <button onClick={handleSave} disabled={!form.title} className="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-sm disabled:opacity-50"><Save className="w-4 h-4 inline mr-1"/>Kaydet</button>
          </div>
        </div>
      )}

      {/* List + Detail */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Left: List */}
        <div className="space-y-1">
          {loading ? <div className="text-center py-8 text-[var(--color-text-muted)]">Yükleniyor...</div> :
           filtered.length === 0 ? <div className="text-center py-8 text-[var(--color-text-muted)]">Henüz senaryo yok</div> :
           filtered.map(s => (
            <div key={s.id} onClick={()=>setSelectedId(s.id)}
              className={cn("flex items-center gap-3 px-3 py-2.5 rounded-lg cursor-pointer transition-colors border",
                selectedId===s.id ? "bg-blue-500/10 border-blue-500/30" : "bg-[var(--color-bg-secondary)] border-transparent hover:bg-[var(--color-bg-tertiary)]")}>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-mono text-[var(--color-text-muted)]">{s.key}</span>
                  <span className="text-sm font-medium truncate">{s.title}</span>
                </div>
                <div className="flex items-center gap-1.5 mt-1">
                  <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-medium", TYPE_COLORS[s.type]||"bg-gray-500/20 text-gray-400")}>{s.type}</span>
                  <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-medium", PRIORITY_COLORS[s.priority]||"")}>{s.priority}</span>
                  <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-medium", STATUS_COLORS[s.status]||"")}>{s.status}</span>
                  <span className="text-[10px] text-[var(--color-text-muted)]">{s.stepCount} adım</span>
                  {s.requirementKey && <span className="text-[10px] text-blue-400">📎 {s.requirementKey}</span>}
                </div>
              </div>
              <div className="flex items-center gap-1">
                <button onClick={e=>{e.stopPropagation();handleEdit(s);}} className="p-1 rounded hover:bg-[var(--color-bg-tertiary)]"><Edit3 className="w-3.5 h-3.5"/></button>
                <button onClick={e=>{e.stopPropagation();handleDelete(s.id);}} className="p-1 rounded hover:bg-red-500/20 text-red-400"><Trash2 className="w-3.5 h-3.5"/></button>
              </div>
            </div>
          ))}
        </div>

        {/* Right: Detail + Steps */}
        <div className="space-y-4">
          {detail ? (
            <div className="p-4 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)] space-y-4">
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-xs font-mono text-[var(--color-text-muted)]">{detail.key}</span>
                  <h4 className="text-base font-semibold">{detail.title}</h4>
                </div>
                {detail.description && <p className="text-sm text-[var(--color-text-muted)] mt-1">{detail.description}</p>}
                {detail.preconditions && <div className="mt-2 p-2 rounded bg-yellow-500/10 text-xs text-yellow-400"><strong>Ön Koşullar:</strong> {detail.preconditions}</div>}
              </div>

              {/* Steps */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <h5 className="text-sm font-semibold">Adımlar ({steps.length})</h5>
                  <div className="flex gap-1.5">
                    <button onClick={()=>setSteps([...steps,{action:"",expectedResult:"",testData:"",notes:""}])} className="text-xs px-2 py-1 rounded bg-blue-600 hover:bg-blue-700 text-white"><Plus className="w-3 h-3 inline mr-0.5"/>Adım</button>
                    <button onClick={handleSaveSteps} className="text-xs px-2 py-1 rounded bg-green-600 hover:bg-green-700 text-white"><Save className="w-3 h-3 inline mr-0.5"/>Kaydet</button>
                  </div>
                </div>
                {steps.map((step, i) => (
                  <div key={i} className="flex gap-2 mb-2 p-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)]">
                    <span className="text-xs font-mono text-[var(--color-text-muted)] mt-2 w-6 text-center">{i+1}</span>
                    <div className="flex-1 space-y-1">
                      <input value={step.action} onChange={e=>{const n=[...steps];n[i].action=e.target.value;setSteps(n);}} placeholder="İşlem *" className="w-full px-2 py-1 rounded bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-xs"/>
                      <input value={step.expectedResult} onChange={e=>{const n=[...steps];n[i].expectedResult=e.target.value;setSteps(n);}} placeholder="Beklenen Sonuç *" className="w-full px-2 py-1 rounded bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-xs"/>
                      <input value={step.testData} onChange={e=>{const n=[...steps];n[i].testData=e.target.value;setSteps(n);}} placeholder="Test Verisi" className="w-full px-2 py-1 rounded bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-xs"/>
                    </div>
                    <button onClick={()=>setSteps(steps.filter((_,j)=>j!==i))} className="p-1 rounded hover:bg-red-500/20 text-red-400 self-start mt-1"><Trash2 className="w-3 h-3"/></button>
                  </div>
                ))}
                {steps.length === 0 && <p className="text-xs text-[var(--color-text-muted)] text-center py-4">Henüz adım eklenmedi</p>}
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center h-64 text-[var(--color-text-muted)] text-sm">Detay görüntülemek için bir senaryo seçin</div>
          )}
        </div>
      </div>
    </div>
  );
}
