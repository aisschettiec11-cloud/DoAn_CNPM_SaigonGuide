import { test, expect } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
const bootstrapFile = path.resolve("../../.local/bootstrap.env");
const creds = () => {
  const lines = fs.readFileSync(bootstrapFile, "utf8").trim().split("\n");
  return Object.fromEntries(
    lines.map((l) => {
      const i = l.indexOf("=");
      return [l.slice(0, i), l.slice(i + 1)];
    }),
  );
};
async function decline(page: import("@playwright/test").Page) {
  const button = page.getByRole("button", { name: "Từ chối", exact: true });
  if (await button.isVisible()) await button.click();
}
test("Guest explores six bilingual landmarks without an account", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await page.goto("/");
  await decline(page);
  await expect(page.locator(".place-card")).toHaveCount(6);
  await page.getByRole("button", { name: "EN", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Where to today?" }),
  ).toBeVisible();
  await expect(page.locator(".place-card").first()).toContainText(
    "War Remnants Museum",
  );
  await page.locator(".place-card").first().click();
  await expect(page.locator(".transcript")).toContainText("museum");
  await expect(
    page.getByRole("button", { name: "Listen to the story" }),
  ).toBeVisible();
  expect(errors).toEqual([]);
});
test("GPS denial preserves manual reading and real audio playback", async ({
  page,
}) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, "geolocation", {
      value: {
        watchPosition: (_s: unknown, error: (e: { code: number }) => void) => {
          setTimeout(() => error({ code: 1 }), 0);
          return 1;
        },
        clearWatch: () => {},
      },
    });
  });
  await page.goto("/");
  await decline(page);
  await page.getByRole("button", { name: "Bật định vị" }).click();
  await expect(page.locator(".gps-status")).toContainText("từ chối");
  await page.locator(".place-card").first().click();
  await page
    .getByRole("button", { name: "Nghe thuyết minh", exact: true })
    .click();
  await expect(
    page.getByRole("button", { name: "Tạm dừng", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Tạm dừng", exact: true }).click();
  await expect(page.locator(".global-player")).toContainText("Đã tạm dừng");
  await page
    .getByRole("button", { name: "Dừng thuyết minh", exact: true })
    .click();
  await expect(page.locator(".transcript")).toBeVisible();
});
test("Search empty state and tile failure keep the list usable", async ({
  page,
}) => {
  await page.route("**/api/v1/tiles/**", (r) => r.abort());
  await page.goto("/");
  await decline(page);
  await expect(page.locator(".place-card")).toHaveCount(6);
  await page
    .getByRole("textbox", { name: "Tìm địa điểm", exact: true })
    .fill("khong-ton-tai");
  await expect(
    page.getByRole("heading", { name: "Chưa tìm thấy điểm đến" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Xem tất cả" }).click();
  await expect(page.locator(".place-card")).toHaveCount(6);
});
test("Registration, own profile, User403 and logout", async ({ page }) => {
  await page.goto("/register");
  await decline(page);
  await page.getByLabel("Tên hiển thị").fill("Khách kiểm thử");
  await page
    .getByLabel("Email", { exact: true })
    .fill(`e2e-${Date.now()}@example.com`);
  await page.locator("input[type=password]").fill("E2e-private-password-123");
  await page
    .getByRole("button", { name: "Tạo tài khoản", exact: true })
    .click();
  await expect(page).toHaveURL(/\/$/);
  await page.goto("/account");
  await expect(
    page.getByRole("heading", { name: "Hồ sơ của bạn" }),
  ).toBeVisible();
  await page.getByLabel("Tên hiển thị").fill("Tên đã sửa");
  await page.getByRole("button", { name: "Lưu hồ sơ" }).click();
  await expect(page.getByRole("status")).toContainText("Đã lưu");
  const r = await page.request.get("/api/v1/admin/dashboard");
  expect(r.status()).toBe(403);
  await page.getByRole("button", { name: "Đăng xuất", exact: true }).click();
  await expect(page.getByRole("link", { name: "Đăng nhập" })).toBeVisible();
});
test("Admin edits a landmark and reads real dashboard aggregates", async ({
  page,
}) => {
  const c = creds();
  await page.goto("/login");
  await decline(page);
  await page.getByLabel("Email", { exact: true }).fill(c.Bootstrap__AdminEmail);
  await page.locator("input[type=password]").fill(c.Bootstrap__AdminPassword);
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Nhìn lại những hành trình." }),
  ).toBeVisible();
  await expect(page.locator(".kpi")).toHaveCount(4);
  await page.getByRole("link", { name: "Địa điểm & nội dung" }).click();
  await page
    .getByRole("button", { name: "Sửa dinh-doc-lap", exact: true })
    .click();
  await expect(
    page.getByRole("heading", { name: "Biên tập câu chuyện." }),
  ).toBeVisible();
  const input = page.getByRole("textbox", { name: "Mô tả ngắn", exact: true });
  const previous = await input.inputValue();
  await input.fill(previous + " ");
  await page.getByRole("button", { name: "Lưu nội dung", exact: true }).click();
  await expect(page.getByRole("status")).toContainText("Đã lưu");
  await input.fill(previous);
  await page.getByRole("button", { name: "Lưu nội dung", exact: true }).click();
  await expect(page.getByRole("status")).toContainText("Đã lưu");
});
test("Mobile layout fits 390px with working navigation", async ({ page }) => {
  for (const width of [360, 390, 768, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto("/");
    await expect(page.locator(".place-card")).toHaveCount(6);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await decline(page);
  await expect(page.locator(".place-card")).toHaveCount(6);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
  await page.screenshot({
    path: "test-results/mobile-home.png",
    fullPage: true,
  });
});
test("Desktop product screenshot", async ({ page }) => {
  await page.goto("/");
  await decline(page);
  await expect(page.locator(".place-card")).toHaveCount(6);
  await expect
    .poll(() => page.locator(".leaflet-tile-loaded").count())
    .toBeGreaterThan(0);
  await page.screenshot({
    path: "test-results/desktop-home.png",
    fullPage: true,
  });
});

test("Consent records accepted views and playback, withdrawal stops collection", async ({
  page,
}) => {
  const requests: { type: string }[] = [];
  page.on("request", (r) => {
    if (r.url().endsWith("/analytics/events"))
      try {
        requests.push(...r.postDataJSON().events);
      } catch {
        /* Not an event body */
      }
  });
  await page.goto("/");
  await page.getByRole("button", { name: "Đồng ý", exact: true }).click();
  await expect(page.locator(".consent-banner")).toHaveCount(0);
  await expect
    .poll(() => requests.filter((x) => x.type === "page_view").length)
    .toBe(1);
  const view = page.waitForResponse(
    async (r) =>
      r.url().endsWith("/analytics/events") &&
      r
        .request()
        .postDataJSON()
        ?.events?.some((e: { type: string }) => e.type === "place_view"),
  );
  await page.locator(".place-card").first().click();
  expect((await (await view).json()).results[0].status).toBe("accepted");
  const started = page.waitForResponse(
    async (r) =>
      r.url().endsWith("/analytics/events") &&
      r
        .request()
        .postDataJSON()
        ?.events?.some((e: { type: string }) => e.type === "narration_started"),
  );
  await page
    .getByRole("button", { name: "Nghe thuyết minh", exact: true })
    .click();
  expect((await (await started).json()).results[0].status).toBe("accepted");
  await page.getByRole("button", { name: "Tạm dừng", exact: true }).click();
  await page
    .getByRole("button", { name: "Tiếp tục phát", exact: true })
    .click();
  await expect(
    page.getByRole("button", { name: "Tạm dừng", exact: true }),
  ).toBeVisible();
  expect(requests.filter((x) => x.type === "narration_started")).toHaveLength(
    1,
  );
  await page
    .getByRole("button", { name: "Dừng thuyết minh", exact: true })
    .click();
  await page
    .getByRole("link", { name: "Quyền riêng tư", exact: true })
    .first()
    .click();
  await page.getByRole("button", { name: "Từ chối / thu hồi" }).click();
  await expect(page.locator(".privacy-page .success")).toContainText(
    "Đã dừng thống kê",
  );
  const before = requests.length;
  await page.getByRole("link", { name: "Khám phá", exact: true }).click();
  await expect(page.locator(".place-card")).toHaveCount(6);
  await page.locator(".place-card").first().click();
  await expect(page.locator(".transcript")).toBeVisible();
  expect(requests).toHaveLength(before);
});
