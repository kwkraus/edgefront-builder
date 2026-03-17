import { test, expect } from '@playwright/test';

test.describe('About page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/about');
  });

  test('has correct page title', async ({ page }) => {
    await expect(page).toHaveTitle(/About - EdgeFront Builder/);
  });

  test('displays the main heading', async ({ page }) => {
    await expect(page.getByRole('heading', { level: 1, name: 'About EdgeFront Builder' })).toBeVisible();
  });

  test('displays The Application section', async ({ page }) => {
    await expect(page.getByRole('heading', { level: 2, name: 'The Application' })).toBeVisible();
    // Verify key descriptive text is present
    await expect(page.getByText('local-first webinar analytics workspace')).toBeVisible();
  });

  test('displays all feature list items', async ({ page }) => {
    await expect(page.getByText('Local series and session management')).toBeVisible();
    await expect(page.getByText('Session-scoped CSV imports')).toBeVisible();
    await expect(page.getByText('Registration, attendance, and Q&A summaries')).toBeVisible();
    await expect(page.getByText('Engagement metrics and account influence analytics')).toBeVisible();
  });

  test('displays Our Team section', async ({ page }) => {
    await expect(page.getByRole('heading', { level: 2, name: 'Our Team' })).toBeVisible();
    await expect(page.getByText('Built by EdgeFront')).toBeVisible();
  });

  test('header About link navigates to about page', async ({ page }) => {
    // Go to a different page first (login is public)
    await page.goto('/login');
    // Click the About link in the header
    await page.getByRole('link', { name: 'About' }).click();
    await expect(page).toHaveURL(/\/about/);
    await expect(page.getByRole('heading', { level: 1, name: 'About EdgeFront Builder' })).toBeVisible();
  });

  test('page is accessible without authentication', async ({ page }) => {
    // The page should load without being redirected to login
    await expect(page).toHaveURL(/\/about/);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });
});
