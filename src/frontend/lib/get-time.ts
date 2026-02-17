type TimeResponse = {
  time: string;
};

export async function getBackendTime(): Promise<string> {
  const backendBaseUrl = process.env.BACKEND_API_BASE_URL;

  if (!backendBaseUrl) {
    throw new Error("Missing BACKEND_API_BASE_URL environment variable.");
  }

  const response = await fetch(`${backendBaseUrl}/api/time`, {
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch time: ${response.status}`);
  }

  const data = (await response.json()) as TimeResponse;
  return data.time;
}
