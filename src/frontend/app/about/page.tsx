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
          EdgeFront Builder is a webinar management platform that integrates
          with Microsoft Teams. It helps organizations plan, publish, and track
          webinar series and individual sessions — streamlining the workflow
          from draft to published webinar with real-time sync from Teams.
        </p>
        <ul className="list-disc list-inside space-y-1" style={bodyStyle}>
          <li>Series management</li>
          <li>Teams webinar publishing</li>
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
          management simple and effective. We are committed to seamless
          Microsoft 365 integration and to helping organizations measure and
          improve their webinar engagement.
        </p>
      </section>
    </article>
  );
}
