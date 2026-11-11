import { expect, test } from "@playwright/test";

test("register, sign in, and see a completed mock investigation", async ({ page }) => {
  const email = `e2e-${Date.now()}@example.com`;

  await page.goto("/register");
  await page.getByLabel("First name").fill("Ada");
  await page.getByLabel("Last name").fill("Lovelace");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill("CorrectHorse1");
  await page.getByRole("button", { name: "Create an account" }).click();
  await expect(page.getByRole("heading", { name: "Workspace" })).toBeVisible();

  await page.getByRole("button", { name: "Sign out" }).click();
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill("CorrectHorse1");
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("heading", { name: "Workspace" })).toBeVisible();

  await page.getByRole("link", { name: "New investigation" }).first().click();
  await page.locator("textarea").fill("Vitamin C prevents the common cold.");
  await page.getByRole("button", { name: "Analyze claim" }).click();

  await expect(page.getByText("This is not the probability the claim is true.")).toBeVisible({ timeout: 60_000 });
  await expect(page.getByText("These sources are development fixtures, not live web search.")).toBeVisible();
});
