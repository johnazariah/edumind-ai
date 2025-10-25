import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
    testDir: './tests',
    timeout: 60_000,
    expect: { timeout: 5000 },
    fullyParallel: false,
    retries: 0,
    reporter: [['list'], ['html', { open: 'never' }]],
    use: {
        headless: true,
        baseURL: 'http://localhost:5049',
        viewport: { width: 1280, height: 800 },
        actionTimeout: 10_000,
        ignoreHTTPSErrors: true
    },
    projects: [
        { name: 'chromium', use: { ...devices['Desktop Chrome'] } }
    ]
});
