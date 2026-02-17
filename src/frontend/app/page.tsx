import { getBackendTime } from "@/lib/get-time";

export default async function Home() {
  const backendTime = await getBackendTime();

  return (
    <div className="flex min-h-screen items-center justify-center bg-background text-foreground">
      <main className="px-6 py-24 text-center">
        <h1 className="text-4xl font-semibold tracking-tight sm:text-5xl">
          EdgeFront Builder
        </h1>
        <p className="mt-4 text-base text-muted-foreground sm:text-lg">
          {backendTime}
        </p>
      </main>
    </div>
  );
}
