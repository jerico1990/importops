# ImportOps Front V11.11 — descarga PDF de cotización para cliente

- Conserva la vista web actual de la cotización.
- Cuando el usuario es CLIENT y la cotización ya fue enviada, muestra `Descargar cotización PDF`.
- Descarga el PDF desde `GET /api/quotations/{id}/pdf` respetando cookies de autenticación y nombre de archivo del backend.
