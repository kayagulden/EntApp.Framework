"use client";
import { useState, useEffect, useCallback } from "react";
import { Plus, Trash2, Loader2, Save, X, AlertTriangle, Shield } from "lucide-react";
import { cn } from "@/lib/utils";

const API = "/api/pm";
const CAT_OPTS = ["Technical","Schedule","Budget","Resource","Scope","External","Organizational"];
const STATUS_OPTS = ["Open","Identified","Mitigating","Mitigated","Closed","Accepted"];
const MIT_STATUS = ["Planned","InProgress","Completed","Cancelled"];
const cellColor = (s:number) => s>=15?"bg-red-500/70":s>=10?"bg-orange-500/50":s>=5?"bg-yellow-500/40":"bg-green-500/40";

interface Risk { id:string; title:string; category:string; status:string; probability:number; impact:number; riskScore:number; ownerUserId?:string|null; mitigationActionCount:number; createdAt:string; }
interface RiskDetail { id:string; title:string; description?:string|null; category:string; status:string; probability:number; impact:number; riskScore:number; mitigationPlan?:string|null; ownerUserId?:string|null; createdAt:string; updatedAt?:string|null; mitigationActions?:MitAction[]|null; }
interface MitAction { id:string; title:string; description?:string|null; status:string; assigneeUserId?:string|null; dueDate?:string|null; completedAt?:string|null; createdAt:string; }
interface MatrixCell { probability:number; impact:number; riskScore:number; count:number; risks:{id:string;title:string;status:string}[]; }
interface Matrix { totalRisks:number; openRisks:number; mitigatedRisks:number; closedRisks:number; criticalCount:number; highCount:number; mediumCount:number; lowCount:number; cells:MatrixCell[]; }

