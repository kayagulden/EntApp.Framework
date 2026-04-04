"use client";

import { useEffect, useRef, useCallback } from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
  HubConnectionState,
} from "@microsoft/signalr";

export type ChangeType = "Created" | "Updated" | "Deleted";

export interface EntityChangePayload {
  entityType: string;
  entityId: string;
  changeType: ChangeType;
  data: unknown;
}

interface UseSignalROptions {
  /** Entity adı (ör: "Country") — gruba katılmak için */
  entityName: string;
  /** Değişiklik geldiğinde çağrılacak callback */
  onEntityChanged?: (payload: EntityChangePayload) => void;
  /** Bağlantı aktif mi? (ör: sayfa görünür değilse kapatılabilir) */
  enabled?: boolean;
}

const HUB_URL = "/hubs/entapp";

/**
 * SignalR hub'ına bağlanır, "{entityName}:list" grubuna katılır,
 * "EntityChanged" event'ini dinler.
 */
export function useSignalR({
  entityName,
  onEntityChanged,
  enabled = true,
}: UseSignalROptions) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbackRef = useRef(onEntityChanged);

  // Callback'i ref ile tut — effect dependency'den kaçın
  callbackRef.current = onEntityChanged;

  const connect = useCallback(async () => {
    if (!enabled || !entityName) return;

    // Zaten bağlıysa skip
    if (
      connectionRef.current &&
      connectionRef.current.state === HubConnectionState.Connected
    ) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    // Event handler
    connection.on("EntityChanged", (payload: EntityChangePayload) => {
      callbackRef.current?.(payload);
    });

    // Reconnect sonrası gruba tekrar katıl
    connection.onreconnected(async () => {
      try {
        await connection.invoke("JoinGroup", `${entityName}:list`);
      } catch {
        // reconnect join failed — ignore
      }
    });

    try {
      await connection.start();
      await connection.invoke("JoinGroup", `${entityName}:list`);
      connectionRef.current = connection;
    } catch (err) {
      console.warn("[SignalR] Connection failed:", err);
    }
  }, [entityName, enabled]);

  const disconnect = useCallback(async () => {
    const conn = connectionRef.current;
    if (!conn) return;

    try {
      if (conn.state === HubConnectionState.Connected) {
        await conn.invoke("LeaveGroup", `${entityName}:list`);
      }
      await conn.stop();
    } catch {
      // disconnect error — ignore
    }
    connectionRef.current = null;
  }, [entityName]);

  useEffect(() => {
    connect();
    return () => {
      disconnect();
    };
  }, [connect, disconnect]);

  return { connection: connectionRef.current };
}
