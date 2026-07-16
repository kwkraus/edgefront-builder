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
          EdgeFront Builder is a webinar management platform focused on local
          event planning, participation data, and engagement analytics. It
          helps organizations manage webinar series and individual sessions
          while preparing event data for ingestion-first reporting workflows.
        </p>
        <ul className="list-disc list-inside space-y-1" style={bodyStyle}>
          <li>Series management</li>
          <li>Registration &amp; attendance tracking</li>
          <li>Engagement metrics &amp; analytics</li>
        </ul>
      </section>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold" style={headingStyle}>
          Our Team
        </h2>
        <p style={bodyStyle}>
          Built by EdgeFront — a team focused on making enterprise webinar
          management simple and effective. We are committed to helping
          organizations measure and improve webinar engagement with clear,
          reliable event data.
        </p>
      </section>
    </article>
  );
}
