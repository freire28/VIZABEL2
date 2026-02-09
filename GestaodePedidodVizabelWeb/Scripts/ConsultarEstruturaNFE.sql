-- Script para consultar estrutura da tabela NFE
SELECT 
    COLUMN_NAME AS NomeColuna,
    DATA_TYPE AS TipoDados,
    CHARACTER_MAXIMUM_LENGTH AS TamanhoMaximo,
    IS_NULLABLE AS PermiteNull,
    COLUMN_DEFAULT AS ValorPadrao,
    NUMERIC_PRECISION AS PrecisaoNumerica,
    NUMERIC_SCALE AS EscalaNumerica
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'NFE'
ORDER BY ORDINAL_POSITION;














