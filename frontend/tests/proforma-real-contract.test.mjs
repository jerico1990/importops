import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const read = (p) => fs.readFileSync(p, 'utf8');

test('frontend exposes V10.14 proforma endpoints', () => {
  const api = read('src/features/quotations/quotations.api.ts');
  for (const fragment of ['/proforma', '/api/quotations/concepts', '/suppliers', '/cargo', '/customs-taxes']) {
    assert.ok(api.includes(fragment), `missing ${fragment}`);
  }
});

test('quotation detail renders real proforma UI', () => {
  const page = read('src/pages/quotations/QuotationDetailPage.tsx');
  assert.ok(page.includes('QuotationProformaPanel'));
  const panel = read('src/features/quotations/QuotationProformaPanel.tsx');
  for (const label of ['Valor aduanero', 'Proveedores y mercancía', 'Derechos aduaneros', 'Gastos y servicios', 'Total proforma']) {
    assert.ok(panel.includes(label), `missing ${label}`);
  }
});

test('quotation item uses configurable quotation concepts', () => {
  const dialog = read('src/features/quotations/QuotationItemDialog.tsx');
  assert.ok(dialog.includes('quotationsApi.concepts'));
  assert.ok(dialog.includes('quotationConceptId'));
});
