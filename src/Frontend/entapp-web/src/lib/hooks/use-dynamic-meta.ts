import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { EntityMetadata, MenuGroup } from "@/types/dynamic";

const DYNAMIC_API_BASE = "/api/v1/dynamic";

/**
 * Entity metadata'sını fetch eder ve cache'ler.
 * Metadata nadiren değişir → staleTime: 5dk.
 */
export function useDynamicMeta(entityName: string | undefined) {
  return useQuery<EntityMetadata>({
    queryKey: ["dynamic-meta", entityName],
    queryFn: async () => {
      const { data } = await apiClient.get<EntityMetadata>(
        `${DYNAMIC_API_BASE}/meta/${entityName}`
      );
      return data;
    },
    enabled: !!entityName,
    staleTime: 5 * 60 * 1000, // 5 dakika
    gcTime: 10 * 60 * 1000,
  });
}

/**
 * Sidebar dynamic menu'yü fetch eder.
 * Menu nadiren değişir → staleTime: 10dk.
 */
export function useDynamicMenu() {
  return useQuery<MenuGroup[]>({
    queryKey: ["dynamic-menu"],
    queryFn: async () => {
      const { data } = await apiClient.get<MenuGroup[]>(
        `${DYNAMIC_API_BASE}/meta/menu`
      );
      return data;
    },
    staleTime: 10 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
    retry: 1,
  });
}
