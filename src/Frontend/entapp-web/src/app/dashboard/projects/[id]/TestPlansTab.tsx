"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Trash2, Edit3, Save, X, Search, TestTube2, Play, CheckCircle2, XCircle, AlertCircle, MinusCircle } from "lucide-react";
import { cn } from "@/lib/utils";

const API = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5212";

interface TestPlan {
  id: string; key: string; title: string; status: string;
  sprintId?: string; milestoneId?: string; startDate?: string; endDate?: string;
  assignedTesterId?: string; scenarioCount: number; passCount: number; failCount: number; notRunCount: number; createdAt: string;
}
interface PlanScenario {
  testPlanScenarioId: string; testScenarioId: string; testScenarioKey: string; testScenarioTitle: string;
  testScenarioType: string; testScenarioPriority: string; assignedTesterId?: string;
  lastResult?: string; lastExecutedAt?: string; sortOrder: number;
}
interface PlanDetail extends TestPlan { description?: string; scenarios?: PlanScenario[]; }
interface ProjectScenario { id: string; key: string; title: string; type: string; }

const STATUS_COLORS: Record<string,string> = { Draft:"bg-gray-500/20 text-gray-400", Active:"bg-blue-500/20 text-blue-400", InExecution:"bg-yellow-500/20 text-yellow-400", Completed:"bg-green-500/20 text-green-400", Cancelled:"bg-red-500/20 text-red-400" };
const RESULT_ICONS: Record<string, React.ReactNode> = { Pass:<CheckCircle2 className="w-4 h-4 text-green-400"/>, Fail:<XCircle className="w-4 h-4 text-red-400"/>, Blocked:<AlertCircle className="w-4 h-4 text-orange-400"/>, Skipped:<MinusCircle className="w-4 h-4 text-gray-400"/>, NotRun:<MinusCircle className="w-4 h-4 text-gray-500"/> };

