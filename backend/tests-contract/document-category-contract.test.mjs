import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const iface = fs.readFileSync(new URL('../src/ImportOps.Application/Catalogs/ICatalogService.cs', import.meta.url), 'utf8');
const controller = fs.readFileSync(new URL('../src/ImportOps.Api/Controllers/CatalogsController.cs', import.meta.url), 'utf8');
const service = fs.readFileSync(new URL('../src/ImportOps.Infrastructure/Services/CatalogService.cs', import.meta.url), 'utf8');
const admin = fs.readFileSync(new URL('../src/ImportOps.Infrastructure/Services/CatalogAdministrationService.cs', import.meta.url), 'utf8');

test('backend exposes controlled document categories', () => {
  assert.match(iface, /GetDocumentCategoriesAsync/);
  assert.match(controller, /HttpGet\("document-categories"\)/);
  assert.match(service, /COMMERCIAL/);
  assert.match(service, /LOGISTICS/);
  assert.match(service, /CUSTOMS/);
  assert.match(service, /FINANCIAL/);
  assert.match(service, /OTHER/);
});

test('document type admin rejects categories outside controlled set', () => {
  assert.match(admin, /DocumentCategoryCodes/);
  assert.match(admin, /Categoría de documento inválida/);
});
