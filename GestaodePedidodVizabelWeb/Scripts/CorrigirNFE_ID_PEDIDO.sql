-- Script para adicionar o campo ID_PEDIDO na tabela NFE e criar a Foreign Key
-- Execute este script no banco de dados Vizabel

USE [Vizabel]
GO

-- Verificar se a coluna ID_PEDIDO já existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'NFE' AND COLUMN_NAME = 'ID_PEDIDO'
)
BEGIN
    -- Adicionar a coluna ID_PEDIDO
    ALTER TABLE [dbo].[NFE]
    ADD [ID_PEDIDO] [int] NULL;
    
    PRINT 'Coluna ID_PEDIDO adicionada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Coluna ID_PEDIDO já existe na tabela NFE.';
END
GO

-- Verificar se a Foreign Key já existe
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_NFE_PEDIDOS'
)
BEGIN
    -- Criar a Foreign Key
    ALTER TABLE [dbo].[NFE]
    ADD CONSTRAINT [FK_NFE_PEDIDOS] 
    FOREIGN KEY ([ID_PEDIDO]) 
    REFERENCES [dbo].[PEDIDOS] ([ID_PEDIDO])
    ON DELETE SET NULL;
    
    PRINT 'Foreign Key FK_NFE_PEDIDOS criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Foreign Key FK_NFE_PEDIDOS já existe.';
END
GO

-- Verificar se a coluna CHAVE_ACESSO precisa ser alterada para 255 caracteres
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'NFE' 
    AND COLUMN_NAME = 'CHAVE_ACESSO'
    AND CHARACTER_MAXIMUM_LENGTH < 255
)
BEGIN
    ALTER TABLE [dbo].[NFE]
    ALTER COLUMN [CHAVE_ACESSO] [varchar](255) NULL;
    
    PRINT 'Coluna CHAVE_ACESSO alterada para 255 caracteres!';
END
ELSE
BEGIN
    PRINT 'Coluna CHAVE_ACESSO já está com tamanho correto ou não existe.';
END
GO

-- Verificar se a coluna DATA_SAIDA existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'NFE' AND COLUMN_NAME = 'DATA_SAIDA'
)
BEGIN
    ALTER TABLE [dbo].[NFE]
    ADD [DATA_SAIDA] [datetime] NULL;
    
    PRINT 'Coluna DATA_SAIDA adicionada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Coluna DATA_SAIDA já existe na tabela NFE.';
END
GO

-- Verificar se a coluna REGIME_TRIBUTARIO existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'NFE' AND COLUMN_NAME = 'REGIME_TRIBUTARIO'
)
BEGIN
    ALTER TABLE [dbo].[NFE]
    ADD [REGIME_TRIBUTARIO] [varchar](50) NULL;
    
    PRINT 'Coluna REGIME_TRIBUTARIO adicionada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Coluna REGIME_TRIBUTARIO já existe na tabela NFE.';
END
GO

-- Verificar estrutura final da tabela NFE
SELECT 
    COLUMN_NAME AS NomeColuna,
    DATA_TYPE AS TipoDados,
    CHARACTER_MAXIMUM_LENGTH AS TamanhoMaximo,
    IS_NULLABLE AS PermiteNull,
    COLUMN_DEFAULT AS ValorPadrao
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'NFE'
ORDER BY ORDINAL_POSITION;
GO

PRINT 'Script executado com sucesso!';
GO









