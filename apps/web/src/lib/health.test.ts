import { expect, test } from "vitest";
import { healthLabel } from "./health";

test("describes a healthy API", () => {
  expect(healthLabel("ok")).toBe("API is reachable");
});

test("describes an unreachable API", () => {
  expect(healthLabel("unavailable")).toBe("API is unavailable");
});

test("describes a check still in progress", () => {
  expect(healthLabel(undefined)).toBe("Checking the API");
});
