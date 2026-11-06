USE `ImportOpsDB`;

CREATE TABLE IF NOT EXISTS `sys__APP_SEQUENCE` (
  `sequence_key` VARCHAR(64) NOT NULL,
  `next_value` BIGINT NOT NULL,
  PRIMARY KEY (`sequence_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `sys__APP_SEQUENCE` (`sequence_key`, `next_value`)
SELECT 'REQUEST_NO', COALESCE(MAX(CAST(SUBSTRING_INDEX(`request_no`, '-', -1) AS UNSIGNED)), 0)
FROM `sales__IMPORT_REQUEST`
ON DUPLICATE KEY UPDATE `next_value` = VALUES(`next_value`);

INSERT INTO `sys__APP_SEQUENCE` (`sequence_key`, `next_value`)
SELECT 'QUOTATION_NO', COALESCE(MAX(CAST(SUBSTRING_INDEX(SUBSTRING_INDEX(`quotation_no`, '-V', 1), '-', -1) AS UNSIGNED)), 0)
FROM `sales__QUOTATION`
ON DUPLICATE KEY UPDATE `next_value` = VALUES(`next_value`);

INSERT INTO `sys__APP_SEQUENCE` (`sequence_key`, `next_value`)
SELECT 'OPERATION_NO', COALESCE(MAX(CAST(SUBSTRING_INDEX(`operation_no`, '-', -1) AS UNSIGNED)), 0)
FROM `ops__IMPORT_OPERATION`
ON DUPLICATE KEY UPDATE `next_value` = VALUES(`next_value`);
