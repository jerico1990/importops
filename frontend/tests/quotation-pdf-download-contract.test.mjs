import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const page = fs.readFileSync(new URL('../src/pages/quotations/QuotationDetailPage.tsx', import.meta.url), 'utf8');
const api = fs.readFileSync(new URL('../src/features/quotations/quotations.api.ts', import.meta.url), 'utf8');

test('client quotation view exposes PDF download without replacing current view', () => {
  assert.match(page, /Descargar cotización PDF/);
  assert.match(page, /clientMode/);
  assert.match(page, /downloadPdf\(quote\.quotationId/);
  assert.match(api, /\/api\/quotations\/\$\{quotationId\}\/pdf/);
});
