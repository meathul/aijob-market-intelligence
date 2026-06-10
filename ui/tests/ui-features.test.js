const fs = require('fs');
const path = require('path');
const assert = require('assert');

console.log('Starting static structure assertions on UI features...');

const uiRoot = path.join(__dirname, '../src/aijob-ui');

// Helper to check file contents
function assertFileContains(filePath, substrings) {
  const fullPath = path.resolve(uiRoot, filePath);
  assert.ok(fs.existsSync(fullPath), `File does not exist: ${filePath}`);
  const content = fs.readFileSync(fullPath, 'utf8');
  for (const sub of substrings) {
    assert.ok(
      content.includes(sub),
      `Expected file ${filePath} to contain "${sub}" but it did not.`
    );
  }
}

try {
  // 1. Verify routing is configured to permit existing users to access onboarding (no guard on /onboarding)
  console.log('Checking app.routes.ts configuration...');
  assertFileContains('src/app/app.routes.ts', [
    "path: 'onboarding'",
    "loadComponent: () =>"
  ]);

  // Ensure onboardingPageGuard is not protecting '/onboarding'
  const routesContent = fs.readFileSync(path.resolve(uiRoot, 'src/app/app.routes.ts'), 'utf8');
  const onboardingRouteBlock = routesContent.match(/path:\s*'onboarding'[^]*?loadComponent/);
  if (onboardingRouteBlock) {
    assert.ok(
      !onboardingRouteBlock[0].includes('onboardingPageGuard'),
      'onboardingPageGuard should be removed from the onboarding path to allow profile updates.'
    );
  }

  // 2. Verify sidebar contains switchAccount trigger and routerLink to onboarding
  console.log('Checking sidebar.component templates...');
  assertFileContains('src/app/layout/sidebar/sidebar.component.html', [
    'switchAccount()',
    'routerLink="/onboarding"'
  ]);

  // 3. Verify onboarding setup form contains a Cancel action for existing profiles
  console.log('Checking onboarding/profile-setup form controls...');
  assertFileContains('src/app/onboarding/pages/profile-setup-page/profile-setup-page.component.html', [
    'isExistingUser()',
    'Cancel'
  ]);
  assertFileContains('src/app/onboarding/pages/profile-setup-page/profile-setup-page.component.ts', [
    'readonly isExistingUser = signal(false)',
    'this.isExistingUser.set('
  ]);

  // 4. Verify admin control panel on reports contains the ingestion trigger
  console.log('Checking reports-page admin controls...');
  assertFileContains('src/app/reports/pages/reports-page/reports-page.component.html', [
    'triggerSync()',
    'Sync Jobs Now'
  ]);
  assertFileContains('src/app/reports/pages/reports-page/reports-page.component.ts', [
    'triggerSync()',
    'adminApi.triggerFetch()'
  ]);

  console.log('✔ All UI static structure assertion checks passed successfully!');
} catch (error) {
  console.error('❌ UI structural assertions failed:');
  console.error(error.message);
  process.exit(1);
}
