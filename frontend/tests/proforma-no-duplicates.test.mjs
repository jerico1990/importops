import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const source = fs.readFileSync('src/pages/quotations/QuotationDetailPage.tsx', 'utf8');

test('QuotationProformaPanel import exists exactly once', () => {
  assert.equal((source.match(/import \{ QuotationProformaPanel \}/g) || []).length, 1);
});

test('Proforma tab exists exactly once', () => {
  assert.equal((source.match(/\{ value: "PROFORMA", label: "Proforma" \}/g) || []).length, 1);
});

test('Proforma panel render exists exactly once', () => {
  assert.equal((source.match(/<QuotationProformaPanel quote=\{quote\}/g) || []).length, 1);
});