export default function RisksTab({ projectId }: { projectId: string }) {
  const [risks, setRisks] = useState<Risk[]>([]);
  const [matrix, setMatrix] = useState<Matrix|null>(null);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<RiskDetail|null>(null);
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editId, setEditId] = useState<string|null>(null);
  const [form, setForm] = useState({title:"",category:"Technical",probability:"3",impact:"3",description:"",mitigationPlan:""});
  const [mitForm, setMitForm] = useState({title:"",description:"",dueDate:""});
  const [showMitForm, setShowMitForm] = useState(false);
  const [tab, setTab] = useState<"list"|"matrix">("list");

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [r1,r2] = await Promise.all([
        fetch(`${API}/projects/${projectId}/risks`).then(r=>r.ok?r.json():[]),
        fetch(`${API}/projects/${projectId}/risks/matrix`).then(r=>r.ok?r.json():null),
      ]);
      setRisks(r1); setMatrix(r2);
    } catch {} finally { setLoading(false); }
  }, [projectId]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const loadDetail = async (id:string) => {
    const r = await fetch(`${API}/risks/${id}`);
    if (r.ok) setSelected(await r.json());
  };

  const resetForm = () => { setShowForm(false); setEditId(null); setForm({title:"",category:"Technical",probability:"3",impact:"3",description:"",mitigationPlan:""}); };

  const handleSave = async () => {
    if (!form.title.trim()) return;
    setSaving(true);
    try {
      const body = { title:form.title, category:form.category, probability:+form.probability, impact:+form.impact, description:form.description||null, mitigationPlan:form.mitigationPlan||null };
      if (editId) await fetch(`${API}/risks/${editId}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
      else await fetch(`${API}/projects/${projectId}/risks`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify(body) });
      resetForm(); fetchData();
    } catch {} finally { setSaving(false); }
  };

  const handleDelete = async (id:string) => {
    if (!confirm("Risk silinecek. Emin misiniz?")) return;
    await fetch(`${API}/risks/${id}`, { method:"DELETE" });
    if (selected?.id===id) setSelected(null);
    fetchData();
  };

  const handleStatusChange = async (id:string, status:string) => {
    await fetch(`${API}/risks/${id}/status`, { method:"PATCH", headers:{"Content-Type":"application/json"}, body:JSON.stringify({status}) });
    fetchData(); if (selected?.id===id) loadDetail(id);
  };

  const addMitAction = async () => {
    if (!mitForm.title.trim()||!selected) return;
    setSaving(true);
    try {
      await fetch(`${API}/risks/${selected.id}/actions`, { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({title:mitForm.title,description:mitForm.description||null,dueDate:mitForm.dueDate||null}) });
      setMitForm({title:"",description:"",dueDate:""}); setShowMitForm(false); loadDetail(selected.id); fetchData();
    } catch {} finally { setSaving(false); }
  };

  const updateMitStatus = async (actionId:string, status:string) => {
    await fetch(`${API}/risks/actions/${actionId}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({status}) });
    if (selected) loadDetail(selected.id);
  };

  const delMitAction = async (actionId:string) => {
    await fetch(`${API}/risks/actions/${actionId}`, { method:"DELETE" });
    if (selected) loadDetail(selected.id); fetchData();
  };

  const fmtDate = (d?:string|null) => d ? new Date(d).toLocaleDateString("tr-TR",{day:"2-digit",month:"short",year:"numeric"}) : "";
  const scoreLabel = (s:number) => s>=15?"Kritik":s>=10?"Yüksek":s>=5?"Orta":"Düşük";
  const scoreBadge = (s:number) => s>=15?"bg-red-500/20 text-red-400 border-red-500/30":s>=10?"bg-orange-500/20 text-orange-400 border-orange-500/30":s>=5?"bg-yellow-500/20 text-yellow-400 border-yellow-500/30":"bg-green-500/20 text-green-400 border-green-500/30";

  const startEdit = (r:Risk) => {
    setEditId(r.id);
    setForm({title:r.title,category:r.category,probability:String(r.probability),impact:String(r.impact),description:"",mitigationPlan:""});
    setShowForm(true);
  };

  return (
    <div className="flex gap-4 h-[calc(100vh-280px)]">
      <div className={cn("flex flex-col min-w-0 transition-all", selected ? "flex-1" : "w-full")}>
        {/* Toolbar */}
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <h3 className="text-sm font-semibold text-[var(--color-text)] flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-amber-400" /> Risk Yönetimi
              <span className="text-xs font-normal text-[var(--color-text-muted)]">({risks.length})</span>
            </h3>
            <div className="flex rounded-lg border border-[var(--color-border)] overflow-hidden ml-2">
              <button onClick={()=>setTab("list")} className={cn("px-3 py-1 text-[10px] font-medium",tab==="list"?"bg-indigo-600 text-white":"text-[var(--color-text-muted)]")}>Liste</button>
              <button onClick={()=>setTab("matrix")} className={cn("px-3 py-1 text-[10px] font-medium",tab==="matrix"?"bg-indigo-600 text-white":"text-[var(--color-text-muted)]")}>Matris</button>
            </div>
          </div>
          <button onClick={()=>{resetForm();setShowForm(true);}} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-medium transition-colors shadow-lg shadow-indigo-500/20">
            <Plus className="w-3.5 h-3.5" /> Yeni Risk
          </button>
        </div>

        {/* Summary Cards */}
        {matrix && (
          <div className="grid grid-cols-4 gap-2 mb-3">
            {[{l:"Kritik",v:matrix.criticalCount,c:"text-red-400 bg-red-500/10 border-red-500/20"},{l:"Yüksek",v:matrix.highCount,c:"text-orange-400 bg-orange-500/10 border-orange-500/20"},{l:"Orta",v:matrix.mediumCount,c:"text-yellow-400 bg-yellow-500/10 border-yellow-500/20"},{l:"Düşük",v:matrix.lowCount,c:"text-green-400 bg-green-500/10 border-green-500/20"}].map(x=>(
              <div key={x.l} className={cn("rounded-lg border p-2.5 text-center",x.c)}>
                <div className="text-lg font-bold">{x.v}</div>
                <div className="text-[10px] font-medium">{x.l}</div>
              </div>
            ))}
          </div>
        )}

        {/* Form */}
        {showForm && (
          <div className="mb-4 rounded-xl border border-indigo-500/30 bg-indigo-500/5 p-4 space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-semibold text-[var(--color-text)]">{editId?"Risk Düzenle":"Yeni Risk"}</h4>
              <button onClick={resetForm} className="p-1 rounded hover:bg-[var(--color-border)]"><X className="w-4 h-4 text-[var(--color-text-muted)]" /></button>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <input placeholder="Başlık *" value={form.title} onChange={e=>setForm(f=>({...f,title:e.target.value}))} className="px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] col-span-2 focus:ring-2 focus:ring-indigo-500/40 focus:outline-none" />
              <select value={form.category} onChange={e=>setForm(f=>({...f,category:e.target.value}))} className="px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                {CAT_OPTS.map(c=><option key={c} value={c}>{c}</option>)}
              </select>
              <div className="grid grid-cols-2 gap-2">
                <select value={form.probability} onChange={e=>setForm(f=>({...f,probability:e.target.value}))} className="px-2 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                  {[1,2,3,4,5].map(n=><option key={n} value={n}>P:{n}</option>)}
                </select>
                <select value={form.impact} onChange={e=>setForm(f=>({...f,impact:e.target.value}))} className="px-2 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]">
                  {[1,2,3,4,5].map(n=><option key={n} value={n}>I:{n}</option>)}
                </select>
              </div>
            </div>
            <textarea placeholder="Açıklama" value={form.description} onChange={e=>setForm(f=>({...f,description:e.target.value}))} rows={2} className="w-full px-3 py-2 rounded-lg text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] resize-none focus:ring-2 focus:ring-indigo-500/40 focus:outline-none" />
            <div className="flex gap-2">
              <button onClick={handleSave} disabled={saving||!form.title.trim()} className="flex items-center gap-1.5 px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-medium">
                {saving?<Loader2 className="w-3.5 h-3.5 animate-spin"/>:<Save className="w-3.5 h-3.5"/>} {editId?"Güncelle":"Oluştur"}
              </button>
              <button onClick={resetForm} className="px-4 py-2 rounded-lg text-xs border border-[var(--color-border)] text-[var(--color-text-muted)]">İptal</button>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <div className="flex items-center justify-center py-16"><Loader2 className="w-6 h-6 text-indigo-400 animate-spin" /></div>
          ) : tab==="matrix" && matrix ? (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] p-5">
              <h4 className="text-sm font-semibold text-[var(--color-text)] mb-4">5×5 Risk Matrisi</h4>
              <div className="flex gap-3">
                <div className="flex flex-col justify-between py-1 text-[9px] text-[var(--color-text-muted)] font-medium w-4 items-center">
                  {[5,4,3,2,1].map(p=><div key={p} className="h-14 flex items-center">{p}</div>)}
                </div>
                <div className="flex-1">
                  <div className="grid grid-cols-5 gap-1">
                    {[5,4,3,2,1].map(p=>
                      [1,2,3,4,5].map(i=>{
                        const cell = matrix.cells.find(c=>c.probability===p&&c.impact===i);
                        const score = p*i;
                        return (
                          <div key={`${p}-${i}`} className={cn("h-14 rounded-lg flex flex-col items-center justify-center cursor-default transition-all hover:scale-105 border border-white/10",cellColor(score))}
                            title={`P:${p} × I:${i} = ${score}${cell?.count?` (${cell.count} risk)`:""}`}>
                            <span className="text-xs font-bold text-white/90">{score}</span>
                            {cell&&cell.count>0&&<span className="text-[9px] font-medium text-white/80 bg-black/20 px-1.5 rounded-full mt-0.5">{cell.count}</span>}
                          </div>
                        );
                      })
                    )}
                  </div>
                  <div className="grid grid-cols-5 gap-1 mt-1">
                    {[1,2,3,4,5].map(i=><div key={i} className="text-center text-[9px] text-[var(--color-text-muted)] font-medium">{i}</div>)}
                  </div>
                  <div className="text-center text-[10px] text-[var(--color-text-muted)] mt-1 font-medium">Etki →</div>
                </div>
              </div>
              <div className="flex gap-4 mt-4 justify-center flex-wrap">
                {[{l:"Düşük (1-4)",c:"bg-green-500/40"},{l:"Orta (5-9)",c:"bg-yellow-500/40"},{l:"Yüksek (10-14)",c:"bg-orange-500/50"},{l:"Kritik (15-25)",c:"bg-red-500/70"}].map(x=>(
                  <div key={x.l} className="flex items-center gap-1.5"><div className={cn("w-3 h-3 rounded",x.c)}/><span className="text-[10px] text-[var(--color-text-muted)]">{x.l}</span></div>
                ))}
              </div>
            </div>
          ) : risks.length===0 ? (
            <div className="text-center py-16 text-[var(--color-text-muted)]">
              <AlertTriangle className="w-12 h-12 mx-auto opacity-20 mb-3" />
              <p className="text-sm">Henüz risk tanımlanmamış</p>
              <p className="text-xs mt-1">&quot;Yeni Risk&quot; butonuyla başlayın</p>
            </div>
          ) : (
            <div className="space-y-2">
              {risks.map(r=>(
                <div key={r.id} onClick={()=>loadDetail(r.id)}
                  className={cn("rounded-xl border p-3 cursor-pointer transition-all hover:shadow-lg group",
                    selected?.id===r.id?"border-indigo-500/40 bg-indigo-500/5 shadow-lg shadow-indigo-500/10":"border-[var(--color-border)] bg-[var(--color-card-bg)] hover:border-indigo-500/20")}>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2.5 flex-1 min-w-0">
                      <div className={cn("w-9 h-9 rounded-lg flex items-center justify-center text-xs font-bold text-white shrink-0",cellColor(r.riskScore))}>{r.riskScore}</div>
                      <div className="min-w-0">
                        <div className="text-sm font-medium text-[var(--color-text)] truncate">{r.title}</div>
                        <div className="flex items-center gap-2 mt-0.5 flex-wrap">
                          <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-slate-500/15 text-slate-400">{r.category}</span>
                          <span className={cn("text-[10px] px-1.5 py-0.5 rounded-full border font-medium",scoreBadge(r.riskScore))}>{scoreLabel(r.riskScore)}</span>
                          <span className="text-[10px] text-[var(--color-text-muted)]">P:{r.probability} × I:{r.impact}</span>
                          {r.mitigationActionCount>0&&<span className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-0.5"><Shield className="w-3 h-3"/> {r.mitigationActionCount}</span>}
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" onClick={e=>e.stopPropagation()}>
                      <select value={r.status} onChange={e=>handleStatusChange(r.id,e.target.value)} className="text-[9px] rounded border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-1 py-0.5">
                        {STATUS_OPTS.map(s=><option key={s} value={s}>{s}</option>)}
                      </select>
                      <button onClick={()=>startEdit(r)} className="p-1 rounded hover:bg-[var(--color-border)] text-[var(--color-text-muted)]"><Save className="w-3 h-3"/></button>
                      <button onClick={()=>handleDelete(r.id)} className="p-1 rounded hover:bg-red-500/10 text-[var(--color-text-muted)] hover:text-red-400"><Trash2 className="w-3 h-3"/></button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Detail Panel */}
      {selected && (
        <div className="w-[420px] shrink-0 border-l border-[var(--color-border)] pl-4 overflow-y-auto">
          <div className="flex items-center justify-between mb-3">
            <h4 className="text-sm font-semibold text-[var(--color-text)]">Risk Detayı</h4>
            <button onClick={()=>setSelected(null)} className="p-1 rounded hover:bg-[var(--color-border)]"><X className="w-4 h-4 text-[var(--color-text-muted)]"/></button>
          </div>
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <div className={cn("w-11 h-11 rounded-lg flex items-center justify-center text-sm font-bold text-white",cellColor(selected.riskScore))}>{selected.riskScore}</div>
              <div>
                <div className="text-sm font-semibold text-[var(--color-text)]">{selected.title}</div>
                <div className="text-[10px] text-[var(--color-text-muted)]">{selected.category} · P:{selected.probability} × I:{selected.impact}</div>
              </div>
            </div>
            {selected.description&&<p className="text-xs text-[var(--color-text-muted)] bg-[var(--color-bg)] rounded-lg p-2.5 border border-[var(--color-border)]">{selected.description}</p>}
            {selected.mitigationPlan&&<div><h5 className="text-[10px] font-semibold text-[var(--color-text)] mb-1">Azaltma Planı</h5><p className="text-xs text-[var(--color-text-muted)]">{selected.mitigationPlan}</p></div>}
            <div>
              <div className="flex items-center justify-between mb-2">
                <h5 className="text-xs font-semibold text-[var(--color-text)] flex items-center gap-1"><Shield className="w-3.5 h-3.5 text-emerald-400"/>Aksiyonlar</h5>
                <button onClick={()=>setShowMitForm(true)} className="text-[10px] text-indigo-400 hover:text-indigo-300 flex items-center gap-1"><Plus className="w-3 h-3"/>Ekle</button>
              </div>
              {showMitForm&&(
                <div className="mb-2 p-2.5 rounded-lg border border-emerald-500/30 bg-emerald-500/5 space-y-2">
                  <input placeholder="Aksiyon başlığı" value={mitForm.title} onChange={e=>setMitForm(f=>({...f,title:e.target.value}))} className="w-full px-2.5 py-1.5 rounded text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"/>
                  <input type="date" value={mitForm.dueDate} onChange={e=>setMitForm(f=>({...f,dueDate:e.target.value}))} className="w-full px-2.5 py-1.5 rounded text-xs bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)]"/>
                  <div className="flex gap-1">
                    <button onClick={addMitAction} disabled={!mitForm.title.trim()} className="px-3 py-1 rounded text-[10px] bg-emerald-600 text-white disabled:opacity-50">Ekle</button>
                    <button onClick={()=>setShowMitForm(false)} className="px-3 py-1 rounded text-[10px] border border-[var(--color-border)] text-[var(--color-text-muted)]">İptal</button>
                  </div>
                </div>
              )}
              <div className="space-y-1.5">
                {(selected.mitigationActions||[]).map(a=>(
                  <div key={a.id} className="flex items-center gap-2 p-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)]">
                    <div className="flex-1 min-w-0">
                      <div className="text-xs font-medium text-[var(--color-text)] truncate">{a.title}</div>
                      <div className="text-[10px] text-[var(--color-text-muted)]">{a.dueDate?fmtDate(a.dueDate):""} {a.completedAt?`✓ ${fmtDate(a.completedAt)}`:""}</div>
                    </div>
                    <select value={a.status} onChange={e=>updateMitStatus(a.id,e.target.value)} className="text-[9px] rounded border border-[var(--color-border)] bg-[var(--color-bg)] text-[var(--color-text)] px-1 py-0.5">
                      {MIT_STATUS.map(s=><option key={s} value={s}>{s}</option>)}
                    </select>
                    <button onClick={()=>delMitAction(a.id)} className="p-0.5 rounded hover:bg-red-500/10 text-[var(--color-text-muted)] hover:text-red-400"><Trash2 className="w-3 h-3"/></button>
                  </div>
                ))}
              </div>
            </div>
            <div className="text-[10px] text-[var(--color-text-muted)] pt-2 border-t border-[var(--color-border)]">Oluşturulma: {fmtDate(selected.createdAt)}</div>
          </div>
        </div>
      )}
    </div>
  );
}
