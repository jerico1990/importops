import test from 'node:test';import assert from 'node:assert/strict';import fs from 'node:fs';
const read=p=>fs.readFileSync(new URL(`../${p}`,import.meta.url),'utf8');
test('proforma sends relational ids for regime and customs office',()=>{const s=read('src/features/quotations/QuotationProformaPanel.tsx');assert.match(s,/customsRegimeId:.*Number/);assert.match(s,/customsOfficeId:.*Number/);assert.doesNotMatch(s,/customsRegime:f\.customsRegime/)});
test('customs process uses same master ids',()=>{const s=read('src/features/customs/CustomsFormDialog.tsx');assert.match(s,/customsRegimeId/);assert.match(s,/customsOfficeId/);assert.match(s,/catalogsApi\.customsRegimes/);assert.match(s,/catalogsApi\.customsOffices/)});
test('customs tax uses master concept id',()=>{const s=read('src/features/customs/CustomsTaxDialog.tsx');assert.match(s,/customsTaxConceptId:Number/);assert.doesNotMatch(s,/description:description/)});
test('customs master catalogs are maintainable',()=>{const s=read('src/pages/configuration/CatalogsPage.tsx');for(const k of ['customs-regimes','customs-offices','customs-tax-concepts'])assert.match(s,new RegExp(k))});
test('searchable select renders through portal to avoid modal clipping',()=>{const s=read('src/components/ui/SearchableSelect.tsx');assert.match(s,/createPortal/);assert.match(s,/document\.body/);assert.match(s,/z-\[1000\]/)});
