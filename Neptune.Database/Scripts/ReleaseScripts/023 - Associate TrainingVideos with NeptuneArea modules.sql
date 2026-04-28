-- NPT-1033: Associate existing training videos with their NeptuneArea module based on video name
UPDATE dbo.TrainingVideo
SET NeptuneAreaID = 2 -- OCStormwaterTools (Inventory Module)
WHERE VideoName LIKE 'Inventory Module%';

UPDATE dbo.TrainingVideo
SET NeptuneAreaID = 1 -- Trash
WHERE VideoName LIKE 'Trash Module%';
