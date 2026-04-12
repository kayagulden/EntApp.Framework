SELECT column_name FROM information_schema.columns
WHERE table_schema='iam' AND table_name='users'
ORDER BY ordinal_position;