export default function TestPlansTab({ projectId }: { projectId: string }) {
  const [plans, setPlans] = useState<TestPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string|null>(null);
  const [selectedId, setSelectedId] = useState<string|null>(null);
  const [detail, setDetail] = useState<PlanDetail|null>(null);
  const [form, setForm] = useState({ title:"", description:"", startDate:"", endDate:"" });
  const [allScenarios, setAllScenarios] = useState<ProjectScenario[]>([]);
  const [showAddScenario, setShowAddScenario] = useState(false);
  const [execForm, setExecForm] = useState<{scenarioId:string;result:string;notes:string}|null>(null);

  const fetchPlans = useCallback(async () => {
    setLoading(true);
    const res = await fetch(`${API}/api/pm/projects/${projectId}/test-plans`);
    if (res.ok) setPlans(await res.json());
    setLoading(false);
  }, [projectId]);

  useEffect(() => { fetchPlans(); }, [fetchPlans]);

  const fetchDetail = async (id: string) => {
    const res = await fetch(`${API}/api/pm/test-plans/${id}`);
    if (res.ok) setDetail(await res.json());
  };

  const fetchAllScenarios = async () => {
    const res = await fetch(`${API}/api/pm/projects/${projectId}/test-scenarios`);
    if (res.ok) setAllScenarios(await res.json());
  };

  useEffect(() => { if (selectedId) fetchDetail(selectedId); }, [selectedId]);

  const handleSave = async () => {
    const body = { title: form.title, description: form.description || undefined, startDate: form.startDate || undefined, endDate: form.endDate || undefined };
    if (editingId) {
      await fetch(`${API}/api/pm/test-plans/${editingId}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
    } else {
      const res = await fetch(`${API}/api/pm/projects/${projectId}/test-plans`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
      if (res.ok) { const r = await res.json(); setSelectedId(r.id); }
    }
    setShowForm(false); setEditingId(null); setForm({title:"",description:"",startDate:"",endDate:""});
    fetchPlans();
  };

  const handleDelete = async (id: string) => {
    await fetch(`${API}/api/pm/test-plans/${id}`, { method:"DELETE" });
    if (selectedId === id) { setSelectedId(null); setDetail(null); }
    fetchPlans();
  };

  const handleAddScenario = async (scenarioId: string) => {
    if (!selectedId) return;
    await fetch(`${API}/api/pm/test-plans/${selectedId}/scenarios`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ testScenarioId: scenarioId }) });
    fetchDetail(selectedId); fetchPlans();
  };

  const handleRemoveScenario = async (scenarioId: string) => {
    if (!selectedId) return;
    await fetch(`${API}/api/pm/test-plans/${selectedId}/scenarios/${scenarioId}`, { method:"DELETE" });
    fetchDetail(selectedId); fetchPlans();
  };

  const handleExecute = async () => {
    if (!selectedId || !execForm) return;
    await fetch(`${API}/api/pm/test-plans/${selectedId}/scenarios/${execForm.scenarioId}/execute`, {
      method:"POST", headers:{"Content-Type":"application/json"},
      body:JSON.stringify({ result: execForm.result, notes: execForm.notes || undefined })
    });
    setExecForm(null); fetchDetail(selectedId); fetchPlans();
  };

  const handleStatusChange = async (id: string, status: string) => {
    await fetch(`${API}/api/pm/test-plans/${id}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ status }) });
    fetchDetail(id); fetchPlans();
  };

  const progressBar = (p: TestPlan) => {
    const total = p.scenarioCount || 1;
    const passW = (p.passCount / total) * 100;
    const failW = (p.failCount / total) * 100;
    return (
      <div className="flex h-1.5 rounded-full overflow-hidden bg-gray-700 w-full">
        <div className="bg-green-500" style={{width:`${passW}%`}}/>
        <div className="bg-red-500" style={{width:`${failW}%`}}/>
      </div>
    );
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold flex items-center gap-2"><TestTube2 className="w-5 h-5 text-purple-400"/>Test Planları</h3>
        <button onClick={()=>{setShowForm(true);setEditingId(null);setForm({title:"",description:"",startDate:"",endDate:""});}}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-purple-600 hover:bg-purple-700 text-white text-sm"><Plus className="w-4 h-4"/>Yeni Plan</button>
      </div>

      {showForm && (
        <div className="p-4 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)] space-y-3">
          <h4 className="text-sm font-semibold">{editingId?"Plan Düzenle":"Yeni Test Planı"}</h4>
          <input value={form.title} onChange={e=>setForm({...form,title:e.target.value})} placeholder="Başlık *" className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          <textarea value={form.description} onChange={e=>setForm({...form,description:e.target.value})} placeholder="Açıklama" rows={2} className="w-full px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          <div className="grid grid-cols-2 gap-3">
            <input type="date" value={form.startDate} onChange={e=>setForm({...form,startDate:e.target.value})} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
            <input type="date" value={form.endDate} onChange={e=>setForm({...form,endDate:e.target.value})} className="px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] text-sm"/>
          </div>
          <div className="flex gap-2 justify-end">
            <button onClick={()=>{setShowForm(false);setEditingId(null);}} className="px-3 py-1.5 rounded-lg text-sm hover:bg-[var(--color-bg-tertiary)]"><X className="w-4 h-4 inline mr-1"/>İptal</button>
            <button onClick={handleSave} disabled={!form.title} className="px-4 py-1.5 rounded-lg bg-purple-600 hover:bg-purple-700 text-white text-sm disabled:opacity-50"><Save className="w-4 h-4 inline mr-1"/>Kaydet</button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Plans list */}
        <div className="space-y-2">
          {loading ? <div className="text-center py-8 text-[var(--color-text-muted)]">Yükleniyor...</div> :
           plans.length===0 ? <div className="text-center py-8 text-[var(--color-text-muted)]">Henüz plan yok</div> :
           plans.map(p => (
            <div key={p.id} onClick={()=>setSelectedId(p.id)}
              className={cn("p-3 rounded-lg cursor-pointer transition-colors border",
                selectedId===p.id?"bg-purple-500/10 border-purple-500/30":"bg-[var(--color-bg-secondary)] border-transparent hover:bg-[var(--color-bg-tertiary)]")}>
              <div className="flex items-center justify-between mb-1">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-mono text-[var(--color-text-muted)]">{p.key}</span>
                  <span className="text-sm font-medium">{p.title}</span>
                </div>
                <div className="flex items-center gap-1">
                  <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-medium", STATUS_COLORS[p.status]||"")}>{p.status}</span>
                  <button onClick={e=>{e.stopPropagation();handleDelete(p.id);}} className="p-1 rounded hover:bg-red-500/20 text-red-400"><Trash2 className="w-3.5 h-3.5"/></button>
                </div>
              </div>
              <div className="flex items-center gap-3 text-[10px] text-[var(--color-text-muted)]">
                <span>{p.scenarioCount} senaryo</span>
                <span className="text-green-400">✓ {p.passCount}</span>
                <span className="text-red-400">✗ {p.failCount}</span>
                <span>○ {p.notRunCount}</span>
              </div>
              {p.scenarioCount > 0 && <div className="mt-1.5">{progressBar(p)}</div>}
            </div>
          ))}
        </div>

        {/* Detail */}
        <div>
          {detail ? (
            <div className="p-4 rounded-xl bg-[var(--color-bg-secondary)] border border-[var(--color-border)] space-y-4">
              <div className="flex items-center justify-between">
                <div><span className="text-xs font-mono text-[var(--color-text-muted)]">{detail.key}</span> <span className="text-base font-semibold ml-2">{detail.title}</span></div>
                <div className="flex gap-1">
                  {detail.status==="Draft" && <button onClick={()=>handleStatusChange(detail.id,"Active")} className="text-xs px-2 py-1 rounded bg-blue-600 text-white">Aktif Et</button>}
                  {detail.status==="Active" && <button onClick={()=>handleStatusChange(detail.id,"InExecution")} className="text-xs px-2 py-1 rounded bg-yellow-600 text-white">Çalıştır</button>}
                  {detail.status==="InExecution" && <button onClick={()=>handleStatusChange(detail.id,"Completed")} className="text-xs px-2 py-1 rounded bg-green-600 text-white">Tamamla</button>}
                </div>
              </div>
              {detail.description && <p className="text-sm text-[var(--color-text-muted)]">{detail.description}</p>}

              <div className="flex items-center justify-between">
                <h5 className="text-sm font-semibold">Senaryolar ({detail.scenarios?.length||0})</h5>
                <button onClick={()=>{fetchAllScenarios();setShowAddScenario(!showAddScenario);}} className="text-xs px-2 py-1 rounded bg-blue-600 hover:bg-blue-700 text-white"><Plus className="w-3 h-3 inline mr-0.5"/>Ekle</button>
              </div>

              {showAddScenario && (
                <div className="p-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)] max-h-40 overflow-y-auto space-y-1">
                  {allScenarios.filter(s => !detail.scenarios?.some(ds => ds.testScenarioId === s.id)).map(s => (
                    <div key={s.id} onClick={()=>handleAddScenario(s.id)} className="flex items-center gap-2 px-2 py-1.5 rounded hover:bg-[var(--color-bg-tertiary)] cursor-pointer text-xs">
                      <span className="font-mono text-[var(--color-text-muted)]">{s.key}</span><span>{s.title}</span>
                    </div>
                  ))}
                  {allScenarios.filter(s => !detail.scenarios?.some(ds => ds.testScenarioId === s.id)).length === 0 && <p className="text-xs text-center text-[var(--color-text-muted)] py-2">Eklenecek senaryo kalmadı</p>}
                </div>
              )}

              {detail.scenarios?.map(s => (
                <div key={s.testPlanScenarioId} className="flex items-center gap-3 px-3 py-2 rounded-lg bg-[var(--color-bg-primary)] border border-[var(--color-border)]">
                  <div className="w-5">{s.lastResult ? RESULT_ICONS[s.lastResult] || RESULT_ICONS.NotRun : RESULT_ICONS.NotRun}</div>
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium truncate">{s.testScenarioKey} — {s.testScenarioTitle}</div>
                    <div className="text-[10px] text-[var(--color-text-muted)]">{s.lastResult||"NotRun"} {s.lastExecutedAt ? `• ${new Date(s.lastExecutedAt).toLocaleDateString("tr-TR")}` : ""}</div>
                  </div>
                  <div className="flex gap-1">
                    <button onClick={()=>setExecForm({scenarioId:s.testScenarioId,result:"Pass",notes:""})} className="p-1 rounded hover:bg-green-500/20 text-green-400" title="Çalıştır"><Play className="w-3.5 h-3.5"/></button>
                    <button onClick={()=>handleRemoveScenario(s.testScenarioId)} className="p-1 rounded hover:bg-red-500/20 text-red-400" title="Kaldır"><Trash2 className="w-3.5 h-3.5"/></button>
                  </div>
                </div>
              ))}

              {execForm && (
                <div className="p-3 rounded-lg bg-[var(--color-bg-primary)] border border-blue-500/30 space-y-2">
                  <h5 className="text-xs font-semibold">Test Sonucu Kaydet</h5>
                  <select value={execForm.result} onChange={e=>setExecForm({...execForm,result:e.target.value})} className="w-full px-3 py-1.5 rounded bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-sm">
                    {["Pass","Fail","Blocked","Skipped"].map(r=><option key={r}>{r}</option>)}
                  </select>
                  <textarea value={execForm.notes} onChange={e=>setExecForm({...execForm,notes:e.target.value})} placeholder="Notlar" rows={2} className="w-full px-3 py-1.5 rounded bg-[var(--color-bg-secondary)] border border-[var(--color-border)] text-xs"/>
                  <div className="flex gap-2 justify-end">
                    <button onClick={()=>setExecForm(null)} className="text-xs px-2 py-1 rounded hover:bg-[var(--color-bg-tertiary)]">İptal</button>
                    <button onClick={handleExecute} className="text-xs px-3 py-1 rounded bg-green-600 hover:bg-green-700 text-white">Kaydet</button>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="flex items-center justify-center h-64 text-[var(--color-text-muted)] text-sm">Detay görüntülemek için bir plan seçin</div>
          )}
        </div>
      </div>
    </div>
  );
}
