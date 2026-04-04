import axios from "axios";

// API istekleri Next.js rewrite proxy üzerinden gider (next.config.ts → localhost:5212)
// baseURL boş bırakılır, böylece /api/v1/... yolları proxy'den geçer.
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 15000,
});

// ── JWT Interceptor ──────────────────────────────────
apiClient.interceptors.request.use(
  (config) => {
    // Token varsa header'a ekle
    if (typeof window !== "undefined") {
      const token = sessionStorage.getItem("access_token");
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ── Response Interceptor ─────────────────────────────
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Token expired — redirect to login
      if (typeof window !== "undefined") {
        sessionStorage.removeItem("access_token");
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
