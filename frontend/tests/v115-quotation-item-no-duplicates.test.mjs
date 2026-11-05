import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const file = fs.readFileSync(new URL('../src/features/quotations/QuotationItemDialog.tsx', import.meta.url), 'utf8');

test('QuotationItemDialog declares V11.5 concept state exactly once', () => {
  assert.equal((file.match(/const \[concepts, setConcepts\]/g) ?? []).length, 1);
  assert.equal((file.match(/const \[quotationConceptId, setQuotationConceptId\]/g) ?? []).length, 1);
});

test('QuotationItemDialog renders concept selector exactly once', () => {
  assert.equal((file.match(/>Concepto de proforma<\/span>/g) ?? []).length, 1);
});
