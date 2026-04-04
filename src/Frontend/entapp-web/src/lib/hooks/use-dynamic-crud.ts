import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { PagedResult, PagedRequest, LookupDto } from "@/types/dynamic";

const DYNAMIC_API_BASE = "/api/v1/dynamic";

/**
 * Sayfalanmış entity listesi.
 */
export function useDynamicList(
  entityName: string | undefined,
  params: PagedRequest = {}
) {
  return useQuery<PagedResult<Record<string, unknown>>>({
    queryKey: ["dynamic-list", entityName, params],
    queryFn: async () => {
      const { data } = await apiClient.get<
        PagedResult<Record<string, unknown>>
      >(`${DYNAMIC_API_BASE}/${entityName}`, {
        params: {
          pageNumber: params.pageNumber ?? 1,
          pageSize: params.pageSize ?? 20,
          sortBy: params.sortBy,
          sortDescending: params.sortDescending,
          search: params.search,
        },
      });
      return data;
    },
    enabled: !!entityName,
    placeholderData: (previousData) => previousData,
  });
}

/**
 * Tekil kayıt.
 */
export function useDynamicGetById(
  entityName: string | undefined,
  id: string | undefined
) {
  return useQuery<Record<string, unknown>>({
    queryKey: ["dynamic-detail", entityName, id],
    queryFn: async () => {
      const { data } = await apiClient.get<Record<string, unknown>>(
        `${DYNAMIC_API_BASE}/${entityName}/${id}`
      );
      return data;
    },
    enabled: !!entityName && !!id,
  });
}

/**
 * Yeni kayıt oluştur.
 */
export function useDynamicCreate(entityName: string) {
  const queryClient = useQueryClient();

  return useMutation<{ id: string }, Error, Record<string, unknown>>({
    mutationFn: async (body) => {
      const { data } = await apiClient.post<{ id: string }>(
        `${DYNAMIC_API_BASE}/${entityName}`,
        body
      );
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["dynamic-list", entityName],
      });
    },
  });
}

/**
 * Kayıt güncelle.
 */
export function useDynamicUpdate(entityName: string) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    Error,
    { id: string; body: Record<string, unknown> }
  >({
    mutationFn: async ({ id, body }) => {
      await apiClient.put(`${DYNAMIC_API_BASE}/${entityName}/${id}`, body);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["dynamic-list", entityName],
      });
    },
  });
}

/**
 * Kayıt sil (soft delete).
 */
export function useDynamicDelete(entityName: string) {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: async (id) => {
      await apiClient.delete(`${DYNAMIC_API_BASE}/${entityName}/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["dynamic-list", entityName],
      });
    },
  });
}

/**
 * Lookup arama (dropdown/combobox için).
 */
export function useDynamicLookup(
  entityName: string | undefined,
  search?: string
) {
  return useQuery<LookupDto[]>({
    queryKey: ["dynamic-lookup", entityName, search],
    queryFn: async () => {
      const { data } = await apiClient.get<LookupDto[]>(
        `${DYNAMIC_API_BASE}/${entityName}/lookup`,
        { params: { search, take: 20 } }
      );
      return data;
    },
    enabled: !!entityName,
    staleTime: 30 * 1000,
  });
}
