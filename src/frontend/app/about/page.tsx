import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "About - EdgeFront Builder",
};

const headingStyle: React.CSSProperties = {
  color: "var(--fgColor-default, var(--color-fg-default))",
};

const bodyStyle: React.CSSProperties = {
  color: "var(--fgColor-muted, var(--color-fg-muted))",
  lineHeight: 1.7,
};

export default function AboutPage() {
  return (
    <article className="mx-auto max-w-3xl space-y-10">
      <h1 className="text-3xl font-bold tracking-tight" style={headingStyle}>
        About EdgeFront Builder
      </h1>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold" style={headingStyle}>
          The Application
        </h2>
        <p style={bodyStyle}>
          EdgeFront Builder is a local-first webinar analytics workspace. Teams
          sign-in stays in place for secure access, while series and sessions
          are managed locally and enriched with CSV imports for registrations,
          attendance, and Q&amp;A analytics.
        </p>
        <ul className="list-disc list-inside space-y-1" style={bodyStyle}>
          <li>Local series and session management</li>
          <li>Session-scoped CSV imports</li>
          <li>Registration, attendance, and Q&amp;A summaries</li>
          <li>Engagement metrics and account influence analytics</li>
        </ul>
      </section>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold" style={headingStyle}>
          Our Team
        </h2>
        <p style={bodyStyle}>
          Built by EdgeFront — a team focused on making enterprise webinar
          analytics simple and effective. We are committed to secure Microsoft
          365 sign-in and to helping organizations measure and improve webinar
          engagement from local datasets.
        </p>
      </section>
    </article>
  );
}
