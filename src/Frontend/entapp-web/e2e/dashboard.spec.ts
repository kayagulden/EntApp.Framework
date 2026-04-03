import { test, expect } from "@playwright/test";

test.describe("Dashboard", () => {
  test("should load dashboard page", async ({ page }) => {
    await page.goto("/dashboard");
    await expect(page).toHaveTitle(/EntApp/);
  });

  test("should display stat cards", async ({ page }) => {
    await page.goto("/dashboard");
    await expect(page.getByText("Toplam Kullanıcı")).toBeVisible();
    await expect(page.getByText("Aktif Oturum")).toBeVisible();
  });

  test("should navigate to users page", async ({ page }) => {
    await page.goto("/dashboard");
    await page.click('a[href="/dashboard/users"]');
    await expect(page.getByText("Kullanıcılar")).toBeVisible();
  });

  test("should toggle theme", async ({ page }) => {
    await page.goto("/dashboard");
    const html = page.locator("html");

    // Dark mode by default
    await expect(html).toHaveClass(/dark/);

    // Toggle to light
    await page.click('button[aria-label="Toggle theme"]');
    await expect(html).not.toHaveClass(/dark/);
  });
});
