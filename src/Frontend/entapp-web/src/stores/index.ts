import { create } from "zustand";
import { persist } from "zustand/middleware";

// ── Seed Users (Dev Mode) ───────────────────────────
export const DEV_USERS = [
  { id: "868b6d11-0110-4182-9cc3-9a155f140fe4", userName: "ahmet.yilmaz", fullName: "Ahmet Yılmaz", role: "IT Lead" },
  { id: "b7dd400d-9aa8-4cc3-b973-a362e34ff39b", userName: "elif.demir", fullName: "Elif Demir", role: "IT Support" },
  { id: "dfbd1ff2-8ba9-424d-8e3b-00e5e35d8edc", userName: "mehmet.kaya", fullName: "Mehmet Kaya", role: "DBA" },
  { id: "84188840-d0dc-4080-845a-c0f25192ce22", userName: "ayse.celik", fullName: "Ayşe Çelik", role: "Analyst" },
  { id: "96a07d00-94c7-4f08-bc30-80b32fbbb139", userName: "can.ozturk", fullName: "Can Öztürk", role: "Manager" },
] as const;

export type DevUser = (typeof DEV_USERS)[number];

// ── Auth Store ──────────────────────────────────────
interface AuthState {
  isAuthenticated: boolean;
  user: {
    id: string;
    userName: string;
    fullName: string;
    role: string;
  } | null;
  setUser: (user: AuthState["user"]) => void;
  loginAs: (userId: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      isAuthenticated: true,
      user: { ...DEV_USERS[0] },
      setUser: (user) => set({ isAuthenticated: !!user, user }),
      loginAs: (userId: string) => {
        const found = DEV_USERS.find((u) => u.id === userId);
        if (found) {
          set({ isAuthenticated: true, user: { ...found } });
        }
      },
      logout: () => {
        if (typeof window !== "undefined") {
          sessionStorage.removeItem("access_token");
        }
        set({ isAuthenticated: false, user: null });
      },
    }),
    {
      name: "entapp-auth",
      partialize: (state) => ({ user: state.user, isAuthenticated: state.isAuthenticated }),
    }
  )
);

// ── UI Store ────────────────────────────────────────
interface UiState {
  sidebarOpen: boolean;
  sidebarCollapsed: boolean;
  toggleSidebar: () => void;
  toggleCollapse: () => void;
  setSidebarOpen: (open: boolean) => void;
}

export const useUiStore = create<UiState>((set) => ({
  sidebarOpen: true,
  sidebarCollapsed: false,
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
  toggleCollapse: () =>
    set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),
  setSidebarOpen: (open) => set({ sidebarOpen: open }),
}));
