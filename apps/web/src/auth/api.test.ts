import { describe, expect, it } from "vitest";
import { describeApiError } from "./api";

describe("describeApiError", () => {
  it("uses the API message for a failed login", () => {
    const error = describeApiError(401, {
      code: "INVALID_CREDENTIALS",
      message: "Email or password is incorrect.",
    });

    expect(error.message).toBe("Email or password is incorrect.");
    expect(error.code).toBe("INVALID_CREDENTIALS");
  });

  it("lists field errors from a validation response", () => {
    const error = describeApiError(400, {
      code: "VALIDATION_FAILED",
      message: "The request is invalid.",
      details: [{ field: "password", message: "Password must include a digit." }],
    });

    expect(error.message).toBe("The request is invalid.");
    expect(error.details).toEqual(["Password must include a digit."]);
  });
});
