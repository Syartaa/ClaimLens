export function healthLabel(status: string | undefined): string {
  if (status === "ok") {
    return "API is reachable";
  }

  if (status === "unavailable") {
    return "API is unavailable";
  }

  return "Checking the API";
}
