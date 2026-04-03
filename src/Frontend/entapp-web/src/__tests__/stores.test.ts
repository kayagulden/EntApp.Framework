import { describe, it, expect } from "vitest";
import { cn } from "../lib/utils";
import { useAuthStore, useUiStore } from "../stores";

describe("cn utility", () => {
  it("merges class names", () => {
    const result = cn("foo", "bar");
    expect(result).toContain("foo");
    expect(result).toContain("bar");
  });

  it("handles falsy values", () => {
    const result = cn("foo", undefined, false, "bar");
    expect(result).not.toContain("undefined");
  });
});

describe("Auth Store", () => {
  it("initializes with no user", () => {
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
  });

  it("sets user correctly", () => {
    const user = { id: "123", name: "Test", email: "t@t.com", roles: ["Admin"] };
    useAuthStore.getState().setUser(user);
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
    expect(useAuthStore.getState().user?.name).toBe("Test");
    useAuthStore.getState().logout();
  });

  it("logs out correctly", () => {
    useAuthStore.getState().setUser({ id: "1", name: "x", email: "x@x.com", roles: [] });
    useAuthStore.getState().logout();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(useAuthStore.getState().user).toBeNull();
  });
});

describe("UI Store", () => {
  it("initializes with sidebar open", () => {
    expect(useUiStore.getState().sidebarOpen).toBe(true);
    expect(useUiStore.getState().sidebarCollapsed).toBe(false);
  });

  it("toggles sidebar collapse", () => {
    useUiStore.getState().toggleCollapse();
    expect(useUiStore.getState().sidebarCollapsed).toBe(true);
    useUiStore.getState().toggleCollapse();
    expect(useUiStore.getState().sidebarCollapsed).toBe(false);
  });
});
