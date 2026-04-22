"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getFlowDefinition, type FlowDefinitionDetailDto } from "@/lib/api/state-flow";
import { useStateFlowDesignerStore } from "@/stores/state-flow-store";
import { StateFlowDesigner } from "@/components/state-flow/StateFlowDesigner";

export default function FlowDesignerPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const store = useStateFlowDesignerStore();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        const flow = await getFlowDefinition(id);
        if (!flow) {
          setError("Akış bulunamadı");
          return;
        }
        store.setFlow(flow.id, flow.name, flow.status, flow.version, flow.entityType);
        store.setStates(flow.states);
        store.setTransitions(flow.transitions);
        store.setDirty(false);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Yükleme hatası");
      } finally {
        setLoading(false);
      }
    }
    load();

    return () => {
      store.reset();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  if (loading) {
    return (
      <div className="designer-loading">
        <div className="designer-loading__spinner" />
        <p>Akış yükleniyor...</p>
        <style jsx>{`
          .designer-loading {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 80vh;
            gap: 16px;
            color: #888;
            font-family: 'Inter', sans-serif;
          }
          .designer-loading__spinner {
            width: 32px;
            height: 32px;
            border: 3px solid rgba(139,92,246,0.2);
            border-top-color: #8b5cf6;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
          }
          @keyframes spin { to { transform: rotate(360deg); } }
        `}</style>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: 48, textAlign: "center", color: "#ef4444" }}>
        <h2>Hata</h2>
        <p>{error}</p>
        <button onClick={() => router.push("/admin/state-flows")} style={{ marginTop: 16, color: "#8b5cf6", background: "none", border: "none", cursor: "pointer" }}>
          ← Listeye Dön
        </button>
      </div>
    );
  }

  return (
    <div className="designer-page">
      <div className="designer-page__header">
        <button
          className="designer-page__back"
          onClick={() => router.push("/admin/state-flows")}
        >
          ← Geri
        </button>
        <h2 className="designer-page__title">
          {store.flowName}
          <span className="designer-page__meta">
            {store.entityType} • v{store.flowVersion}
          </span>
        </h2>
        {store.isDirty && <span className="designer-page__dirty">● Kaydedilmemiş değişiklikler</span>}
      </div>
      <StateFlowDesigner flowId={id} />

      <style jsx>{`
        .designer-page {
          display: flex;
          flex-direction: column;
          height: calc(100vh - 64px);
          font-family: 'Inter', sans-serif;
        }
        .designer-page__header {
          display: flex;
          align-items: center;
          gap: 16px;
          padding: 12px 24px;
          background: rgba(0,0,0,0.3);
          border-bottom: 1px solid rgba(255,255,255,0.06);
        }
        .designer-page__back {
          background: none;
          border: none;
          color: #8b5cf6;
          cursor: pointer;
          font-size: 14px;
          font-weight: 500;
          padding: 4px 8px;
          border-radius: 6px;
          transition: background 0.2s;
        }
        .designer-page__back:hover { background: rgba(139,92,246,0.1); }
        .designer-page__title {
          font-size: 16px;
          font-weight: 600;
          color: #e0e0e0;
          display: flex;
          align-items: center;
          gap: 10px;
        }
        .designer-page__meta {
          font-size: 12px;
          font-weight: 400;
          color: #888;
        }
        .designer-page__dirty {
          font-size: 12px;
          color: #f59e0b;
          margin-left: auto;
        }
      `}</style>
    </div>
  );
}
