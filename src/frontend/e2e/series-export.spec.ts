/**
 * E2E tests for the "Download Series Data" export button on the series detail page.
 *
 * ── Auth strategy ────────────────────────────────────────────────────────────
 * A valid next-auth v4 session token is generated with `encode` from
 * `next-auth/jwt` (signed with NEXTAUTH_SECRET from .env.local) and injected
 * as the `next-auth.session-token` cookie before each test so that
 * `getServerSession(authOptions)` in the server component sees an authenticated
 * user and does not redirect to /login.
 *
 * ── Backend mocking strategy ─────────────────────────────────────────────────
 * All *browser-side* fetch calls (client hydration re-fetches and the export
 * API call itself) are intercepted via `page.route()`.
 *
 * IMPORTANT: The Next.js App Router server component that renders `/series/[id]`
 * makes server-side Node.js fetch calls to BACKEND_API_BASE_URL (localhost:5187).
 * Those requests originate from the dev-server Node process and are NOT
 * interceptable by Playwright's page.route(). They require a live backend or
 * a local stub service to be reachable at that address. For a fully offline
 * test run, point BACKEND_API_BASE_URL / NEXT_PUBLIC_BACKEND_API_BASE_URL to a
 * local mock HTTP server before starting `npm run dev`.
 *
 * The three tests below cover:
 *   1. Export button is visible on the series detail page.
 *   2. Clicking the button triggers a browser file download with the correct
 *      filename (resolved from the Content-Disposition header mock).
 *   3. A server error (HTTP 500) causes an error banner to appear on the page.
 */
import { test, expect, type BrowserContext } from '@playwright/test'
import { encode } from 'next-auth/jwt'

// ── Constants ─────────────────────────────────────────────────────────────────

/**
 * Must match NEXTAUTH_SECRET in .env.local so that the injected cookie passes
 * `getServerSession(authOptions)` inside the Next.js server component.
 */
const NEXTAUTH_SECRET = 'NFB3bPhTe11U9QEm+GQ72rjQ63e2Zhkn0dsC4lsWvq8='

/**
 * Arbitrary series ID used throughout these tests. The backend (or mock) must
 * serve this ID from GET /api/v1/series/:id for the page to render correctly.
 */
const SERIES_ID = 'e2e-test-series-001'

// ── Mock fixtures ─────────────────────────────────────────────────────────────

/** Minimal SeriesResponse shape expected by SeriesDetailView. */
const MOCK_SERIES = {
  seriesId: SERIES_ID,
  title: 'E2E Test Webinar Series',
  status: 'Draft',
  draftSessionCount: 0,
  publishedSessionCount: 0,
  createdAt: '2024-01-01T10:00:00.000Z',
  updatedAt: '2024-01-02T10:00:00.000Z',
}

/** Minimal SeriesMetricsResponse shape expected by MetricsPanel. */
const MOCK_METRICS = {
  totalRegistrations: 0,
  totalAttendees: 0,
  uniqueAccountsInfluenced: 0,
  warmAccounts: [],
}

// ── Auth helper ───────────────────────────────────────────────────────────────

/**
 * Generates a valid next-auth v4 JWE session token and injects it as the
 * `next-auth.session-token` cookie so that the Next.js server component's
 * `getServerSession()` call succeeds without a real Azure AD login flow.
 *
 * The token payload includes `accessToken` (used by SeriesDetailView to call
 * the backend on behalf of the authenticated user).
 */
async function injectSessionCookie(context: BrowserContext): Promise<void> {
  const sessionToken = await encode({
    token: {
      name: 'E2E Test User',
      email: 'e2e-test@example.com',
      sub: 'e2e-test-user-id',
      // accessToken is the custom JWT field declared in types/next-auth.d.ts;
      // it is passed to exportSeriesMarkdown() and all other API helpers.
      accessToken: 'e2e-test-access-token',
    },
    secret: NEXTAUTH_SECRET,
  })

  await context.addCookies([
    {
      name: 'next-auth.session-token', // HTTP (non-secure) cookie name used by next-auth v4
      value: sessionToken,
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      secure: false,
      sameSite: 'Lax',
    },
  ])
}

// ── Test suite ────────────────────────────────────────────────────────────────

test.describe('Series detail page — Download Series Data export', () => {
  /**
   * Before every test:
   *   1. Inject a valid session cookie so the server component doesn't redirect
   *      to /login.
   *   2. Register page.route() stubs for all browser-side backend calls made by
   *      SeriesDetailView after client hydration (series, sessions, metrics).
   *   3. Navigate to the series detail page.
   */
  test.beforeEach(async ({ context, page }) => {
    await injectSessionCookie(context)

    // Stub GET /api/v1/series/:id  (exact path — does NOT match sub-routes).
    await page.route(`**/api/v1/series/${SERIES_ID}`, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, json: MOCK_SERIES })
      } else {
        await route.continue()
      }
    })

    // Stub GET /api/v1/series/:id/sessions
    await page.route(`**/api/v1/series/${SERIES_ID}/sessions`, async (route) => {
      await route.fulfill({ status: 200, json: [] })
    })

    // Stub GET /api/v1/series/:id/metrics
    await page.route(`**/api/v1/series/${SERIES_ID}/metrics`, async (route) => {
      await route.fulfill({ status: 200, json: MOCK_METRICS })
    })

    await page.goto(`/series/${SERIES_ID}`)
  })

  // ── Test 1: button visibility ─────────────────────────────────────────────

  test('export button is visible on the series detail page', async ({ page }) => {
    await expect(
      page.getByRole('button', { name: 'Download Series Data' }),
    ).toBeVisible()
  })

  // ── Test 2: successful download ───────────────────────────────────────────

  test('clicking Download Series Data triggers a file download with the correct filename', async ({
    page,
  }) => {
    // Intercept the export API call and return a mock markdown blob.
    // The filename is derived from the Content-Disposition header by
    // exportSeriesMarkdown() and set as the <a download="..."> attribute.
    await page.route(
      `**/api/v1/series/${SERIES_ID}/export/markdown`,
      async (route) => {
        await route.fulfill({
          status: 200,
          headers: {
            'Content-Type': 'text/markdown; charset=utf-8',
            'Content-Disposition': 'attachment; filename="test-series.md"',
          },
          body: '# Test Series\n\n**Status:** Draft\n',
        })
      },
    )

    // Start listening for the download event BEFORE clicking the button so we
    // don't miss it. Promise.all ensures both arms race in the right order.
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('button', { name: 'Download Series Data' }).click(),
    ])

    expect(download.suggestedFilename()).toBe('test-series.md')
  })

  // ── Test 3: API error surfaces as error banner ────────────────────────────

  test('export button shows an error banner when the API returns a server error', async ({
    page,
  }) => {
    // Intercept the export call and simulate a 500 Internal Server Error.
    await page.route(
      `**/api/v1/series/${SERIES_ID}/export/markdown`,
      async (route) => {
        await route.fulfill({ status: 500 })
      },
    )

    await page.getByRole('button', { name: 'Download Series Data' }).click()

    // Call chain on error:
    //   exportSeriesMarkdown()  → throws new Error('Export failed: 500')
    //   handleExportMarkdown()  → setExportError('Export failed: 500')
    //   <ErrorBanner message="Export failed: 500" /> renders a Primer
    //   <Banner variant="critical" title="Export failed: 500" />, which
    //   outputs the title as visible text in a [data-banner-title] element.
    await expect(page.getByText(/Export failed: 500/)).toBeVisible()
  })
})
