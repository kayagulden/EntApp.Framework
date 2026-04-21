UPDATE pm.board_columns SET "Name" = 'Bekleyenler' WHERE "Name" LIKE 'Bekle%';
UPDATE pm.board_columns SET "Name" = 'Yapılacak' WHERE "Name" LIKE 'Yap%';
UPDATE pm.board_columns SET "Name" = 'İşlemde' WHERE "Name" LIKE '%lemde';
UPDATE pm.board_columns SET "Name" = 'İnceleme' WHERE "Name" LIKE '%nceleme';
UPDATE pm.board_columns SET "Name" = 'Tamamlandı' WHERE "Name" LIKE 'Tamamla%';
UPDATE pm.board_columns SET "Name" = 'İptal' WHERE "Name" LIKE '%ptal';
SELECT "Name", "Order" FROM pm.board_columns ORDER BY "Order";
