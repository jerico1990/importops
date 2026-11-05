import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const source = fs.readFileSync(new URL('../src/features/configuration/CatalogFormDialog.tsx', import.meta.url), 'utf8');
const api = fs.readFileSync(new URL('../src/features/catalogs/catalogs.api.ts', import.meta.url), 'utf8');

test('catalog form dialog reserves enough height for searchable standard lists', () => {
  assert.match(source, /className="sm:min-h-\[560px\]/);
});

test('port country uses active countries catalog instead of free text ISO input', () => {
  assert.match(source, /catalogsApi\s*\.\s*countries\(\)/);
  assert.match(source, /placeholder="Buscar país registrado\.\.\."/);
  assert.doesNotMatch(source, /País \(ISO2\)<Input/);
});

test('document category is a controlled dropdown loaded from backend', () => {
  assert.match(api, /documentCategories:\s*\(\)\s*=>/);
  assert.match(source, /catalogsApi\s*\.\s*documentCategories\(\)/);
  assert.match(source, /placeholder="Seleccionar categoría\.\.\."/);
  assert.doesNotMatch(source, /Categoría<Input/);
});

const pageSource = fs.readFileSync(new URL('../src/pages/configuration/CatalogsPage.tsx', import.meta.url), 'utf8');

test('ports list country filter also uses registered countries instead of free text ISO', () => {
  assert.match(pageSource, /catalogsApi\s*\.\s*countries\(\)/);
  assert.match(pageSource, /Todos los países/);
  assert.doesNotMatch(pageSource, /placeholder="País ISO2"/);
});

const dialogSource = fs.readFileSync(new URL('../src/components/ui/Dialog.tsx', import.meta.url), 'utf8');

test('dialog content grows inside tall modals so dropdown lists have usable space', () => {
  assert.match(dialogSource, /min-h-0 flex-1 overflow-y-auto/);
});
