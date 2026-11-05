import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const read = (p) => fs.readFileSync(p, 'utf8');

test('money formatter always renders thousands and exactly two decimals', () => {
  const src = read('src/lib/format.ts');
  assert.match(src, /minimumFractionDigits:\s*2/);
  assert.match(src, /maximumFractionDigits:\s*2/);
});

test('shared MoneyInput exists and keeps formatted display separate from numeric value', () => {
  const src = read('src/components/ui/MoneyInput.tsx');
  assert.match(src, /formatMoneyInput/);
  assert.match(src, /parseMoneyInput/);
  assert.match(src, /onValueChange/);
});

test('all known monetary entry forms use MoneyInput', () => {
  for (const path of [
    'src/features/finance/FinanceEntryDialog.tsx',
    'src/features/customs/CustomsTaxDialog.tsx',
    'src/features/quotations/QuotationItemDialog.tsx',
    'src/features/requests/RequestFormDialog.tsx',
  ]) {
    const src = read(path);
    assert.match(src, /MoneyInput/, `${path} must use MoneyInput`);
  }
});

test('finance calculated pending service amount uses shared monetary formatter', () => {
  const src = read('src/features/finance/FinanceEntryDialog.tsx');
  assert.doesNotMatch(src, /baseAmount-x\.collectedAmount\)\.toFixed\(2\)/);
  assert.match(src, /formatAmount/);
});

test('MoneyInput keeps the user typing text untouched until blur and only then formats it', () => {
  const src = read('src/components/ui/MoneyInput.tsx');
  assert.match(src, /useRef/);
  assert.match(src, /if \(!isFocusedRef\.current\)/);
  assert.match(src, /setDisplay\(typingValue\)/);
  assert.match(src, /onValueChange\(parseMoneyInput\(typingValue\)\)/);
  assert.match(src, /setDisplay\(formatMoneyInput\(parseMoneyInput\(display\)\)\)/);
});
