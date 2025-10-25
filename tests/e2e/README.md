Playwright E2E tests for EduMind Student App

Prerequisites:

- Node.js and npm available (dev container includes Node)
- Aspire services running locally (use `./scripts/start-aspire.sh`)

Quick start:

```bash
cd tests/e2e
npm install
npm run install-browsers
npm test
```

Notes:

- Tests assume the Student App is available at <http://localhost:5049> (playwright.config.ts baseURL)
- If the Review page isn't implemented the test will tolerate a 404 but still validate the results page loaded
- Use `npm run test:headed` to watch a headed run
