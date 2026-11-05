# V11.4.1 - MoneyInput focus/typing fix

- El campo monetario conserva el texto que el usuario está escribiendo mientras mantiene el foco.
- No se aplica separador de miles ni dos decimales durante cada pulsación.
- Se aceptan entradas con o sin separadores de miles (por ejemplo `2000`, `2,000`, `150000`, `150,000`).
- Al perder el foco, el valor se normaliza al formato `#,##0.00`.
- El valor entregado al formulario/API continúa sin separadores de miles.
- Se evita que las actualizaciones del valor controlado reescriban el contenido del input mientras está enfocado, eliminando el salto del cursor al final.
